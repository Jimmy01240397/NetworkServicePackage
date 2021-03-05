using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace UnityNetwork
{
    public class SerializationData
    {
        static readonly string[] type = new string[] { "Byte[]", "SByte[]", "Int16[]", "Int32[]", "Int64[]", "UInt16[]", "UInt32[]", "UInt64[]", "Single[]", "Double[]", "Decimal[]", "Char[]", "String[]", "Boolean[]", "Object[]", "Dictionary`2", "Byte", "SByte", "Int16", "Int32", "Int64", "UInt16", "UInt32", "UInt64", "Single", "Double", "Decimal", "Char", "String", "Boolean", "Object", "null" };

        public static byte[] GetBytesLength(int cont)
        {
            List<byte> vs = new List<byte>();
            for (int i = cont / 128; i != 0; i = cont / 128)
            {
                vs.Add((byte)(cont % 128 + 128));
                cont = i;
            }
            vs.Add((byte)(cont % 128));
            return vs.ToArray();
        }

        public static int GetIntLength(BinaryReader reader)
        {
            List<byte> vs = new List<byte>();
            byte a;
            do
            {
                a = reader.ReadByte();
                vs.Add((byte)(a % 128));
            } while (a >= 128);
            int x = 0;
            for (int i = 0; i < vs.Count; i++)
            {
                x += (int)(vs[i] * Math.Pow(128, i));
            }
            return x;
        }

        public static byte[] ToBytes(object thing)
        {
            byte[] output = null;
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                output = Typing(writer, thing);
                writer.Close();
                stream.Close();
            }
            return output;
        }

        public static object ToObject(byte[] input)
        {
            object output = null;
            using (MemoryStream stream = new MemoryStream(input))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                output = GetTyp(reader);
                reader.Close();
                stream.Close();
            }
            return output;
        }

        public static byte[] Typing(BinaryWriter writer, object thing)
        {
            if (thing != null)
            {
                switch (thing.GetType().Name)
                {
                    case "Byte[]":
                        {
                            byte[] c = (byte[])thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(GetBytesLength(c.Length));
                            writer.Write(c);
                            break;
                        }
                    case "SByte[]":
                        {
                            sbyte[] c = (sbyte[])thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(GetBytesLength(c.Length));
                            for (int ii = 0; ii < c.Length; ii++)
                            {
                                writer.Write(c[ii]);
                            }
                            break;
                        }
                    case "Int16[]":
                        {
                            short[] c = (short[])thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(GetBytesLength(c.Length));
                            for (int ii = 0; ii < c.Length; ii++)
                            {
                                writer.Write(c[ii]);
                            }
                            break;
                        }
                    case "Int32[]":
                        {
                            int[] c = (int[])thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(GetBytesLength(c.Length));
                            for (int ii = 0; ii < c.Length; ii++)
                            {
                                writer.Write(c[ii]);
                            }
                            break;
                        }
                    case "Int64[]":
                        {
                            long[] c = (long[])thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(GetBytesLength(c.Length));
                            for (int ii = 0; ii < c.Length; ii++)
                            {
                                writer.Write(c[ii]);
                            }
                            break;
                        }
                    case "UInt16[]":
                        {
                            ushort[] c = (ushort[])thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(GetBytesLength(c.Length));
                            for (int ii = 0; ii < c.Length; ii++)
                            {
                                writer.Write(c[ii]);
                            }
                            break;
                        }
                    case "UInt32[]":
                        {
                            uint[] c = (uint[])thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(GetBytesLength(c.Length));
                            for (int ii = 0; ii < c.Length; ii++)
                            {
                                writer.Write(c[ii]);
                            }
                            break;
                        }
                    case "UInt64[]":
                        {
                            ulong[] c = (ulong[])thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(GetBytesLength(c.Length));
                            for (int ii = 0; ii < c.Length; ii++)
                            {
                                writer.Write(c[ii]);
                            }
                            break;
                        }
                    case "Single[]":
                        {
                            float[] c = (float[])thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(GetBytesLength(c.Length));
                            for (int ii = 0; ii < c.Length; ii++)
                            {
                                writer.Write(c[ii]);
                            }
                            break;
                        }
                    case "Double[]":
                        {
                            double[] c = (double[])thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(GetBytesLength(c.Length));
                            for (int ii = 0; ii < c.Length; ii++)
                            {
                                writer.Write(c[ii]);
                            }
                            break;
                        }
                    case "Decimal[]":
                        {
                            decimal[] c = (decimal[])thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(GetBytesLength(c.Length));
                            for (int ii = 0; ii < c.Length; ii++)
                            {
                                writer.Write(c[ii]);
                            }
                            break;
                        }
                    case "Char[]":
                        {
                            char[] c = (char[])thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(GetBytesLength(c.Length));
                            writer.Write(c);
                            break;
                        }
                    case "String[]":
                        {
                            string[] c = (string[])thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(GetBytesLength(c.Length));
                            for (int ii = 0; ii < c.Length; ii++)
                            {
                                writer.Write(c[ii]);
                            }
                            break;
                        }
                    case "Boolean[]":
                        {
                            bool[] c = (bool[])thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(GetBytesLength(c.Length));
                            for (int ii = 0; ii < c.Length; ii++)
                            {
                                writer.Write(c[ii]);
                            }
                            break;
                        }
                    case "Object[]":
                        {
                            object[] c = (object[])thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(GetBytesLength(c.Length));
                            for (int ii = 0; ii < c.Length; ii++)
                            {
                                Typing(writer, c[ii]);
                            }
                            break;
                        }
                    case "Dictionary`2":
                        {
                            Type datatype = thing.GetType();
                            Type[] Subdatatype = datatype.GetGenericArguments();
                            IDictionary c = (IDictionary)thing;
                            writer.Write((byte)(Array.IndexOf(type, datatype.Name)));
                            writer.Write((byte)(Array.IndexOf(type, Subdatatype[0].Name)));
                            writer.Write((byte)(Array.IndexOf(type, Subdatatype[1].Name)));
                            writer.Write(GetBytesLength(c.Count));

                            Array keys = Array.CreateInstance(Subdatatype[0], c.Keys.Count);
                            Array values = Array.CreateInstance(Subdatatype[1], c.Values.Count);
                            c.Keys.CopyTo(keys, 0);
                            c.Values.CopyTo(values, 0);
                            for (int i = 0; i < c.Count; i++)
                            {
                                Typing(writer, keys.GetValue(i));
                                Typing(writer, values.GetValue(i));
                            }
                            break;
                        }
                    case "Byte":
                        {
                            byte c = (byte)thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(c);
                            break;
                        }
                    case "SByte":
                        {
                            sbyte c = (sbyte)thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(c);
                            break;
                        }
                    case "Int16":
                        {
                            short c = (short)thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(c);
                            break;
                        }
                    case "Int32":
                        {
                            int c = (int)thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(c);
                            break;
                        }
                    case "Int64":
                        {
                            long c = (long)thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(c);
                            break;
                        }
                    case "UInt16":
                        {
                            ushort c = (ushort)thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(c);
                            break;
                        }
                    case "UInt32":
                        {
                            uint c = (uint)thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(c);
                            break;
                        }
                    case "UInt64":
                        {
                            ulong c = (ulong)thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(c);
                            break;
                        }
                    case "Single":
                        {
                            float c = (float)thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(c);
                            break;
                        }
                    case "Double":
                        {
                            double c = (double)thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(c);
                            break;
                        }
                    case "Decimal":
                        {
                            decimal c = (decimal)thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(c);
                            break;
                        }
                    case "Char":
                        {
                            char c = (char)thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(c);
                            break;
                        }
                    case "String":
                        {
                            string c = (string)thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(c);
                            break;
                        }
                    case "Boolean":
                        {
                            bool c = (bool)thing;
                            writer.Write((byte)(Array.IndexOf(type, thing.GetType().Name)));
                            writer.Write(c);
                            break;
                        }
                    default:
                        {
                            writer.Write((byte)type.Length);
                            writer.Write(thing.ToString());
                            break;
                        }
                }
            }
            else
            {
                writer.Write((byte)(Array.IndexOf(type, "null")));
                writer.Write(false);
            }
            MemoryStream stream = (MemoryStream)writer.BaseStream;
            return stream.ToArray();
        }

        public static object GetTyp(BinaryReader reader)
        {
            byte data = reader.ReadByte();
            object get;
            if (data < type.Length)
            {
                string typ = type[data];
                switch (typ)
                {
                    case "Byte[]":
                        {
                            byte[] d = reader.ReadBytes(GetIntLength(reader));
                            get = d;
                            break;
                        }
                    case "SByte[]":
                        {
                            sbyte[] d = new sbyte[GetIntLength(reader)];
                            for (int ii = 0; ii < d.Length; ii++)
                            {
                                d[ii] = reader.ReadSByte();
                            }
                            get = d;
                            break;
                        }
                    case "Int16[]":
                        {
                            short[] d = new short[GetIntLength(reader)];
                            for (int ii = 0; ii < d.Length; ii++)
                            {
                                d[ii] = reader.ReadInt16();
                            }
                            get = d;
                            break;
                        }
                    case "Int32[]":
                        {
                            int[] d = new int[GetIntLength(reader)];
                            for (int ii = 0; ii < d.Length; ii++)
                            {
                                d[ii] = reader.ReadInt32();
                            }
                            get = d;
                            break;
                        }
                    case "Int64[]":
                        {
                            long[] d = new long[GetIntLength(reader)];
                            for (int ii = 0; ii < d.Length; ii++)
                            {
                                d[ii] = reader.ReadInt64();
                            }
                            get = d;
                            break;
                        }
                    case "UInt16[]":
                        {
                            ushort[] d = new ushort[GetIntLength(reader)];
                            for (int ii = 0; ii < d.Length; ii++)
                            {
                                d[ii] = reader.ReadUInt16();
                            }
                            get = d;
                            break;
                        }
                    case "UInt32[]":
                        {
                            uint[] d = new uint[GetIntLength(reader)];
                            for (int ii = 0; ii < d.Length; ii++)
                            {
                                d[ii] = reader.ReadUInt32();
                            }
                            get = d;
                            break;
                        }
                    case "UInt64[]":
                        {
                            ulong[] d = new ulong[GetIntLength(reader)];
                            for (int ii = 0; ii < d.Length; ii++)
                            {
                                d[ii] = reader.ReadUInt64();
                            }
                            get = d;
                            break;
                        }
                    case "Single[]":
                        {
                            float[] d = new float[GetIntLength(reader)];
                            for (int ii = 0; ii < d.Length; ii++)
                            {
                                d[ii] = reader.ReadSingle();
                            }
                            get = d;
                            break;
                        }
                    case "Double[]":
                        {
                            double[] d = new double[GetIntLength(reader)];
                            for (int ii = 0; ii < d.Length; ii++)
                            {
                                d[ii] = reader.ReadDouble();
                            }
                            get = d;
                            break;
                        }
                    case "Decimal[]":
                        {
                            decimal[] d = new decimal[GetIntLength(reader)];
                            for (int ii = 0; ii < d.Length; ii++)
                            {
                                d[ii] = reader.ReadDecimal();
                            }
                            get = d;
                            break;
                        }
                    case "Char[]":
                        {
                            char[] d = reader.ReadChars(GetIntLength(reader));
                            get = d;
                            break;
                        }
                    case "String[]":
                        {
                            string[] d = new string[GetIntLength(reader)];
                            for (int ii = 0; ii < d.Length; ii++)
                            {
                                d[ii] = reader.ReadString();
                            }
                            get = d;
                            break;
                        }
                    case "Boolean[]":
                        {
                            bool[] d = new bool[GetIntLength(reader)];
                            for (int ii = 0; ii < d.Length; ii++)
                            {
                                d[ii] = reader.ReadBoolean();
                            }
                            get = d;
                            break;
                        }
                    case "Object[]":
                        {
                            object[] d = new object[GetIntLength(reader)];
                            for (int ii = 0; ii < d.Length; ii++)
                            {
                                d[ii] = GetTyp(reader);
                            }
                            get = d;
                            break;
                        }
                    case "Dictionary`2":
                        {
                            string[] typenames = new string[] { type[reader.ReadByte()], type[reader.ReadByte()] };
                            typenames[0] = (typenames[0] == "Dictionary`2" ? "System.Collections.Generic." : "System.") + typenames[0];
                            typenames[1] = (typenames[1] == "Dictionary`2" ? "System.Collections.Generic." : "System.") + typenames[1];
                            Type[] types = new Type[] { Type.GetType(typenames[0]), Type.GetType(typenames[1]) };

                            Type thistype = typeof(Dictionary<,>).MakeGenericType(types);
                            System.Reflection.MethodInfo method = thistype.GetMethod("Add");

                            IDictionary d = (IDictionary)Activator.CreateInstance(thistype);
                            int count = GetIntLength(reader);
                            for (int ii = 0; ii < count; ii++)
                            {
                                object key = GetTyp(reader);
                                object value = GetTyp(reader);
                                method.Invoke(d, new object[] { key, value });
                            }
                            get = d;
                            break;
                        }
                    case "Byte":
                        {
                            get = reader.ReadByte();
                            break;
                        }
                    case "SByte":
                        {
                            get = reader.ReadSByte();
                            break;
                        }
                    case "Int16":
                        {
                            get = reader.ReadInt16();
                            break;
                        }
                    case "Int32":
                        {
                            get = reader.ReadInt32();
                            break;
                        }
                    case "Int64":
                        {
                            get = reader.ReadInt64();
                            break;
                        }
                    case "UInt16":
                        {
                            get = reader.ReadUInt16();
                            break;
                        }
                    case "UInt32":
                        {
                            get = reader.ReadUInt32();
                            break;
                        }
                    case "UInt64":
                        {
                            get = reader.ReadUInt64();
                            break;
                        }
                    case "Single":
                        {
                            get = reader.ReadSingle();
                            break;
                        }
                    case "Double":
                        {
                            get = reader.ReadDouble();
                            break;
                        }
                    case "Decimal":
                        {
                            get = reader.ReadDecimal();
                            break;
                        }
                    case "Char":
                        {
                            get = reader.ReadChar();
                            break;
                        }
                    case "String":
                        {
                            get = reader.ReadString();
                            break;
                        }
                    case "Boolean":
                        {
                            get = reader.ReadBoolean();
                            break;
                        }
                    case "null":
                        {
                            bool a = reader.ReadBoolean();
                            get = null;
                            break;
                        }
                    default:
                        {
                            get = typ;
                            break;
                        }
                }
            }
            else
            {
                get = reader.ReadString();
            }
            return get;
        }


        /// <summary>
        /// AES加密演算法
        /// </summary>
        /// <param name="plainText">明文位元組</param>
        /// <param name="strKey">金鑰</param>
        /// <returns>返回加密後的密文位元組陣列</returns>
        public static byte[] AESEncrypt(byte[] inputByteArray, byte[] _keyData, string strKey)
        {
            //分組加密演算法
            SymmetricAlgorithm des = Rijndael.Create();
            //設定金鑰及金鑰向量
            des.Key = Encoding.UTF8.GetBytes(strKey);
            des.IV = _keyData;
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            byte[] cipherBytes = ms.ToArray();//得到加密後的位元組陣列
            cs.Close();
            ms.Close();
            return cipherBytes;
        }

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="cipherText">密文位元組陣列</param>
        /// <param name="strKey">金鑰</param>
        /// <returns>返回解密後的位元組陣列</returns>
        public static byte[] AESDecrypt(byte[] cipherText, byte[] _keyData, string strKey)
        {
            SymmetricAlgorithm des = Rijndael.Create();
            des.Key = Encoding.UTF8.GetBytes(strKey);
            des.IV = _keyData;
            byte[] decryptBytes = new byte[cipherText.Length];
            MemoryStream ms = new MemoryStream(cipherText);
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Read);
            cs.Read(decryptBytes, 0, decryptBytes.Length);
            cs.Close();
            ms.Close();
            return decryptBytes;
        }


        static public byte[] Lock(byte[] bs, string key, bool _Lock)
        {
            byte[] encryptBytes;
            if (key != "")
            {
                if (_Lock)
                {
                    byte[] _key1 = new byte[16];
                    for (int i = 0; i < 16; i++)
                    {
                        _key1[i] = (byte)new Random(Guid.NewGuid().GetHashCode()).Next(0, 255);
                    }
                    encryptBytes = AESEncrypt(bs, _key1, key);

                    MemoryStream stream = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(stream);

                    writer.Write(_Lock);
                    writer.Write(_key1, 0, 16);
                    writer.Write(encryptBytes, 0, encryptBytes.Length);

                    writer.Close();
                    stream.Close();

                    encryptBytes = stream.ToArray();
                }
                else
                {
                    MemoryStream stream = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(stream);

                    writer.Write(_Lock);
                    writer.Write(bs, 0, bs.Length);

                    writer.Close();
                    stream.Close();

                    encryptBytes = stream.ToArray();
                }
            }
            else
            {
                MemoryStream stream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(stream);

                writer.Write(false);
                writer.Write(bs, 0, bs.Length);

                writer.Close();
                stream.Close();

                encryptBytes = stream.ToArray();
            }
            byte[] b;
            Compress(out b, encryptBytes);
            return b;
        }

        static public byte[] UnLock(byte[] bs, string key)
        {
            byte[] _out;
            MemoryStream stream = new MemoryStream(bs);
            BinaryReader reader = new BinaryReader(stream);
            bool _Lock = reader.ReadBoolean();
            if (_Lock)
            {
                byte[] _key1 = reader.ReadBytes(16);
                byte[] data = reader.ReadBytes(bs.Length - 17);
                _out = AESDecrypt(data, _key1, key);
            }
            else
            {
                _out = reader.ReadBytes(bs.Length - 1);
            }
            reader.Close();
            stream.Close();
            return _out;
        }

        // 寫字串
        static public void Compress(out byte[] _bytes, byte[] bytes)
        {
            MemoryStream stream;
            BinaryWriter writer;
            stream = new MemoryStream();

            int byteLength = bytes.Length;
            using (GZipStream compressionStream = new GZipStream(stream, CompressionMode.Compress))
            {
                compressionStream.Write(bytes, 0, bytes.Length);
            }
            stream.Close();

            byte[] bytes2 = stream.ToArray();
            stream.Dispose();
            stream = null;

            stream = new MemoryStream();
            writer = new BinaryWriter(stream);

            writer.Write(byteLength > bytes2.Length);

            writer.Write(byteLength);
            if (byteLength > bytes2.Length)
            {
                writer.Write(bytes2.Length);
                writer.Write(bytes2);
            }
            else
            {
                writer.Write(bytes);
            }
            writer.Close();
            stream.Close();
            _bytes = stream.ToArray();
        }

        // 讀取一個字串
        static public void Decompress(byte[] _bytes, int index, out byte[] str, out int length)
        {
            MemoryStream stream = new MemoryStream(_bytes);
            BinaryReader reader = new BinaryReader(stream);
            reader.ReadBytes(index);
            bool compress = reader.ReadBoolean();
            int q = reader.ReadInt32();
            byte[] bs = reader.ReadBytes(compress ? reader.ReadInt32() : q);

            reader.Close();
            reader.Dispose();
            stream.Close();
            stream.Dispose();
            stream = null;

            if (compress)
            {
                stream = new MemoryStream(bs);

                str = new byte[q];

                using (GZipStream decompressionStream = new GZipStream(stream, CompressionMode.Decompress))
                {
                    decompressionStream.Read(str, 0, q);
                }
                length = bs.Length + 9;
            }
            else
            {
                str = bs;
                length = bs.Length + 5;
            }
        }

        static private byte HexToByte(string hex)
        {
            if (hex.Length > 2 || hex.Length <= 0)
                throw new ArgumentException("hex must be 1 or 2 characters in length");
            byte newByte = byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return newByte;
        }

        static public byte[] WriteString(string str)
        {
            byte[] bytes = new byte[str.Length / 2];
            int j = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                string hex = new String(new Char[] { str[j], str[j + 1] });
                bytes[i] = HexToByte(hex);
                j = j + 2;
            }
            return bytes;
        }

        static public string ReadString(byte[] bytes)
        {
            StringBuilder str2 = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                str2.Append(bytes[i].ToString("X2"));
            }
            return str2.ToString();
        }
    }
}