using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityNetwork;

namespace UnityNetwork.Server
{
    public class PeerUDPBase
    {
        private IPEndPoint _socket;
        private NetUDPServer a;
        public string Key { get; private set; } = "";
        private int cantlink = 0;
        public readonly object Lock = new object();

        List<string> SendKey = new List<string>();
        Dictionary<string, NetBitStream> Sendthing = new Dictionary<string, NetBitStream>();

        public IPEndPoint socket
        {
            get { return _socket; }
        }

        public PeerUDPBase(IPEndPoint peer, NetUDPServer _server)
        {
            _socket = peer;
            a = _server;
        }

        public virtual void OnOperationRequest(Response response)
        {

        }

        public virtual void OnDisconnect()
        {

        }

        public void Close()
        {
            _socket = null;
        }

        public void OffLine()
        {
            if (a != null && _socket != null)
            {
                a.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "伺服器端主動斷線", _socket);
                NetBitStream stream2 = new NetBitStream();
                Response b = new Response();
                b.DebugMessage = "伺服器端主動斷線";
                stream2.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                stream2.WriteResponse2(b, "");
                stream2.EncodeHeader();
                a.Send(stream2, _socket);
            }
        }

        public void ErrorOffLine(string Message)
        {
            if (a != null && _socket != null)
            {
                a.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, Message, _socket);
                try
                {
                    NetBitStream stream2 = new NetBitStream();
                    Response b = new Response();
                    b.DebugMessage = "伺服器端主動斷線";
                    stream2.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                    stream2.WriteResponse2(b, "");
                    stream2.EncodeHeader();
                    a.Send(stream2, _socket);
                }
                catch(Exception)
                {

                }
            }
        }

        string SetSendKey()
        {
            string a;
            lock (SendKey)
            {
                for (a = Guid.NewGuid().ToString(); SendKey.Contains(a); a = Guid.NewGuid().ToString()) { }
                SendKey.Add(a);
            }
            return a;
        }

        public void Reply(byte Code, Dictionary<byte, Object> Parameter, short ReturnCode, string DebugMessage, bool _Lock = true)
        {
            string sendkey = SetSendKey();
            ThreadPool.QueueUserWorkItem((aa) =>
            {
                try
                {
                    if (Key != "" && _socket != null)
                    {
                        try
                        {
                            NetBitStream stream = new NetBitStream();
                            Response b = new Response(Code, Parameter, ReturnCode, DebugMessage);
                            stream.BeginWrite((ushort)MessageIdentifiers.ID.ID_CHAT);
                            stream.WriteResponse2(b, Key, _Lock);
                            stream.EncodeHeader();
                            lock (SendKey)
                            {
                                Sendthing.Add(sendkey, stream);
                                while (SendKey.Count != 0)
                                {
                                    if (Sendthing.ContainsKey(SendKey[0]))
                                    {
                                        a.Send(Sendthing[SendKey[0]], _socket);
                                        Sendthing.Remove(SendKey[0]);
                                        SendKey.RemoveAt(0);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                            cantlink = 0;
                        }
                        catch (Exception e)
                        {
                            if (a != null && _socket != null)
                            {
                                cantlink++;
                                a.CatchMessage(_socket.ToString() + " " + e.Message + " from Reply cantlink:" + cantlink);
                                if (cantlink > 50)
                                {
                                    a.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message, _socket);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (a != null)
                    {
                        a.CatchMessage(e.Message + " from Reply");
                    }
                }
            });
        }

        public void Tell(byte Code, Dictionary<byte, Object> Parameter, bool _Lock = true)
        {
            string sendkey = SetSendKey();
            ThreadPool.QueueUserWorkItem((aa) =>
            {
                try
                {
                    if (Key != "" && _socket != null)
                    {
                        try
                        {

                            NetBitStream stream = new NetBitStream();
                            Response b = new Response(Code, Parameter);
                            stream.BeginWrite((ushort)MessageIdentifiers.ID.ID_CHAT2);
                            stream.WriteResponse2(b, Key, _Lock);
                            stream.EncodeHeader();
                            lock (SendKey)
                            {
                                Sendthing.Add(sendkey, stream);
                                while (SendKey.Count != 0)
                                {
                                    if (Sendthing.ContainsKey(SendKey[0]))
                                    {
                                        a.Send(Sendthing[SendKey[0]], _socket);
                                        Sendthing.Remove(SendKey[0]);
                                        SendKey.RemoveAt(0);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                            cantlink = 0;
                        }
                        catch (Exception e)
                        {
                            if (a != null && _socket != null)
                            {
                                cantlink++;
                                a.CatchMessage(_socket.ToString() + " " + e.ToString() + " from Tell cantlink:" + cantlink);
                                if (cantlink > 50)
                                {
                                    a.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message, _socket);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (a != null)
                    {
                        a.CatchMessage(e.Message + " from Reply");
                    }
                }
            });
        }

        public void P2PTell(Response response, bool _Lock = true)
        {
            string sendkey = SetSendKey();
            ThreadPool.QueueUserWorkItem((aa) =>
            {
                try
                {
                    if (Key != "" && _socket != null)
                    {
                        try
                        {

                            NetBitStream stream = new NetBitStream();
                            stream.BeginWrite((ushort)MessageIdentifiers.ID.P2P_SERVER_CALL);
                            stream.WriteResponse2(response, Key, _Lock);
                            stream.EncodeHeader();
                            lock (SendKey)
                            {
                                Sendthing.Add(sendkey, stream);
                                while (SendKey.Count != 0)
                                {
                                    if (Sendthing.ContainsKey(SendKey[0]))
                                    {
                                        a.Send(Sendthing[SendKey[0]], _socket);
                                        Sendthing.Remove(SendKey[0]);
                                        SendKey.RemoveAt(0);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                            cantlink = 0;
                        }
                        catch (Exception e)
                        {
                            if (a != null && _socket != null)
                            {
                                cantlink++;
                                a.CatchMessage(_socket.ToString() + " " + e.ToString() + " from Tell cantlink:" + cantlink);
                                if (cantlink > 50)
                                {
                                    a.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message, _socket);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (a != null)
                    {
                        a.CatchMessage(e.Message + " from Reply");
                    }
                }
            });
        }

        public void NotImportReply(byte Code, Dictionary<byte, Object> Parameter, short ReturnCode, string DebugMessage, bool _Lock = true)
        {
            ThreadPool.QueueUserWorkItem((aa) =>
            {
                try
                {
                    if (Key != "" && _socket != null)
                    {
                        try
                        {
                            NetBitStream stream = new NetBitStream();
                            Response b = new Response(Code, Parameter, ReturnCode, DebugMessage);
                            stream.BeginWrite((ushort)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT);
                            stream.WriteResponse2(b, Key, _Lock);
                            stream.EncodeHeader();
                            a.Send(stream, _socket);
                            cantlink = 0;
                        }
                        catch (Exception e)
                        {
                            if (a != null && _socket != null)
                            {
                                cantlink++;
                                a.CatchMessage(_socket.ToString() + " " + e.Message + " from Reply cantlink:" + cantlink);
                                if (cantlink > 50)
                                {
                                    a.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message, _socket);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (a != null)
                    {
                        a.CatchMessage(e.Message + " from Reply");
                    }
                }
            });
        }

        public void NotImportTell(byte Code, Dictionary<byte, Object> Parameter, bool _Lock = true)
        {
            ThreadPool.QueueUserWorkItem((aa) =>
            {
                try
                {
                    if (Key != "" && _socket != null)
                    {
                        try
                        {

                            NetBitStream stream = new NetBitStream();
                            Response b = new Response(Code, Parameter);
                            stream.BeginWrite((ushort)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT2);
                            stream.WriteResponse2(b, Key, _Lock);
                            stream.EncodeHeader();
                            a.Send(stream, _socket);
                            cantlink = 0;
                        }
                        catch (Exception e)
                        {
                            if (a != null && _socket != null)
                            {
                                cantlink++;
                                a.CatchMessage(_socket.ToString() + " " + e.ToString() + " from Tell cantlink:" + cantlink);
                                if (cantlink > 50)
                                {
                                    a.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message, _socket);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (a != null)
                    {
                        a.CatchMessage(e.Message + " from Reply");
                    }
                }

            });
        }

        public void SendMessageNotImportReply(byte Code, Dictionary<byte, Object> Parameter, short ReturnCode, string DebugMessage, bool _Lock = true)
        {
            try
            {
                if (Key != "" && _socket != null)
                {
                    try
                    {
                        Response b = new Response(Code, Parameter, ReturnCode, DebugMessage);
                        MemoryStream stream = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter(stream);

                        byte[] vs = b.Serialization();

                        writer.Write(_Lock);
                        writer.Write(_socket.ToString());
                        writer.Write(Key);
                        writer.Write((ushort)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT);
                        writer.Write(vs.Length);
                        writer.Write(vs);

                        writer.Close();
                        stream.Close();

                        MessageTell.SendMessage(stream.ToArray(), "myqueue");

                        writer.Dispose();
                        stream.Dispose();

                        cantlink = 0;
                    }
                    catch (Exception e)
                    {
                        if (a != null && _socket != null)
                        {
                            cantlink++;
                            a.CatchMessage(_socket.ToString() + " " + e.Message + " from Reply cantlink:" + cantlink);
                            if (cantlink > 50)
                            {
                                a.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message, _socket);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (a != null)
                {
                    a.CatchMessage(e.Message + " from Reply");
                }
            }
        }

        public void SendMessageNotImportTell(byte Code, Dictionary<byte, Object> Parameter, bool _Lock = true)
        {
            try
            {
                if (Key != "" && _socket != null)
                {
                    try
                    {
                        Response b = new Response(Code, Parameter);
                        MemoryStream stream = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter(stream);

                        byte[] vs = b.Serialization();

                        writer.Write(_Lock);
                        writer.Write(_socket.ToString());
                        writer.Write(Key);
                        writer.Write((ushort)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT2);
                        writer.Write(vs.Length);
                        writer.Write(vs);

                        writer.Close();
                        stream.Close();

                        MessageTell.SendMessage(stream.ToArray(), "myqueue");

                        writer.Dispose();
                        stream.Dispose();
                        cantlink = 0;
                    }
                    catch (Exception e)
                    {
                        if (a != null && _socket != null)
                        {
                            cantlink++;
                            a.CatchMessage(_socket.ToString() + " " + e.ToString() + " from Tell cantlink:" + cantlink);
                            if (cantlink > 50)
                            {
                                a.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message, _socket);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (a != null)
                {
                    a.CatchMessage(e.Message + " from Reply");
                }
            }
        }

        public void GetKey(string key)
        {
            if (Key == "")
            {
                Key = key;
            }
        }

        public void check()
        {
            try
            {
                List<int> port = new List<int>();
                if (File.Exists(Environment.CurrentDirectory + "\\Port.txt"))
                {
                    string[] vs = File.ReadAllLines(Environment.CurrentDirectory + "\\Port.txt");
                    for (int i = 0; i < vs.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(vs[i]))
                        {
                            port.Add(Convert.ToInt32(vs[i]));
                        }
                    }
                }
                NetBitStream stream = new NetBitStream();
                Response response = new Response(0, new Dictionary<byte, object>() { { 0, port.ToArray()} });
                stream.BeginWrite((ushort)MessageIdentifiers.ID.CHECKING);
                stream.WriteResponse2(response, "");
                stream.EncodeHeader();
                a.Send(stream, _socket);
                cantlink = 0;
            }
            catch (Exception e)
            {
                if (a != null)
                {
                    cantlink++;
                    a.CatchMessage(_socket.ToString() + " " + e.Message + " cantlink:" + cantlink);
                    if (cantlink > 50 && _socket != null)
                    {
                        a.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message, _socket);
                    }
                }
            }
        }
    }
}