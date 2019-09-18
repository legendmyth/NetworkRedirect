using Protocol;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ClientRedirect
{
    class Program
    {
        static String fromIp = "192.168.100.64";
        static String toIp = "127.0.0.1";

        static int toPort = 5001;
        static int fromPort = 12345;

        static int bufferSize = 1024 * 1024;

        //static List<Socket> clientsTo=new List<Socket>();
        private static Dictionary<Int32,ClientSocket> clientsTo=new Dictionary<int, ClientSocket>();
        static Socket clientFrom;

        static byte[] clientFromReciveBuffer = new byte[bufferSize];
        static byte[] clientToReciveBuffer = new byte[bufferSize];
        private static byte[] KeepAlive(int onOff, int keepAliveTime, int keepAliveInterval)
        {
            byte[] array = new byte[12];
            BitConverter.GetBytes(onOff).CopyTo(array, 0);
            BitConverter.GetBytes(keepAliveTime).CopyTo(array, 4);
            BitConverter.GetBytes(keepAliveInterval).CopyTo(array, 8);
            return array;
        }
        public static void Main(string[] args)
        {
            clientFrom = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientFrom.IOControl(IOControlCode.KeepAliveValues, KeepAlive(1, 0x7d0, 200), null);
            clientFrom.Connect(fromIp, fromPort);          
            clientFrom.BeginReceive(clientFromReciveBuffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(ClientFromReciveCallBack), clientFrom);


            Console.ReadKey();
        }

        private static void ClientFromReciveCallBack(IAsyncResult asyncResult)
        {
            Socket socket = asyncResult.AsyncState as Socket;
            Console.WriteLine("ClientFromReciveCallBack asyncResult.IsCompleted:" + asyncResult.IsCompleted);
            if (IsOnline(socket))
            {
                int size = socket.EndReceive(asyncResult);
                byte[] tmp = new byte[size];
                Array.Copy(clientFromReciveBuffer, tmp, size);
                Console.WriteLine("client from recive :"+GetHexString(tmp," "));
                ProtocolData pd = ProtocolData.convertToProtocolData(tmp);
                if (pd.MessageType == MessageType.Connect)
                {
                    ClientSocket clientSocket = new ClientSocket();
                    clientSocket.Id = pd.ClientId;
                    clientSocket.Socket= new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    clientSocket.Socket.IOControl(IOControlCode.KeepAliveValues, KeepAlive(1, 0x7d0, 200), null);                    
                    clientSocket.Socket.Bind(new IPEndPoint(IPAddress.Any, pd.Port));
                    clientSocket.Socket.Connect(toIp, toPort);
                    //clientSocket.Socket.
                    clientSocket.Buffer = new byte[bufferSize];
                    clientsTo.Add(pd.ClientId, clientSocket);
                    clientSocket.Socket.BeginReceive(clientSocket.Buffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(ClientToReciveCallBack), clientSocket);

                }
                else if (pd.MessageType==MessageType.Close)// IsOnline(clientTo.Socket))
                {
                    if(!clientsTo.ContainsKey(pd.ClientId))
                    {
                        Console.WriteLine(String.Format("close 找不到id为{0}的客户端编号",pd.ClientId));
                    }
                    else
                    {
                        ClientSocket clientSocket = clientsTo[pd.ClientId];
                        if (IsOnline(clientSocket.Socket))
                        {
                            clientSocket.Socket.Shutdown(SocketShutdown.Both);
                            clientSocket.Socket.Close();
                        }
                        clientsTo.Remove(pd.ClientId);
                    }
                    
                }
                else if(pd.MessageType==MessageType.SendMessage)
                {
                    if (!clientsTo.ContainsKey(pd.ClientId))
                    {
                        Console.WriteLine(String.Format("sendMessage 找不到id为{0}的客户端编号", pd.ClientId));
                    }
                    else
                    {
                        ClientSocket clientSocket = clientsTo[pd.ClientId];
                        clientSocket.Socket.BeginSend(pd.Data, 0, pd.Data.Length, SocketFlags.None, new AsyncCallback(ClientToSendCallBack), clientSocket);
                    }
                    
                }
                else
                {
                    Console.WriteLine("未识别的命令");
                }
                socket.BeginReceive(clientFromReciveBuffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(ClientFromReciveCallBack), socket);

            }
            else//断线重连
            {
                clientFrom = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientFrom.IOControl(IOControlCode.KeepAliveValues, KeepAlive(1, 0x7d0, 200), null);
                clientFrom.Connect(fromIp, fromPort);
                clientFrom.BeginReceive(clientFromReciveBuffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(ClientFromReciveCallBack), clientFrom);
            }
        }

       
        private static void ClientToReciveCallBack(IAsyncResult asyncResult)
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
                        clientFrom.BeginSend(protocolData.toByte(), 0, protocolData.toByte().Length, SocketFlags.None, new AsyncCallback(ClientFromSendCallBack), clientFrom);
                    }
                    clientSocket.Socket.BeginReceive(clientSocket.Buffer, 0, bufferSize, SocketFlags.None, new AsyncCallback(ClientToReciveCallBack), clientSocket);
                }
                else
                {
                    if (IsOnline(clientFrom))
                    {
                        ProtocolData protocolData = new ProtocolData();
                        protocolData.ClientId = clientSocket.Id;
                        protocolData.MessageType = MessageType.Close;
                        clientFrom.BeginSend(protocolData.toByte(), 0, protocolData.toByte().Length, SocketFlags.None, new AsyncCallback(ClientFromSendCallBack), clientFrom);
                    }
                }
            }
        }
        private static void ClientFromSendCallBack(IAsyncResult asyncResult)
        {
            Socket socket = asyncResult.AsyncState as Socket;
            if (IsOnline(socket))
            {
                int size = socket.EndSend(asyncResult);
            }
        }
        private static void ClientToSendCallBack(IAsyncResult asyncResult)
        {
            ClientSocket clientSocket = asyncResult.AsyncState as ClientSocket;
            if (IsOnline(clientSocket.Socket))
            {
                int size = clientSocket.Socket.EndSend(asyncResult);
            }
        }
        public static String GetHexString(byte[] data, String split)
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
