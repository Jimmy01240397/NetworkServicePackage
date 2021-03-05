using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace UnityNetwork.Server
{
    public class PeerTCPBase
    {
        protected TcpClient _socket;
        public NetTCPServer a;
        public string Key { get; private set; } = "";
        private int cantlink = 0;

        List<string> SendKey = new List<string>();
        Dictionary<string, NetBitStream> Sendthing = new Dictionary<string, NetBitStream>();

        public TcpClient socket
        {
            get { return _socket; }
        }

        public PeerTCPBase(TcpClient peer, NetTCPServer _server)
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
            _socket.Close();
            _socket = null;
        }

        public void OffLine()
        {
            if (a != null && _socket!=null)
            {
                if (_socket.Connected)
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
                                a.CatchMessage(_socket.Client.RemoteEndPoint.ToString() + " " + e.Message + " from Reply cantlink:" + cantlink);
                                if (cantlink > 50)
                                {
                                    a.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message, _socket);
                                }
                            }
                        }
                    }
                }
                catch(Exception e)
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
                                a.CatchMessage(_socket.Client.RemoteEndPoint.ToString() + " " + e.ToString() + " from Tell cantlink:" + cantlink);
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
                        a.CatchMessage(e.Message + " from Tell");
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
                                a.CatchMessage(_socket.Client.RemoteEndPoint.ToString() + " " + e.Message + " from Reply cantlink:" + cantlink);
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
                                a.CatchMessage(_socket.Client.RemoteEndPoint.ToString() + " " + e.ToString() + " from Tell cantlink:" + cantlink);
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
                        a.CatchMessage(e.Message + " from Tell");
                    }
                }
            });
        }

        public void GetKey(string key)
        {
            if(Key == "")
            {
                Key = key;
            }
        }

        public void check()
        {
            try
            {
                NetBitStream stream = new NetBitStream();
                stream.BeginWrite((ushort)MessageIdentifiers.ID.CHECKING);
                stream.EncodeHeader();
                a.Send(stream, _socket);
                cantlink = 0;
            }
            catch (Exception e)
            {
                if (a != null)
                {
                    cantlink++;
                    a.CatchMessage(_socket.Client.RemoteEndPoint.ToString() + " " + e.Message + " from check cantlink:" + cantlink);
                    if (cantlink > 50)
                    {
                        a.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message, _socket);
                    }
                }
            }
        }
    }
}
