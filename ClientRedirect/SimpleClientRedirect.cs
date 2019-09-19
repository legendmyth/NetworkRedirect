using Protocol;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Network
{
    class SimpleClientRedirect
    {
        static int protocalHeadSize = 13;
        public SimpleClientRedirect(String fromIp,int fromPort,String toIp,int toPort)
        {
            this.fromIp = fromIp;
            this.fromPort = fromPort;
            this.toIp = toIp;
            this.toPort = toPort;
        }
        public void StartClientRedirect()
        {
            clientFrom = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //clientFrom.IOControl(IOControlCode.KeepAliveValues, KeepAlive(1, 0x7d0, 200), null);
            clientFrom.Connect(fromIp, fromPort);
            clientFrom.BeginReceive(clientFromReciveBuffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(ClientFromReciveCallBack), clientFrom);
        }
        private String fromIp = "192.168.100.64";
        private String toIp = "127.0.0.1";

        private int toPort = 5001;
        private int fromPort = 12345;

        private static int bufferSize = 10240;
        
        private Dictionary<Int32, ClientSocket> clientsTo = new Dictionary<int, ClientSocket>();
        private Socket clientFrom;

        private byte[] clientFromReciveBuffer = new byte[bufferSize];
        private byte[] clientToReciveBuffer = new byte[bufferSize];
        private byte[] KeepAlive(int onOff, int keepAliveTime, int keepAliveInterval)
        {
            byte[] array = new byte[12];
            BitConverter.GetBytes(onOff).CopyTo(array, 0);
            BitConverter.GetBytes(keepAliveTime).CopyTo(array, 4);
            BitConverter.GetBytes(keepAliveInterval).CopyTo(array, 8);
            return array;
        }


        private void ClientFromReciveCallBack(IAsyncResult asyncResult)
        {
            Socket socket = asyncResult.AsyncState as Socket;
            Console.WriteLine("ClientFromReciveCallBack asyncResult.IsCompleted:" + asyncResult.IsCompleted);
            if (IsOnline(socket))
            {
                int size = socket.EndReceive(asyncResult);
                byte[] tmp = new byte[size];
                Array.Copy(clientFromReciveBuffer, tmp, size);
                Console.WriteLine("client from recive :" + GetHexString(tmp, " "));
                DealData(socket, tmp);
                socket.BeginReceive(clientFromReciveBuffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(ClientFromReciveCallBack), socket);

            }
            else//断线重连
            {
                //clientFrom = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //clientFrom.IOControl(IOControlCode.KeepAliveValues, KeepAlive(1, 0x7d0, 200), null);
                //clientFrom.Connect(fromIp, fromPort);
                //clientFrom.BeginReceive(clientFromReciveBuffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(ClientFromReciveCallBack), clientFrom);
            }
        }

        private void DealData(Socket socket, byte[] buffer)
        {
            if (buffer.Length < protocalHeadSize)
            {
                byte[] t = new byte[protocalHeadSize];
                Array.Copy(buffer, t, buffer.Length);
                int s = protocalHeadSize - buffer.Length;
                for (int i = 0; i < s; i++)
                {
                    socket.Receive(t, buffer.Length + i, 1, SocketFlags.None);
                }
                DealData(socket, t);
            }

            //Console.WriteLine(DateTime.Now.ToString("HH: mm:ss.fff") + "转发接口接收到数据 :" + GetHexString(tmp, " "));
            ProtocolData tmpData = ProtocolData.convertToProtocolData(buffer);
            if (tmpData.DataSize > buffer.Length)
            {
                byte[] t = new byte[tmpData.DataSize];
                Array.Copy(buffer, t, buffer.Length);
                int s = tmpData.DataSize - buffer.Length;
                for (int i = 0; i < s; i++)
                {
                    socket.Receive(t, buffer.Length + i, 1, SocketFlags.None);
                }
                DealData(socket, t);
            }
            else if (tmpData.DataSize == buffer.Length)
            {
                if (tmpData.MessageType == MessageType.Connect)
                {
                    ClientSocket clientSocket = new ClientSocket();
                    clientSocket.Id = tmpData.ClientId;
                    clientSocket.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    clientSocket.Socket.Connect(toIp, toPort);
                    clientSocket.Buffer = new byte[bufferSize];
                    clientsTo.Add(tmpData.ClientId, clientSocket);
                    clientSocket.Socket.BeginReceive(clientSocket.Buffer, 0, bufferSize - protocalHeadSize, SocketFlags.None, new AsyncCallback(ClientToReciveCallBack), clientSocket);

                }
                else if (tmpData.MessageType == MessageType.Close)//如果接收方断开连接，则主动断开连接
                {
                    ClientSocket clientSocket = this.clientsTo[tmpData.ClientId];
                    if (IsOnline(clientSocket.Socket))
                    {
                        clientSocket.Socket.Shutdown(SocketShutdown.Both);
                        clientSocket.Socket.Close();
                    }
                    this.clientsTo.Remove(tmpData.ClientId);
                }
                else if (tmpData.MessageType == MessageType.SendMessage)
                {
                    ClientSocket clientSocket = this.clientsTo[tmpData.ClientId];
                    if (IsOnline(clientSocket.Socket))
                    {
                        clientSocket.Socket.BeginSend(tmpData.Data, 0, tmpData.Data.Length, SocketFlags.None, new AsyncCallback(ClientToSendCallBack), clientSocket);
                    }
                }
                else
                {
                    Console.WriteLine("未识别的指令");
                }
            }
            else
            {
                byte[] temp1 = new byte[tmpData.DataSize];
                byte[] temp2 = new byte[buffer.Length - tmpData.DataSize];
                Array.Copy(buffer, 0, temp1, 0, temp1.Length);
                Array.Copy(buffer, tmpData.DataSize, temp2, 0, temp2.Length);
                DealData(socket, temp1);
                DealData(socket, temp2);
            }

        }



        private void ClientToReciveCallBack(IAsyncResult asyncResult)
        {
            ClientSocket clientSocket = asyncResult.AsyncState as ClientSocket;
            lock (clientSocket)
            {
                Console.WriteLine("ClientToReciveCallBack asyncResult.IsCompleted:" + asyncResult.IsCompleted);
                if (IsOnline(clientSocket.Socket))
                {

                    int size = clientSocket.Socket.EndReceive(asyncResult);
                    byte[] tmp = new byte[size];
                    Array.Copy(clientSocket.Buffer, tmp, size);
                    Console.WriteLine("client to recive :" + GetHexString(tmp, " "));
                    if (IsOnline(clientFrom))
                    {
                        ProtocolData protocolData = new ProtocolData();
                        protocolData.ClientId = clientSocket.Id;
                        protocolData.MessageType = MessageType.SendMessage;
                        protocolData.Data = tmp;
                        //protocolData.Port = (clientSocket.Socket.RemoteEndPoint as IPEndPoint).Port;
                        protocolData.DataSize = size + protocalHeadSize;
                        clientFrom.BeginSend(protocolData.toByte(), 0, protocolData.toByte().Length, SocketFlags.None, new AsyncCallback(ClientFromSendCallBack), clientFrom);
                    }
                    clientSocket.Socket.BeginReceive(clientSocket.Buffer, 0, bufferSize- protocalHeadSize, SocketFlags.None, new AsyncCallback(ClientToReciveCallBack), clientSocket);
                }
                else
                {
                    if (IsOnline(clientFrom))
                    {
                        ProtocolData protocolData = new ProtocolData();
                        protocolData.ClientId = clientSocket.Id;
                        protocolData.MessageType = MessageType.Close;
                        protocolData.DataSize = protocalHeadSize;
                        //protocolData.Port = (clientSocket.Socket.RemoteEndPoint as IPEndPoint).Port;
                        clientFrom.BeginSend(protocolData.toByte(), 0, protocolData.toByte().Length, SocketFlags.None, new AsyncCallback(ClientFromSendCallBack), clientFrom);
                    }
                }
            }
        }
        private void ClientFromSendCallBack(IAsyncResult asyncResult)
        {
            Socket socket = asyncResult.AsyncState as Socket;
            if (IsOnline(socket))
            {
                int size = socket.EndSend(asyncResult);
            }
        }
        private void ClientToSendCallBack(IAsyncResult asyncResult)
        {
            ClientSocket clientSocket = asyncResult.AsyncState as ClientSocket;
            if (IsOnline(clientSocket.Socket))
            {
                int size = clientSocket.Socket.EndSend(asyncResult);
            }
        }
        public String GetHexString(byte[] data, String split)
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

        private static bool IsOnline(Socket s)
        {
            return (s != null) && s.Connected && !(s.Poll(100, SelectMode.SelectRead) && (s.Available == 0));
            //return (s != null) && s.Poll(100, SelectMode.SelectWrite) && s.Poll(100, SelectMode.SelectRead) && (s.Available == 1) && s.Connected;
        }
    }
}
