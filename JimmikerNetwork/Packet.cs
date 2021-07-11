using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JimmikerNetwork
{
    public class Packet : IDisposable
    {
        public const int header_length = 4;

        public object peer { get; set; }

        public object state { get; set; }

        public byte[] Bytes
        {
            get
            {
                if (stream == null) return null;

                byte[] steamdata = stream.ToArray();
                byte[] data = new byte[header_length + steamdata.Length];
                BitConverter.GetBytes(steamdata.Length).CopyTo(data, 0);
                stream.ToArray().CopyTo(data, header_length);
                return data;
            }
            set
            {
                if (value == null)
                {
                    CloseStream();
                    return;
                }

                if(value.Length < header_length)
                {
                    stream = new MemoryStream();
                    return;
                }

                int nowlen = value.Length - header_length;
                int uselen = BitConverter.ToInt32(value, 0);

                byte[] data = new byte[uselen];
                Array.Copy(value, header_length, data, 0, Math.Min(nowlen, uselen));
                stream = new MemoryStream(data);
            }
        }
        public int BodyLength { get; private set; } = 0;

        public int Length
        {
            get { return header_length + (int)stream.Length; }
        }

        MemoryStream stream;
        BinaryWriter writer;
        BinaryReader reader;

        public Packet(object peer, byte[] bytes = null, object state = null, bool withoutlen = false)
        {
            this.peer = peer;
            if (withoutlen)
            {
                stream = new MemoryStream();
                writer = new BinaryWriter(stream);
                WriteBytes(bytes);
                CloseWrite();
                ResetPosition();
            }
            else
            {
                Bytes = bytes;
            }
            this.state = state;
        }

        public void BeginWrite(PacketType msdid)
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
            writer.Write((byte)msdid);            
        }

        public void WriteSendData(SendData sendData, string key, SerializationData.LockType _Lock)
        {
            byte[] bs = sendData.AllToByte(key, _Lock);
            writer.Write(bs);
        }

        public void WriteSendDataByte(byte[] response, string key, SerializationData.LockType _Lock)
        {
            byte[] bs = SerializationData.Lock(response, key, _Lock);
            writer.Write(bs);
        }

        public void WriteBytes(byte[] data)
        {
            if(stream == null) stream = new MemoryStream();
            if(writer == null) writer = new BinaryWriter(stream);
            writer.Write(data);
        }

        public void CloseWrite()
        {
            writer.Flush();
            writer.Close();
            writer.Dispose();
            writer = null;
            byte[] data = Bytes;
            CloseStream();
            Bytes = data;
            stream.Position = stream.Length;
        }

        public void CloseStream()
        {
            if (writer != null) CloseWrite();
            if (reader != null) CloseRead();

            if (stream != null)
            {
                stream.Flush();
                stream.Close();
                stream.Dispose();
                stream = null;
            }
        }

        public PacketType BeginRead()
        {
            ResetPosition();

            reader = new BinaryReader(stream);

            return (PacketType)reader.ReadByte();
        }

        public PacketType BeginRead(byte[] bytes)
        {
            Bytes = bytes;

            ResetPosition();

            reader = new BinaryReader(stream);

            return (PacketType)reader.ReadByte();
        }

        public SendData ReadSendData(string key)
        {
            SendData sendData = new SendData(stream.ToArray(), (int)stream.Position, out int cont, key);
            stream.Position += cont;
            return sendData;
        }

        public void ResetPosition()
        {
            stream.Position = 0;
        }

        public void CloseRead()
        {
            reader.Close();
            reader.Dispose();
            reader = null;
            byte[] data = Bytes;
            CloseStream();
            Bytes = data;
            stream.Position = stream.Length;
        }

        public void Dispose()
        {
            CloseStream();
        }
    }
}