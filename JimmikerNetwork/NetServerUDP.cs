using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using JimmikerNetwork.Server;

namespace JimmikerNetwork
{
    class NetServerUDP : INetServer
    {
        public int MaxConnections { get; set; } = -1;

        public ProtocolType type { get; private set; } = ProtocolType.Udp;

        public SerializationData.RSAKeyPair RSAkey { get; private set; }

        public List<PeerBase> SocketList { get; private set; }
        public List<Packet> Packets { get; private set; }
        public Dictionary<EndPoint, PeerBase> ToPeer { get; private set; }
        public Dictionary<object, string> SocketToKey { get; private set; }

        Dictionary<EndPoint, bool> clientcheck;
        object checklock = new object();

        Dictionary<EndPoint, List<Packet>> OnListenClient;

        Socket listener;

        object SendLock = new object();

        object CloseLock = new object();

        byte[] m_Buffer = new byte[65536];

        bool run = false;

        public event Action<string> GetMessage;

        const uint IOC_IN = 0x80000000;
        const uint IOC_VENDOR = 0x18000000;
        const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

        private EndPoint listenerIP = null;
        public EndPoint ListenerIP
        {
            get
            {
                if (listenerIP == null)
                {
                    listenerIP = listener.LocalEndPoint;
                }
                if(listener == null)
                {
                    return listenerIP;
                }
                try
                {
                    if (listener.LocalEndPoint == null)
                    {
                        return listenerIP;
                    }
                    if (!listenerIP.Equals(listener.LocalEndPoint))
                    {
                        listenerIP = listener.LocalEndPoint;
                    }
                }
                catch (Exception) { }
                return listenerIP;
            }
        }

        public NetServerUDP()
        {
            //_maxConnections = maxConnections;

            SocketList = new List<PeerBase>();
            Packets = new List<Packet>();
            ToPeer = new Dictionary<EndPoint, PeerBase>();
            SocketToKey = new Dictionary<object, string>();
            OnListenClient = new Dictionary<EndPoint, List<Packet>>();
            clientcheck = new Dictionary<EndPoint, bool>();
        }

        public bool CreateServer(IPAddress ip, int listenPort, out string a)
        {
            IPEndPoint ipe = new IPEndPoint(ip, listenPort);
            try
            {
                RSAkey = SerializationData.GenerateRSAKeys(2048);
                listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                listener.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                if ((byte)Environment.OSVersion.Platform >= 0 && (byte)Environment.OSVersion.Platform <= 3)
                {
                    listener.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                }
                listener.Bind(ipe);
                EndPoint remoteEP = new IPEndPoint(IPAddress.IPv6Any, 0);
                listener.BeginReceiveFrom(m_Buffer, 0, m_Buffer.Length, SocketFlags.None, ref remoteEP, Receive, listener);
                StartThread();

                a = ListenerIP.ToString() + " 成功建立伺服器";
            }
            catch (SocketException)
            {
                RSAkey = new SerializationData.RSAKeyPair();
                a = ipe.ToString() + "無法建立伺服器";
                return false;
            }
            return true;

        }

        void StartThread()
        {
            run = true;
            new Thread(clientchecker).Start();
        }

        void StopThread()
        {
            run = false;
        }

