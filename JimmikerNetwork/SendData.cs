using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Chuon;

namespace JimmikerNetwork
{
    public struct SendData
    {

        public string ID;

        /// <summary>
        /// Your commant code.
        /// </summary>
        public byte Code;

        /// <summary>
        /// Your primary data want to send.
        /// </summary>
        public object Parameters;

        /// <summary>
        /// Return Code code (for Response)
        /// </summary>
        public short ReturnCode;

        /// <summary>
        /// Return Debug Message (for Response)
        /// </summary>
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
                            data = ID;
                            break;
                        }
                    case 1:
                        {
                            data = Code;
                            break;
                        }
                    case 2:
                        {
                            data = Parameters;
                            break;
                        }
                    case 3:
                        {
                            data = ReturnCode;
                            break;
                        }
                    case 4:
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
                            if (value == null) ID = null;
                            else ID = StringTool.BytesToHex(StringTool.HexToBytes((string)value));
                            break;
                        }
                    case 1:
                        {
                            Code = (byte)value;
                            break;
                        }
                    case 2:
                        {
                            Parameters = (Dictionary<byte, object>)value;
                            break;
                        }
                    case 3:
                        {
                            ReturnCode = (short)value;
                            break;
                        }
                    case 4:
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
            this.ID = null;
            this.Code = 0;
            this.Parameters = null;
            this.ReturnCode = 0;
            this.DebugMessage = "";
            ByteToAll(bytes, index, out cont, key);
        }

        public SendData(string ID, byte Code, object Parameters, short ReturnCode, string DebugMessage)
        {
            if (ID == null) this.ID = null;
            else this.ID = StringTool.BytesToHex(StringTool.HexToBytes(ID));
            this.Code = Code;
            this.Parameters = Parameters;
            this.ReturnCode = ReturnCode;
            this.DebugMessage = DebugMessage;
        }

        public SendData(byte Code, object Parameters, short ReturnCode, string DebugMessage)
        {
            this.ID = null;
            this.Code = Code;
            this.Parameters = Parameters;
            this.ReturnCode = ReturnCode;
            this.DebugMessage = DebugMessage;
        }

        public SendData(string ID, byte Code, object Parameters)
        {
            if (ID == null) this.ID = null;
            else this.ID = StringTool.BytesToHex(StringTool.HexToBytes(ID));
            this.Code = Code;
            this.Parameters = Parameters;
            this.ReturnCode = 0;
            this.DebugMessage = "";
        }
        public SendData(byte Code, object Parameters)
        {
            this.ID = null;
            this.Code = Code;
            this.Parameters = Parameters;
            this.ReturnCode = 0;
            this.DebugMessage = "";
        }

        public SendData(string DebugMessage)
        {
            this.ID = null;
            this.Code = 0;
            this.Parameters = null;
            this.ReturnCode = 0;
            this.DebugMessage = DebugMessage;
        }
        #endregion

        /// <summary>
        /// transform everything to binary
        /// </summary>
        /// <param name="key">encrypt key</param>
        /// <param name="_Lock">encrypt type</param>
        /// <returns>binary</returns>
        public byte[] AllToByte(string key, EncryptAndCompress.LockType _Lock = EncryptAndCompress.LockType.None)
        {
            object[] datas = new object[] { string.IsNullOrEmpty(ID) ? null : StringTool.HexToBytes(ID), Code, Parameters, ReturnCode, DebugMessage };
            return EncryptAndCompress.Lock(new ChuonBinary(datas).ToArray(), key, _Lock);
        }

        /// <summary>
        /// transform everything to SendData
        /// </summary>
        /// <param name="b">binary</param>
        /// <param name="index">transform from</param>
        /// <param name="length">transform length</param>
        /// <param name="key">decrypt key</param>
        public void ByteToAll(byte[] b, int index, out int length, string key)
        {
            if (b.Length <= index)
            {
                length = 0;
                return;
            }
            byte[] a = EncryptAndCompress.Decompress(b, index, out length);
            byte[] bytes = EncryptAndCompress.UnLock(a, key);
            try
            {
                object[] datas = (object[])new ChuonBinary(bytes).ToObject();
                ID = datas[0] != null ? StringTool.BytesToHex((byte[])datas[0]).ToLower() : null;
                Code = (byte)datas[1];
                Parameters = datas[2];
                ReturnCode = (short)datas[3];
                DebugMessage = (string)datas[4];
            }
            catch (System.Exception e)
            {
                SendData g = new SendData(0, new Dictionary<byte, object> { { 0, "錯誤" } }, 0, e.ToString());
                CopyIn((SendData)g);
            }
        }

        public void CopyIn(SendData read)
        {
            ID = read.ID;
            Code = read.Code;
            Parameters = read.Parameters;
            ReturnCode = read.ReturnCode;
            DebugMessage = read.DebugMessage;
        }

        public bool Equals(SendData a)
        {
            return ID == a.ID && Code == a.Code && Parameters == a.Parameters && ReturnCode == a.ReturnCode && DebugMessage == a.DebugMessage;
        }

        public override bool Equals(object obj)
        {
            if (!obj.GetType().Equals(GetType()))
                return false;

            return Equals((SendData)obj);
        }

        public static bool Equals(SendData a, SendData b)
        {
            return a.ID == b.ID && a.Code == b.Code && a.Parameters == b.Parameters && a.ReturnCode == b.ReturnCode && a.DebugMessage == b.DebugMessage;
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