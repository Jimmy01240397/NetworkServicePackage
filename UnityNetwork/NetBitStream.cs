using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System;
using System.Net;

namespace UnityNetwork
{
    public class NetBitStream
    {
        // *******************************
        // 定義訊息頭和體的長度
        // *******************************
        // 頭 int32 4個位元組
        public const int header_length = 4;

        // 身體 最大512個節
        public const int max_body_length = 512;

        // *******************************
        // 定義位元組長度
        // *******************************
        // byte 1個位元組
        public const int BYTE_LEN = 1;

        // int 4個位元組
        public const int INT32_LEN = 4;

        // short 2個位元組
        public const int SHORT16_LEN = 2;

        // int 占4個位元組
        public const int FLOAT_LEN = 4;

        // *******************************
        // 資料流程
        // *******************************
        // byte流
        private byte[] _bytes = null;
        public byte[] BYTES
        {
            get
            {
                return _bytes;
            }
            set
            {
                _bytes = value;
            }
        }

        public Response thing;
        private MemoryStream stream;
        private BinaryWriter writer;
        private BinaryReader reader;
        // 當前資料體長
        private int _bodyLenght = 0;
        public int BodyLength
        {
            get { return _bodyLenght; }
        }


        // 總長
        public int Length
        {
            get { return header_length + _bodyLenght; }
        }

        // 使用資料流程的Socket
        public TcpClient _socketTCP = null;
        public IPEndPoint _socketUDP = null;

        // 構造函數
        public NetBitStream()
        {
            _bodyLenght = 0;
            thing = new Response();
        }

        // 寫訊息ID
        public void BeginWrite(ushort msdid)
        {
            // 初始化體長為0
            _bodyLenght = 0;
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
            writer.Write((int)0);
            writer.Close();
            stream.Close();
            _bytes = stream.ToArray();

            // 寫入訊息識別字
            this.WriteUShort(msdid);
        }

        // 寫無符號短整型
        public void WriteUShort(ushort number)
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
            writer.Write(_bytes);
            writer.Write(number);
            writer.Close();
            stream.Close();
            _bytes = stream.ToArray();

            _bodyLenght += SHORT16_LEN;
        }

        public void WriteResponse(Response response, string key, bool _Lock)
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
            byte[] bs = response.AllToByte(key, _Lock);
            writer.Write(_bytes);
            writer.Write(bs);
            writer.Close();
            stream.Close();
            _bytes = stream.ToArray();

            _bodyLenght += bs.Length;
        }

        public void WriteResponse2(Response response, string key, bool _Lock = false)
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
            byte[] bs = response.AllToByte2(key, _Lock);
            writer.Write(_bytes);
            writer.Write(bs);
            writer.Close();
            stream.Close();
            _bytes = stream.ToArray();
            _bodyLenght += bs.Length;
        }

        public void WriteResponseByte(byte[] response, string key, bool _Lock)
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
            byte[] bs = Response.Lock(response, key, _Lock);
            writer.Write(_bytes);
            writer.Write(bs);
            writer.Close();
            stream.Close();
            _bytes = stream.ToArray();
            _bodyLenght += bs.Length;
        }

        // 開始讀取
        public void BeginReadTCP(NetPacket packet, out ushort msgid)
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
            writer.Write(packet._bytes);
            writer.Close();
            stream.Close();
            _bytes = stream.ToArray();

            this._socketTCP = packet._peerTCP;

            _bodyLenght = 0;

            this.ReadUShort(out msgid);
        }

        // 開始讀取版本2 忽略訊息ID
        public void BeginReadTCP2(NetPacket packet)
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
            writer.Write(packet._bytes);
            writer.Close();
            stream.Close();
            _bytes = stream.ToArray();

            this._socketTCP = packet._peerTCP;

            _bodyLenght = 0;

            _bodyLenght += SHORT16_LEN;
        }

        // 開始讀取
        public void BeginReadUDP(NetPacket packet, out ushort msgid)
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
            writer.Write(packet._bytes);
            writer.Close();
            stream.Close();
            _bytes = stream.ToArray();

            this._socketUDP = packet._peerUDP;

            _bodyLenght = 0;

            this.ReadUShort(out msgid);
        }

        // 開始讀取版本2 忽略訊息ID
        public void BeginReadUDP2(NetPacket packet)
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
            writer.Write(packet._bytes);
            writer.Close();
            stream.Close();
            _bytes = stream.ToArray();

            this._socketUDP = packet._peerUDP;

            _bodyLenght = 0;

            _bodyLenght += SHORT16_LEN;
        }

        public void ReadUShort(out ushort number)
        {
            number = 0;

            stream = new MemoryStream(_bytes);
            reader = new BinaryReader(stream);
            reader.ReadBytes(header_length + _bodyLenght);
            number = (ushort)reader.ReadInt16();
            reader.Close();
            stream.Close();

            _bodyLenght += SHORT16_LEN;
        }

        public void ReadResponse(string key)
        {
            int a;
            thing.ByteToAll(_bytes, header_length + _bodyLenght, out a, key);
            _bodyLenght += a;
        }

        public void ReadResponse2(string key)
        {
            int a;
            thing.ByteToAll2(_bytes, header_length + _bodyLenght, out a, key);
            _bodyLenght += a;
        }

        public bool CopyBytes(byte[] bs)
        {
            if (bs.Length > _bytes.Length)
                return false;

            bs.CopyTo(_bytes, 0);

            // 取得體長
            _bodyLenght = System.BitConverter.ToInt32(_bytes, 0);

            return true;
        }

        // 獲取體長
        public void EncodeHeader()
        {
            byte[] bs = System.BitConverter.GetBytes(_bodyLenght);

            bs.CopyTo(_bytes, 0);
        }

        // 計算體長
        public void DecodeHeader()
        {
            _bodyLenght = System.BitConverter.ToInt32(_bytes, 0);
        }
    }
}