        private void ListenClient(Packet getpacket)
        {
            IPEndPoint client = (IPEndPoint)getpacket.peer;

            getpacket.CloseStream();

            if (client != null)
            {
                lock (OnListenClient)
                {
                    OnListenClient.Add(client, new List<Packet>());
                }

                #region Set def var
                const int ReadTimeout = 1000;
                #endregion

                #region Set Read func
                SendData onRead(PacketType checkType, string key, Func<SendData, bool> datacheck)
                {
                    SendData ReadData = new SendData();
                    List<Packet> clientdata;
                    lock (OnListenClient)
                    {
                        clientdata = OnListenClient[client];
                    }
                    SpinWait.SpinUntil(() => clientdata.Count > 0, ReadTimeout);
                    if (clientdata.Count > 0)
                    {
                        Packet nowpacket;
                        lock (clientdata)
                        {
                            nowpacket = clientdata[0];
                            clientdata.RemoveAt(0);
                        }

                        using (nowpacket)
                        {
                            if (nowpacket.BeginRead() != checkType)
                            {
                                lock (OnListenClient)
                                {
                                    OnListenClient.Remove(client);
                                }
                                return new SendData();
                            }
                            ReadData = nowpacket.ReadSendData(key);
                        }
                    }
                    else
                    {
                        lock (OnListenClient)
                        {
                            OnListenClient.Remove(client);
                        }
                        return new SendData();
                    }
                    if (!datacheck(ReadData))
                    {
                        lock (OnListenClient)
                        {
                            OnListenClient.Remove(client);
                        }
                        return new SendData();
                    }
                    return ReadData;
                }
                #endregion

                #region Set Send func
                void onSend(PacketType sendType, string key, SerializationData.LockType lockType, SendData send)
                {
                    using (Packet packet = new Packet(client))
                    {
                        packet.BeginWrite(sendType);
                        packet.WriteSendData(send, key, lockType);
                        Send(packet, client);
                    }
                }
                #endregion

                #region Send RSA key
                onSend(PacketType.RSAKEY, "", SerializationData.LockType.None, new SendData(0, RSAkey.PublicKeyBytes));
                #endregion

                #region Get AES key
                SendData sendData = onRead(PacketType.AESKEY, RSAkey.PrivateKey, (a) => true);
                if (sendData == new SendData())
                    return;

                string AESkey = (string)sendData.Parameters;
                #endregion

                #region Send AES Check
                onSend(PacketType.AESKEY, AESkey, SerializationData.LockType.AES, new SendData(0, "Connect check"));
                #endregion

                #region Get CONNECT_SUCCESSFUL
                sendData = onRead(PacketType.CONNECT_SUCCESSFUL, AESkey, (a) => a.Parameters.ToString() == "Connect successful");
                if (sendData == new SendData())
                    return;
                #endregion

                #region On CONNECT
                onSend(PacketType.CONNECT_SUCCESSFUL, AESkey, SerializationData.LockType.AES, new SendData(0, "On Connect"));
                PushPacket(PacketType.CONNECT_SUCCESSFUL, AESkey, client);

                lock (OnListenClient)
                {
                    OnListenClient.Remove(client);
                }
                #endregion
            }
        }


        public void ConnectSuccessful(Func<object, INetServer, PeerBase> AddPeerFunc, Packet packet)
        {
            SocketToKey.Add(packet.peer, (string)packet.state);
            PeerBase peer = AddPeerFunc(packet.peer, this);
            SocketList.Add(peer);
            ToPeer.Add((EndPoint)packet.peer, peer);
            lock (checklock)
            {
                clientcheck.Add((EndPoint)packet.peer, true);
            }
        }


        public void SendCheck(EndPoint peer)
        {
            using (Packet packet = new Packet(peer))
            {
                packet.BeginWrite(PacketType.CHECK);
                Send(packet, packet.peer);
            }
        }

        Stopwatch checkerTime = new Stopwatch();

        void clientchecker()
        {
            checkerTime.Stop();
            checkerTime.Reset();
            checkerTime.Start();
            while (run)
            {
                Thread.Sleep(3000);
                if (!run) break;
                for (int i = 0; i < SocketList.Count; i++)
                {
                    SendCheck((EndPoint)SocketList[i].socket);
                }
                if (checkerTime.ElapsedMilliseconds > 10000)
                {
                    lock (checklock)
                    {
                        for (int i = 0; i < SocketList.Count; i++)
                        {
                            if (clientcheck.ContainsKey((EndPoint)SocketList[i].socket))
                            {
                                if (!clientcheck[(EndPoint)SocketList[i].socket])
                                {
                                    PushPacket(PacketType.CONNECTION_LOST, "10秒未回應", (EndPoint)SocketList[i].socket);
                                }
                            }
                        }
                        for (int i = 0; i < SocketList.Count; i++)
                        {
                            if (clientcheck.ContainsKey((EndPoint)SocketList[i].socket))
                            {
                                clientcheck[(EndPoint)SocketList[i].socket] = false;
                            }
                        }
                    }
                    checkerTime.Stop();
                    checkerTime.Reset();
                    checkerTime.Start();
                }
            }
        }


