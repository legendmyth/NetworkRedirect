using System;
using System.Collections.Generic;
using System.Text;

namespace Protocol
{
    public class ProtocolData
    {
        private int dataSize;
        private int clientId;
        private MessageType messageType;
        private byte[] data;
        private int port;
        public int ClientId
        {
            get { return clientId; }
            set { clientId = value; }
        }

        public MessageType MessageType
        {
            get { return messageType; }
            set { messageType = value; }
        }

        public byte[] Data
        {
            get { return data; }
            set { data = value; }
        }

        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        public int DataSize
        {
            get { return dataSize; }
            set { dataSize = value; }
        }

        public static ProtocolData convertToProtocolData(byte[] data)
        {
            if (data.Length < 13)
            {
                throw new Exception("无法解析数据,数据长度太小");
            }
            if (data[12] > 0x02)
            {
                throw new Exception("无法解析数据,无法识别消息类型");
            }
            else
            {
                ProtocolData protocolData = new ProtocolData();
                protocolData.ClientId = (data[0] << 24) + (data[1] << 16) + (data[2] << 8) + data[3];
                protocolData.Port = (data[4] << 24) + (data[5] << 16) + (data[6] << 8) + data[7];
                protocolData.DataSize = (data[8] << 24) + (data[9] << 16) + (data[10] << 8) + data[11];
                protocolData.MessageType = data[12] == 0x00 ? MessageType.Connect : (data[12] == 0x01 ? MessageType.SendMessage : MessageType.Close);

                if (data.Length > 13)
                {
                    byte[] tmp = new byte[data.Length - 13];
                    Array.Copy(data, 13, tmp, 0, tmp.Length);
                    protocolData.data = tmp;
                }
                return protocolData;
            }
        }

        public static byte[] convertToBytes(ProtocolData protocolData)
        {
            byte[] tmp = new byte[protocolData.Data != null ? protocolData.Data.Length + 13 : 13];
            if (protocolData.Data != null)
            {
                Array.Copy(protocolData.Data, 0, tmp, 13, protocolData.Data.Length);
            }            
            tmp[0] = (byte)(protocolData.clientId >> 24);
            tmp[1] = (byte)(protocolData.clientId >> 16);
            tmp[2] = (byte)(protocolData.clientId >> 8);
            tmp[3] = (byte)(protocolData.clientId >> 0);

            tmp[4] = (byte)(protocolData.port >> 24);
            tmp[5] = (byte)(protocolData.port >> 16);
            tmp[6] = (byte)(protocolData.port >> 8);
            tmp[7] = (byte)(protocolData.port >> 0);

            tmp[8] = (byte)(protocolData.DataSize >> 24);
            tmp[9] = (byte)(protocolData.DataSize >> 16);
            tmp[10] = (byte)(protocolData.DataSize >> 8);
            tmp[11] = (byte)(protocolData.DataSize >> 0);

            tmp[12] = (byte)protocolData.MessageType;
            return tmp;
        }

        public byte[] toByte()
        {

            byte[] tmp = new byte[this.data != null ? this.data.Length + 13 : 13];
            if (this.data != null)
            {
                Array.Copy(this.Data, 0, tmp, 13, this.Data.Length);
            }
            
            tmp[0] = (byte)(this.clientId >> 24);
            tmp[1] = (byte)(this.clientId >> 16);
            tmp[2] = (byte)(this.clientId >> 8);
            tmp[3] = (byte)(this.clientId >> 0);

            tmp[4] = (byte)(this.port >> 24);
            tmp[5] = (byte)(this.port >> 16);
            tmp[6] = (byte)(this.port >> 8);
            tmp[7] = (byte)(this.port >> 0);

            tmp[8] = (byte)(this.DataSize >> 24);
            tmp[9] = (byte)(this.DataSize >> 16);
            tmp[10] = (byte)(this.DataSize >> 8);
            tmp[11] = (byte)(this.DataSize >> 0);

            tmp[12] = (byte)this.MessageType;
            return tmp;
        }
    }

    public enum MessageType
    {
        Connect = 0,
        SendMessage = 1,
        Close = 2,
    }
}
