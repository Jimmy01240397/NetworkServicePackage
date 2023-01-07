using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Diagnostics;

namespace JimmikerNetwork.Client
{
    public class ClientLinker
    {
        Thread NetThread;
        bool run = false;

        INetClient client;
        ClientListen listener;

        Dictionary<string, (Action<SendData>, Stopwatch, int)> AskCallbacks;

        public LinkCobe linkstate { get; private set; } = LinkCobe.None;

        public ProtocolType type { get; private set; }

        public bool EnableP2P
        {
            get
            {
                return client.EnableP2P;
            }
            set
            {
                client.EnableP2P = value;
            }
        }

        public string Key
        {
            get
            {
                return client.AESkey;
            }
        }

        public List<PeerForP2PBase> P2PSocketList
        {
            get
            {
                return client.P2PSocketList;
            }
        }
        public Dictionary<EndPoint, PeerForP2PBase> P2PToPeer
        {
            get
            {
                return client.P2PToPeer;
            }
        }

        public ClientLinker(ClientListen listen, ProtocolType protocolType)
        {
            listener = listen;
            type = protocolType;
            AskCallbacks = new Dictionary<string, (Action<SendData>, Stopwatch, int)>();
            switch (type)
            {
                case ProtocolType.Tcp:
                    {
                        client = new NetClientTCP();
                        break;
                    }
                case ProtocolType.Udp:
                    {
                        client = new NetClientUDP();
                        break;
                    }
            }
        }

        public ClientLinker(ClientListen listen, ProtocolType protocolType, bool enableP2P)
        {
            listener = listen;
            type = protocolType;
            switch (type)
            {
                case ProtocolType.Tcp:
                    {
                        client = new NetClientTCP(enableP2P);
                        break; 
                    }
                case ProtocolType.Udp:
                    {
                        client = new NetClientUDP(enableP2P);
                        break;
                    }
            }
        }

        public bool Connect(string serverhost, int port)
        {
            client.GetMessage += OnGetMessage;
            bool b = client.Connect(serverhost, port);
            return b;
        }

        /// <summary>
        /// Start P2P Connect
        /// </summary>
        /// <param name="IPPort">Connect Target</param>
        /// <param name="callback">Connect Callback(Connect IP, Connect Public IP, Connect Peer, Successful)</param>
        public void StartP2PConnect(IPEndPoint IPPort, Action<EndPoint, EndPoint, PeerForP2PBase, bool> callback)
        {
            client.StartP2PConnect(IPPort, callback);
        }

        /// <summary>
        /// Wait P2P Connect
        /// </summary>
        /// <param name="IPPort">Connect Target</param>
        /// <param name="callback">Connect Callback(Connect IP, Connect Public IP, Connect Peer, Successful)</param>
        public void WaitP2PConnect(IPEndPoint IPPort, Action<EndPoint, EndPoint, PeerForP2PBase, bool> callback)
        {
            client.WaitP2PConnect(IPPort, callback);
        }

        public void RunUpdateThread()
        {
            if (!run)
            {
                run = true;
                NetThread = new Thread(() =>
                {
                    while (run)
                    {
                        Update();
                        Thread.Sleep(10);
                    }
                });
                NetThread.Start();
            }
        }

        public void StopUpdateThread()
        {
            run = false;
            NetThread = null;
        }

        private void OnGetMessage(string i)
        {
            if (listener != null)
            {
                listener.DebugReturn(i);
            }
        }

        public void Disconnect()
        {
            if (client != null)
            {
                for (int i = 0; i < client.P2PSocketList.Count; i++)
                {
                    client.P2PSocketList[i].Close();
                }
                Thread a = new Thread(new ThreadStart(exit));
                a.Start();
            }
        }

        private void exit()
        {
            SpinWait.SpinUntil(() => client.P2PSocketList.Count == 0);

            if (type == ProtocolType.Udp)
            {
                using (Packet packet = new Packet(client.RemoteEndPoint))
                {
                    packet.BeginWrite(PacketType.CONNECTION_LOST);
                    client.Send(packet);
                }
            }
            client.PushPacket(PacketType.CONNECTION_LOST, "主動斷線");
        }

        public virtual void Ask(byte Code, object Parameter, Action<SendData> callback, int callbacktimeout, bool Lock = true)
        {
            if (linkstate == LinkCobe.Connect)
            {
                using (Packet packet = new Packet(client.RemoteEndPoint))
                {
                    string ID = null;
                    if(callback != null)
                    {
                        ID = Guid.NewGuid().ToString().Replace("-", "").ToLower();
                        Stopwatch stopwatch = new Stopwatch();
                        if(callbacktimeout > 0) stopwatch.Start();
                        lock (AskCallbacks)
                        {
                            AskCallbacks.Add(ID, (callback, stopwatch, callbacktimeout));
                        }
                    }

                    packet.BeginWrite(PacketType.Request);
                    packet.WriteSendData(new SendData(ID, Code, Parameter), Key, Lock ? EncryptAndCompress.LockType.AES : EncryptAndCompress.LockType.None);
                    client.Send(packet);
                }
            }
        }