        private void Receive(System.IAsyncResult ar)
        {
            EndPoint ipe = new IPEndPoint(IPAddress.IPv6Any, 0);

            byte[] data = null;
            PacketType msgid = (PacketType)(-1);
            Packet packet = null;
            try
            {
                int num = listener.EndReceiveFrom(ar, ref ipe);
                if (num >= 4)
                {
                    data = new byte[num];
                    Buffer.BlockCopy(m_Buffer, 0, data, 0, num);
                    if (BitConverter.ToInt32(data, 0) == data.Length - 4)
                    {
                        packet = new Packet(ipe, data, null);
                        msgid = packet.BeginRead();
                        packet.ResetPosition();
                    }
                }
                EndPoint remoteEP = new IPEndPoint(IPAddress.IPv6Any, 0);
                listener.BeginReceiveFrom(m_Buffer, 0, m_Buffer.Length, SocketFlags.None, ref remoteEP, Receive, listener);
                if (packet == null) return;
            }
            catch (SocketException e)
            {
                DebugMessage("Server Close On Receive:" + e.ToString());
                Close();
                return;
            }
            catch (Exception e)
            {
                DebugMessage("Receive:" + e.ToString());
                EndPoint remoteEP = new IPEndPoint(IPAddress.IPv6Any, 0);
                listener.BeginReceiveFrom(m_Buffer, 0, m_Buffer.Length, SocketFlags.None, ref remoteEP, Receive, listener);
                return;
            }
            lock (OnListenClient)
            {
                if (OnListenClient.ContainsKey((EndPoint)packet.peer))
                {
                    OnListenClient[(EndPoint)packet.peer].Add(packet);
                    return;
                }
            }
            if (msgid > PacketType.SendAllowTypeTop && msgid < PacketType.SendAllowTypeEnd && ToPeer.ContainsKey((EndPoint)packet.peer))
            {
                PushPacket(packet);
                lock (checklock)
                {
                    clientcheck[(EndPoint)packet.peer] = true;
                }
            }
            else
            {
                switch (msgid)
                {
                    case PacketType.ON_CONNECT:
                        {
                            ListenClient(packet);
                            break;
                        }
                    case PacketType.CONNECTION_LOST:
                        {
                            if (ToPeer.ContainsKey((EndPoint)packet.peer))
                            {
                                packet.BeginRead();
                                SendData sendData = packet.ReadSendData("");
                                PushPacket(PacketType.CONNECTION_LOST, sendData.DebugMessage, packet.peer);
                            }
                            packet.CloseStream();
                            break;
                        }
                    case PacketType.P2P_SERVER_CALL:
                        {
                            if (ToPeer.ContainsKey((EndPoint)packet.peer))
                            {
                                packet.BeginRead();
                                SendData sendData = packet.ReadSendData(SocketToKey[packet.peer]);

                                string remoteendpoint = (string)((object[])sendData.Parameters)[0];
                                string localendpoint = (string)((object[])sendData.Parameters)[1];
                                object thedata = ((object[])sendData.Parameters)[2];

                                if (remoteendpoint != packet.peer.ToString() && ToPeer.TryGetValue(TraceRoute.IPEndPointParse(remoteendpoint, AddressFamily.InterNetworkV6), out PeerBase peer))
                                {
                                    ((object[])sendData.Parameters)[0] = packet.peer.ToString();
                                    ((object[])sendData.Parameters)[1] = remoteendpoint;
                                    using (Packet sendpacket = new Packet(peer.socket))
                                    {
                                        sendpacket.BeginWrite(PacketType.P2P_SERVER_CALL);
                                        sendpacket.WriteSendData(sendData, SocketToKey[TraceRoute.IPEndPointParse(remoteendpoint, AddressFamily.InterNetworkV6)], SerializationData.LockType.AES);
                                        Send(sendpacket, peer.socket);
                                    }
                                    //DebugMessage(((Client.P2PCode)sendData.Code).ToString() + ":" + packet.peer.ToString() + " to " + remoteendpoint.ToString());
                                }
                                else
                                {
                                    ((object[])sendData.Parameters)[1] = packet.peer.ToString();
                                    using (Packet sendpacket = new Packet(packet.peer))
                                    {
                                        sendpacket.BeginWrite(PacketType.P2P_SERVER_FAILED);
                                        sendpacket.WriteSendData(sendData, SocketToKey[packet.peer], SerializationData.LockType.AES);
                                        Send(sendpacket, packet.peer);
                                    }
                                }
                            }
                            packet.CloseStream();
                            break;
                        }
                    case PacketType.P2P_GET_PUBLIC_ENDPOINT:
                        {
                            using (Packet sendpacket = new Packet(packet.peer))
                            {
                                sendpacket.BeginWrite(PacketType.P2P_GET_PUBLIC_ENDPOINT);
                                sendpacket.WriteSendData(new SendData(0, packet.peer.ToString()), "", SerializationData.LockType.None);
                                Send(sendpacket, packet.peer);
                            }
                            packet.CloseStream();
                            break;
                        }
                    case PacketType.CHECK:
                        {
                            if (ToPeer.ContainsKey((EndPoint)packet.peer))
                            {
                                lock (checklock)
                                {
                                    clientcheck[(EndPoint)packet.peer] = true;
                                }
                            }
                            packet.CloseStream();
                            break;
                        }
                    case PacketType.ClientDebugMessage:
                        {
                            if (ToPeer.ContainsKey((EndPoint)packet.peer))
                            {
                                packet.BeginRead();
                                SendData sendData = packet.ReadSendData(SocketToKey[packet.peer]);
                                DebugMessage("ClientDebugMessage:" + packet.peer.ToString() + " " + sendData.DebugMessage);
                            }

                            packet.CloseStream();
                            break;
                        }
                    default:
                        {
                            DebugMessage(msgid.ToString());
                            //PushPacket(PacketType.CONNECTION_LOST, "不正確的標頭資訊 Receive", packet.peer);
                            packet.CloseStream();
                            break;
                        }
                }
            }
        }

