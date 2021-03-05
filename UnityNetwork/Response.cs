using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Collections;

namespace UnityNetwork
{
    [Serializable]
    public class Response
    {
        public const int SHORT16_LEN = 2;
        public byte Code;
        public Dictionary<byte, Object> Parameters;
        public short ReturnCode;
        public string DebugMessage;

        #region 建構子
        public Response()
        {
            Parameters = new Dictionary<byte, object>();
        }

        public Response(byte[] bytes, int index, string key)
        {
            ByteToAll2(bytes, index, out int cont, key);
        }

        public Response(byte Code, Dictionary<byte, Object> Parameters, short ReturnCode, string DebugMessage)
        {
            this.Code = Code;
            this.Parameters = Parameters;
            this.ReturnCode = ReturnCode;
            this.DebugMessage = DebugMessage;
        }

        public Response(byte Code, Dictionary<byte, Object> Parameters)
        {
            this.Code = Code;
            this.Parameters = Parameters;
            this.ReturnCode = 0;
            this.DebugMessage = "";
        }

        public Response(string DebugMessage)
        {
            this.DebugMessage = DebugMessage;
        }
        #endregion

        public byte[] AllToByte(string key, bool _Lock = false)
        {
            MemoryStream stream = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
            return SerializationData.Lock(stream.ToArray(), key, _Lock);
        }

        public byte[] AllToByte2(string key, bool _Lock = false)
        {
            return SerializationData.Lock(Serialization(), key, _Lock);
        }

        public byte[] Serialization()
        {
            object[] datas = new object[] { Code, Parameters, ReturnCode, DebugMessage };
            return SerializationData.ToBytes(datas);
        }

        public void ByteToAll(byte[] b, int index, out int length, string key)
        {
            byte[] a;
            SerializationData.Decompress(b, index, out a, out length);
            byte[] bytes = SerializationData.UnLock(a, key);

            try
            {
                MemoryStream stream = new MemoryStream(bytes);
                IFormatter formatter = new BinaryFormatter();
                stream.Seek(0, SeekOrigin.Begin);
                object UnserializeObj = formatter.Deserialize(stream);
                AllWriteIn(this, (Response)UnserializeObj);
            }
            catch (System.Exception)
            {
                Response g = new Response(0, new Dictionary<byte, object> { { 0, "錯誤" } }, 0, "");
                AllWriteIn(this, (Response)g);
            }
        }


        public void ByteToAll2(byte[] b, int index, out int length, string key)
        {
            if (b.Length <= index)
            {
                length = 0;
                return;
            }
            byte[] a;
            SerializationData.Decompress(b, index, out a, out length);
            byte[] bytes = SerializationData.UnLock(a, key);
            try
            {
                object[] datas = (object[])SerializationData.ToObject(bytes);
                Code = (byte)datas[0];
                Parameters = (Dictionary<byte, object>)datas[1];
                ReturnCode = (short)datas[2];
                DebugMessage = (string)datas[3];
            }
            catch (System.Exception e)
            {
                Response g = new Response(0, new Dictionary<byte, object> { { 0, "錯誤" } }, 0, e.ToString());
                AllWriteIn(this, (Response)g);
            }
        }

        public void AllWriteIn(Response write, Response read)
        {
            write.Code = read.Code;
            write.Parameters = read.Parameters;
            write.ReturnCode = read.ReturnCode;
            write.DebugMessage = read.DebugMessage;
        }
    }
}
