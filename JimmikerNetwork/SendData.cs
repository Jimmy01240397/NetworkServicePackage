using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JimmikerNetwork
{
    public struct SendData
    {
        public byte Code;
        public object Parameters;
        public short ReturnCode;
        public string DebugMessage;

        public object this[byte key]
        {
            get
            {
                object data;
                switch(key)
                {
                    case 0:
                        {
                            data = Code;
                            break;
                        }
                    case 1:
                        {
                            data = Parameters;
                            break;
                        }
                    case 2:
                        {
                            data = ReturnCode;
                            break;
                        }
                    case 3:
                        {
                            data = DebugMessage;
                            break;
                        }
                    default:
                        {
                            throw new KeyNotFoundException();
                        }
                }
                return data;
            }
            set
            {
                switch (key)
                {
                    case 0:
                        {
                            Code = (byte)value;
                            break;
                        }
                    case 1:
                        {
                            Parameters = (Dictionary<byte, object>)value;
                            break;
                        }
                    case 2:
                        {
                            ReturnCode = (short)value;
                            break;
                        }
                    case 3:
                        {
                            DebugMessage = (string)value;
                            break;
                        }
                    default:
                        {
                            throw new KeyNotFoundException();
                        }
                }
            }
        }

        #region 建構子
        public SendData(byte[] bytes, int index, out int cont, string key)
        {
            this.Code = 0;
            this.Parameters = null;
            this.ReturnCode = 0;
            this.DebugMessage = "";
            ByteToAll(bytes, index, out cont, key);
        }

        public SendData(byte Code, object Parameters, short ReturnCode, string DebugMessage)
        {
            this.Code = Code;
            this.Parameters = Parameters;
            this.ReturnCode = ReturnCode;
            this.DebugMessage = DebugMessage;
        }

        public SendData(byte Code, object Parameters)
        {
            this.Code = Code;
            this.Parameters = Parameters;
            this.ReturnCode = 0;
            this.DebugMessage = "";
        }

        public SendData(string DebugMessage)
        {
            this.Code = 0;
            this.Parameters = null;
            this.ReturnCode = 0;
            this.DebugMessage = DebugMessage;
        }
        #endregion

        public byte[] AllToByte(string key, SerializationData.LockType _Lock = SerializationData.LockType.None)
        {
            object[] datas = new object[] { Code, Parameters, ReturnCode, DebugMessage };
            return SerializationData.Lock(SerializationData.ToBytes(datas), key, _Lock);
        }

        public void ByteToAll(byte[] b, int index, out int length, string key)
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
                Parameters = datas[1];
                ReturnCode = (short)datas[2];
                DebugMessage = (string)datas[3];
            }
            catch (System.Exception e)
            {
                SendData g = new SendData(0, new Dictionary<byte, object> { { 0, "錯誤" } }, 0, e.ToString());
                AllWriteIn((SendData)g);
            }
        }

        public void AllWriteIn(SendData read)
        {
            Code = read.Code;
            Parameters = read.Parameters;
            ReturnCode = read.ReturnCode;
            DebugMessage = read.DebugMessage;
        }

        public bool Equals(SendData a)
        {
            return Code == a.Code && Parameters == a.Parameters && ReturnCode == a.ReturnCode && DebugMessage == a.DebugMessage;
        }

        public override bool Equals(object obj)
        {
            if (!obj.GetType().Equals(GetType()))
                return false;

            return Equals((SendData)obj);
        }

        public static bool Equals(SendData a, SendData b)
        {
            return a.Code == b.Code && a.Parameters == b.Parameters && a.ReturnCode == b.ReturnCode && a.DebugMessage == b.DebugMessage;
        }

        public static bool operator ==(SendData a, SendData b)
        {
            return Equals(a, b);
        }

        public static bool operator !=(SendData a, SendData b)
        {
            return !Equals(a, b);
        }
    }
}