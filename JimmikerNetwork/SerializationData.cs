using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace JimmikerNetwork
{
    public static class SerializationData
    {
        #region TypeName
        static readonly string[] type = new string[] { "Byte[]", "SByte[]", "Int16[]", "Int32[]", "Int64[]", "UInt16[]", "UInt32[]", "UInt64[]", "Single[]", "Double[]", "Decimal[]", "Char[]", "String[]", "Boolean[]", "Object[]", "Dictionary`2", "Byte", "SByte", "Int16", "Int32", "Int64", "UInt16", "UInt32", "UInt64", "Single", "Double", "Decimal", "Char", "String", "Boolean", "Object", "null" };
        static readonly string[] typelist = new string[] { "byte[]", "sbyte[]", "short[]", "int[]", "long[]", "ushort[]", "uint[]", "ulong[]", "float[]", "double[]", "decimal[]", "char[]", "string[]", "bool[]", "object[]", "Dictionary", "byte", "sbyte", "short", "int", "long", "ushort", "uint", "ulong", "float", "double", "decimal", "char", "string", "bool", "object" };
        static readonly string[] typelist2 = new string[] { "Byte[]", "SByte[]", "Int16[]", "Int32[]", "Int64[]", "UInt16[]", "UInt32[]", "UInt64[]", "Single[]", "Double[]", "Decimal[]", "Char[]", "String[]", "Boolean[]", "Object[]", "Dictionary`2", "Byte", "SByte", "Int16", "Int32", "Int64", "UInt16", "UInt32", "UInt64", "Single", "Double", "Decimal", "Char", "String", "Boolean", "Object" };

        static string ToTrueTypeName(string type)
        {
            string typenames = type;
            if (Array.IndexOf(typelist2, type) == -1 && type != "null")
            {
                typenames = typelist2[Array.IndexOf(typelist, type)];
            }
            return typenames;
        }

        static string ToSimpleTypeName(string type)
        {
            string typenames = type;
            if (Array.IndexOf(typelist, type) == -1 && type != "null")
            {
                typenames = typelist[Array.IndexOf(typelist2, type)];
            }
            return typenames;
        }

        static Type TypeNameToType(string typename)
        {
            return Type.GetType((typename == "Dictionary`2" ? "System.Collections.Generic." : "System.") + typename);
        }
        #endregion

        #region Length
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
        #endregion

        #region ToBytes and ToObject
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
        #endregion

        #region Typing
        static void TypingArray(BinaryWriter writer, object thing)
        {
            Array c = (Array)thing;
            string typename = thing.GetType().Name;
            writer.Write((byte)(Array.IndexOf(type, typename)));
            writer.Write(GetBytesLength(c.Length));
            typename = typename.RemoveString("[", "]");
            if (typename == "Byte" || typename == "Char")
            {
                typename += "[]";
                System.Reflection.MethodInfo write = typeof(BinaryWriter).GetMethod("Write", new Type[] { TypeNameToType(typename) });
                write.Invoke(writer, new object[] { c });
            }
            else
            {
                System.Reflection.MethodInfo write = typeof(BinaryWriter).GetMethod("Write", new Type[] { TypeNameToType(typename) });
                for (int ii = 0; ii < c.Length; ii++)
                {
                    if (typename == "Object")
                    {
                        Typing(writer, c.GetValue(ii));
                    }
                    else
                    {
                        write.Invoke(writer, new object[] { c.GetValue(ii) });
                    }
                }
            }
        }

        static void TypingNotArray(BinaryWriter writer, object thing)
        {
            string typename = thing.GetType().Name;
            writer.Write((byte)(Array.IndexOf(type, typename)));
            System.Reflection.MethodInfo write = typeof(BinaryWriter).GetMethod("Write", new Type[] { TypeNameToType(typename) });
            write.Invoke(writer, new object[] { thing });
        }

        public static byte[] Typing(BinaryWriter writer, object thing)
        {
            if (thing != null)
            {
                if (thing.GetType().Name.Contains("[]"))
                {
                    TypingArray(writer, thing);
                }
                else if (thing.GetType().Name == "Dictionary`2")
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
                }
                else if (Array.IndexOf(type, thing.GetType().Name) != -1)
                {
                    TypingNotArray(writer, thing);
                }
                else
                {
                    writer.Write((byte)type.Length);
                    writer.Write(thing.ToString());
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
        #endregion

        #region GetTyp
        static object GetTypArray(string typ, BinaryReader reader)
        {
            int count = GetIntLength(reader);
            typ = typ.RemoveString("[", "]");
            if (typ == "Byte" || typ == "Char")
            {
                System.Reflection.MethodInfo method = typeof(BinaryReader).GetMethod("Read" + TypeNameToType(typ).Name + "s");
                return method.Invoke(reader, new object[] { count });
            }
            else
            {
                Array d = Array.CreateInstance(TypeNameToType(typ), count);
                System.Reflection.MethodInfo method = typeof(BinaryReader).GetMethod("Read" + TypeNameToType(typ).Name);
                for (int i = 0; i < d.Length; i++)
                {
                    if (typ == "Object")
                    {
                        d.SetValue(GetTyp(reader), i);
                    }
                    else
                    {
                        d.SetValue(method.Invoke(reader, null), i);
                    }
                }
                return d;
            }
        }

        public static object GetTyp(BinaryReader reader)
        {
            byte data = reader.ReadByte();
            object get;
            if (data < type.Length)
            {
                string typ = type[data];
                if (typ.Contains("[]"))
                {
                    get = GetTypArray(typ, reader);
                }
                else if (typ == "Dictionary`2")
                {
                    string[] typenames = new string[] { type[reader.ReadByte()], type[reader.ReadByte()] };
                    Type[] types = new Type[] { TypeNameToType(typenames[0]), TypeNameToType(typenames[1]) };

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
                }
                else if(typ == "null")
                {
                    bool a = reader.ReadBoolean();
                    get = null;
                }
                else if (Array.IndexOf(type, typ) != -1)
                {
                    System.Reflection.MethodInfo method = typeof(BinaryReader).GetMethod("Read" + TypeNameToType(typ).Name);
                    get = method.Invoke(reader, null);
                }
                else
                {
                    get = typ;
                }
            }
            else
            {
                get = reader.ReadString();
            }
            return get;
        }
        #endregion

        #region 字串處理
        static int Matches(string input, char a)
        {
            string[] j = Split(input, a);
            return j.Length + 1;
        }

        public static string[] Split(string input, char a)
        {
            List<string> vs = new List<string>();
            int now = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '\\')
                {
                    i++;
                }
                else if (input[i] == a)
                {
                    vs.Add(input.Substring(now, i - now));
                    now = i + 1;
                }
            }
            vs.Add(input.Substring(now, input.Length - now));
            return vs.ToArray();
        }

        public static string FormattingString(string input)
        {
            StringBuilder stringBuilder = new StringBuilder(input);
            for (int i = 0; i < stringBuilder.Length; i++)
            {
                if (stringBuilder[i] == '\\')
                {
                    stringBuilder.Remove(i, 1);
                }
            }
            return stringBuilder.ToString();
        }

        public static string BeforeFormatString(string input, char[] a)
        {
            StringBuilder stringBuilder = new StringBuilder(input);
            for (int i = 0; i < stringBuilder.Length; i++)
            {
                if (Array.IndexOf(a, stringBuilder[i]) != -1 || stringBuilder[i] == '\\')
                {
                    stringBuilder.Insert(i, "\\");
                    i++;
                }
            }
            return stringBuilder.ToString();
        }

        public static string[] TakeString(this string text, char a, char b)
        {
            List<string> q = new List<string>(Split(text, b));
            if (a == b)
            {
                if (q.Count % 2 == 0)
                {
                    q.RemoveAt(q.Count - 1);
                }
                for (int i = 0; i < q.Count; i++)
                {
                    q.RemoveAt(i);
                }
                for (int i = 0; i < q.Count; i++)
                {
                    q[i] = FormattingString(q[i]);
                }
                return q.ToArray();
            }
            q.RemoveAt(q.Count - 1);
            for (int i = 0; i < q.Count;)
            {
                if (q[i] != "")
                {
                    if (Matches(q[i], a) != Matches(q[i], b) + 1)
                    {
                        q[i] += b.ToString() + q[i + 1];
                        q.RemoveAt(i + 1);
                    }
                    else
                    {
                        i++;
                    }
                }
                else
                {
                    q[i - 1] += b.ToString();
                    q.RemoveAt(i);
                }
            }
            List<string> vs = new List<string>();
            foreach (string s in q)
            {
                int found = 0;
                for (int i = 0; i < s.Length; i++)
                {
                    if (s[i] == '\\')
                    {
                        i++;
                    }
                    else if (s[i] == a)
                    {
                        found = i;
                        break;
                    }
                }
                if (found != -1)
                {
                    if (found + 1 == s.Length)
                    {
                        vs.Add("");
                    }
                    else
                    {
                        vs.Add(s.Substring(found + 1));
                    }
                }
            }
            return vs.ToArray();
        }

        public static string RemoveString(this string input, params string[] arg)
        {
            for (int i = 0; i < arg.Length; i++)
            {
                input = input.Replace(arg[i], "");
            }
            return input;
        }

        static string printTab(bool enable, int cont)
        {
            if (!enable)
            {
                return "";
            }
            string ans = "\r\n";
            for (int i = 0; i < cont; i++)
            {
                ans += "\t";
            }
            return ans;
        }

        #endregion

        #region ObjectToString
        static string ObjectToStringForArray(int cont, object thing, bool enter)
        {
            Array c = (Array)thing;
            string type = typelist[Array.IndexOf(typelist2, thing.GetType().Name)];
            string a = "";
            if (c.Length > 0)
            {
                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                if (type == "byte[]")
                {
                    a += BytesToHex((byte[])c) + printTab(enter, cont) + "}";
                }
                else
                {
                    string makestring(string leftright, bool isstringorchar, int index)
                    {
                        if(isstringorchar)
                            return leftright + BeforeFormatString(c.GetValue(index).ToString(), new char[] { '\'', '\"', '{', '}', '[', ']', ',', ':' }) + leftright;
                        else
                            return c.GetValue(index).ToString();
                    }
                    for (int i = 0; i < c.Length - 1; i++)
                    {
                        if (type == "object[]")
                            a += ObjectToString(cont + 1, c.GetValue(i), enter) + "," + printTab(enter, cont + 1);
                        else
                            a += makestring(type == "char[]" ? "\'" : "\"", type == "char[]" || type == "string[]", i) + ",";
                    }
                    if (type == "object[]")
                        a += ObjectToString(cont + 1, c.GetValue(c.Length - 1), enter) + printTab(enter, cont) + "}";
                    else
                        a += makestring(type == "char[]" ? "\'" : "\"", type == "char[]" || type == "string[]", c.Length - 1) + "}";
                }
            }
            else
            {
                a += "NotThing";
            }
            return a;
        }

        static string ObjectToString(int cont, object thing, bool enter)
        {
            string a = "";
            if (thing != null)
            {
                string typ = typelist[Array.IndexOf(typelist2, thing.GetType().Name)];
                a += typ + ":";
                if (typ.Contains("[]"))
                {
                    a += ObjectToStringForArray(cont, thing, enter);
                }
                else if (typ == "Dictionary")
                {
                    Type datatype = thing.GetType();
                    Type[] Subdatatype = datatype.GetGenericArguments();
                    IDictionary c = (IDictionary)thing;
                    a += printTab(enter, cont) + "{" + printTab(enter, cont + 1) + typelist[Array.IndexOf(typelist2, Subdatatype[0].Name)] + ":" + typelist[Array.IndexOf(typelist2, Subdatatype[1].Name)] + ":";

                    if (c.Count > 0)
                    {
                        Array keys = Array.CreateInstance(Subdatatype[0], c.Keys.Count);
                        Array values = Array.CreateInstance(Subdatatype[1], c.Values.Count);
                        c.Keys.CopyTo(keys, 0);
                        c.Values.CopyTo(values, 0);
                        for (int i = 0; i < c.Count; i++)
                        {
                            a += printTab(enter, cont + 1) + "{" + printTab(enter, cont + 2) + ObjectToString(cont + 2, keys.GetValue(i), enter) + "," + printTab(enter, cont + 2) + ObjectToString(cont + 2, values.GetValue(i), enter) + printTab(enter, cont + 1) + "}";
                        }
                    }
                    else
                    {
                        a += "NotThing";
                    }
                    a += printTab(enter, cont) + "}";
                }
                else if (Array.IndexOf(typelist, typ) != -1)
                {
                    string makestring(string leftright, bool isstringorchar)
                    {
                        if (isstringorchar)
                            return leftright + BeforeFormatString(thing.ToString(), new char[] { '\'', '\"', '{', '}', '[', ']', ',', ':' }) + leftright;
                        else
                            return thing.ToString();
                    }
                    a += makestring(typ == "char" ? "\'" : "\"", typ == "char" || typ == "string");
                }
                else
                {
                    a += thing.ToString();
                }
            }
            else
            {
                a += "null";
            }
            return a;
        }

        public static string ObjectToString(object thing)
        {
            return ObjectToString(0, thing, false);
        }

        public static string ObjectToStringWithEnter(object thing)
        {
            return ObjectToString(0, thing, true);
        }
        #endregion

        #region StringToObject
        static object StringToObjectForArray(string thing)
        {
            string[] vs = Split(thing, ':');
            string typ = vs[0].RemoveString(" ", "\n", "\r", "\t", "[", "]" );

            string typenames = ToTrueTypeName(typ);
            Type[] types = new Type[] { TypeNameToType(typenames) };

            int found = thing.IndexOf(':');
            if (thing.Substring(found + 1) != "NotThing")
            {
                string a = thing.Substring(found + 1).TakeString('{', '}')[0];

                if (typ == "byte")
                {
                    a = a.Replace(" ", "");
                    return HexToBytes(a);
                }
                else
                {
                    string[] b = null;
                    if (typ == "object")
                    {
                        b = TakeString(a, '{', '}');
                        for (int i = 0; i < b.Length; i++)
                        {
                            int index = a.IndexOf("{" + b[i] + "}");
                            a = a.Substring(0, index) + "[" + i + "]" + a.Substring(index + b[i].Length + 2);
                        }
                        string[] bb = Split(a, ',');
                        for (int i = 0; i < bb.Length; i++)
                        {
                            for (int ii = 0; ii < b.Length; ii++)
                            {
                                bb[i] = bb[i].Replace("[" + ii + "]", "{" + b[ii] + "}");
                            }
                        }
                        b = bb;
                    }
                    else
                    {
                        b = Split(a, ',');
                    }

                    Type thistype = typeof(List<>).MakeGenericType(types);
                    IList c = (IList)Activator.CreateInstance(thistype);
                    System.Reflection.MethodInfo toarray = thistype.GetMethod("ToArray");

                    if (typ == "object")
                    {
                        for (int i = 0; i < b.Length; i++)
                        {
                            c.Add(StringToObject(b[i]));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < b.Length; i++)
                        {
                            object[] data = new object[] { b[i] };
                            switch (typ)
                            {
                                case "char":
                                    {
                                        data[0] = b[i].TakeString('\'', '\'')[0];
                                        break;
                                    }
                                case "string":
                                    {
                                        data[0] = b[i].TakeString('\"', '\"')[0];
                                        break;
                                    }
                                case "bool":
                                    {
                                        data[0] = b[i].Replace(" ", "");
                                        break;
                                    }
                            }
                            System.Reflection.MethodInfo method = typeof(Convert).GetMethod("To" + types[0].Name);
                            c.Add(method.Invoke(null, data));
                        }
                    }
                    return toarray.Invoke(c, null);
                }
            }
            else
            {
                return Array.CreateInstance(types[0], 0);
            }
        }

        static object StringToObjectForNotArray(string thing)
        {
            string[] vs = Split(thing, ':');
            string typ = vs[0].RemoveString(" ", "\n", "\r", "\t", "[", "]");

            string typenames = ToTrueTypeName(typ);

            int found = thing.IndexOf(':');
            string a = thing.Substring(found + 1);
            switch(typ)
            {
                case "char":
                    {
                        a = a.TakeString('\'', '\'')[0];
                        break;
                    }
                case "string":
                    {
                        a = a.TakeString('\"', '\"')[0];
                        break;
                    }
                case "bool":
                    {
                        a = a.Replace(" ", "");
                        break;
                    }
            }
            System.Reflection.MethodInfo method = typeof(Convert).GetMethod("To" + TypeNameToType(typenames).Name, new Type[] { typeof(string) });
            object[] data = new object[] { a };
            object get = method.Invoke(null, data);
            if(typ == "byte")
            {
                get = (byte)get;
            }
            return get;
        }

        public static object StringToObject(string thing)
        {
            string[] vs = Split(thing, ':');
            string typ = ToSimpleTypeName(vs[0].RemoveString(" ", "\n", "\r", "\t" ));
            object get;
            if (typ.Contains("[]"))
            {
                get = StringToObjectForArray(thing);
            }
            else if(typ == "Dictionary")
            {
                int found = thing.IndexOf(':');
                string _data = thing.Substring(found + 1).TakeString('{', '}')[0];

                int data_index = _data.IndexOf(':');
                int data_index2 = _data.IndexOf(':', data_index + 1);

                string[] data = new string[] { _data.Substring(0, data_index), _data.Substring(data_index + 1, data_index2 - data_index - 1), _data.Substring(data_index2 + 1) };
                data[0] = data[0].RemoveString(" ", "\n", "\r", "\t" );
                data[1] = data[1].RemoveString(" ", "\n", "\r", "\t" );
                string[] typenames = new string[] { typelist2[Array.IndexOf(typelist, data[0])], typelist2[Array.IndexOf(typelist, data[1])] };
                Type[] types = new Type[] { TypeNameToType(typenames[0]), TypeNameToType(typenames[1]) };

                Type thistype = typeof(Dictionary<,>).MakeGenericType(types);

                get = Activator.CreateInstance(thistype);

                if (data[2] != "NotThing")
                {
                    System.Reflection.MethodInfo method = thistype.GetMethod("Add");

                    string[] a = data[2].TakeString('{', '}');

                    for (int i = 0; i < a.Length; i++)
                    {
                        string[] b = a[i].TakeString('{', '}');
                        for (int ii = 0; ii < b.Length; ii++)
                        {
                            int index = a[i].IndexOf("{" + b[ii] + "}");
                            a[i] = a[i].Substring(0, index) + "[" + ii + "]" + a[i].Substring(index + b[ii].Length + 2);
                        }
                        string[] nowdata = Split(a[i], ',');
                        for (int ii = 0; ii < nowdata.Length; ii++)
                        {
                            for (int iii = 0; iii < b.Length; iii++)
                            {
                                nowdata[ii] = nowdata[ii].Replace("[" + iii + "]", "{" + b[iii] + "}");
                            }
                        }
                        object key = StringToObject(nowdata[0]);
                        object value = StringToObject(nowdata[1]);
                        method.Invoke(get, new object[] { key, value });
                    }
                }
            }
            else if(typ == "null")
            {
                get = null;
            }
            else if(Array.IndexOf(typelist, typ) != -1)
            {
                get = StringToObjectForNotArray(thing);
            }
            else
            {
                get = typ;
            }
            return get;
        }
        #endregion

        #region BytesToString And StringToBytes
        public static string BytesToString(byte[] input)
        {
            object datas = ToObject(input);
            return ObjectToString(datas);
        }

        public static string BytesToStringWithEnter(byte[] input)
        {
            object datas = ToObject(input);
            return ObjectToStringWithEnter(datas);
        }

        public static byte[] StringToBytes(string input)
        {
            object data = StringToObject(input);
            return ToBytes(data);
        }
        #endregion

        #region 加密

        #region AES
        static public string GenerateAESKey()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }

        /// <summary>
        /// AES加密演算法
        /// </summary>
        /// <param name="plainText">明文位元組</param>
        /// <param name="strKey">金鑰</param>
        /// <returns>返回加密後的密文位元組陣列</returns>
        public static byte[] AESEncrypt(byte[] inputByteArray, byte[] IV, string strKey)
        {
            //分組加密演算法
            SymmetricAlgorithm des = Rijndael.Create();
            //設定金鑰及金鑰向量
            des.Key = Encoding.UTF8.GetBytes(strKey);
            des.IV = IV;
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
        public static byte[] AESDecrypt(byte[] cipherText, byte[] IV, string strKey)
        {
            SymmetricAlgorithm des = Rijndael.Create();
            des.Key = Encoding.UTF8.GetBytes(strKey);
            des.IV = IV;
            byte[] decryptBytes = new byte[cipherText.Length];
            MemoryStream ms = new MemoryStream(cipherText);
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Read);
            cs.Read(decryptBytes, 0, decryptBytes.Length);
            cs.Close();
            ms.Close();
            return decryptBytes;
        }
        #endregion

        #region RSA

        public struct RSAKeyPair
        {
            public string PrivateKey { get; private set; }
            public string PublicKey { get; private set; }

            public byte[] PrivateKeyBytes
            {
                get
                {
                    return Convert.FromBase64String(PrivateKey);
                }
            }
            public byte[] PublicKeyBytes
            {
                get
                {
                    return Convert.FromBase64String(PublicKey);
                }
            }

            public RSAKeyPair(string PrivateKey, string PublicKey)
            {
                this.PrivateKey = PrivateKey;
                this.PublicKey = PublicKey;
            }

            public RSAKeyPair(string PublicKey)
            {
                this.PrivateKey = "";
                this.PublicKey = PublicKey;
            }

            public RSAKeyPair(byte[] PrivateKey, byte[] PublicKey)
            {
                this.PrivateKey = Convert.ToBase64String(PrivateKey);
                this.PublicKey = Convert.ToBase64String(PublicKey);
            }

            public RSAKeyPair(byte[] PublicKey)
            {
                this.PrivateKey = "";
                this.PublicKey = Convert.ToBase64String(PublicKey);
            }
        }

        static public int GetRSAKeySize(string Key)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(Convert.FromBase64String(Key));
            return rsa.KeySize;
        }

        static public RSAKeyPair GenerateRSAKeys(int size)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(size);
            return new RSAKeyPair(rsa.ExportCspBlob(true), rsa.ExportCspBlob(false));
        }

        static public byte[] RSAEncrypt(string publicKey, byte[] IV, byte[] content)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(Convert.FromBase64String(publicKey));

            int buffersize = (rsa.KeySize / 8) - 11;

            byte[] newIV = new byte[buffersize];
            Array.Copy(IV, 0, newIV, 0, Math.Min(buffersize, IV.Length));

            for (int i = IV.Length; i < buffersize; i++)
            {
                int now = (newIV[i - IV.Length] + newIV[i - IV.Length + 1]) % 256;
                newIV[i] = (byte)now;
            }

            using (MemoryStream stream = new MemoryStream())
            using(BinaryWriter writer = new BinaryWriter(stream))
            {
                for (int i = 0; i < content.Length; i += buffersize)
                {
                    int copyLength = Math.Min(content.Length - i, buffersize);
                    byte[] buffer = new byte[copyLength];
                    Array.Copy(content, i, buffer, 0, copyLength);

                    BitArray bitArray = new BitArray(buffer);
                    byte[] nowIV = new byte[copyLength];
                    Array.Copy(newIV, 0, nowIV, 0, copyLength);
                    BitArray bitIV = new BitArray(nowIV);
                    byte[] newbuffer = new byte[copyLength];
                    bitArray.Xor(bitIV).CopyTo(newbuffer, 0);

                    byte[] encryptdata = rsa.Encrypt(newbuffer, false);
                    writer.Write(encryptdata);

                    Array.Copy(encryptdata, 0, newIV, 0, newIV.Length);
                }
                writer.Close();
                stream.Close();
                return stream.ToArray();
            }
        }

        static public byte[] RSADecrypt(string privateKey, byte[] IV, byte[] encryptedContent)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(Convert.FromBase64String(privateKey));

            int buffersize = rsa.KeySize / 8;
            int Decryptsize = (rsa.KeySize / 8) - 11;

            byte[] newIV = new byte[Decryptsize];
            Array.Copy(IV, 0, newIV, 0, Math.Min(Decryptsize, IV.Length));

            for (int i = IV.Length; i < Decryptsize; i++)
            {
                int now = (newIV[i - IV.Length] + newIV[i - IV.Length + 1]) % 256;
                newIV[i] = (byte)now;
            }

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                for (int i = 0; i < encryptedContent.Length; i += buffersize)
                {
                    int copyLength = Math.Min(encryptedContent.Length - i, buffersize);
                    byte[] buffer = new byte[copyLength];
                    Array.Copy(encryptedContent, i, buffer, 0, copyLength);

                    byte[] decryptString = rsa.Decrypt(buffer, false);

                    BitArray bitArray = new BitArray(decryptString);
                    byte[] nowIV = new byte[decryptString.Length];
                    Array.Copy(newIV, 0, nowIV, 0, decryptString.Length);
                    BitArray bitIV = new BitArray(nowIV);
                    byte[] newbuffer = new byte[decryptString.Length];
                    bitArray.Xor(bitIV).CopyTo(newbuffer, 0);

                    writer.Write(newbuffer);

                    Array.Copy(buffer, 0, newIV, 0, newIV.Length);
                }
                writer.Close();
                stream.Close();
                return stream.ToArray();
            }
        }

        static public byte[] RSASignData(string privateKey, byte[] content, object halg)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(Convert.FromBase64String(privateKey));

            var encryptString = rsa.SignData(content, halg);

            return encryptString;
        }

        static public bool RSAVerifyData(string publicKey, byte[] content, byte[] signature, object halg)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(Convert.FromBase64String(publicKey));

            var Verify = rsa.VerifyData(content, halg, signature);

            return Verify;
        }

        #endregion

        public enum LockType
        {
            None,
            AES,
            RSA,
        }

        static public byte[] Lock(byte[] bs, string key, LockType _Lock)
        {
            byte[] encryptBytes = null;

            byte[] setdata(LockType Lock, byte[] IV, byte[] data)
            {
                MemoryStream stream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(stream);

                writer.Write((byte)Lock);
                if (IV != null)
                {
                    writer.Write(IV, 0, 16);
                }
                writer.Write(data);

                writer.Close();
                stream.Close();

                return stream.ToArray();
            }

            if (!string.IsNullOrEmpty(key))
            {
                switch(_Lock)
                {
                    case LockType.None:
                        {
                            encryptBytes = setdata(_Lock, null, bs);
                            break;
                        }
                    case LockType.AES:
                        {
                            byte[] IV = new byte[16];
                            new Random(Guid.NewGuid().GetHashCode()).NextBytes(IV);
                            encryptBytes = setdata(_Lock, IV, AESEncrypt(bs, IV, key));
                            break;
                        }
                    case LockType.RSA:
                        {
                            byte[] IV = new byte[16];
                            new Random(Guid.NewGuid().GetHashCode()).NextBytes(IV);
                            encryptBytes = setdata(_Lock, IV, RSAEncrypt(key, IV, bs));
                            break;
                        }
                }
            }
            else
            {
                encryptBytes = setdata(LockType.None, null, bs);
            }
            byte[] b;
            Compress(out b, encryptBytes);
            return b;
        }

        static public byte[] UnLock(byte[] bs, string key)
        {
            const int TypeLen = 1;
            const int IVLen = 16;

            byte[] _out = null;
            MemoryStream stream = new MemoryStream(bs);
            BinaryReader reader = new BinaryReader(stream);
            LockType _Lock = (LockType)reader.ReadByte();
            switch(_Lock)
            {
                case LockType.None:
                    {
                        _out = reader.ReadBytes(bs.Length - TypeLen);
                        break;
                    }
                case LockType.AES:
                    {
                        byte[] IV = reader.ReadBytes(IVLen);
                        byte[] data = reader.ReadBytes(bs.Length - TypeLen - IVLen);
                        _out = AESDecrypt(data, IV, key);
                        break;
                    }
                case LockType.RSA:
                    {
                        byte[] IV = reader.ReadBytes(IVLen);
                        byte[] data = reader.ReadBytes(bs.Length - TypeLen - IVLen);
                        _out = RSADecrypt(key, IV, data);
                        break;
                    }
            }
            reader.Close();
            stream.Close();
            return _out;
        }

        static public byte[] SignData(string privateKey, string AESKey, byte[] content)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            byte[] data = Lock(content, AESKey, LockType.AES);
            writer.Write(data.Length);
            writer.Write(data);
            writer.Write(RSASignData(privateKey, content, new SHA256CryptoServiceProvider()));
            writer.Close();
            stream.Close();
            return stream.ToArray();
        }

        static public bool VerifyData(string publicKey, string AESKey, byte[] signature)
        {
            MemoryStream stream = new MemoryStream(signature);
            BinaryReader reader = new BinaryReader(stream);
            int datalen = reader.ReadInt32();
            byte[] data = UnLock(reader.ReadBytes(datalen), AESKey);
            byte[] sign = reader.ReadBytes(signature.Length - datalen);
            return RSAVerifyData(publicKey, data, sign, new SHA256CryptoServiceProvider());
        }

        #endregion

        #region 壓縮
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
        #endregion

        #region Hex
        static public byte[] HexToBytes(string str)
        {
            str = str.RemoveString(" ", "\n", "\r", "\t" );
            byte[] bytes = new byte[str.Length / 2];
            int j = 0;

            byte HexToByte(string hex)
            {
                if (hex.Length > 2 || hex.Length <= 0)
                    throw new ArgumentException("hex must be 1 or 2 characters in length");
                byte newByte = byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                return newByte;
            }

            for (int i = 0; i < bytes.Length; i++)
            {
                string hex = new String(new Char[] { str[j], str[j + 1] });
                bytes[i] = HexToByte(hex);
                j = j + 2;
            }
            return bytes;
        }

        static public string BytesToHex(byte[] bytes)
        {
            StringBuilder str2 = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                str2.Append(bytes[i].ToString("X2"));
            }
            return str2.ToString();
        }
        #endregion

        #region 擴充方法

        #endregion
    }
}