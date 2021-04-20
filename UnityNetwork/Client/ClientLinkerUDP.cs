using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UnityNetwork.Client
{
    public class ClientLinkerUDP
    {
        public enum P2PCode
        {
            CallConnect = 0,
            TestCall,
            CallConnectComplete,
            ConnectCompleteWithNAT,
            NATP2PTell
        }

        NetUDPClient client;
        ClientListenUDP listener;


        bool getcheck = true;
        private int cantlink = 0;

        private NetworkManager networkManager;

        private Dictionary<PeerForP2P, bool> Link;

        string _key = "";

        private bool enableP2P = false;

        string MyPublicIPPort = "";

        Dictionary<string, Dictionary<string, object[]>> TestP2PCallConnectEnd = new Dictionary<string, Dictionary<string, object[]>>();
        Dictionary<string, List<string>> TestP2PCallConnectIPList = new Dictionary<string, List<string>>();

        List<IPEndPoint> OnP2PLink = new List<IPEndPoint>();

        public bool EnableP2P
        {
            get
            {
                return enableP2P;
            }
            set
            {
                enableP2P = value;
                if (client != null)
                {
                    client.enableP2P = enableP2P;
                }
            }
        }

        public string key
        {
            get
            {
                return _key;
            }
            private set
            {
                _key = value;
                client.key = value;
            }
        }

        public IPEndPoint MyIP
        {
            get
            {
                return client.MyIP;
            }
        }

        public System.Collections.ArrayList P2PSocketList
        {
            get
            {
                return networkManager._socketList;
            }
        }
        public Dictionary<string, object> ToPeerP2PIP
        {
            get
            {
                return networkManager.ToPeerUDPIP;
            }
        }
        public Dictionary<IPEndPoint, object> ToPeerP2P
        {
            get
            {
                return networkManager.ToPeerUDP;
            }
        }
        public int PacketSize
        {
            get
            {
                return networkManager.PacketSize;
            }
        }
        public int PacketCount
        {
            get
            {
                return networkManager.PacketCount;
            }
            set
            {
                networkManager.PacketCount = value;
            }
        }

        object locker = new object();

        List<string> SendKey = new List<string>();
        Dictionary<string, NetBitStream> Sendthing = new Dictionary<string, NetBitStream>();

        public ClientLinkerUDP(ClientListenUDP a)
        {
            listener = (ClientListenUDP)a;
        }


        public bool Connect(string ip, int port)
        {
            if (client != null)
            {
                client.Cheak -= OnCheck;
                client.GetMessage -= OnGetMessage;
            }
            client = null;
            networkManager = null;
            SpinWait.SpinUntil(() => getcheck);
            networkManager = new NetworkManager();
            Link = new Dictionary<PeerForP2P, bool>();
            client = new NetUDPClient(networkManager);
            client.enableP2P = enableP2P;
            client.Cheak += OnCheck;
            client.GetMessage += OnGetMessage;
            bool b = client.Connect(ip, port);
            return b;
        }

        private void OnGetMessage(string i)
        {
            if (listener != null)
            {
                listener.DebugReturn(i);
            }
        }

        private void OnCheck(ushort ID, NetBitStream stream)
        {
            if (ID >= (ushort)MessageIdentifiers.ID.CHECKING && ID <= (ushort)MessageIdentifiers.ID.P2P_SERVER_CALL)
            {
                lock (locker)
                {
                    getcheck = true;
                }
                if (ID == (ushort)MessageIdentifiers.ID.CHECKING)
                {
                    NetPacket packet = new NetPacket(stream.BYTES.Length);
                    stream.BYTES.CopyTo(packet._bytes, 0);

                    NetBitStream stream2 = new NetBitStream();
                    stream2.BeginReadUDP2(packet);

                    stream2.ReadResponse2("");
                    stream2.EncodeHeader();
                    check((int[])stream2.thing.Parameters[0]);
                }
            }
        }

        public void Disconnect()
        {
            try
            {
                NetBitStream stream = new NetBitStream();
                Response b = new Response();
                b.DebugMessage = "主動斷線";
                stream.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                stream.WriteResponse2(b, "");
                stream.EncodeHeader();
                client.Send(stream);
            }
            catch (Exception)
            {

            }

            if (enableP2P)
            {
                for (int i = 0; i < networkManager._socketList.Count; i++)
                {
                    ((PeerForP2P)networkManager._socketList[i]).OffLine();
                }
            }

            if (listener != null)
            {
                listener.DebugReturn("Disconnect");
            }
            client.Cheak -= OnCheck;
            client.GetMessage -= OnGetMessage;
            client.Disconnect(0);
            client = null;
            Link.Clear();
            Link = null;
            networkManager.Clear();
            networkManager = null;
            listener = null;
            key = "";
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

        public void ask(byte Code, Dictionary<byte, Object> Parameter, bool _Lock = true)
        {
            string sendkey = SetSendKey();
            ThreadPool.QueueUserWorkItem((aa) =>
            {
                if (key != "")
                {
                    try
                    {
                        NetBitStream stream = new NetBitStream();
                        Response b = new Response(Code, Parameter);
                        stream.BeginWrite((ushort)MessageIdentifiers.ID.ID_CHAT);
                        stream.WriteResponse2(b, key, _Lock);
                        stream.EncodeHeader();
                        lock (SendKey)
                        {
                            Sendthing.Add(sendkey, stream);
                            while (SendKey.Count != 0)
                            {
                                if (Sendthing.ContainsKey(SendKey[0]))
                                {
                                    client.Send(Sendthing[SendKey[0]]);
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
                        OnGetMessage(e.ToString());
                        if (client != null)
                        {
                            cantlink++;
                            if (cantlink > 50)
                            {
                                client.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message);
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

        private string[] GetAllMyIP()
        {
            IPAddress[] ipv4Addresses;
            ipv4Addresses = Array.FindAll(Dns.GetHostEntry(string.Empty).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
            string[] xx = new string[ipv4Addresses.Length];
            for (int i = 0; i < xx.Length; i++)
            {
                xx[i] = new IPEndPoint(ipv4Addresses[i], MyIP.Port).ToString();
            }
            return xx;
        }

        public void StartP2PConnect(IPEndPoint IPPort)
        {
            if (enableP2P)
            {
                if (!OnP2PLink.Contains(IPPort))
                {
                    client.P2PConnectServer(new Response((byte)P2PCode.CallConnect, new Dictionary<byte, Object>() { { 0, IPPort.ToString() }, { 1, GetAllMyIP() }, { 3, true } }));
                }
            }
            else
            {
                throw new Exception("P2P mode is not enable.");
            }
        }

        private void P2PConnecting(string IPPort, Action callback)
        {
            try
            {
                NetBitStream stream = new NetBitStream();
                Response b = new Response(0, new Dictionary<byte, object>() { { 0, MyPublicIPPort } });
                stream.BeginWrite((ushort)MessageIdentifiers.ID.P2P_CONNECTION);
                stream.WriteResponse2(b, key, false);
                stream.EncodeHeader();
                if (!client.Send(stream, TraceRoute.IPEndPointParse(IPPort)))
                {
                    callback();
                }
                else
                {
                    new Thread(() =>
                    {
                        Thread.Sleep(500);
                        //listener.DebugReturn(IPPort.ToString());
                        if (!ToPeerP2PIP.ContainsKey(IPPort))
                        {
                            callback();
                        }

                    }).Start();
                }
            }
            catch (Exception)
            {
                callback();
            }
        }

        public void NotImportask(byte Code, Dictionary<byte, Object> Parameter, bool _Lock = true)
        {
            ThreadPool.QueueUserWorkItem((aa) =>
            {
                if (key != "")
                {
                    try
                    {
                        NetBitStream stream = new NetBitStream();
                        Response b = new Response(Code, Parameter);
                        stream.BeginWrite((ushort)MessageIdentifiers.ID.ID_CHAT);
                        stream.WriteResponse2(b, key, _Lock);
                        stream.EncodeHeader();
                        client.Send(stream);
                        cantlink = 0;
                    }
                    catch (Exception e)
                    {
                        OnGetMessage(e.ToString());
                        if (client != null)
                        {
                            cantlink++;
                            if (cantlink > 50)
                            {
                                client.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message);
                            }
                        }
                    }
                }
            });
        }

        private void CheckKey()
        {
            if (key != "")
            {
                try
                {
                    NetBitStream stream = new NetBitStream();
                    Response b = new Response(0, new Dictionary<byte, object>() { { 0, (new Random(Guid.NewGuid().GetHashCode())).Next(0, 10000) } });
                    stream.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                    stream.WriteResponse2(b, key, true);
                    stream.EncodeHeader();
                    client.Send(stream);
                }
                catch (Exception e)
                {
                    client.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message);
                }
            }
        }

        private void check(int[] ints)
        {
            try
            {
                NetBitStream stream = new NetBitStream();
                stream.BeginWrite((ushort)MessageIdentifiers.ID.CHECKING);
                stream.EncodeHeader();
                client.Send(stream);
                for (int i = 0; i < ints.Length; i++)
                {
                    client.SetAuxiliaryServer(stream, ints[i]);
                }
                cantlink = 0;
            }
            catch (Exception e)
            {
                if (client != null)
                {
                    cantlink++;
                    if (cantlink > 50)
                    {
                        client.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message);
                    }
                }
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

            #region 判斷自己或對方是否只有PublicIP
            bool onlyPublicIP = true;
            if (IPs.Length > 0)
            {
                for (int i = 0; i < MyIPs.Length; i++)
                {
                    IPEndPoint endPoint = TraceRoute.IPEndPointParse(MyIPs[i]);
                    if (checkIP(endPoint.Address, new string[] { "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" }))
                    {
                        onlyPublicIP = false;
                        break;
                    }
                }
            }
            #endregion

            #region 新增TestP2PCallConnect要連線的IP紀錄
            TestP2PCallConnectEnd.Add(publicIP.ToString(), new Dictionary<string, object[]>());
            TestP2PCallConnectIPList.Add(publicIP.ToString(), new List<string>());
            #endregion

            #region 非同步測試函式組
            Func<Socket, IPEndPoint, short, int, KeyValuePair<string, object[]>> runtest = (socket, ip, ttl, port) =>
            {
                short dettl = socket.Ttl;
                socket.Ttl = ttl;
                byte[] data = new byte[6];
                socket.SendTo(data, ip);
                socket.Ttl = dettl;

                EndPoint retip = new IPEndPoint(IPAddress.Any, 0);
                socket.ReceiveTimeout = 2000;

                bool isprivate = checkIP(ip.Address, new string[] { "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" });

                client.P2PConnectServer(new Response((byte)P2PCode.TestCall, new Dictionary<byte, object>() { { 0, publicIP.ToString() }, { 1, isprivate ? MyIPs : new string[] { MyPublicIPPort } }, { 3, port } }));

                KeyValuePair<string, object[]> ans;
                try
                {
                    data = new byte[1024];
                    int len = socket.ReceiveFrom(data, ref retip);
                    byte[] outdata = new byte[len];
                    Array.Copy(data, outdata, len);
                    NetBitStream stream = new NetBitStream();
                    stream.BYTES = outdata;
                    stream.ReadUShort(out ushort num);
                    stream.ReadResponse2("");
                    ans = new KeyValuePair<string, object[]>(socket.LocalEndPoint.ToString(), new object[] { ip, ttl, true, stream.thing.Parameters[0] });
                }
                catch (SocketException)
                {
                    ans = new KeyValuePair<string, object[]>(socket.LocalEndPoint.ToString(), new object[] { ip, ttl, false });
                }
                /*(new Thread(() =>
                {
                    listener.DebugReturn("Testing");
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    while(stopwatch.ElapsedMilliseconds < 5000)
                    {
                        try
                        {
                            socket.ReceiveFrom(data, ref retip);
                            listener.DebugReturn("Has data " + retip.ToString());
                        }
                        catch (Exception)
                        {

                        }
                    }
                    stopwatch.Stop();
                    stopwatch.Reset();
                    listener.DebugReturn("Testing End");*/
                    socket.Close();
                //})).Start();
                return ans;
            };

            void callback(IAsyncResult ar)
            {
                object state = ar.AsyncState;
                KeyValuePair<string, object[]> ans = runtest.EndInvoke(ar);
                lock (TestP2PCallConnectEnd)
                {
                    TestP2PCallConnectEnd[publicIP.ToString()].Add(ans.Key, ans.Value);
                }
            }
            #endregion

            #region 雙方皆有私網IP時的測試
            if (!onlyPublicIP)
            {
                foreach (string ip in IPs)
                {
                    IPEndPoint endPoint = TraceRoute.IPEndPointParse(ip);
                    if (!checkIP(endPoint.Address, new string[] { "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" })) continue;

                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.Bind(new IPEndPoint(IPAddress.Any, 0));
                    TestP2PCallConnectIPList[publicIP.ToString()].Add(socket.LocalEndPoint.ToString());
                    runtest.BeginInvoke(socket, endPoint, 64, ((IPEndPoint)socket.LocalEndPoint).Port, callback, null);
                }
            }
            #endregion

            #region 公網IP與TTL測試

            void testpublicip(short ttl)
            {
                //listener.DebugReturn(publicenumer[i].ToString());
                #region 建立測試用Socket
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(new IPEndPoint(IPAddress.Any, 0));
                TestP2PCallConnectIPList[publicIP.ToString()].Add(socket.LocalEndPoint.ToString());
                #endregion

                #region 取得外網對內PORT
                NetBitStream netBitStream = new NetBitStream();
                netBitStream.BeginWrite((ushort)MessageIdentifiers.ID.P2P_GET_PUBLIC_ENDPOINT);
                netBitStream.EncodeHeader();
                socket.SendTo(netBitStream.BYTES, client.RemoteIP);
                socket.ReceiveTimeout = 500;
                try
                {
                    byte[] data = new byte[1024];
                    EndPoint retip = new IPEndPoint(IPAddress.Any, 0);
                    int len = socket.ReceiveFrom(data, ref retip);
                    byte[] outdata = new byte[len];
                    Array.Copy(data, outdata, len);
                    NetBitStream stream = new NetBitStream();
                    stream.BYTES = outdata;
                    stream.ReadUShort(out ushort num);
                    stream.ReadResponse2("");
                    IPEndPoint GetIPPORT = TraceRoute.IPEndPointParse(stream.thing.Parameters[0].ToString());
                    #endregion

                    runtest.BeginInvoke(socket, publicIP, ttl, GetIPPORT.Port, callback, null);

                #region 取得失敗直接結束
                }
                catch (SocketException)
                {
                    TestP2PCallConnectEnd[publicIP.ToString()].Add(socket.LocalEndPoint.ToString(), new object[] { publicIP, ttl, false });
                }
                #endregion
            }
            List<IPAddress> publicenumer = TraceRoute.GetTraceRoute(publicIP.Address.ToString(), 50);
            for (int i = 0; i < publicenumer.Count; i++)
            {
                if (publicenumer[i] != null && !checkIP(publicenumer[i], new string[] { "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" }))
                {
                    testpublicip((short)(i + 1));
                }
            }
            testpublicip(64);
            #endregion

            #region 等全部測試完成後輸出測試結果
            Thread.Sleep(500);

            bool canlink = false;
            string linkip = "";
            for (int i = 0; i < TestP2PCallConnectIPList[publicIP.ToString()].Count; i++)
            {
                SpinWait.SpinUntil(() => TestP2PCallConnectEnd[publicIP.ToString()].ContainsKey(TestP2PCallConnectIPList[publicIP.ToString()][i]));
                lock (TestP2PCallConnectEnd)
                {
                    if (!canlink && (bool)TestP2PCallConnectEnd[publicIP.ToString()][TestP2PCallConnectIPList[publicIP.ToString()][i]][2])
                    {
                        canlink = true;
                        linkip = TestP2PCallConnectIPList[publicIP.ToString()][i];
                    }
                }
            }
            object[] ansdata = null;
            lock (TestP2PCallConnectEnd)
            {
                if (canlink)
                {
                    ansdata = TestP2PCallConnectEnd[publicIP.ToString()][linkip];
                }

                TestP2PCallConnectEnd.Remove(publicIP.ToString());
                TestP2PCallConnectIPList.Remove(publicIP.ToString());
            }
            return ansdata;
            #endregion
        }

        public void Update()
        {
            NetPacket packet = null;
            for (packet = networkManager.GetPacket(); packet != null; packet = networkManager.GetPacket())
            {
                try
                {
                    // 獲得訊息ID
                    ushort msgid = 0;
                    packet.TOID(out msgid);

                    switch (msgid)
                    {
                        case (ushort)MessageIdentifiers.ID.CONNECTION_REQUEST_ACCEPTED:
                            {
                                Thread b = new Thread(new ThreadStart(check));
                                b.Start();
                                Thread a = new Thread(new ThreadStart(check2));
                                a.Start();
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.CONNECTION_LOST:
                            {
                                if (packet._error == "")
                                {
                                    NetBitStream stream = new NetBitStream();
                                    stream.BeginReadUDP2(packet);
                                    stream.ReadResponse2("");
                                    stream.EncodeHeader();
                                    packet._error = stream.thing.DebugMessage;
                                }
                                if (listener != null)
                                {
                                    listener.DebugReturn(packet._error);
                                    listener.OnStatusChanged((LinkCobe)1);
                                }
                                Disconnect();
                                key = "";
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.ID_CHAT:
                            {
                                NetBitStream stream = new NetBitStream();
                                stream.BeginReadUDP2(packet);
                                if (listener != null)
                                {
                                    listener.OnOperationResponse(packet.response);
                                }
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.ID_CHAT2:
                            {
                                NetBitStream stream = new NetBitStream();
                                stream.BeginReadUDP2(packet);
                                if (listener != null)
                                {
                                    listener.OnEvent(packet.response);
                                }
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.CONNECTION_ATTEMPT_FAILED:
                            {
                                if (listener != null)
                                {
                                    listener.DebugReturn(packet._error);
                                    listener.OnStatusChanged((LinkCobe)2);
                                }
                                Disconnect();
                                key = "";
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.LOADING_NOW:
                            {
                                if (listener != null)
                                {
                                    listener.OnStatusChanged((LinkCobe)3);
                                    listener.Loading(packet._error);
                                }
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.CHECKING:
                            {
                                lock (locker)
                                {
                                    getcheck = true;
                                }
                                NetBitStream stream = new NetBitStream();
                                stream.BeginReadUDP2(packet);
                                stream.ReadResponse2("");
                                stream.EncodeHeader();
                                check((int[])(stream.thing.Parameters[0]));
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.KEY:
                            {
                                NetBitStream stream = new NetBitStream();
                                stream.BeginReadUDP2(packet);
                                stream.ReadResponse2("");
                                stream.EncodeHeader();
                                if (stream.thing.Code == 0)
                                {
                                    key = stream.thing.DebugMessage;
                                    CheckKey();
                                }
                                else if (stream.thing.Code == 1)
                                {
                                    if (listener != null)
                                    {
                                        listener.OnStatusChanged((LinkCobe)0);
                                    }
                                }
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.P2P_SERVER_CALL:
                            {
                                const byte RemotePublicIP = 0;
                                const byte Data = 1;
                                const byte LocalPublicIP = 2;

                                switch ((P2PCode)packet.response.Code)
                                {
                                    case P2PCode.CallConnect:
                                        {
                                            MyPublicIPPort = packet.response.Parameters[LocalPublicIP].ToString();

                                            IPEndPoint iPEndPoint = TraceRoute.IPEndPointParse(packet.response.Parameters[RemotePublicIP].ToString());

                                            if (!OnP2PLink.Contains(iPEndPoint))
                                            {
                                                OnP2PLink.Add(iPEndPoint);
                                            }

                                            if (!client.P2PAllowList.Contains(iPEndPoint))
                                            {
                                                client.P2PAllowList.Add(iPEndPoint);
                                            }

                                            string[] IPs = (string[])packet.response.Parameters[Data];
                                            Func<string[], IPEndPoint, object[]> testfunc = TestP2PCallConnect;
                                            testfunc.BeginInvoke(IPs, iPEndPoint, (ar) =>
                                            {
                                                NetPacket _packet = (NetPacket)ar.AsyncState;
                                                object[] ans = testfunc.EndInvoke(ar);
                                                if(ans != null)
                                                {
                                                    NetBitStream stream = new NetBitStream();
                                                    stream.BeginWrite((ushort)MessageIdentifiers.ID.P2P_CHECKING);
                                                    stream.EncodeHeader();
                                                    client.SendSetTTL(stream, (IPEndPoint)ans[0], (short)ans[1]);
                                                    client.P2PConnectServer(new Response((byte)P2PCode.CallConnectComplete, new Dictionary<byte, object>() { { RemotePublicIP, iPEndPoint.ToString() }, { Data, ans[3] } }));
                                                }
                                                else if((bool)_packet.response.Parameters[3])
                                                {
                                                    client.P2PConnectServer(new Response((byte)P2PCode.CallConnect, new Dictionary<byte, object>() { { RemotePublicIP, iPEndPoint.ToString() }, { Data, GetAllMyIP() }, { 3, false } }));
                                                }
                                                else
                                                {
                                                    client.P2PConnectServer(new Response((byte)P2PCode.ConnectCompleteWithNAT, new Dictionary<byte, object>() { { RemotePublicIP, iPEndPoint.ToString() } }));
                                                    PeerForP2P peerForP2P = listener.P2PAddPeer(iPEndPoint, iPEndPoint, client, false);
                                                    networkManager.ToPeerUDP.Add(iPEndPoint, peerForP2P);
                                                    networkManager.ToPeerUDPIP.Add(iPEndPoint.ToString(), peerForP2P);
                                                    networkManager._socketList.Add(peerForP2P);
                                                    Link.Add(peerForP2P, true);
                                                    peerForP2P.OnLink();
                                                }
                                            }, packet);
                                            break;
                                        }
                                    case P2PCode.TestCall:
                                        {
                                            MyPublicIPPort = packet.response.Parameters[LocalPublicIP].ToString(); 
                                            IPEndPoint iPEndPoint = TraceRoute.IPEndPointParse(packet.response.Parameters[RemotePublicIP].ToString());

                                            if (!OnP2PLink.Contains(iPEndPoint))
                                            {
                                                OnP2PLink.Add(iPEndPoint);
                                            }

                                            if (!client.P2PAllowList.Contains(iPEndPoint))
                                            {
                                                client.P2PAllowList.Add(iPEndPoint);
                                            }
                                            string[] IPs = (string[])packet.response.Parameters[Data];

                                            void sendtest(string ip)
                                            {
                                                NetBitStream stream = new NetBitStream();
                                                stream.BeginWrite((ushort)MessageIdentifiers.ID.P2P_CHECKING);
                                                Response response = new Response(0, new Dictionary<byte, object>() { { 0, ip } });
                                                stream.WriteResponse2(response, key, false);
                                                stream.EncodeHeader();
                                                client.Send(stream, TraceRoute.IPEndPointParse(TraceRoute.IPEndPointParse(ip).Address.ToString() + ":" + packet.response.Parameters[3].ToString()));
                                            }

                                            if (IPs.Length == 0)
                                            {
                                                sendtest(iPEndPoint.ToString());
                                            }
                                            else
                                            {
                                                for (int i = 0; i < IPs.Length; i++)
                                                {
                                                    sendtest(IPs[i]);
                                                    //listener.DebugReturn("Send to : " + TraceRoute.IPEndPointParse(TraceRoute.IPEndPointParse(IPs[i]).Address.ToString() + ":" + packet.response.Parameters[3].ToString()));
                                                }
                                                //check(new int[0]);
                                            }
                                            break;
                                        }
                                    case P2PCode.CallConnectComplete:
                                        {
                                            MyPublicIPPort = packet.response.Parameters[LocalPublicIP].ToString();
                                            IPEndPoint iPEndPoint = TraceRoute.IPEndPointParse(packet.response.Parameters[RemotePublicIP].ToString());
                                            string ip = packet.response.Parameters[Data].ToString();

                                            P2PConnecting(ip, () =>
                                            {
                                                client.P2PConnectServer(new Response((byte)P2PCode.ConnectCompleteWithNAT, new Dictionary<byte, object>() { { RemotePublicIP, iPEndPoint.ToString() } }));
                                                PeerForP2P peerForP2P = listener.P2PAddPeer(iPEndPoint, iPEndPoint, client, false);
                                                networkManager.ToPeerUDP.Add(iPEndPoint, peerForP2P);
                                                networkManager.ToPeerUDPIP.Add(iPEndPoint.ToString(), peerForP2P);
                                                networkManager._socketList.Add(peerForP2P);
                                                Link.Add(peerForP2P, true);
                                                peerForP2P.OnLink();
                                            });
                                            break;
                                        }
                                    case P2PCode.ConnectCompleteWithNAT:
                                        {
                                            IPEndPoint iPEndPoint = TraceRoute.IPEndPointParse(packet.response.Parameters[RemotePublicIP].ToString());
                                            PeerForP2P peerForP2P = listener.P2PAddPeer(iPEndPoint, iPEndPoint, client, false);
                                            networkManager.ToPeerUDP.Add(iPEndPoint, peerForP2P);
                                            networkManager.ToPeerUDPIP.Add(packet.response.Parameters[RemotePublicIP].ToString(), peerForP2P);
                                            networkManager._socketList.Add(peerForP2P);
                                            Link.Add(peerForP2P, true);
                                            peerForP2P.OnLink();
                                            break;
                                        }
                                    case P2PCode.NATP2PTell:
                                        {
                                            IPEndPoint iPEndPoint = TraceRoute.IPEndPointParse(packet.response.Parameters[RemotePublicIP].ToString());
                                            NetBitStream stream = new NetBitStream();
                                            stream.BYTES = (byte[])packet.response.Parameters[Data];
                                            stream._socketUDP = iPEndPoint;
                                            stream.DecodeHeader();
                                            client.PushPacket2(stream);
                                            break;
                                        }
                                }
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.P2P_CONNECTION:
                            {
                                IPEndPoint iPEndPoint = TraceRoute.IPEndPointParse(packet.response.Parameters[0].ToString());
                                if (iPEndPoint != packet._peerUDP)
                                {
                                    client.P2PAllowList.Remove(iPEndPoint);
                                    client.P2PAllowList.Add(packet._peerUDP);
                                }
                                PeerForP2P peerForP2P = listener.P2PAddPeer(packet._peerUDP, iPEndPoint, client, true);
                                networkManager.ToPeerUDP.Add(packet._peerUDP, peerForP2P);
                                networkManager.ToPeerUDPIP.Add(packet._peerUDP.ToString(), peerForP2P);
                                networkManager._socketList.Add(peerForP2P);
                                Link.Add(peerForP2P, true);
                                peerForP2P.OnLink();

                                if (packet.response.Code == 0)
                                {
                                    NetBitStream stream = new NetBitStream();
                                    Response b = new Response(1, new Dictionary<byte, object>() { { 0, MyPublicIPPort } });
                                    stream.BeginWrite((ushort)MessageIdentifiers.ID.P2P_CONNECTION);
                                    stream.WriteResponse2(b, key, false);
                                    stream.EncodeHeader();
                                    client.Send(stream, packet._peerUDP);
                                }
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.P2P_CHECKING:
                            {
                                lock (Link)
                                {
                                    if (networkManager.ToPeerUDP.ContainsKey(packet._peerUDP))
                                    {
                                        if (Link.ContainsKey((PeerForP2P)networkManager.ToPeerUDP[packet._peerUDP]))
                                        {
                                            //listener.DebugReturn(packet._peerUDP + ", check");
                                            Link[(PeerForP2P)networkManager.ToPeerUDP[packet._peerUDP]] = true;
                                        }
                                    }
                                }
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.P2P_ID_CHAT:
                            {
                                //listener.DebugReturn(packet.response.Code + " " + packet.response.Parameters[0].ToString());
                                try
                                {
                                    if (networkManager.ToPeerUDP.ContainsKey(packet._peerUDP))
                                    {
                                        ((PeerForP2P)networkManager.ToPeerUDP[packet._peerUDP]).OnOperationRequest(packet.response);
                                    }
                                }
                                catch (Exception e)
                                {
                                    listener.DebugReturn(((PeerForP2P)networkManager.ToPeerUDP[packet._peerUDP]).socket.ToString() + " " + e.Message + "From OnOperationRequest");
                                }
                                lock (Link)
                                {
                                    if (networkManager.ToPeerUDP.ContainsKey(packet._peerUDP))
                                    {
                                        if (Link.ContainsKey((PeerForP2P)networkManager.ToPeerUDP[packet._peerUDP]))
                                        {
                                            Link[(PeerForP2P)networkManager.ToPeerUDP[packet._peerUDP]] = true;
                                        }
                                    }
                                }
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.P2P_LOST:
                            {
                                PeerForP2P peerForP2P = (PeerForP2P)networkManager.ToPeerUDP[packet._peerUDP];
                                if (packet._error == "")
                                {
                                    NetBitStream stream = new NetBitStream();
                                    stream.BeginReadUDP2(packet);
                                    stream.ReadResponse2("");
                                    stream.EncodeHeader();
                                    packet._error = stream.thing.DebugMessage;
                                }
                                listener.DebugReturn(peerForP2P.socket.ToString() + "+" + packet._error);
                                if (client != null && peerForP2P.socket != null)
                                {
                                    NetBitStream stream2 = new NetBitStream();
                                    Response b = new Response();
                                    b.DebugMessage = "伺服器端主動關閉";
                                    listener.DebugReturn("Disconnect on 伺服器端主動關閉");
                                    stream2.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                                    stream2.WriteResponse2(b, "");
                                    stream2.EncodeHeader();
                                    this.client.Send(stream2, peerForP2P.socket);
                                }
                                try
                                {
                                    peerForP2P.OnDisconnect();
                                }
                                catch (Exception e)
                                {
                                    listener.DebugReturn(peerForP2P.socket.ToString() + " " + e.Message + "From OnDisconnect");
                                }
                                if (networkManager._socketList.Contains(peerForP2P))
                                {
                                    networkManager._socketList.Remove(peerForP2P);
                                }
                                lock (Link)
                                {
                                    Link.Remove(peerForP2P);
                                }
                                ToPeerP2P.Remove(packet._peerUDP);
                                ToPeerP2PIP.Remove(packet._peerUDP.ToString());
                                client.P2PAllowList.Remove(packet._peerUDP);
                                OnP2PLink.Remove(peerForP2P.PublicIP);
                                peerForP2P.Close();
                                peerForP2P = null;
                                break;
                            }
                        default:
                            {
                                // 錯誤
                                break;
                            }
                    }
                }
                catch (Exception e)
                {
                    if (listener != null)
                    {
                        listener.DebugReturn(e.ToString());
                    }
                }
                packet = null;

            }// end fore
        }

        private void Doing(object thing)
        {
            Response response = (Response)thing;
            if (listener != null)
            {
                switch (response.Code)
                {
                    case 1:
                        {
                            listener.OnStatusChanged((LinkCobe)response.ReturnCode);
                            break;
                        }
                    case 2:
                        {
                            if (key != "")
                            {
                                listener.OnOperationResponse((Response)response.Parameters[0]);
                            }
                            break;
                        }
                    case 3:
                        {
                            if (key != "")
                            {
                                listener.OnEvent((Response)response.Parameters[0]);
                            }
                            break;
                        }
                }
            }
        }

        protected void check()
        {
            try
            {
                while (client != null)
                {
                    if (networkManager._socketList != null)
                    {
                        for (int i = 0; i < networkManager._socketList.Count; i++)
                        {
                            if (((PeerForP2P)networkManager._socketList[i]) != null)
                            {
                                ((PeerForP2P)networkManager._socketList[i]).check();
                            }
                        }
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception e)
            {
                listener.DebugReturn(e.Message + " from UDPAppllicationCheck");
            }
        }

        private void check2()
        {
            while (getcheck)
            {
                lock (locker)
                {
                    getcheck = false;
                }
                if (Link != null)
                {
                    lock (Link)
                    {
                        foreach (PeerForP2P peerForP2P in networkManager._socketList)
                        {
                            if (Link.ContainsKey(peerForP2P))
                            {
                                Link[peerForP2P] = false;
                            }
                        }
                    }
                }
                Thread.Sleep(5000);
                if (networkManager != null)
                {
                    for (int i = 0; i < networkManager._socketList.Count; i++)
                    {
                        lock (Link)
                        {
                            if (Link.ContainsKey((PeerForP2P)networkManager._socketList[i]))
                            {
                                if (!Link[(PeerForP2P)networkManager._socketList[i]])
                                {
                                    client.P2PPushPacket((ushort)MessageIdentifiers.ID.P2P_LOST, "超過5秒沒有回應", ((PeerForP2P)networkManager._socketList[i]).socket);
                                }
                            }
                            else
                            {
                                client.P2PPushPacket((ushort)MessageIdentifiers.ID.P2P_LOST, "超過5秒沒有回應", ((PeerForP2P)networkManager._socketList[i]).socket);
                            }
                        }
                    }
                }
            }
            if (networkManager != null)
            {
                for (int i = 0; i < networkManager._socketList.Count; i++)
                {
                    lock (Link)
                    {
                        client.P2PPushPacket((ushort)MessageIdentifiers.ID.P2P_LOST, "超過5秒沒有回應", ((PeerForP2P)networkManager._socketList[i]).socket);
                    }
                }
            }
            if (client != null)
            {
                client.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "超過5秒沒有回應");
            }
            getcheck = true;
        }
    }
}