        public void DebugMessage(string message)
        {
            GetMessage?.Invoke(message);
        }

        public void Close()
        {
            lock (SendLock)
            {
                if (listener != null)
                {
                    try
                    {
                        listener.Shutdown(SocketShutdown.Both);
                    }
                    finally
                    {
                        listener.Close();
                        listener = null;
                    }
                }

                StopThread();

                SocketList.Clear();
                Packets.Clear();
                ToPeer.Clear();
                SocketToKey.Clear();
                clientcheck.Clear();
                OnListenClient.Clear();

                GetMessage = null;

                RSAkey = new SerializationData.RSAKeyPair();
            }
        }

        public void Disconnect(object socket)
        {
            Disconnect(socket, -1);
        }

        public void Disconnect(object socket, int timeout)
        {
            if (socket != null)
            {
                if (ToPeer.ContainsKey((EndPoint)socket))
                {
                    if (SocketList.Contains(ToPeer[(EndPoint)socket]))
                    {
                        SocketList.Remove(ToPeer[(EndPoint)socket]);
                    }
                }
                if (SocketToKey.ContainsKey((EndPoint)socket))
                {
                    SocketToKey.Remove((EndPoint)socket);
                }
                if (ToPeer.ContainsKey((EndPoint)socket))
                {
                    ToPeer.Remove((EndPoint)socket);
                }
                lock (checklock)
                {
                    if (clientcheck.ContainsKey((EndPoint)socket))
                    {
                        clientcheck.Remove((EndPoint)socket);
                    }
                }
            }
        }

        public Packet GetPacket()
        {
            Packet packet = null;
            lock (Packets)
            {
                if (Packets.Count != 0)
                {
                    packet = Packets[0];
                    Packets.RemoveAt(0);
                }
            }
            return packet;
        }

        public void PushPacket(PacketType msgid, string exception, object peer)
        {
            Packet packet = new Packet(peer, null, exception);
            packet.BeginWrite(msgid);
            packet.CloseWrite();
            packet.ResetPosition();
            PushPacket(packet);
        }

        public void PushPacket(Packet stream)
        {
            if (stream.peer != null)
            {
                lock (Packets)
                {
                    Packets.Add(stream);
                }
            }
        }

        public bool Send(Packet bts, object peer)
        {
            EndPoint client = (EndPoint)peer;
            if (SendLock != null)
            {
                lock (SendLock)
                {
                    if (client != null)
                    {
                        listener.BeginSendTo(bts.Bytes, 0, bts.Length, SocketFlags.None, client, SendCallback, client);
                        return true;
                    }
                }
            }
            return false;
        }


        private void SendCallback(System.IAsyncResult ar)
        {
            EndPoint client = (EndPoint)ar.AsyncState;
            try
            {
                lock (SendLock)
                {
                    listener.EndSendTo(ar);
                }
            }
            catch (System.Exception e)
            {
                DebugMessage("SendCallback: " + e.ToString());
                //錯誤
            }
        }
    }
}