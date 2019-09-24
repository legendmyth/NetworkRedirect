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

        public int DataSize
        {
            get { return dataSize; }
            set { dataSize = value; }
        }

        public static ProtocolData convertToProtocolData(byte[] data)
        {
            if (data.Length < 12)
            {
                throw new Exception("无法解析数据,数据长度太小");
            }
            if (data[8] > 0x02)
            {
                throw new Exception("无法解析数据,无法识别消息类型");
            }
            else
            {
                ProtocolData protocolData = new ProtocolData();
                protocolData.ClientId = (data[3] << 24) + (data[2] << 16) + (data[1] << 8) + data[0];
                protocolData.DataSize = (data[7] << 24) + (data[6] << 16) + (data[5] << 8) + data[4];
                protocolData.MessageType = data[8] == 0x00 ? MessageType.Connect : (data[8] == 0x01 ? MessageType.SendMessage : MessageType.Close);
                if (data.Length > 12)
                {
                    byte[] tmp = new byte[data.Length - 12];
                    Array.Copy(data, 12, tmp, 0, tmp.Length);
                    protocolData.data = tmp;
                }
                return protocolData;
            }
        }

        public static byte[] convertToBytes(ProtocolData protocolData)
        {
            byte[] tmp = new byte[protocolData.Data != null ? protocolData.Data.Length + 12 : 12];
            if (protocolData.Data != null)
            {
                Array.Copy(protocolData.Data, 0, tmp, 12, protocolData.Data.Length);
            }            
            tmp[3] = (byte)(protocolData.clientId >> 24);
            tmp[2] = (byte)(protocolData.clientId >> 16);
            tmp[1] = (byte)(protocolData.clientId >> 8);
            tmp[0] = (byte)(protocolData.clientId >> 0);
            tmp[7] = (byte)(protocolData.DataSize >> 24);
            tmp[6] = (byte)(protocolData.DataSize >> 16);
            tmp[5] = (byte)(protocolData.DataSize >> 8);
            tmp[4] = (byte)(protocolData.DataSize >> 0);
            tmp[8] = (byte)protocolData.MessageType;
            return tmp;
        }

        public byte[] toByte()
        {

            byte[] tmp = new byte[this.data != null ? this.data.Length + 12 : 12];
            if (this.data != null)
            {
                Array.Copy(this.Data, 0, tmp, 12, this.Data.Length);
            }
            
            tmp[3] = (byte)(this.clientId >> 24);
            tmp[2] = (byte)(this.clientId >> 16);
            tmp[1] = (byte)(this.clientId >> 8);
            tmp[0] = (byte)(this.clientId >> 0);
            tmp[7] = (byte)(this.DataSize >> 24);
            tmp[6] = (byte)(this.DataSize >> 16);
            tmp[5] = (byte)(this.DataSize >> 8);
            tmp[4] = (byte)(this.DataSize >> 0);
            tmp[8] = (byte)this.MessageType;
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
