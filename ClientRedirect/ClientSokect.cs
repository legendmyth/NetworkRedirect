using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Network
{
    public class ClientSocket
    {
        private int id;
        private byte[] buffer;
        private Socket socket;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public byte[] Buffer
        {
            get { return buffer; }
            set { buffer = value; }
        }

        public Socket Socket
        {
            get { return socket; }
            set { socket = value; }
        }
    }
}
