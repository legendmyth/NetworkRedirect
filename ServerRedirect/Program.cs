using System;
using System.Collections.Generic;
using System.Text;

namespace ServerRedirect
{
    class Program
    {
        static void Main(string[] args)
        {
            SimpleServerNew simpleServer = new SimpleServerNew();
            simpleServer.OpenServer(12345, 34567);
            Console.ReadKey();
        }
    }
}
