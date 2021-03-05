#if EnableMessageTell

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace UnityNetwork.Server
{
    public class UDPServerInvokeSend
    {
        public delegate void GetError(string message);
        public event GetError OGetError;
        public UdpClient udpClient;

        int port = 0;

        public UDPServerInvokeSend(int Port)
        {
            port = Port;
            udpClient = new UdpClient(Port);
            File.AppendAllLines("Port.txt", new string[] { Port.ToString() });
            udpClient.BeginReceive(new AsyncCallback(Receive), udpClient);
        }

        ~UDPServerInvokeSend()
        {
            List<string> list = new List<string>(File.ReadAllLines("Port.txt"));
            if (list.Contains(port.ToString()))
            {
                list.Remove(port.ToString());
                File.WriteAllLines("Port.txt", list.ToArray());
            }
        }

        public void Close()
        {
            udpClient.Close();
            udpClient = null;
            while (true)
            {
                if (File.Exists("Port.txt"))
                {
                    try
                    {
                        List<string> list = new List<string>(File.ReadAllLines("Port.txt"));
                        if (list.Contains(port.ToString()))
                        {
                            list.Remove(port.ToString());
                            File.WriteAllLines("Port.txt", list.ToArray());
                        }
                        break;
                    }
                    catch (Exception)
                    {

                    }
                }
                else
                {
                    break;
                }
            }
        }

        void Receive(System.IAsyncResult ar)
        {
            UdpClient uc = (UdpClient)ar.AsyncState;

            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                byte[] bytes = uc.EndReceive(ar, ref iPEndPoint);

                uc.BeginReceive(new AsyncCallback(Receive), uc);
            }
            catch (System.Exception)
            {
            }
        }

        public void Update()
        {
            try
            {
                if (MessageTell.CanRead("myqueue"))
                {
                    byte[] vs = MessageTell.GetMessage("myqueue");
                    if (vs != null)
                    {
                        if (vs.Length > 0)
                        {
                            ThreadPool.QueueUserWorkItem((aa) =>
                            {
                                MemoryStream stream2 = new MemoryStream(vs);
                                BinaryReader reader = new BinaryReader(stream2);

                                bool _Lock = reader.ReadBoolean();
                                string IP = reader.ReadString();
                                string Key = reader.ReadString();
                                ushort ID = reader.ReadUInt16();
                                byte[] byte2 = reader.ReadBytes(reader.ReadInt32());
                                reader.Close();
                                stream2.Close();
                                reader.Dispose();
                                stream2.Dispose();
                                if (Key != "")
                                {
                                    try
                                    {

                                        NetBitStream stream = new NetBitStream();
                                        stream.BeginWrite(ID);
                                        stream.WriteResponseByte(byte2, Key, _Lock);
                                        stream.EncodeHeader();

                                        udpClient.Send(stream.BYTES, stream.Length, IP.Split(':')[0], Convert.ToInt32(IP.Split(':')[1]));

                                        /*stream2 = new MemoryStream();
                                        BinaryWriter writer = new BinaryWriter(stream2);

                                        writer.Write(IP);
                                        writer.Write(stream.Length);
                                        writer.Write(stream.BYTES);

                                        writer.Close();
                                        stream2.Close();

                                        MessageTell.SendMessage(stream2.ToArray(), "myqueue3");

                                        writer.Dispose();
                                        stream2.Dispose();*/
                                    }
                                    catch (Exception e)
                                    {
                                        OGetError?.Invoke(e.ToString());
                                    }
                                }
                            });
                        }
                    }
                }
            }
            catch(Exception e)
            {
                OGetError?.Invoke(e.ToString());
            }
        }
    }
}

#endif