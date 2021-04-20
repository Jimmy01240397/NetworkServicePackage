using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace UnityNetwork.Client
{
    public class PeerForP2P
    {
        private IPEndPoint _socket;

        private NetUDPClient client;
        public string Key { get; private set; } = "";
        private int cantlink = 0;

        public readonly object Lock = new object();

        public bool NATPass { get; private set; } = false;

        List<string> SendKey = new List<string>();
        Dictionary<string, NetBitStream> Sendthing = new Dictionary<string, NetBitStream>();

        public IPEndPoint socket
        {
            get { return _socket; }
        }

        public IPEndPoint PublicIP { get; private set; }

        public PeerForP2P(IPEndPoint peer, IPEndPoint publicIP, NetUDPClient client, bool NATPass)
        {
            _socket = peer;
            PublicIP = publicIP;
            this.client = client;
            this.NATPass = NATPass;
        }

        public virtual void OnOperationRequest(Response response)
        {

        }

        public virtual void OnDisconnect()
        {

        }

        public virtual void OnLink()
        {

        }

        public void Close()
        {
            _socket = null;
            PublicIP = null;
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

        public void OffLine()
        {
            try
            {
                if (client != null && _socket != null)
                {
                    client.P2PPushPacket((ushort)MessageIdentifiers.ID.P2P_LOST, "伺服器端主動斷線", _socket);
                    NetBitStream stream2 = new NetBitStream();
                    Response b = new Response();
                    b.DebugMessage = "伺服器端主動斷線";
                    stream2.BeginWrite((ushort)MessageIdentifiers.ID.P2P_LOST);
                    stream2.WriteResponse2(b, "");
                    stream2.EncodeHeader();
                    client.Send(stream2, _socket);
                }
            }
            catch(Exception e)
            {
                client?.OnGetMessage(e.ToString());
            }
        }

        public void P2PTell(byte Code, Dictionary<byte, Object> Parameter, bool _Lock = true)
        {
            string sendkey = SetSendKey();
            ThreadPool.QueueUserWorkItem((aa) =>
            {
                if (/*Key != "" && */_socket != null)
                {
                    try
                    {
                        NetBitStream stream = new NetBitStream();
                        Response b = new Response(Code, Parameter);
                        stream.BeginWrite((ushort)MessageIdentifiers.ID.P2P_ID_CHAT);
                        stream.WriteResponse2(b, Key, false);
                        stream.EncodeHeader();
                        lock (SendKey)
                        {
                            Sendthing.Add(sendkey, stream);
                            while (SendKey.Count != 0)
                            {
                                if (Sendthing.ContainsKey(SendKey[0]))
                                {
                                    if (NATPass)
                                    {
                                        client.Send(Sendthing[SendKey[0]], _socket);
                                    }
                                    else
                                    {
                                        Response response = new Response((byte)ClientLinkerUDP.P2PCode.NATP2PTell, new Dictionary<byte, object>() { { 0, _socket.ToString() }, { 1, Sendthing[SendKey[0]].BYTES } });
                                        client.P2PConnectServer(response);
                                    }
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
                        client.OnGetMessage(e.ToString());
                        if (client != null)
                        {
                            cantlink++;
                            if (cantlink > 50)
                            {
                                client.P2PPushPacket((ushort)MessageIdentifiers.ID.P2P_LOST, e.Message, _socket);
                            }
                        }
                    }
                }
                else
                {
                    SendKey.Remove(sendkey);
                }
            });
        }

        public void check()
        {
            try
            {
                NetBitStream stream = new NetBitStream();
                stream.BeginWrite((ushort)MessageIdentifiers.ID.P2P_CHECKING);
                stream.EncodeHeader();
                if (NATPass)
                {
                    client.Send(stream, _socket);
                }
                else
                {
                    Response response = new Response((byte)ClientLinkerUDP.P2PCode.NATP2PTell, new Dictionary<byte, object>() { { 0, _socket.ToString()}, { 1, stream.BYTES } });
                    client.P2PConnectServer(response);
                }
                cantlink = 0;
            }
            catch (Exception e)
            {
                if (client != null)
                {
                    cantlink++;
                    client.OnGetMessage(_socket.ToString() + " " + e.Message + " cantlink:" + cantlink);
                    if (cantlink > 50 && _socket != null)
                    {
                        client.P2PPushPacket((ushort)MessageIdentifiers.ID.P2P_LOST, e.Message, _socket);
                    }
                }
            }
        }
    }
}