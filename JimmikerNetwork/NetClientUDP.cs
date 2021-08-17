using JimmikerNetwork.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace JimmikerNetwork
{
    class NetClientUDP : INetClient
    {
        Socket socket;

        public bool EnableP2P { get; set; } = false;

        public SerializationData.RSAKeyPair RSAkey { get; private set; }

        public string AESkey { get; private set; }

        public SerializationData.RSAKeyPair P2PRSAkey { get; private set; }

        public Dictionary<object, string> P2PSocketToKey { get; private set; }

        public List<PeerForP2PBase> P2PSocketList { get; private set; }
        public Dictionary<EndPoint, PeerForP2PBase> P2PToPeer { get; private set; }

        public List<Packet> Packets { get; private set; }

        object SendLock = new object();

        List<IPEndPoint> OnP2PConnect;
        Dictionary<IPEndPoint, Stopwatch> OnP2PWait;
        Dictionary<IPEndPoint, Action<EndPoint, EndPoint, PeerForP2PBase, bool>> OnP2PConnectAction;
        Dictionary<EndPoint, List<Packet>> OnListenP2PClient;
        Dictionary<EndPoint, EndPoint> P2PToRealEndPoint;
        Dictionary<EndPoint, EndPoint> P2PToNewEndPoint;

        Dictionary<EndPoint, bool> P2Pclientcheck;

        //object CloseLock = new object();

        public EndPoint LocalPublicEndPoint { get; private set; }

        private EndPoint localEndPoint;
        public EndPoint LocalEndPoint
        {
            get
            {
                if(localEndPoint == null)
                {
                    localEndPoint = socket.LocalEndPoint;
                }
                if (socket == null)
                {
                    return localEndPoint;
                }
                try
                {
                    if (socket.LocalEndPoint == null)
                    {
                        return localEndPoint;
                    }
                    if (!localEndPoint.Equals(socket.LocalEndPoint))
                    {
                        localEndPoint = socket.LocalEndPoint;
                    }
                }
                catch (Exception) { }
                return localEndPoint;
            }
        }

        public EndPoint RemoteEndPoint { get; private set; }

        public event Action<string> GetMessage;

        byte[] m_Buffer = new byte[65536];
        bool run = false;

        bool checkbool = false;
        object checklock = new object();

        const uint IOC_IN = 0x80000000;
        const uint IOC_VENDOR = 0x18000000;
        const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

        public NetClientUDP()
        {
            Setup();
            EnableP2P = false;
        }

        public NetClientUDP(bool enableP2P)
        {
            Setup();
            EnableP2P = enableP2P;
        }

        private void Setup()
        {
            Packets = new List<Packet>();
            P2PSocketList = new List<PeerForP2PBase>();
            P2PToPeer = new Dictionary<EndPoint, PeerForP2PBase>();
            P2PSocketToKey = new Dictionary<object, string>();
            P2Pclientcheck = new Dictionary<EndPoint, bool>();
            OnP2PConnect = new List<IPEndPoint>();
            OnP2PWait = new Dictionary<IPEndPoint, Stopwatch>();
            OnP2PConnectAction = new Dictionary<IPEndPoint, Action<EndPoint, EndPoint, PeerForP2PBase, bool>>();
            OnListenP2PClient = new Dictionary<EndPoint, List<Packet>>();
            P2PToRealEndPoint = new Dictionary<EndPoint, EndPoint>();
            P2PToNewEndPoint = new Dictionary<EndPoint, EndPoint>();
        }

        private void OnStop()
        {
            Packets.Clear();
            P2PSocketList.Clear();
            P2PToPeer.Clear();
            P2PSocketToKey.Clear();
            P2Pclientcheck.Clear();
            OnP2PConnect.Clear();
            OnP2PWait.Clear();
            OnP2PConnectAction.Clear();
            OnListenP2PClient.Clear();
            P2PToRealEndPoint.Clear();
            P2PToNewEndPoint.Clear();
        }

        #region C2S
        public bool Connect(string serverhost, int remotePort)
        {
            if (socket != null)
            {
                socket.Close();
                socket = null;
            }

            socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            if ((byte)Environment.OSVersion.Platform >= 0 && (byte)Environment.OSVersion.Platform <= 3)
            {
                socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
            }
            socket.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
            try
            {
                EndPoint endPoint = LocalEndPoint;

                IPAddress address;
                if (!IPAddress.TryParse(serverhost, out address))
                {
                    address = Dns.GetHostEntry(serverhost).AddressList[0];
                }
                RemoteEndPoint = new IPEndPoint(address.MapToIPv6(), remotePort);

                DebugMessage("正在嘗試連線IP:" + RemoteEndPoint.ToString());
                using (Packet packet = new Packet(RemoteEndPoint))
                {
                    //Packet packet = new Packet(RemoteEndPoint);
                    packet.BeginWrite(PacketType.ON_CONNECT);
                    Send(packet);
                }
                EndPoint remoteEP = new IPEndPoint(IPAddress.IPv6Any, 0);
                socket.BeginReceiveFrom(m_Buffer, 0, m_Buffer.Length, SocketFlags.None, ref remoteEP, ConnectionCallback, socket);

                //socket.Send(ipe, ConnectionCallback, socket);
            }
            catch (System.Exception e)
            {
                // 連接失敗
                PushPacket(PacketType.CONNECTION_ATTEMPT_FAILED, e.ToString());
                return false;
            }
            return true;
        }

        private void ConnectionCallback(System.IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            try
            {
                EndPoint ipe = new IPEndPoint(IPAddress.IPv6Any, 0);
                Packet getpacket = new Packet(ipe, Getbytes(socket.EndReceiveFrom(ar, ref ipe)), null);
                RemoteEndPoint = ipe;
                PacketType msgid = getpacket.BeginRead();

                #region Set def var
                int defReadTimeout = socket.ReceiveTimeout;
                #endregion

                #region Set Read func
                SendData onRead(PacketType checkType, string key, Func<SendData, bool> datacheck)
                {
                    SendData ReadData = new SendData();
                    try
                    {
                        socket.ReceiveTimeout = 1000;

                        EndPoint nowip = new IPEndPoint(IPAddress.IPv6Any, 0);
                        byte[] data = Getbytes(socket.ReceiveFrom(m_Buffer, 0, m_Buffer.Length, SocketFlags.None, ref nowip));
                        while (!nowip.Equals(ipe))
                        {
                            data = Getbytes(socket.ReceiveFrom(m_Buffer, 0, m_Buffer.Length, SocketFlags.None, ref nowip));
                        }

                        socket.ReceiveTimeout = defReadTimeout;
                        using (Packet packet = new Packet(socket, data, null))
                        {
                            if (packet.BeginRead() != checkType)
                            {
                                PushPacket(PacketType.CONNECTION_ATTEMPT_FAILED, "bad Receive");
                                Disconnect();
                                return new SendData();
                            }
                            ReadData = packet.ReadSendData(key);
                        }
                    }
                    catch (Exception e)
                    {
                        PushPacket(PacketType.CONNECTION_ATTEMPT_FAILED, e.ToString());
                        Disconnect();
                        return new SendData();
                    }
                    if (!datacheck(ReadData))
                    {
                        PushPacket(PacketType.CONNECTION_ATTEMPT_FAILED, "bad Receive");
                        Disconnect();
                        return new SendData();
                    }
                    return ReadData;
                }
                #endregion

                #region Set Send func
                void onSend(PacketType sendType, string key, SerializationData.LockType lockType, SendData send)
                {
                    using (Packet packet = new Packet(RemoteEndPoint))
                    {
                        packet.BeginWrite(sendType);
                        packet.WriteSendData(send, key, lockType);
                        Send(packet);
                    }
                }
                #endregion

                #region Start Get Public Key

                SendData sendData = getpacket.ReadSendData("");
                getpacket.Dispose();
                if (msgid != PacketType.RSAKEY)
                    return;

                RSAkey = new SerializationData.RSAKeyPair((byte[])sendData.Parameters);
                #endregion

                #region Generate And Send AES Key
                AESkey = SerializationData.GenerateAESKey();
                onSend(PacketType.AESKEY, RSAkey.PublicKey, SerializationData.LockType.RSA, new SendData(0, AESkey));
                #endregion

                #region Check AES Key
                sendData = onRead(PacketType.AESKEY, AESkey, (a) => a.Parameters.ToString() == "Connect check");
                if (sendData == new SendData())
                    return;
                #endregion

                #region Send CONNECT_SUCCESSFUL
                onSend(PacketType.CONNECT_SUCCESSFUL, AESkey, SerializationData.LockType.AES, new SendData(0, "Connect successful"));
                #endregion

                #region On CONNECT
                sendData = onRead(PacketType.CONNECT_SUCCESSFUL, AESkey, (a) => a.Parameters.ToString() == "On Connect");
                if (sendData == new SendData())
                    return;

                P2PRSAkey = SerializationData.GenerateRSAKeys(2048);

                PushPacket(PacketType.CONNECT_SUCCESSFUL, "");


                byte[] bytes = new byte[Packet.header_length];
                ipe = new IPEndPoint(IPAddress.IPv6Any, 0);
                socket.BeginReceiveFrom(m_Buffer, 0, m_Buffer.Length, SocketFlags.None, ref ipe, Receive, socket);

                #endregion

            }
            catch (Exception e)
            {
                PushPacket(PacketType.CONNECTION_ATTEMPT_FAILED, e.ToString());
                DebugMessage(e.ToString());
                //Disconnect(0);
            }
        }

        public void ConnectSuccessful(Packet packet)
        {
            lock (checklock)
            {
                checkbool = true;
            }
            StartThread();
        }

        public void Disconnect()
        {
            Disconnect(-1);
        }

        public void Disconnect(int timeout)
        {
            lock (SendLock)
            {
                if (socket != null)
                {
                    try
                    {
                        socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception) { }
                    finally
                    {
                        if (timeout == -1)
                        {
                            socket.Close();
                        }
                        else
                        {
                            socket.Close(timeout);
                        }
                        socket = null;
                    }
                }

                StopThread();

                GetMessage = null;

                RemoteEndPoint = null;

                OnStop();
                AESkey = null;
                RSAkey = new SerializationData.RSAKeyPair();
            }
        }
        #endregion

        #region P2P
        private void P2PPacketTurning(P2PCode code, IPEndPoint PeerRemoteEndPoint, object data)
        {
            using (Packet packet = new Packet(RemoteEndPoint))
            {
                packet.BeginWrite(PacketType.P2P_SERVER_CALL);
                SendData sendData = new SendData((byte)code, new object[] { PeerRemoteEndPoint.ToString(), LocalPublicEndPoint == null ? null : LocalPublicEndPoint.ToString(), data });
                packet.WriteSendData(sendData, AESkey, SerializationData.LockType.AES);
                Send(packet);
            }
        }

        public void P2PNATPacketSend(Packet packet)
        {
            P2PPacketTurning(P2PCode.NATP2PTell, (IPEndPoint)packet.peer, packet.Bytes);
        }

        /// <summary>
        /// Start P2P Connect
        /// </summary>
        /// <param name="IPPort">Connect Target</param>
        /// <param name="callback">Connect Callback(Connect IP, Connect Public IP, Successful)</param>
        public void StartP2PConnect(IPEndPoint IPPort, Action<EndPoint, EndPoint, PeerForP2PBase, bool> callback)
        {
            if (EnableP2P)
            {
                if (!OnP2PConnect.Contains(IPPort))
                {
                    P2PPacketTurning(P2PCode.CallConnect, IPPort, new object[] { GetAllMyIP(), true });
                    OnP2PConnect.Add(IPPort);
                    OnP2PConnectAction.Add(IPPort, callback);
                }
            }
            else
            {
                throw new P2PException("P2P mode is not enable.");
            }
        }

        /// <summary>
        /// Wait P2P Connect
        /// </summary>
        /// <param name="IPPort">Connect Target</param>
        /// <param name="callback">Connect Callback(Connect IP, Connect Public IP, Successful)</param>
        public void WaitP2PConnect(IPEndPoint IPPort, Action<EndPoint, EndPoint, PeerForP2PBase, bool> callback)
        {
            if (EnableP2P)
            {
                if (!OnP2PConnectAction.ContainsKey(IPPort))
                {
                    OnP2PConnectAction.Add(IPPort, callback);
                    lock (OnP2PWait)
                    {
                        if (!OnP2PConnect.Contains(IPPort))
                        {
                            Stopwatch stopwatch = new Stopwatch();
                            stopwatch.Start();
                            OnP2PWait.Add(IPPort, stopwatch);
                        }
                    }
                }
            }
            else
            {
                throw new P2PException("P2P mode is not enable.");
            }
        }

        private bool checkIP(IPAddress ip, string[] ipnetmask)
        {
            bool data = false;
            for (int i = 0; i < ipnetmask.Length; i++)
            {
                data = data || ip.IsInSubnet(ipnetmask[i]);
            }
            return data;
        }

        private object[] TestP2PCallConnect(string[] IPs, IPEndPoint publicIP)
        {
            string[] MyIPs = GetAllMyIP();

            int ReadTime = 1000;

            #region 判斷自己或對方是否只有PublicIP
            bool onlyPublicIP = true;
            if (IPs.Length > 0)
            {
                for (int i = 0; i < IPs.Length; i++)
                {
                    IPEndPoint endPoint = TraceRoute.IPEndPointParse(IPs[i], AddressFamily.InterNetworkV6);
                    if (checkIP(endPoint.Address, new string[] { "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" }))
                    {
                        onlyPublicIP = false;
                        break;
                    }
                }
                if (!onlyPublicIP)
                {
                    onlyPublicIP = true;
                    for (int i = 0; i < MyIPs.Length; i++)
                    {
                        IPEndPoint endPoint = TraceRoute.IPEndPointParse(MyIPs[i], AddressFamily.InterNetworkV6);
                        if (checkIP(endPoint.Address, new string[] { "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" }))
                        {
                            onlyPublicIP = false;
                            break;
                        }
                    }
                }
            }
            #endregion

            #region 判斷雙方是否有IPV6
            bool haveIPv6 = false;
            if (IPs.Length > 0)
            {
                bool AhaveIPv6 = false;
                for (int i = 0; i < IPs.Length; i++)
                {
                    IPEndPoint endPoint = TraceRoute.IPEndPointParse(IPs[i], AddressFamily.InterNetworkV6);
                    if (endPoint.Address.IsIPv6Unicast())
                    {
                        AhaveIPv6 = true;
                        break;
                    }
                }
                if(AhaveIPv6)
                {
                    for (int i = 0; i < MyIPs.Length; i++)
                    {
                        IPEndPoint endPoint = TraceRoute.IPEndPointParse(MyIPs[i], AddressFamily.InterNetworkV6);
                        if (endPoint.Address.IsIPv6Unicast())
                        {
                            haveIPv6 = true;
                            break;
                        }
                    }
                }
            }
            #endregion

            #region 新增TestP2PCallConnect要連線的IP紀錄
            Dictionary<EndPoint, object[]>  TestP2PCallConnectEnd = new Dictionary<EndPoint, object[]>();
            List<EndPoint> TestP2PCallConnectIPList = new List<EndPoint>();
            #endregion

            #region 非同步測試函式組
            Func<Socket, IPEndPoint, int, KeyValuePair<EndPoint, object[]>> runtest = (socket, ip, port) =>
            {
                EndPoint localEndPoint = TraceRoute.IPEndPointParse(socket.LocalEndPoint.ToString(), AddressFamily.InterNetworkV6);

                byte[] data = new byte[6];
                socket.SendTo(data, ip);


                EndPoint retip = new IPEndPoint(IPAddress.IPv6Any, 0);
                socket.ReceiveTimeout = ReadTime;

                bool isprivate = checkIP(ip.Address, new string[] { "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" });
                bool isipv6 = ip.Address.IsIPv6Unicast();

                string[] useIPs = null;
                if (isprivate)
                {
                    useIPs = Array.FindAll(MyIPs, a => checkIP(TraceRoute.IPEndPointParse(a, AddressFamily.InterNetworkV6).Address, new string[] { "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" }));
                }
                else if (isipv6)
                {
                    useIPs = Array.FindAll(MyIPs, a => TraceRoute.IPEndPointParse(a, AddressFamily.InterNetworkV6).Address.IsIPv6Unicast());
                }
                else
                {
                    useIPs = new string[] { LocalPublicEndPoint.ToString() };
                }

                P2PPacketTurning(P2PCode.TestCall, publicIP, new object[] { useIPs, port });
                KeyValuePair<EndPoint, object[]> ans;
                try
                {
                    data = new byte[65536];
                    int len = socket.ReceiveFrom(data, ref retip);
                    byte[] outdata = new byte[len];
                    Array.Copy(data, outdata, len);
                    using (Packet packet = new Packet(publicIP, outdata))
                    {
                        packet.BeginRead();
                        SendData sendData = packet.ReadSendData("");
                        ans = new KeyValuePair<EndPoint, object[]>(localEndPoint, new object[] { ip, true, sendData.Parameters });
                    }
                }
                catch (SocketException)
                {
                    ans = new KeyValuePair<EndPoint, object[]>(localEndPoint, new object[] { ip, false });
                }
                socket.Close();
                return ans;
            };

            void callback(IAsyncResult ar)
            {
                object state = ar.AsyncState;
                KeyValuePair<EndPoint, object[]> ans = runtest.EndInvoke(ar);
                lock (TestP2PCallConnectEnd)
                {
                    if (!TestP2PCallConnectEnd.ContainsKey(ans.Key))
                    {
                        TestP2PCallConnectEnd.Add(ans.Key, ans.Value);
                    }
                }
            }
            #endregion

            #region 雙方皆有私網IP時的測試
            if (!onlyPublicIP)
            {
                foreach (string ip in IPs)
                {
                    IPEndPoint endPoint = TraceRoute.IPEndPointParse(ip, AddressFamily.InterNetworkV6);
                    if (!checkIP(endPoint.Address, new string[] { "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" })) continue;

                    Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                    socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                    if ((byte)Environment.OSVersion.Platform >= 0 && (byte)Environment.OSVersion.Platform <= 3)
                    {
                        socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                    }
                    socket.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
                    EndPoint localEndPoint = TraceRoute.IPEndPointParse(socket.LocalEndPoint.ToString(), AddressFamily.InterNetworkV6);
                    TestP2PCallConnectIPList.Add(localEndPoint);
                    runtest.BeginInvoke(socket, endPoint, ((IPEndPoint)localEndPoint).Port, callback, null);
                }
            }
            #endregion

            #region 雙方皆有IPv6時的測試
            if (haveIPv6)
            {
                foreach (string ip in IPs)
                {
                    IPEndPoint endPoint = TraceRoute.IPEndPointParse(ip, AddressFamily.InterNetworkV6);
                    if (!endPoint.Address.IsIPv6Unicast()) continue;

                    Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                    socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                    if ((byte)Environment.OSVersion.Platform >= 0 && (byte)Environment.OSVersion.Platform <= 3)
                    {
                        socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                    }
                    socket.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
                    EndPoint localEndPoint = TraceRoute.IPEndPointParse(socket.LocalEndPoint.ToString(), AddressFamily.InterNetworkV6);
                    TestP2PCallConnectIPList.Add(localEndPoint);
                    runtest.BeginInvoke(socket, endPoint, ((IPEndPoint)localEndPoint).Port, callback, null);
                }
            }
            #endregion

            #region 公網IP測試

            void testpublicip()
            {
                //listener.DebugReturn(publicenumer[i].ToString());
                #region 建立測試用Socket
                Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                if ((byte)Environment.OSVersion.Platform >= 0 && (byte)Environment.OSVersion.Platform <= 3)
                {
                    socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                }
                socket.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
                EndPoint localEndPoint = TraceRoute.IPEndPointParse(socket.LocalEndPoint.ToString(), AddressFamily.InterNetworkV6);
                TestP2PCallConnectIPList.Add(localEndPoint);
                #endregion

                #region 取得外網對內PORT
                using (Packet packet = new Packet(RemoteEndPoint))
                {
                    packet.BeginWrite(PacketType.P2P_GET_PUBLIC_ENDPOINT);
                    socket.SendTo(packet.Bytes, RemoteEndPoint);
                    socket.ReceiveTimeout = ReadTime;
                }

                try
                {
                    byte[] data = new byte[65536];
                    EndPoint retip = new IPEndPoint(IPAddress.IPv6Any, 0);
                    int len = socket.ReceiveFrom(data, ref retip);
                    byte[] outdata = new byte[len];
                    Array.Copy(data, outdata, len);
                    IPEndPoint GetIPPORT;
                    using (Packet packet = new Packet(retip, outdata))
                    {
                        packet.BeginRead();
                        SendData sendData = packet.ReadSendData("");
                        GetIPPORT = TraceRoute.IPEndPointParse(sendData.Parameters.ToString(), AddressFamily.InterNetworkV6);
                    }
                    #endregion

                    runtest.BeginInvoke(socket, publicIP, GetIPPORT.Port, callback, null);

                    #region 取得失敗直接結束
                }
                catch (SocketException e)
                {
                    if (!TestP2PCallConnectEnd.ContainsKey(localEndPoint))
                    {
                        TestP2PCallConnectEnd.Add(localEndPoint, new object[] { publicIP, false });
                    }
                    socket.Close();
                }
                #endregion
            }

            /*{
                List<IPAddress> publicenumer = TraceRoute.GetTraceRoute(publicIP.Address.ToString(), 50);
                int ttl = 0;
                for (ttl = publicenumer.Count - 1; ttl >= 0; ttl--)
                {
                    if (!checkIP(publicenumer[ttl], new string[] { "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" })) break;
                }
                for (; ttl < publicenumer.Count; ttl++)
                {
                    if ((publicenumer[ttl] == null || checkIP(publicenumer[ttl], new string[] { "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" }))) break;
                }
                ttl++;
                testpublicip((short)ttl);
            }*/
            testpublicip();
            #endregion

            #region 等全部測試完成後輸出測試結果
            Thread.Sleep(500);

            bool canlink = false;
            EndPoint linkip = null;
            for (int i = 0; i < TestP2PCallConnectIPList.Count; i++)
            {
                SpinWait.SpinUntil(() => TestP2PCallConnectEnd.ContainsKey(TestP2PCallConnectIPList[i]));
                lock (TestP2PCallConnectEnd)
                {
                    if (!canlink && (bool)TestP2PCallConnectEnd[TestP2PCallConnectIPList[i]][1])
                    {
                        canlink = true;
                        linkip = TestP2PCallConnectIPList[i];
                    }
                }
            }
            object[] ansdata = null;
            lock (TestP2PCallConnectEnd)
            {
                if (canlink)
                {
                    ansdata = TestP2PCallConnectEnd[linkip];
                }

                TestP2PCallConnectEnd.Clear();
                TestP2PCallConnectIPList.Clear();
            }
            return ansdata;
            #endregion
        }

        public void P2PConnectSuccessful(Func<object, object, INetClient, bool, PeerForP2PBase> P2PAddPeer, Packet packet)
        {
            IPEndPoint client = (IPEndPoint)packet.peer;
            lock (checklock)
            {
                if (!P2PToPeer.ContainsKey(client))
                {
                    object[] data = (object[])packet.state;

                    IPEndPoint realip = (IPEndPoint)data[0];

                    OnP2PConnect.Remove(realip);

                    P2PSocketToKey.Add(client, data[1].ToString());
                    if (!P2Pclientcheck.ContainsKey(client))
                    {
                        P2Pclientcheck.Add(client, true);
                    }
                    PeerForP2PBase peer = P2PAddPeer(client, realip, this, (bool)data[2]);
                    P2PSocketList.Add(peer);
                    P2PToPeer.Add(client, peer);
                    if (OnP2PConnectAction.ContainsKey(realip))
                    {
                        var doing = OnP2PConnectAction[realip];
                        OnP2PConnectAction.Remove(realip);
                        doing?.Invoke(client, realip, peer, true);
                    }
                }
            }
        }

        private void ListenP2PClient(Packet getpacket)
        {
            IPEndPoint client = (IPEndPoint)getpacket.peer;
            PacketType msgid = getpacket.BeginRead();

            if (client != null)
            {
                lock (OnListenP2PClient)
                {
                    OnListenP2PClient.Add(client, new List<Packet>());
                }

                #region Set def var
                const int ReadTimeout = 1000;
                #endregion

                #region Set Read func
                SendData onRead(PacketType checkType, string key, Func<SendData, bool> datacheck, Action<SendData> failedcallback)
                {
                    SendData ReadData = new SendData();
                    List<Packet> clientdata;
                    lock (OnListenP2PClient)
                    {
                        clientdata = OnListenP2PClient[client];
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
                                failedcallback?.Invoke(ReadData);
                                return new SendData();
                            }
                            ReadData = nowpacket.ReadSendData(key);
                        }
                    }
                    else
                    {
                        failedcallback?.Invoke(ReadData);
                        return new SendData();
                    }
                    if (!datacheck(ReadData))
                    {
                        failedcallback?.Invoke(ReadData);
                        return new SendData();
                    }
                    return ReadData;
                }
                #endregion

                #region Set Send func
                void onSend(PacketType sendType, IPEndPoint endPoint, string key, SerializationData.LockType lockType, SendData send, bool NAT = false)
                {
                    using (Packet packet = new Packet(endPoint))
                    {
                        packet.BeginWrite(sendType);
                        packet.WriteSendData(send, key, lockType);
                        if (NAT)
                        {
                            P2PNATPacketSend(packet);
                        }
                        else
                        {
                            Send(packet);
                        }
                    }
                }
                #endregion

                SendData sendData = getpacket.ReadSendData("");
                getpacket.CloseStream();

                switch(msgid)
                {
                    case PacketType.P2P_SERVER_CALL:
                        {
                            switch((P2PCode)sendData.Code)
                            {
                                case P2PCode.CallConnectComplete:
                                    {
                                        onSend(PacketType.P2P_CONNECTION, client, "", SerializationData.LockType.None, new SendData(0, LocalPublicEndPoint.ToString()));

                                        #region Get RSA key
                                        sendData = onRead(PacketType.RSAKEY, "", (a) => true, (data) => 
                                        {
                                            DebugMessage("ToNAT");
                                            P2PPacketTurning(P2PCode.ConnectCompleteWithNAT, (IPEndPoint)P2PToRealEndPoint[client], null);
                                            P2PToNewEndPoint.Remove(P2PToRealEndPoint[client]);
                                            P2PToRealEndPoint.Remove(client);
                                            OnListenP2PClient.Remove(client);
                                            return;
                                        });
                                        if (sendData == new SendData())
                                            return;

                                        SerializationData.RSAKeyPair P2PKey = new SerializationData.RSAKeyPair((byte[])sendData.Parameters);
                                        #endregion

                                        #region Send AES key
                                        string P2PAESkey = SerializationData.GenerateAESKey();
                                        onSend(PacketType.AESKEY, client, P2PKey.PublicKey, SerializationData.LockType.RSA, new SendData(0, P2PAESkey));
                                        #endregion

                                        #region Check AES Key
                                        sendData = onRead(PacketType.AESKEY, P2PAESkey, (a) => a.Parameters.ToString() == "Connect check", (data) =>
                                        {
                                            IPEndPoint realip = (IPEndPoint)P2PToRealEndPoint[client];

                                            OnP2PConnect.Remove(realip);
                                            if (OnP2PConnectAction.ContainsKey(realip))
                                            {
                                                var doing = OnP2PConnectAction[realip];
                                                OnP2PConnectAction.Remove(realip);
                                                doing?.Invoke(client, realip, null, false);
                                            }
                                            lock (OnListenP2PClient)
                                            {
                                                OnListenP2PClient.Remove(client);
                                            }
                                            P2PToRealEndPoint.Remove(client);
                                            P2PToNewEndPoint.Remove(realip);
                                        });
                                        if(sendData == new SendData())
                                            return;

                                        #endregion

                                        #region Send CONNECT_SUCCESSFUL
                                        onSend(PacketType.P2P_CONNECT_SUCCESSFUL, client, P2PAESkey, SerializationData.LockType.AES, new SendData(0, "Connect successful"));
                                        #endregion

                                        #region On CONNECT
                                        sendData = onRead(PacketType.P2P_CONNECT_SUCCESSFUL, P2PAESkey, (a) => a.Parameters.ToString() == "On Connect", (data) =>
                                        {
                                            IPEndPoint realip = (IPEndPoint)P2PToRealEndPoint[client];

                                            OnP2PConnect.Remove(realip);
                                            if (OnP2PConnectAction.ContainsKey(realip))
                                            {
                                                var doing = OnP2PConnectAction[realip];
                                                OnP2PConnectAction.Remove(realip);
                                                doing?.Invoke(client, realip, null, false);
                                            }
                                            lock (OnListenP2PClient)
                                            {
                                                OnListenP2PClient.Remove(client);
                                            }
                                            P2PToRealEndPoint.Remove(client);
                                            P2PToNewEndPoint.Remove(realip);
                                        });
                                        if (sendData == new SendData())
                                            return;

                                        IPEndPoint therealip = (IPEndPoint)P2PToRealEndPoint[client];

                                        P2PToRealEndPoint.Remove(client);
                                        P2PToNewEndPoint.Remove(therealip);

                                        P2PPushPacket(PacketType.P2P_CONNECT_SUCCESSFUL, P2PAESkey, client, therealip, false);

                                        lock (OnListenP2PClient)
                                        {
                                            foreach (Packet inpacket in OnListenP2PClient[client])
                                            {
                                                PacketType thetype = inpacket.BeginRead();
                                                inpacket.CloseRead();
                                                inpacket.ResetPosition();

                                                if (thetype > PacketType.P2PSendAllowTypeTop && thetype < PacketType.P2PSendAllowTypeEnd)
                                                {
                                                    PushPacket(inpacket);
                                                }
                                            }
                                            OnListenP2PClient.Remove(client);
                                        }
                                        #endregion
                                        break;
                                    }
                                case P2PCode.ConnectCompleteWithNAT:
                                    {
                                        P2PPacketTurning(P2PCode.ConnectCompleteWithNATCallback, client, null);

                                        #region Get RSA key
                                        sendData = onRead(PacketType.RSAKEY, "", (a) => true, (data) =>
                                        {
                                            OnP2PConnect.Remove(client);
                                            if (OnP2PConnectAction.ContainsKey(client))
                                            {
                                                var doing = OnP2PConnectAction[client];
                                                OnP2PConnectAction.Remove(client);
                                                doing?.Invoke(client, client, null, false);
                                            }
                                            lock (OnListenP2PClient)
                                            {
                                                OnListenP2PClient.Remove(client);
                                            }
                                        });
                                        if (sendData == new SendData())
                                            return;

                                        SerializationData.RSAKeyPair P2PKey = new SerializationData.RSAKeyPair((byte[])sendData.Parameters);
                                        #endregion

                                        #region Send AES key
                                        string P2PAESkey = SerializationData.GenerateAESKey();
                                        onSend(PacketType.AESKEY, client, P2PKey.PublicKey, SerializationData.LockType.RSA, new SendData(0, P2PAESkey), true);
                                        #endregion

                                        #region Check RSA Key
                                        sendData = onRead(PacketType.AESKEY, P2PAESkey, (a) => a.Parameters.ToString() == "Connect check", (data) =>
                                        {
                                            OnP2PConnect.Remove(client);
                                            if (OnP2PConnectAction.ContainsKey(client))
                                            {
                                                var doing = OnP2PConnectAction[client];
                                                OnP2PConnectAction.Remove(client);
                                                doing?.Invoke(client, client, null, false);
                                            }
                                            lock (OnListenP2PClient)
                                            {
                                                OnListenP2PClient.Remove(client);
                                            }
                                        });
                                        if (sendData == new SendData())
                                            return;

                                        #endregion

                                        #region Send CONNECT_SUCCESSFUL
                                        onSend(PacketType.P2P_CONNECT_SUCCESSFUL, client, P2PAESkey, SerializationData.LockType.AES, new SendData(0, "Connect successful"), true);
                                        #endregion

                                        #region On CONNECT
                                        sendData = onRead(PacketType.P2P_CONNECT_SUCCESSFUL, P2PAESkey, (a) => a.Parameters.ToString() == "On Connect", (data) =>
                                        {
                                            OnP2PConnect.Remove(client);
                                            if (OnP2PConnectAction.ContainsKey(client))
                                            {
                                                var doing = OnP2PConnectAction[client];
                                                OnP2PConnectAction.Remove(client);
                                                doing?.Invoke(client, client, null, false);
                                            }
                                            lock (OnListenP2PClient)
                                            {
                                                OnListenP2PClient.Remove(client);
                                            }
                                        });
                                        if (sendData == new SendData())
                                            return;

                                        P2PPushPacket(PacketType.P2P_CONNECT_SUCCESSFUL, P2PAESkey, client, client, true);

                                        lock (OnListenP2PClient)
                                        {
                                            foreach (Packet inpacket in OnListenP2PClient[client])
                                            {
                                                PacketType thetype = inpacket.BeginRead();
                                                inpacket.CloseRead();
                                                inpacket.ResetPosition();

                                                if (thetype > PacketType.P2PSendAllowTypeTop && thetype < PacketType.P2PSendAllowTypeEnd)
                                                {
                                                    PushPacket(inpacket);
                                                }
                                            }
                                            OnListenP2PClient.Remove(client);
                                        }

                                        #endregion

                                        break;
                                    }
                                case P2PCode.ConnectCompleteWithNATCallback:
                                    {

                                        #region Send RSA key
                                        onSend(PacketType.RSAKEY, client, "", SerializationData.LockType.None, new SendData(0, P2PRSAkey.PublicKeyBytes), true);
                                        #endregion

                                        #region Get AES key
                                        sendData = onRead(PacketType.AESKEY, P2PRSAkey.PrivateKey, (a) => true, (data) =>
                                        {
                                            OnP2PConnect.Remove(client);
                                            if (OnP2PConnectAction.ContainsKey(client))
                                            {
                                                var doing = OnP2PConnectAction[client];
                                                OnP2PConnectAction.Remove(client);
                                                doing?.Invoke(client, client, null, false);
                                            }
                                            lock (OnListenP2PClient)
                                            {
                                                OnListenP2PClient.Remove(client);
                                            }
                                        });
                                        if (sendData == new SendData())
                                            return;

                                        string P2PAESkey = sendData.Parameters.ToString();
                                        #endregion

                                        #region Send RSA Check
                                        onSend(PacketType.AESKEY, client, P2PAESkey, SerializationData.LockType.AES, new SendData(0, "Connect check"), true);
                                        #endregion

                                        #region Get CONNECT_SUCCESSFUL
                                        sendData = onRead(PacketType.P2P_CONNECT_SUCCESSFUL, P2PAESkey, (a) => a.Parameters.ToString() == "Connect successful", (data) =>
                                        {
                                            OnP2PConnect.Remove(client);
                                            if (OnP2PConnectAction.ContainsKey(client))
                                            {
                                                var doing = OnP2PConnectAction[client];
                                                OnP2PConnectAction.Remove(client);
                                                doing?.Invoke(client, client, null, false);
                                            }
                                            lock (OnListenP2PClient)
                                            {
                                                OnListenP2PClient.Remove(client);
                                            }
                                        });
                                        if (sendData == new SendData())
                                            return;

                                        #endregion

                                        #region On CONNECT
                                        onSend(PacketType.P2P_CONNECT_SUCCESSFUL, client, P2PAESkey, SerializationData.LockType.AES, new SendData(0, "On Connect"), true);
                                        P2PPushPacket(PacketType.P2P_CONNECT_SUCCESSFUL, P2PAESkey, client, client, true);

                                        lock (OnListenP2PClient)
                                        {
                                            foreach (Packet inpacket in OnListenP2PClient[client])
                                            {
                                                PacketType thetype = inpacket.BeginRead();
                                                inpacket.CloseRead();
                                                inpacket.ResetPosition();

                                                if (thetype > PacketType.P2PSendAllowTypeTop && thetype < PacketType.P2PSendAllowTypeEnd)
                                                {
                                                    PushPacket(inpacket);
                                                }
                                            }
                                            OnListenP2PClient.Remove(client);
                                        }
                                        #endregion

                                        break;
                                    }
                                default:
                                    {
                                        return;
                                    }
                            }
                            break;
                        }
                    case PacketType.P2P_CONNECTION:
                        {
                            #region Send RSA key
                            onSend(PacketType.RSAKEY, client, "", SerializationData.LockType.None, new SendData(0, P2PRSAkey.PublicKeyBytes));
                            #endregion

                            #region Get AES key
                            sendData = onRead(PacketType.AESKEY, P2PRSAkey.PrivateKey, (a) => true, (data) =>
                            {
                                IPEndPoint realip = (IPEndPoint)P2PToRealEndPoint[client];

                                OnP2PConnect.Remove(realip);
                                if (OnP2PConnectAction.ContainsKey(realip))
                                {
                                    var doing = OnP2PConnectAction[realip];
                                    OnP2PConnectAction.Remove(realip);
                                    doing?.Invoke(client, realip, null, false);
                                }
                                lock (OnListenP2PClient)
                                {
                                    OnListenP2PClient.Remove(client);
                                }
                                P2PToRealEndPoint.Remove(client);
                                P2PToNewEndPoint.Remove(realip);
                            });
                            if (sendData == new SendData())
                                return;

                            string P2PAESkey = sendData.Parameters.ToString();
                            #endregion

                            #region Send AES Check
                            onSend(PacketType.AESKEY, client, P2PAESkey, SerializationData.LockType.AES, new SendData(0, "Connect check"));
                            #endregion

                            #region Get CONNECT_SUCCESSFUL
                            sendData = onRead(PacketType.P2P_CONNECT_SUCCESSFUL, P2PAESkey, (a) => a.Parameters.ToString() == "Connect successful", (data) =>
                            {
                                IPEndPoint realip = (IPEndPoint)P2PToRealEndPoint[client];

                                OnP2PConnect.Remove(realip);
                                if (OnP2PConnectAction.ContainsKey(realip))
                                {
                                    var doing = OnP2PConnectAction[realip];
                                    OnP2PConnectAction.Remove(realip);
                                    doing?.Invoke(client, realip, null, false);
                                }
                                lock (OnListenP2PClient)
                                {
                                    OnListenP2PClient.Remove(client);
                                }
                                P2PToRealEndPoint.Remove(client);
                                P2PToNewEndPoint.Remove(realip);
                            });
                            if (sendData == new SendData())
                                return;

                            #endregion

                            #region On CONNECT
                            onSend(PacketType.P2P_CONNECT_SUCCESSFUL, client, P2PAESkey, SerializationData.LockType.AES, new SendData(0, "On Connect"));

                            IPEndPoint therealip = (IPEndPoint)P2PToRealEndPoint[client];

                            P2PToRealEndPoint.Remove(client);
                            P2PToNewEndPoint.Remove(therealip);

                            P2PPushPacket(PacketType.P2P_CONNECT_SUCCESSFUL, P2PAESkey, client, therealip, false);

                            lock (OnListenP2PClient)
                            {
                                foreach (Packet inpacket in OnListenP2PClient[client])
                                {
                                    PacketType thetype = inpacket.BeginRead();
                                    inpacket.CloseRead();
                                    inpacket.ResetPosition();

                                    if (thetype > PacketType.P2PSendAllowTypeTop && thetype < PacketType.P2PSendAllowTypeEnd)
                                    {
                                        PushPacket(inpacket);
                                    }
                                }
                                OnListenP2PClient.Remove(client);
                            }
                            #endregion

                            break;
                        }
                    default:
                        {
                            return;
                        }
                }
            }
        }

        public void P2PDisconnect(object socket)
        {
            P2PDisconnect(socket, -1);
        }

        public void P2PDisconnect(object socket, int timeout)
        {
            if (socket != null)
            {
                if (P2PToPeer.ContainsKey((EndPoint)socket))
                {
                    if (P2PSocketList.Contains(P2PToPeer[(EndPoint)socket]))
                    {
                        P2PSocketList.Remove(P2PToPeer[(EndPoint)socket]);
                    }
                }
                if (P2PSocketToKey.ContainsKey((EndPoint)socket))
                {
                    P2PSocketToKey.Remove((EndPoint)socket);
                }
                if (P2PToPeer.ContainsKey((EndPoint)socket))
                {
                    P2PToPeer.Remove((EndPoint)socket);
                }
                lock (checklock)
                {
                    if (P2Pclientcheck.ContainsKey((EndPoint)socket))
                    {
                        P2Pclientcheck.Remove((EndPoint)socket);
                    }
                }
            }
        }
        #endregion

        void StartThread()
        {
            run = true;
            new Thread(LinkChecker).Start();
        }

        void StopThread()
        {
            run = false;
        }

        private void Receive(System.IAsyncResult ar)
        {
            EndPoint ipe = new IPEndPoint(IPAddress.IPv6Any, 0);

            byte[] data = null;
            PacketType msgid = (PacketType)(-1);
            Packet packet = null;
            try
            {
                int num = socket.EndReceiveFrom(ar, ref ipe);
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
                socket.BeginReceiveFrom(m_Buffer, 0, m_Buffer.Length, SocketFlags.None, ref remoteEP, Receive, socket);
                if (packet == null) return;
            }
            catch (SocketException e)
            {
                PushPacket(PacketType.CONNECTION_LOST, "Receive:" + e.ToString());
                return;
            }
            catch (Exception e)
            {
                DebugMessage("Receive:" + e.ToString());
                EndPoint remoteEP = new IPEndPoint(IPAddress.IPv6Any, 0);
                if (socket != null)
                {
                    socket.BeginReceiveFrom(m_Buffer, 0, m_Buffer.Length, SocketFlags.None, ref remoteEP, Receive, socket);
                }
                return;
            }
            #region C2S
            if (RemoteEndPoint.Equals(ipe) && (msgid < PacketType.P2PTypeTop || msgid > PacketType.P2PTypeEnd))
            {
                if (msgid > PacketType.SendAllowTypeTop && msgid < PacketType.SendAllowTypeEnd)
                {
                    PushPacket(packet);
                    lock (checklock)
                    {
                        checkbool = true;
                    }
                }
                else
                {
                    switch (msgid)
                    {
                        case PacketType.CONNECTION_LOST:
                            {
                                packet.BeginRead();
                                SendData sendData = packet.ReadSendData("");
                                PushPacket(PacketType.CONNECTION_LOST, sendData.DebugMessage);
                                packet.CloseStream();
                                break;
                            }
                        case PacketType.CHECK:
                            {
                                lock (checklock)
                                {
                                    checkbool = true;
                                }
                                packet.CloseStream();
                                break;
                            }
                        default:
                            {
                                PushPacket(PacketType.CONNECTION_LOST, "不正確的標頭資訊 Receive");
                                packet.CloseStream();
                                break;
                            }
                    }
                }
            }
            #endregion
            #region P2P
            else
            {
                void P2PPacketdo(Packet inpacket)
                {
                    PacketType thetype = inpacket.BeginRead();
                    inpacket.CloseRead();
                    inpacket.ResetPosition();

                    if (thetype > PacketType.P2PSendAllowTypeTop && thetype < PacketType.P2PSendAllowTypeEnd)
                    {
                        PushPacket(inpacket);
                        lock (checklock)
                        {
                            if (P2Pclientcheck.ContainsKey((EndPoint)inpacket.peer))
                            {
                                P2Pclientcheck[(EndPoint)inpacket.peer] = true;
                            }
                        }
                    }
                    else
                    {
                        switch (thetype)
                        {
                            case PacketType.P2P_CONNECTION_LOST:
                                {
                                    inpacket.BeginRead();
                                    SendData sendData = inpacket.ReadSendData("");
                                    P2PPushPacket(PacketType.P2P_CONNECTION_LOST, sendData.DebugMessage, (EndPoint)inpacket.peer);
                                    inpacket.CloseStream();
                                    break;
                                }
                            case PacketType.P2P_CHECKING:
                                {
                                    lock (checklock)
                                    {
                                        if (P2Pclientcheck.ContainsKey((EndPoint)inpacket.peer))
                                        {
                                            P2Pclientcheck[(EndPoint)inpacket.peer] = true;
                                        }
                                    }
                                    inpacket.CloseStream();
                                    break;
                                }
                            default:
                                {
                                    DebugMessage("不正確的標頭資訊 Receive: " + thetype.ToString());
                                    /*P2PPushPacket(PacketType.CONNECTION_LOST, "不正確的標頭資訊 Receive", (EndPoint)inpacket.peer);
                                    inpacket.CloseStream();*/
                                    break;
                                }
                        }
                    }
                }

                if (RemoteEndPoint.Equals(ipe))
                {
                    packet.BeginRead();
                    SendData sendData = packet.ReadSendData(AESkey);
                    switch (msgid)
                    {
                        case PacketType.P2P_SERVER_CALL:
                            {
                                const byte RemotePublicIP = 0;
                                const byte LocalPublicIP = 1;
                                const byte Data = 2;
                                object[] Parameters = (object[])sendData.Parameters;
                                LocalPublicEndPoint = TraceRoute.IPEndPointParse(Parameters[LocalPublicIP].ToString(), AddressFamily.InterNetworkV6);
                                IPEndPoint remote = TraceRoute.IPEndPointParse(Parameters[RemotePublicIP].ToString(), AddressFamily.InterNetworkV6);
                                switch ((P2PCode)sendData.Code)
                                {
                                    case P2PCode.CallConnect:
                                        {
                                            lock (OnP2PWait)
                                            {
                                                if (!OnP2PConnect.Contains(remote))
                                                {
                                                    OnP2PConnect.Add(remote);
                                                    if (OnP2PWait.ContainsKey(remote))
                                                    {
                                                        OnP2PWait[remote].Stop();
                                                        OnP2PWait.Remove(remote);
                                                    }
                                                }
                                            }

                                            #region Test P2P Call Connect
                                            List<string> IPs = new List<string>((string[])((object[])Parameters[Data])[0]);

                                            if (IPs.Contains(remote.ToString()))
                                            {
                                                IPs.Remove(remote.ToString());
                                            }

                                            object[] getdata = TestP2PCallConnect(IPs.ToArray(), remote);
                                            if (getdata != null)
                                            {
                                                using (Packet sendpacket = new Packet((IPEndPoint)getdata[0]))
                                                {
                                                    sendpacket.BeginWrite(PacketType.P2P_CHECKING);
                                                    Send(sendpacket);
                                                }

                                                P2PPacketTurning(P2PCode.CallConnectComplete, remote, getdata[2]);
                                            }
                                            else if ((bool)((object[])Parameters[Data])[1])
                                            {
                                                P2PPacketTurning(P2PCode.CallConnect, remote, new object[] { GetAllMyIP(), false });
                                            }
                                            else
                                            {
                                                P2PPacketTurning(P2PCode.ConnectCompleteWithNAT, remote, null);
                                            }
                                            #endregion
                                            break;
                                        }
                                    case P2PCode.TestCall:
                                        {
                                            object[] getdata = (object[])Parameters[Data];
                                            string[] IPs = (string[])getdata[0];
                                            int port = (int)getdata[1];

                                            void sendtest(string ip)
                                            {
                                                using (Packet sendpacket = new Packet(TraceRoute.IPEndPointParse(TraceRoute.IPEndPointParse(ip, AddressFamily.InterNetworkV6).Address.ToString() + ":" + port, AddressFamily.InterNetworkV6)))
                                                {
                                                    sendpacket.BeginWrite(PacketType.P2P_CHECKING);
                                                    sendpacket.WriteSendData(new SendData(0, ip), "", SerializationData.LockType.None);
                                                    Send(sendpacket);
                                                }
                                            }

                                            if (IPs.Length == 0)
                                            {
                                                sendtest(remote.ToString());
                                            }
                                            else
                                            {
                                                for (int i = 0; i < IPs.Length; i++)
                                                {
                                                    sendtest(IPs[i]);
                                                }
                                            }
                                            break;
                                        }
                                    case P2PCode.CallConnectComplete:
                                        {
                                            IPEndPoint ip = TraceRoute.IPEndPointParse(Parameters[Data].ToString(), AddressFamily.InterNetworkV6);

                                            Packet apacket = new Packet(ip);
                                            apacket.BeginWrite(msgid);
                                            apacket.WriteSendData(new SendData(sendData.Code, Parameters[Data]), "", SerializationData.LockType.None);
                                            apacket.CloseWrite();
                                            apacket.ResetPosition();

                                            P2PToNewEndPoint.Add(remote, ip);
                                            P2PToRealEndPoint.Add(ip, remote);

                                            apacket.peer = ip;

                                            ListenP2PClient(apacket);
                                            break;
                                        }
                                    case P2PCode.ConnectCompleteWithNAT:
                                        {
                                            Packet apacket = new Packet(remote);
                                            apacket.BeginWrite(msgid);
                                            apacket.WriteSendData(new SendData(sendData.Code, Parameters[Data]), "", SerializationData.LockType.None);
                                            apacket.CloseWrite();
                                            apacket.ResetPosition();

                                            ListenP2PClient(apacket);
                                            break;
                                        }
                                    case P2PCode.ConnectCompleteWithNATCallback:
                                        {
                                            Packet apacket = new Packet(remote);
                                            apacket.BeginWrite(msgid);
                                            apacket.WriteSendData(new SendData(sendData.Code, Parameters[Data]), "", SerializationData.LockType.None);
                                            apacket.CloseWrite();
                                            apacket.ResetPosition();

                                            ListenP2PClient(apacket);
                                            break;
                                        }
                                    case P2PCode.NATP2PTell:
                                        {
                                            Packet apacket = new Packet(remote, (byte[])Parameters[Data]);

                                            lock (OnListenP2PClient)
                                            {
                                                if (OnListenP2PClient.ContainsKey((EndPoint)apacket.peer))
                                                {
                                                    OnListenP2PClient[(EndPoint)apacket.peer].Add(apacket);
                                                    break;
                                                }
                                            }

                                            /*PacketType gettype = apacket.BeginRead();
                                            SendData getsendData = apacket.ReadSendData(P2PRSAkey.PrivateKey);
                                            apacket.CloseRead();
                                            apacket.ResetPosition();*/
                                            P2PPacketdo(apacket);
                                            break;
                                        }
                                }
                                break;
                            }
                        case PacketType.P2P_SERVER_FAILED:
                            {
                                const byte RemotePublicIP = 0;
                                const byte LocalPublicIP = 1;
                                const byte Data = 2;
                                object[] Parameters = (object[])sendData.Parameters;
                                LocalPublicEndPoint = TraceRoute.IPEndPointParse(Parameters[LocalPublicIP].ToString(), AddressFamily.InterNetworkV6);
                                IPEndPoint remote = TraceRoute.IPEndPointParse(Parameters[RemotePublicIP].ToString(), AddressFamily.InterNetworkV6);
                                if (OnP2PConnect.Contains(remote))
                                {
                                    OnP2PConnect.Remove(remote);
                                    if (OnP2PConnectAction.ContainsKey(remote))
                                    {
                                        var action = OnP2PConnectAction[remote];
                                        OnP2PConnectAction.Remove(remote);
                                        action?.Invoke(remote, remote, null, false);
                                    }
                                }
                                lock (OnP2PWait)
                                {
                                    if (OnP2PWait.ContainsKey(remote))
                                    {
                                        OnP2PWait[remote].Stop();
                                        OnP2PWait.Remove(remote);
                                    }
                                }
                                break;
                            }
                    }
                    packet.CloseStream();
                }
                else
                {
                    lock (OnListenP2PClient)
                    {
                        if (OnListenP2PClient.ContainsKey((EndPoint)packet.peer))
                        {
                            OnListenP2PClient[ipe].Add(packet);
                            return;
                        }
                    }
                    if (msgid == PacketType.P2P_CONNECTION)
                    {
                        packet.BeginRead();
                        SendData sendData = packet.ReadSendData("");
                        IPEndPoint ip = TraceRoute.IPEndPointParse(sendData.Parameters.ToString(), AddressFamily.InterNetworkV6);
                        P2PToRealEndPoint.Add((EndPoint)packet.peer, ip);
                        P2PToNewEndPoint.Add(ip, (EndPoint)packet.peer);

                        packet.CloseRead();
                        packet.ResetPosition();

                        ListenP2PClient(packet);
                    }
                    else
                    {
                        P2PPacketdo(packet);
                    }
                }
            }
            #endregion
        }

        private string[] GetAllMyIP()
        {
            IPAddress[] ipAddresses;
            //ipAddresses = Array.FindAll(Dns.GetHostEntry(string.Empty).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
            ipAddresses = Array.FindAll(Dns.GetHostEntry(string.Empty).AddressList, a => (a.AddressFamily == AddressFamily.InterNetwork || (a.AddressFamily == AddressFamily.InterNetworkV6 && a.IsIPv6Unicast())));
            //ipAddresses = Dns.GetHostEntry(string.Empty).AddressList;
            string[] xx = new string[ipAddresses.Length];
            for (int i = 0; i < xx.Length; i++)
            {
                xx[i] = new IPEndPoint(ipAddresses[i].MapToIPv6(), ((IPEndPoint)LocalEndPoint).Port).ToString();
            }
            return xx;
        }

        Stopwatch checkerTime = new Stopwatch();

        public void SendCheck(EndPoint peer)
        {
            using (Packet packet = new Packet(peer))
            {
                packet.BeginWrite(PacketType.CHECK);
                Send(packet);
            }
        }

        public void SendP2PCheck(EndPoint peer)
        {
            if (P2PToPeer.ContainsKey(peer))
            {
                PeerForP2PBase peerbase = P2PToPeer[peer];
                using (Packet packet = new Packet(peer))
                {
                    packet.BeginWrite(PacketType.P2P_CHECKING);
                    if (peerbase.NAT)
                    {
                        P2PNATPacketSend(packet);
                    }
                    else
                    {
                        Send(packet);
                    }
                    Send(packet);
                }
            }
        }

        void LinkChecker()
        {
            checkerTime.Stop();
            checkerTime.Reset();
            checkerTime.Start();
            while (run)
            {
                Thread.Sleep(3000);
                if (!run) break;
                SendCheck(RemoteEndPoint);
                for (int i = 0; i < P2PSocketList.Count; i++)
                {
                    SendP2PCheck((EndPoint)P2PSocketList[i].socket);
                }
                lock (OnP2PWait)
                {
                    List<KeyValuePair<IPEndPoint, Stopwatch>> waitlist = new List<KeyValuePair<IPEndPoint, Stopwatch>>(OnP2PWait);
                    //DebugMessage("LinkChecker OnP2PWait: " + waitlist.Count + " " + OnP2PConnectAction.Count);
                    for (int i = 0; i < waitlist.Count; i++)
                    {
                        if(OnP2PConnect.Contains(waitlist[i].Key))
                        {
                            OnP2PWait[waitlist[i].Key].Stop();
                            OnP2PWait.Remove(waitlist[i].Key);
                        }
                        else
                        {
                            if(waitlist[i].Value.ElapsedMilliseconds > 3000)
                            {
                                OnP2PWait[waitlist[i].Key].Stop();
                                OnP2PWait.Remove(waitlist[i].Key);
                                var doing = OnP2PConnectAction[waitlist[i].Key];
                                OnP2PConnectAction.Remove(waitlist[i].Key);
                                doing(waitlist[i].Key, waitlist[i].Key, null, false);
                            }
                        }
                    }
                }
                if (checkerTime.ElapsedMilliseconds > 10000)
                {
                    lock (checklock)
                    {
                        if (!checkbool)
                        {
                            PushPacket(PacketType.CONNECTION_LOST, "10秒未回應");
                        }
                        checkbool = false;
                        for (int i = 0; i < P2PSocketList.Count; i++)
                        {
                            if (P2Pclientcheck.ContainsKey((EndPoint)P2PSocketList[i].socket))
                            {
                                if (!P2Pclientcheck[(EndPoint)P2PSocketList[i].socket])
                                {
                                    P2PPushPacket(PacketType.P2P_CONNECTION_LOST, "對等端10秒未回應", (EndPoint)P2PSocketList[i].socket);
                                }
                            }
                        }
                        for (int i = 0; i < P2PSocketList.Count; i++)
                        {
                            if (P2Pclientcheck.ContainsKey((EndPoint)P2PSocketList[i].socket))
                            {
                                P2Pclientcheck[(EndPoint)P2PSocketList[i].socket] = false;
                            }
                        }
                    }
                    checkerTime.Stop();
                    checkerTime.Reset();
                    checkerTime.Start();
                }
            }
        }

        byte[] Getbytes(int count)
        {
            byte[] data = new byte[count];
            Buffer.BlockCopy(m_Buffer, 0, data, 0, count);
            return data;
        }

        public void DebugMessage(string message)
        {
            GetMessage?.Invoke(message);
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

        public void PushPacket(PacketType msgid, string exception)
        {
            Packet packet = new Packet(RemoteEndPoint, null, exception);
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

        public void P2PPushPacket(PacketType msgid, string Key, object remote, object remotePublic, bool NAT)
        {
            Packet packet = new Packet(remote, null, new object[] { remotePublic, Key, NAT });
            packet.BeginWrite(msgid);
            packet.CloseWrite();
            packet.ResetPosition();
            PushPacket(packet);
        }

        public void P2PPushPacket(PacketType msgid, string exception, object remote)
        {
            Packet packet = new Packet(remote, null, exception);
            packet.BeginWrite(msgid);
            packet.CloseWrite();
            packet.ResetPosition();
            PushPacket(packet);
        }

        public bool Send(Packet bts)
        {
            if (SendLock != null)
            {
                lock (SendLock)
                {
                    try
                    {
                        //socket.SendTo(bts.Bytes, 0, bts.Length, SocketFlags.None, (EndPoint)bts.peer);
                        socket.BeginSendTo(bts.Bytes, 0, bts.Length, SocketFlags.None, (EndPoint)bts.peer, SendCallback, socket);
                        return true;
                    }
                    catch (System.Exception e)
                    {
                        if (bts.peer == RemoteEndPoint)
                        {
                            PushPacket(PacketType.CONNECTION_LOST, e.ToString());
                        }
                        return false;
                    }
                }
            }
            return false;
        }

        private void SendCallback(System.IAsyncResult ar)
        {
            //Socket ns = (Socket)ar.AsyncState;
            lock (SendLock)
            {
                try
                {
                    socket.EndSendTo(ar);
                }
                catch (SocketException e)
                {
                    PushPacket(PacketType.CONNECTION_LOST, "寄信回檔期間出問題：" + e.ToString());
                }
                catch(Exception e)
                {
                    DebugMessage("SendCallback:" + e.ToString());
                }
            }
        }

        private bool SendSetTTL(Packet bts, short ttl)
        {
            if (SendLock != null)
            {
                lock (SendLock)
                {
                    try
                    {
                        short dettl = socket.Ttl;
                        socket.Ttl = ttl;
                        bool ans = socket.SendTo(bts.Bytes, 0, bts.Length, SocketFlags.None, (EndPoint)bts.peer) != 0;
                        socket.Ttl = dettl;
                        return ans;
                        //ns.Write(bts.BYTES, 0, bts.Length);
                    }
                    catch (System.Exception e)
                    {
                        return false;
                    }
                }
            }
            return false;
        }
    }

    public class P2PException : Exception
    {
        public P2PException(string message) : base(message)
        {
        }
    }
}