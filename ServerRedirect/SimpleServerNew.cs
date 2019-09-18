﻿using Protocol;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerRedirect
{
    public class SimpleServerNew
    {
        private int index = 1;

        private Socket ServerSocketRedirect = null;//转发套接字
        private Socket ServerSocketInput = null;//对外开放的服务套接字
        private static int bufferSize = 1024 * 1024;

        private byte[] redirectReciveBuffer = new byte[bufferSize];
        private byte[] inputReciveBuffer = new byte[bufferSize];

        Socket clientSocketRedirect = null;

        Dictionary<Int32, ClientSocket> clientSocketInputs = new Dictionary<int, ClientSocket>();
        //List<ClientSokect> clientSocketInputs = new List<ClientSokect>();
        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="port">端口号</param>
        public void OpenServer(int portRedirect, int portInput)
        {
            ServerSocketRedirect = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, portRedirect);
            ServerSocketRedirect.Bind(endPoint);
            ServerSocketRedirect.Listen(100);
            ServerSocketRedirect.BeginAccept(new AsyncCallback(ServerSocketRedirectAcceptCallBack), ServerSocketRedirect);
            


            ServerSocketInput = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPointInput = new IPEndPoint(IPAddress.Any, portInput);
            ServerSocketInput.Bind(endPointInput);
            ServerSocketInput.Listen(100);
            ServerSocketInput.BeginAccept(new AsyncCallback(ServerSocketInputAcceptCallBack), ServerSocketInput);

           
        }

        private void ServerSocketRedirectAcceptCallBack(IAsyncResult asyncResult)
        {
            Socket socket = asyncResult.AsyncState as Socket;            
            clientSocketRedirect = socket.EndAccept(asyncResult);
            Console.WriteLine(String.Format("redirect connect success({0})", clientSocketRedirect.AddressFamily));
            clientSocketRedirect.BeginReceive(redirectReciveBuffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(RedirectReciveCallBack), clientSocketRedirect);
        }
        private void ServerSocketInputAcceptCallBack(IAsyncResult asyncResult)
        {
            Socket socket = asyncResult.AsyncState as Socket;
            ClientSocket clientSokect = new ClientSocket();
            clientSokect.Buffer = new byte[bufferSize];
            clientSokect.Id = index++;
            clientSokect.Socket = socket.EndAccept(asyncResult); 
            clientSocketInputs.Add(clientSokect.Id, clientSokect);
            
            if (IsOnline(clientSocketRedirect))
            {
                ProtocolData data = new ProtocolData();
                data.ClientId = clientSokect.Id;
                data.MessageType = MessageType.Connect;
                data.Port = (clientSokect.Socket.RemoteEndPoint as IPEndPoint).Port;
                Console.WriteLine(String.Format(DateTime.Now.ToString("HH:mm:ss.fff")+ "发送连接请求:{0}",GetHexString(data.toByte()," ")));
                clientSocketRedirect.BeginSend(data.toByte(), 0, data.toByte().Length, SocketFlags.None, new AsyncCallback(RedirectSendCallBack), clientSocketRedirect);
            }
            clientSokect.Socket.BeginReceive(clientSokect.Buffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(InputReciveCallBack), clientSokect);
            socket.BeginAccept(new AsyncCallback(ServerSocketInputAcceptCallBack), socket);
        }

        /// <summary>
        /// 转发端口接收到数据
        /// 将数据转换成可识别的协议数据
        /// 然后发送到对应的客户端
        /// </summary>
        /// <param name="asyncResult"></param>
        private void RedirectReciveCallBack(IAsyncResult asyncResult)
        {
            Socket socket = asyncResult.AsyncState as Socket;
            if (IsOnline(socket))//转发端口在线
            {
                int size = socket.EndReceive(asyncResult);
                byte[] tmp = new byte[size];
                Array.Copy(redirectReciveBuffer, tmp, size);
                Console.WriteLine(DateTime.Now.ToString("HH: mm:ss.fff") + "转发接口接收到数据 :" + GetHexString(tmp, " "));
                ProtocolData protocolData = ProtocolData.convertToProtocolData(tmp);
                if (protocolData.MessageType == MessageType.Close)//如果接收方断开连接，则主动断开连接
                {
                    if (!this.clientSocketInputs.ContainsKey(protocolData.ClientId))
                    {
                        Console.WriteLine(String.Format("找不到编号为{0}的客户端", protocolData.ClientId));
                    }
                    else
                    {
                        ClientSocket clientSocket = this.clientSocketInputs[protocolData.ClientId];
                        lock (clientSocket)
                        {
                            if (IsOnline(clientSocket.Socket))
                            {
                                clientSocket.Socket.Shutdown(SocketShutdown.Both);
                                clientSocket.Socket.Close();
                            }
                        }
                        this.clientSocketInputs.Remove(protocolData.ClientId);
                    }

                }
                else if (protocolData.MessageType == MessageType.SendMessage)
                {
                    if (!this.clientSocketInputs.ContainsKey(protocolData.ClientId))
                    {
                        Console.WriteLine(String.Format("找不到编号为{0}的客户端", protocolData.ClientId));
                    }
                    else
                    {
                        ClientSocket clientSocket = this.clientSocketInputs[protocolData.ClientId];
                        lock (clientSocket)
                        {
                            if (IsOnline(clientSocket.Socket))
                            {
                                clientSocket.Socket.BeginSend(protocolData.Data, 0, protocolData.Data.Length, SocketFlags.None, new AsyncCallback(InputSendCallBack), this.clientSocketInputs[protocolData.ClientId]);
                            }
                        }
                    }

                }
                else
                {
                    Console.WriteLine("未识别的指令");
                }
                socket.BeginReceive(redirectReciveBuffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(RedirectReciveCallBack), socket);
            }
        }

        /// <summary>
        /// 输入端接收到数据后转换成协议能识别的数据
        /// 然后通过转发端口发送到接收方
        /// 如果接收之后连接已断开，则发送断开指令到接收方
        /// </summary>
        private void InputReciveCallBack(IAsyncResult asyncResult)
        {
            ClientSocket clientSocket = asyncResult.AsyncState as ClientSocket;
            lock (clientSocketRedirect)
            {
                if (IsOnline(clientSocket.Socket))//客户端在线，则转发接收到的数据
                {
                    int size = clientSocket.Socket.EndReceive(asyncResult);
                    byte[] tmp = new byte[size];
                    Array.Copy(clientSocket.Buffer, tmp, size);
                    Console.WriteLine(DateTime.Now.ToString("HH: mm:ss.fff") + "客户端接收到数据 :" + GetHexString(tmp, " "));
                    if (IsOnline(clientSocketRedirect))
                    {
                        ProtocolData protocolData = new ProtocolData();
                        protocolData.ClientId = clientSocket.Id;
                        protocolData.MessageType = MessageType.SendMessage;
                        protocolData.Data = tmp;
                        clientSocketRedirect.BeginSend(protocolData.toByte(), 0, protocolData.toByte().Length, SocketFlags.None, new AsyncCallback(RedirectSendCallBack), clientSocketRedirect);
                    }
                    clientSocket.Socket.BeginReceive(clientSocket.Buffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(InputReciveCallBack), clientSocket);
                }
                else//客户端掉线，则发送关闭指令
                {
                    if (IsOnline(clientSocketRedirect))
                    {
                        ProtocolData protocolData = new ProtocolData();
                        protocolData.ClientId = clientSocket.Id;
                        protocolData.MessageType = MessageType.Close;
                        clientSocketRedirect.BeginSend(protocolData.toByte(), 0, protocolData.toByte().Length, SocketFlags.None, new AsyncCallback(RedirectSendCallBack), clientSocketRedirect);
                    }
                }
            }
        }

        private void RedirectSendCallBack(IAsyncResult asyncResult)
        {
            Socket socket = asyncResult.AsyncState as Socket;
            if (IsOnline(socket))
            {
                socket.EndSend(asyncResult);
            }
        }


        private void InputSendCallBack(IAsyncResult asyncResult)
        {
            ClientSocket socket = asyncResult.AsyncState as ClientSocket;
            if (IsOnline(socket.Socket))
            {
                socket.Socket.EndSend(asyncResult);
            }
        }

        public String GetHexString(Byte[] data, String split)
        {
            if (data == null)
            {
                return String.Empty;
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("x2") + split);
            }
            return sb.ToString();
        }

        private bool IsOnline(Socket socket)
        {
            return (socket != null) && socket.Connected && !(socket.Poll(1000, SelectMode.SelectRead) && (socket.Available == 0));
        }
    }
}
