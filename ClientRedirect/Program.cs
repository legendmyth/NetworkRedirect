using Network;
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
        public static void Main(string[] args)
        {
            SimpleClientRedirect simpleClientRedirect = new SimpleClientRedirect("192.168.100.64", 12345, "127.0.0.1", 3389);
            simpleClientRedirect.StartClientRedirect();
            Console.ReadKey();
        }
    }
}
