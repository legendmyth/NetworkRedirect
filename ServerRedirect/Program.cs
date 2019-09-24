using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ServerRedirect
{
    class Program
    {
        static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(100,20);
            ThreadPool.SetMaxThreads(200,30);
            SimpleServerNew simpleServer = new SimpleServerNew();
            simpleServer.OpenServer(12345, 34567);
            Console.ReadKey();
        }
    }
}