        public virtual void Ask(byte Code, object Parameter, bool Lock = true)
        {
            Ask(Code, Parameter, null, 0, Lock);
        }

        public virtual void SendDebugMessageToServer(string message)
        {
            if (linkstate == LinkCobe.Connect)
            {
                using (Packet packet = new Packet(client.RemoteEndPoint))
                {
                    packet.BeginWrite(PacketType.ClientDebugMessage);
                    packet.WriteSendData(new SendData(0, null, 0, message), Key, EncryptAndCompress.LockType.AES);
                    client.Send(packet);
                }
            }
        }

        public virtual void Update()
        {
            if (client != null)
            {
                for (Packet packet = client.GetPacket(); packet != null; packet = client.GetPacket())
                {
                    PacketType packetType = packet.BeginRead();
                    EndPoint remote = null;
                    switch (type)
                    {
                        case ProtocolType.Tcp:
                            {
                                if (((Socket)packet.peer).Connected)
                                {
                                    remote = ((Socket)packet.peer).RemoteEndPoint;
                                }
                                else
                                {
                                    remote = new IPEndPoint(IPAddress.Any, 0);
                                }
                                break;
                            }
                        case ProtocolType.Udp:
                            {
                                remote = (EndPoint)packet.peer;
                                break;
                            }
                    }
                    if (listener != null)
                    {
                        switch (packetType)
                        {
                            case PacketType.CONNECT_SUCCESSFUL:
                                {
                                    client.ConnectSuccessful(packet);
                                    linkstate = LinkCobe.Connect;
                                    listener.OnStatusChanged(linkstate);
                                    break;
                                }
                            case PacketType.CONNECTION_LOST:
                                {
                                    client.Disconnect();
                                    listener.DebugReturn(packet.state.ToString());
                                    linkstate = LinkCobe.Lost;
                                    listener.OnStatusChanged(linkstate);
                                    break;
                                }
                            case PacketType.CONNECTION_ATTEMPT_FAILED:
                                {
                                    client.Disconnect();
                                    listener.DebugReturn(packet.state.ToString());
                                    linkstate = LinkCobe.Failed;
                                    listener.OnStatusChanged(linkstate);
                                    break;
                                }
                            case PacketType.ServerTell:
                                {
                                    if (!string.IsNullOrEmpty(Key))
                                    {
                                        SendData sendData = packet.ReadSendData(Key);
                                        listener.OnEvent(sendData);
                                    }
                                    break;
                                }
                            case PacketType.Response:
                                {
                                    if (!string.IsNullOrEmpty(Key))
                                    {
                                        SendData sendData = packet.ReadSendData(Key);
                                        lock (AskCallbacks)
                                        {
                                            if (!string.IsNullOrEmpty(sendData.ID) && AskCallbacks.ContainsKey(sendData.ID.ToLower()))
                                            {
                                                var callback = AskCallbacks[sendData.ID.ToLower()];
                                                callback.Item2.Stop();
                                                AskCallbacks.Remove(sendData.ID.ToLower());
                                                callback.Item1?.Invoke(sendData);
                                            }
                                        }
                                        listener.OnOperationResponse(sendData);
                                    }
                                    break;
                                }
                            case PacketType.P2P_CONNECT_SUCCESSFUL:
                                {
                                    if (!P2PToPeer.ContainsKey(remote))
                                    {
                                        listener.DebugReturn("P2PConnectSuccess: " + remote.ToString());
                                        client.P2PConnectSuccessful(listener.P2PAddPeer, packet);
                                    }
                                    break;
                                }
                            case PacketType.P2P_CONNECTION_LOST:
                                {
                                    if (P2PToPeer.ContainsKey(remote))
                                    {
                                        PeerForP2PBase peer = P2PToPeer[remote];
                                        listener.DebugReturn("P2PConnectLost:" + remote.ToString() + ", error: " + packet.state);
                                        peer.OnDisconnect();
                                        client.P2PDisconnect(packet.peer);
                                        peer = null;
                                    }
                                    break;
                                }
                            case PacketType.P2P_Tell:
                                {
                                    if (P2PToPeer.ContainsKey(remote))
                                    {
                                        PeerForP2PBase peer = P2PToPeer[remote];
                                        SendData sendData = packet.ReadSendData(client.P2PSocketToKey[remote]);
                                        peer.OnGetData(sendData);
                                    }
                                    break;
                                }
                        }
                    }
                    packet.CloseStream();
                }
                lock (AskCallbacks)
                {
                    List<KeyValuePair<string, (Action<SendData>, Stopwatch, int)>> callbacklist = new List<KeyValuePair<string, (Action<SendData>, Stopwatch, int)>>(AskCallbacks);
                    foreach (var askcallback in callbacklist)
                    {
                        if (askcallback.Value.Item3 > 0 && askcallback.Value.Item2.ElapsedMilliseconds > askcallback.Value.Item3)
                        {
                            askcallback.Value.Item2.Stop();
                            AskCallbacks.Remove(askcallback.Key);
                        }
                    }
                }
            }
        }
    }
}