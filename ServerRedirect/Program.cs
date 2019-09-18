using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            ////ProtocolData protocolData = new ProtocolData();
            ////protocolData.ClientId = 10086;
            ////protocolData.Data = new byte[3];
            ////protocolData.MessageType = MessageType.Close;
            ////byte []tmp = protocolData.toByte();
            ////Console.WriteLine("");
            //byte []tmp = new byte[8];
            //tmp[0] = 0;
            //tmp[1] = 0;
            //tmp[2] = 39;
            //tmp[3] = 102;
            //tmp[4] = 2;
            //tmp[5] = 0;
            //ProtocolData p = ProtocolData.convertToProtocolData(tmp);
            //Console.WriteLine("");
            SimpleServerNew simpleServer = new SimpleServerNew();
            simpleServer.OpenServer(12345, 23456);
            Console.ReadKey();
        }
    }
}
