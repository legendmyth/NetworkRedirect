using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ConsoleServer
{
    public class SimpleServer
    {
        private Socket ServerSocketRedirect = null;//转发套接字
        private Socket ServerSocketInput = null;//对外开放的服务套接字
        private static int bufferSize = 1024;

        private byte[] redirectReciveBuffer = new byte[bufferSize];
        private byte[] inputReciveBuffer = new byte[bufferSize];

        Socket clientSocketRedirect = null;

        List<Socket> clientSocketInputs = new List<Socket>();
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
            ServerSocketInput.BeginAccept(new AsyncCallback(ServerSocketInputAcceptCallBack), ServerSocketRedirect);

           
        }

        private void ServerSocketRedirectAcceptCallBack(IAsyncResult asyncResult)
        {
            Socket socket = asyncResult.AsyncState as Socket;
            clientSocketRedirect = socket.EndAccept(asyncResult);
            clientSocketRedirect.BeginReceive(redirectReciveBuffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(RedirectReciveCallBack), clientSocketRedirect);
        }
        private void ServerSocketInputAcceptCallBack(IAsyncResult asyncResult)
        {
            Socket socket = asyncResult.AsyncState as Socket;
            Socket clientSocketRecive = socket.EndAccept(asyncResult);
            clientSocketRecive.BeginReceive(inputReciveBuffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(InputReciveCallBack), clientSocketRecive);
            clientSocketInputs.Add(clientSocketRecive);
        }

        private void RedirectReciveCallBack(IAsyncResult asyncResult)
        {
            Socket socket = asyncResult.AsyncState as Socket;
            if (IsOnline(socket))
            {
                int size = socket.EndReceive(asyncResult);
                byte[] tmp = new byte[size];
                Array.Copy(redirectReciveBuffer, tmp, size);
                Console.WriteLine("redirect Recive :" + GetHexString(tmp, " "));
                for (int i = 0; i < clientSocketInputs.Count; i++)
                {
                    Socket clientSocketRecive = clientSocketInputs[i];
                    if (IsOnline(clientSocketRecive))
                    {
                        clientSocketRecive.BeginSend(tmp, 0, size, SocketFlags.None, new AsyncCallback(InputSendCallBack), clientSocketRecive);
                    }       
                    else
                    {
                        clientSocketInputs.RemoveAt(i);
                        i--;
                    }
                }
                
                socket.BeginReceive(redirectReciveBuffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(RedirectReciveCallBack), clientSocketRedirect);
            }
        }
        private void InputReciveCallBack(IAsyncResult asyncResult)
        {
            Socket socket = asyncResult.AsyncState as Socket;
            if (IsOnline(socket))
            {
                int size = socket.EndReceive(asyncResult);
                byte[] tmp = new byte[size];
                Array.Copy(inputReciveBuffer, tmp, size);
                Console.WriteLine("recive Recive :" + GetHexString(tmp, " "));
                if (IsOnline(clientSocketRedirect))
                {
                    clientSocketRedirect.BeginSend(tmp, 0, size, SocketFlags.None, new AsyncCallback(RedirectSendCallBack), clientSocketRedirect);
                }
                socket.BeginReceive(inputReciveBuffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(InputReciveCallBack), socket);
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
            Socket socket = asyncResult.AsyncState as Socket;
            if (IsOnline(socket))
            {
                socket.EndSend(asyncResult);
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
            return (socket != null) && !((socket.Poll(1000, SelectMode.SelectRead) && (socket.Available == 0)) || !socket.Connected);
        }
    }
}
