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
        static readonly string[] typelist = new string[] { "byte[]", "sbyte[]", "short[]", "int[]", "long[]", "ushort[]", "uint[]", "ulong[]", "float[]", "double[]", "decimal[]", "char[]", "string[]", "bool[]", "object[]", "Dictionary", "byte", "sbyte", "short", "int", "long", "ushort", "uint", "ulong", "float", "double", "decimal", "char", "string", "bool", "object" };
        static readonly string[] typelist2 = new string[] { "Byte[]", "SByte[]", "Int16[]", "Int32[]", "Int64[]", "UInt16[]", "UInt32[]", "UInt64[]", "Single[]", "Double[]", "Decimal[]", "Char[]", "String[]", "Boolean[]", "Object[]", "Dictionary`2", "Byte", "SByte", "Int16", "Int32", "Int64", "UInt16", "UInt32", "UInt64", "Single", "Double", "Decimal", "Char", "String", "Boolean", "Object" };

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

        public static string[] TakeString(string text, char a, char b)
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

        static string RemoveString(string input, string[] arg)
        {
            for(int i = 0; i < arg.Length; i++)
            {
                input = input.Replace(arg[i], "");
            }
            return input;
        }

        static string printTab(bool enable, int cont)
        {
            if(!enable)
            {
                return "";
            }
            string ans = "\r\n";
            for(int i = 0; i < cont; i++)
            {
                ans += "\t";
            }
            return ans;
        }

        static string ObjectToString(int cont, object thing, bool enter)
        {
            string a = "";
            if (thing != null)
            {
                string type = typelist[Array.IndexOf(typelist2, thing.GetType().Name)];
                a += type + ":";
                switch (type)
                {
                    case "byte[]":
                        {
                            byte[] c = (byte[])thing;
                            if (c.Length > 0)
                            {
                                //a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1) + ReadString(c) + printTab(enter, cont) + "}";
                                /*for (int i = 0; i < c.Length - 1; i++)
                                {
                                    a += c[i].ToString() + ",";
                                }
                                a += c[c.Length - 1].ToString() + printTab(enter, cont) + "}";*/
                            }
                            else
                            {
                                a += "NotThing";
                            }
                            break;
                        }
                    case "sbyte[]":
                        {
                            sbyte[] c = (sbyte[])thing;
                            if (c.Length > 0)
                            {
                                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                                for (int i = 0; i < c.Length - 1; i++)
                                {
                                    a += c[i].ToString() + ",";
                                }
                                a += c[c.Length - 1].ToString() + printTab(enter, cont) + "}";
                            }
                            else
                            {
                                a += "NotThing";
                            }
                            break;
                        }
                    case "short[]":
                        {
                            short[] c = (short[])thing;
                            if (c.Length > 0)
                            {
                                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                                for (int i = 0; i < c.Length - 1; i++)
                                {
                                    a += c[i].ToString() + ",";
                                }
                                a += c[c.Length - 1].ToString() + printTab(enter, cont) + "}";
                            }
                            else
                            {
                                a += "NotThing";
                            }
                            break;
                        }
                    case "int[]":
                        {
                            int[] c = (int[])thing;
                            if (c.Length > 0)
                            {
                                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                                for (int i = 0; i < c.Length - 1; i++)
                                {
                                    a += c[i].ToString() + ",";
                                }
                                a += c[c.Length - 1].ToString() + printTab(enter, cont) + "}";
                            }
                            else
                            {
                                a += "NotThing";
                            }
                            break;
                        }
                    case "long[]":
                        {
                            long[] c = (long[])thing;
                            if (c.Length > 0)
                            {
                                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                                for (int i = 0; i < c.Length - 1; i++)
                                {
                                    a += c[i].ToString() + ",";
                                }
                                a += c[c.Length - 1].ToString() + printTab(enter, cont) + "}";
                            }
                            else
                            {
                                a += "NotThing";
                            }
                            break;
                        }
                    case "ushort[]":
                        {
                            ushort[] c = (ushort[])thing;
                            if (c.Length > 0)
                            {
                                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                                for (int i = 0; i < c.Length - 1; i++)
                                {
                                    a += c[i].ToString() + ",";
                                }
                                a += c[c.Length - 1].ToString() + printTab(enter, cont) + "}";
                            }
                            else
                            {
                                a += "NotThing";
                            }
                            break;
                        }
                    case "uint[]":
                        {
                            uint[] c = (uint[])thing;
                            if (c.Length > 0)
                            {
                                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                                for (int i = 0; i < c.Length - 1; i++)
                                {
                                    a += c[i].ToString() + ",";
                                }
                                a += c[c.Length - 1].ToString() + printTab(enter, cont) + "}";
                            }
                            else
                            {
                                a += "NotThing";
                            }
                            break;
                        }
                    case "ulong[]":
                        {
                            ulong[] c = (ulong[])thing;
                            if (c.Length > 0)
                            {
                                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                                for (int i = 0; i < c.Length - 1; i++)
                                {
                                    a += c[i].ToString() + ",";
                                }
                                a += c[c.Length - 1].ToString() + printTab(enter, cont) + "}";
                            }
                            else
                            {
                                a += "NotThing";
                            }
                            break;
                        }
                    case "float[]":
                        {
                            float[] c = (float[])thing;
                            if (c.Length > 0)
                            {
                                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                                for (int i = 0; i < c.Length - 1; i++)
                                {
                                    a += c[i].ToString() + ",";
                                }
                                a += c[c.Length - 1].ToString() + printTab(enter, cont) + "}";
                            }
                            else
                            {
                                a += "NotThing";
                            }
                            break;
                        }
                    case "double[]":
                        {
                            double[] c = (double[])thing;
                            if (c.Length > 0)
                            {
                                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                                for (int i = 0; i < c.Length - 1; i++)
                                {
                                    a += c[i].ToString() + ",";
                                }
                                a += c[c.Length - 1].ToString() + printTab(enter, cont) + "}";
                            }
                            else
                            {
                                a += "NotThing";
                            }
                            break;
                        }
                    case "decimal[]":
                        {
                            decimal[] c = (decimal[])thing;
                            if (c.Length > 0)
                            {
                                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                                for (int i = 0; i < c.Length - 1; i++)
                                {
                                    a += c[i].ToString() + ",";
                                }
                                a += c[c.Length - 1].ToString() + printTab(enter, cont) + "}";
                            }
                            else
                            {
                                a += "NotThing";
                            }
                            break;
                        }
                    case "char[]":
                        {
                            char[] c = (char[])thing;
                            if (c.Length > 0)
                            {
                                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                                for (int i = 0; i < c.Length - 1; i++)
                                {
                                    a += "\'" + c[i].ToString() + "\',";
                                }
                                a += "\'" + BeforeFormatString(c[c.Length - 1].ToString(), new char[] { '\'', '\"', '{', '}', '[', ']', ',', ':' }) + "\'" + printTab(enter, cont) + "}";
                            }
                            else
                            {
                                a += "NotThing";
                            }
                            break;
                        }
                    case "string[]":
                        {
                            string[] c = (string[])thing;
                            if (c.Length > 0)
                            {
                                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                                for (int i = 0; i < c.Length - 1; i++)
                                {
                                    a += "\"" + c[i].ToString() + "\",";
                                }
                                a += "\"" + BeforeFormatString(c[c.Length - 1].ToString(), new char[] { '\'', '\"', '{', '}', '[', ']', ',', ':' }) + "\"" + printTab(enter, cont) + "}";
                            }
                            else
                            {
                                a += "NotThing";
                            }
                            break;
                        }
                    case "bool[]":
                        {
                            bool[] c = (bool[])thing;
                            if (c.Length > 0)
                            {
                                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                                for (int i = 0; i < c.Length - 1; i++)
                                {
                                    a += c[i].ToString() + ",";
                                }
                                a += c[c.Length - 1].ToString() + printTab(enter, cont) + "}";
                            }
                            else
                            {
                                a += "NotThing";
                            }
                            break;
                        }
                    case "object[]":
                        {
                            object[] c = (object[])thing;
                            if (c.Length > 0)
                            {
                                a += printTab(enter, cont) + "{" + printTab(enter, cont + 1);
                                for (int i = 0; i < c.Length - 1; i++)
                                {
                                    a += ObjectToString(cont + 1, c[i], enter) + "," + printTab(enter, cont + 1);
                                }
                                a += ObjectToString(cont + 1, c[c.Length - 1], enter) + printTab(enter, cont) + "}";
                            }
                            else
                            {
                                a += "NotThing";
                            }
                            break;
                        }
                    case "Dictionary":
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
                            break;
                        }
                    case "byte":
                        {
                            byte c = (byte)thing;
                            a += c.ToString();
                            break;
                        }
                    case "sbyte":
                        {
                            sbyte c = (sbyte)thing;
                            a += c.ToString();
                            break;
                        }
                    case "short":
                        {
                            short c = (short)thing;
                            a += c.ToString();
                            break;
                        }
                    case "int":
                        {
                            int c = (int)thing;
                            a += c.ToString();
                            break;
                        }
                    case "long":
                        {
                            long c = (long)thing;
                            a += c.ToString();
                            break;
                        }
                    case "ushort":
                        {
                            ushort c = (ushort)thing;
                            a += c.ToString();
                            break;
                        }
                    case "uint":
                        {
                            uint c = (uint)thing;
                            a += c.ToString();
                            break;
                        }
                    case "ulong":
                        {
                            ulong c = (ulong)thing;
                            a += c.ToString();
                            break;
                        }
                    case "float":
                        {
                            float c = (float)thing;
                            a += c.ToString();
                            break;
                        }
                    case "double":
                        {
                            double c = (double)thing;
                            a += c.ToString();
                            break;
                        }
                    case "decimal":
                        {
                            decimal c = (decimal)thing;
                            a += c.ToString();
                            break;
                        }
                    case "char":
                        {
                            char c = (char)thing;
                            a += "\'" + BeforeFormatString(c.ToString(), new char[] { '\'', '\"', '{', '}', '[', ']', ',', ':' }) + "\'";
                            break;
                        }
                    case "string":
                        {
                            string c = (string)thing;
                            a += "\"" + BeforeFormatString(c.ToString(), new char[] { '\'', '\"', '{', '}', '[', ']', ',', ':' }) + "\"";
                            break;
                        }
                    case "bool":
                        {
                            bool c = (bool)thing;
                            a += c.ToString();
                            break;
                        }
                    default:
                        {
                            a += thing.ToString();
                            break;
                        }
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

        public static object StringToObject(string thing)
        {
            string[] vs = Split(thing, ':');
            string typ = RemoveString(vs[0], new string[] { " ", "\n", "\r", "\t" });
            object get;
            switch (typ)
            {
                case "byte[]":
                    {
                        int found = thing.IndexOf(':');
                        if (thing.Substring(found + 1) != "NotThing")
                        {
                            string a = TakeString(thing.Substring(found + 1), '{', '}')[0].Replace(" ", "");
                            /*string[] b = Split(a, ',');
                            List<byte> c = new List<byte>();
                            for (int i = 0; i < b.Length; i++)
                            {
                                c.Add((byte)Convert.ToInt32(b[i]));
                            }*/
                            get = WriteString(a);//c.ToArray();
                        }
                        else
                        {
                            get = new byte[0];
                        }
                        break;
                    }
                case "sbyte[]":
                    {
                        int found = thing.IndexOf(':');
                        if (thing.Substring(found + 1) != "NotThing")
                        {
                            string a = TakeString(thing.Substring(found + 1), '{', '}')[0];
                            string[] b = Split(a, ',');
                            List<sbyte> c = new List<sbyte>();
                            for (int i = 0; i < b.Length; i++)
                            {
                                c.Add(Convert.ToSByte(b[i]));
                            }
                            get = c.ToArray();
                        }
                        else
                        {
                            get = new sbyte[0];
                        }
                        break;
                    }
                case "short[]":
                    {
                        int found = thing.IndexOf(':');
                        if (thing.Substring(found + 1) != "NotThing")
                        {
                            string a = TakeString(thing.Substring(found + 1), '{', '}')[0];
                            string[] b = Split(a, ',');
                            List<short> c = new List<short>();
                            for (int i = 0; i < b.Length; i++)
                            {
                                c.Add(Convert.ToInt16(b[i]));
                            }
                            get = c.ToArray();
                        }
                        else
                        {
                            get = new short[0];
                        }
                        break;
                    }
                case "int[]":
                    {
                        int found = thing.IndexOf(':');
                        if (thing.Substring(found + 1) != "NotThing")
                        {
                            string a = TakeString(thing.Substring(found + 1), '{', '}')[0];
                            string[] b = Split(a, ',');
                            List<int> c = new List<int>();
                            for (int i = 0; i < b.Length; i++)
                            {
                                c.Add(Convert.ToInt32(b[i]));
                            }
                            get = c.ToArray();
                        }
                        else
                        {
                            get = new int[0];
                        }
                        break;
                    }
                case "long[]":
                    {
                        int found = thing.IndexOf(':');
                        if (thing.Substring(found + 1) != "NotThing")
                        {
                            string a = TakeString(thing.Substring(found + 1), '{', '}')[0];
                            string[] b = Split(a, ',');
                            List<long> c = new List<long>();
                            for (int i = 0; i < b.Length; i++)
                            {
                                c.Add(Convert.ToInt64(b[i]));
                            }
                            get = c.ToArray();
                        }
                        else
                        {
                            get = new long[0];
                        }
                        break;
                    }
                case "ushort[]":
                    {
                        int found = thing.IndexOf(':');
                        if (thing.Substring(found + 1) != "NotThing")
                        {
                            string a = TakeString(thing.Substring(found + 1), '{', '}')[0];
                            string[] b = Split(a, ',');
                            List<ushort> c = new List<ushort>();
                            for (int i = 0; i < b.Length; i++)
                            {
                                c.Add(Convert.ToUInt16(b[i]));
                            }
                            get = c.ToArray();
                        }
                        else
                        {
                            get = new ushort[0];
                        }
                        break;
                    }
                case "uint[]":
                    {
                        int found = thing.IndexOf(':');
                        if (thing.Substring(found + 1) != "NotThing")
                        {
                            string a = TakeString(thing.Substring(found + 1), '{', '}')[0];
                            string[] b = Split(a, ',');
                            List<uint> c = new List<uint>();
                            for (int i = 0; i < b.Length; i++)
                            {
                                c.Add(Convert.ToUInt32(b[i]));
                            }
                            get = c.ToArray();
                        }
                        else
                        {
                            get = new uint[0];
                        }
                        break;
                    }
                case "ulong[]":
                    {
                        int found = thing.IndexOf(':');
                        if (thing.Substring(found + 1) != "NotThing")
                        {
                            string a = TakeString(thing.Substring(found + 1), '{', '}')[0];
                            string[] b = Split(a, ',');
                            List<ulong> c = new List<ulong>();
                            for (int i = 0; i < b.Length; i++)
                            {
                                c.Add(Convert.ToUInt64(b[i]));
                            }
                            get = c.ToArray();
                        }
                        else
                        {
                            get = new ulong[0];
                        }
                        break;
                    }
                case "float[]":
                    {
                        int found = thing.IndexOf(':');
                        if (thing.Substring(found + 1) != "NotThing")
                        {
                            string a = TakeString(thing.Substring(found + 1), '{', '}')[0];
                            string[] b = Split(a, ',');
                            List<float> c = new List<float>();
                            for (int i = 0; i < b.Length; i++)
                            {
                                c.Add(Convert.ToSingle(b[i]));
                            }
                            get = c.ToArray();
                        }
                        else
                        {
                            get = new float[0];
                        }
                        break;
                    }
                case "double[]":
                    {
                        int found = thing.IndexOf(':');
                        if (thing.Substring(found + 1) != "NotThing")
                        {
                            string a = TakeString(thing.Substring(found + 1), '{', '}')[0];
                            string[] b = Split(a, ',');
                            List<double> c = new List<double>();
                            for (int i = 0; i < b.Length; i++)
                            {
                                c.Add(Convert.ToDouble(b[i]));
                            }
                            get = c.ToArray();
                        }
                        else
                        {
                            get = new double[0];
                        }
                        break;
                    }
                case "decimal[]":
                    {
                        int found = thing.IndexOf(':');
                        if (thing.Substring(found + 1) != "NotThing")
                        {
                            string a = TakeString(thing.Substring(found + 1), '{', '}')[0];
                            string[] b = Split(a, ',');
                            List<decimal> c = new List<decimal>();
                            for (int i = 0; i < b.Length; i++)
                            {
                                c.Add(Convert.ToDecimal(b[i]));
                            }
                            get = c.ToArray();
                        }
                        else
                        {
                            get = new decimal[0];
                        }
                        break;
                    }
                case "char[]":
                    {
                        int found = thing.IndexOf(':');
                        if (thing.Substring(found + 1) != "NotThing")
                        {
                            string a = TakeString(thing.Substring(found + 1), '{', '}')[0];
                            string[] b = Split(a, ',');
                            List<char> c = new List<char>();
                            for (int i = 0; i < b.Length; i++)
                            {
                                string ans = TakeString(b[i], '\'', '\'')[0];
                                c.Add(Convert.ToChar(ans));
                            }
                            get = c.ToArray();
                        }
                        else
                        {
                            get = new char[0];
                        }
                        break;
                    }
                case "string[]":
                    {
                        int found = thing.IndexOf(':');
                        if (thing.Substring(found + 1) != "NotThing")
                        {
                            string a = TakeString(thing.Substring(found + 1), '{', '}')[0];
                            string[] b = Split(a, ',');
                            List<string> c = new List<string>();
                            for (int i = 0; i < b.Length; i++)
                            {
                                string ans = TakeString(b[i], '\"', '\"')[0];
                                c.Add(Convert.ToString(ans));
                            }
                            get = c.ToArray();
                        }
                        else
                        {
                            get = new string[0];
                        }
                        break;
                    }
                case "bool[]":
                    {
                        int found = thing.IndexOf(':');
                        if (thing.Substring(found + 1) != "NotThing")
                        {
                            string a = TakeString(thing.Substring(found + 1), '{', '}')[0];
                            string[] b = Split(a, ',');
                            List<bool> c = new List<bool>();
                            for (int i = 0; i < b.Length; i++)
                            {
                                c.Add(Convert.ToBoolean(b[i].Replace(" ", "")));
                            }
                            get = c.ToArray();
                        }
                        else
                        {
                            get = new bool[0];
                        }
                        break;
                    }
                case "object[]":
                    {
                        int found = thing.IndexOf(':');
                        if (thing.Substring(found + 1) != "NotThing")
                        {
                            string a = TakeString(thing.Substring(found + 1), '{', '}')[0];
                            string[] b = TakeString(a, '{', '}');
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
                            List<object> c = new List<object>();
                            for (int i = 0; i < bb.Length; i++)
                            {
                                c.Add(StringToObject(bb[i]));
                            }
                            get = c.ToArray();
                        }
                        else
                        {
                            get = new object[0];
                        }
                        break;
                    }
                case "Dictionary":
                    {
                        int found = thing.IndexOf(':');
                        string _data = TakeString(thing.Substring(found + 1), '{', '}')[0];

                        int data_index = _data.IndexOf(':');
                        int data_index2 = _data.IndexOf(':', data_index + 1);

                        string[] data = new string[] { _data.Substring(0, data_index), _data.Substring(data_index + 1, data_index2 - data_index - 1), _data.Substring(data_index2 + 1) };
                        data[0] = RemoveString(data[0], new string[] { " ", "\n", "\r", "\t" });
                        data[1] = RemoveString(data[1], new string[] { " ", "\n", "\r", "\t" });
                        string[] typenames = new string[] { typelist2[Array.IndexOf(typelist, data[0])], typelist2[Array.IndexOf(typelist, data[1])] };
                        typenames[0] = (typenames[0] == "Dictionary`2" ? "System.Collections.Generic." : "System.") + typenames[0];
                        typenames[1] = (typenames[1] == "Dictionary`2" ? "System.Collections.Generic." : "System.") + typenames[1];
                        Type[] types = new Type[] { Type.GetType(typenames[0]), Type.GetType(typenames[1]) };

                        Type thistype = typeof(Dictionary<,>).MakeGenericType(types);

                        get = Activator.CreateInstance(thistype);

                        if (data[2] != "NotThing")
                        {
                            System.Reflection.MethodInfo method = thistype.GetMethod("Add");

                            string[] a = TakeString(data[2], '{', '}');

                            for (int i = 0; i < a.Length; i++)
                            {
                                string[] b = TakeString(a[i], '{', '}');
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
                        break;
                    }
                case "byte":
                    {
                        int found = thing.IndexOf(':');
                        string a = thing.Substring(found + 1);
                        get = (byte)Convert.ToInt32(a);
                        break;
                    }
                case "sbyte":
                    {
                        int found = thing.IndexOf(':');
                        string a = thing.Substring(found + 1);
                        get = Convert.ToSByte(a);
                        break;
                    }
                case "short":
                    {
                        int found = thing.IndexOf(':');
                        string a = thing.Substring(found + 1);
                        get = Convert.ToInt16(a);
                        break;
                    }
                case "int":
                    {
                        int found = thing.IndexOf(':');
                        string a = thing.Substring(found + 1);
                        get = Convert.ToInt32(a);
                        break;
                    }
                case "long":
                    {
                        int found = thing.IndexOf(':');
                        string a = thing.Substring(found + 1);
                        get = Convert.ToInt64(a);
                        break;
                    }
                case "ushort":
                    {
                        int found = thing.IndexOf(':');
                        string a = thing.Substring(found + 1);
                        get = Convert.ToUInt16(a);
                        break;
                    }
                case "uint":
                    {
                        int found = thing.IndexOf(':');
                        string a = thing.Substring(found + 1);
                        get = Convert.ToUInt32(a);
                        break;
                    }
                case "ulong":
                    {
                        int found = thing.IndexOf(':');
                        string a = thing.Substring(found + 1);
                        get = Convert.ToUInt64(a);
                        break;
                    }
                case "float":
                    {
                        int found = thing.IndexOf(':');
                        string a = thing.Substring(found + 1);
                        get = Convert.ToSingle(a);
                        break;
                    }
                case "double":
                    {
                        int found = thing.IndexOf(':');
                        string a = thing.Substring(found + 1);
                        get = Convert.ToDouble(a);
                        break;
                    }
                case "decimal":
                    {
                        int found = thing.IndexOf(':');
                        string a = thing.Substring(found + 1);
                        get = Convert.ToDecimal(a);
                        break;
                    }
                case "char":
                    {
                        int found = thing.IndexOf(':');
                        string a = TakeString(thing.Substring(found + 1), '\'', '\'')[0];
                        get = Convert.ToChar(a);
                        break;
                    }
                case "string":
                    {
                        int found = thing.IndexOf(':');
                        string a = TakeString(thing.Substring(found + 1), '\"', '\"')[0];
                        get = Convert.ToString(a);
                        break;
                    }
                case "bool":
                    {
                        int found = thing.IndexOf(':');
                        string a = thing.Substring(found + 1).Replace(" ", "");
                        get = Convert.ToBoolean(a);
                        break;
                    }
                case "null":
                    {
                        get = null;
                        break;
                    }
                default:
                    {
                        get = typ;
                        break;
                    }
            }
            return get;
        }

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
            str = RemoveString(str, new string[] { " ", "\n", "\r", "\t" });
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