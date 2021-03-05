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
        NetUDPClient client;
        ClientListenUDP listener;


        bool getcheck = true;
        private int cantlink = 0;

        private NetworkManager networkManager;

        private Dictionary<PeerForP2P, bool> Link;

        string _key = "";

        private bool enableP2P = false;

        string MyIPPort = "";

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

        public void StartP2PConnect(IPEndPoint IPPort)
        {
            if (enableP2P)
            {
                IPAddress[] ipv4Addresses;
                ipv4Addresses = Array.FindAll(Dns.GetHostEntry(string.Empty).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
                string[] xx = new string[ipv4Addresses.Length];
                for(int i = 0; i < xx.Length; i++)
                {
                    xx[i] = ipv4Addresses[i].ToString() + ":" + MyIP.ToString().Split(':')[1];
                }
                client.P2PConnectServer(new Response(0, new Dictionary<byte, Object>() { { 0, IPPort.ToString() }, { 1, xx } }));
            }
            else
            {
                throw new Exception("P2P mode is not enable.");
            }
        }

        public void P2PConnecting(string IPPort, Action callback)
        {
            try
            {
                NetBitStream stream = new NetBitStream();
                Response b = new Response(0, new Dictionary<byte, Object>() { { 0, MyIPPort } });
                stream.BeginWrite((ushort)MessageIdentifiers.ID.P2P_CONNECTION);
                stream.WriteResponse2(b, key, false);
                stream.EncodeHeader();
                if (!client.Send(stream, new IPEndPoint(IPAddress.Parse(IPPort.Split(':')[0]), Convert.ToInt32(IPPort.Split(':')[1]))))
                {
                    callback();
                }
                else
                {
                    new Thread(() =>
                    {
                        Thread.Sleep(500);
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
                                switch (packet.response.Code)
                                {
                                    case 0:
                                        {
                                            MyIPPort = packet.response.Parameters[2].ToString();

                                            IPAddress[] ipv4Addresses;
                                            ipv4Addresses = Array.FindAll(Dns.GetHostEntry(string.Empty).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
                                            string[] xx = new string[ipv4Addresses.Length];
                                            for (int i = 0; i < xx.Length; i++)
                                            {
                                                xx[i] = ipv4Addresses[i].ToString() + ":" + MyIP.ToString().Split(':')[1];
                                            }

                                            NetBitStream stream = new NetBitStream();
                                            stream.BeginWrite((ushort)MessageIdentifiers.ID.P2P_CHECKING);
                                            stream.EncodeHeader();

                                            string[] IP = packet.response.Parameters[0].ToString().Split(':');
                                            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(IP[0]), Convert.ToInt32(IP[1]));
                                            client.P2PAllowList.Add(iPEndPoint);

                                            string[] IPs = (string[])packet.response.Parameters[1];
                                            foreach (string ip in IPs)
                                            {
                                                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip.Split(':')[0]), Convert.ToInt32(ip.Split(':')[1]));
                                                client.Send(stream, endPoint);
                                            }
                                            client.Send(stream, iPEndPoint);

                                            packet.response.Parameters.Remove(2);
                                            packet.response.Parameters[1] = xx;

                                            client.P2PConnectServer(new Response(1, packet.response.Parameters));
                                            break;
                                        }
                                    case 1:
                                        {

                                            MyIPPort = packet.response.Parameters[2].ToString();
                                            string a = packet.response.Parameters[0].ToString();

                                            IPAddress[] ipv4Addresses;
                                            ipv4Addresses = Array.FindAll(Dns.GetHostEntry(string.Empty).AddressList, aa => aa.AddressFamily == AddressFamily.InterNetwork);
                                            string[] xx = new string[ipv4Addresses.Length];
                                            for (int i = 0; i < xx.Length; i++)
                                            {
                                                xx[i] = ipv4Addresses[i].ToString() + ":" + MyIP.ToString().Split(':')[1];
                                            }

                                            NetBitStream stream = new NetBitStream();
                                            stream.BeginWrite((ushort)MessageIdentifiers.ID.P2P_CHECKING);
                                            stream.EncodeHeader();

                                            string[] IP = packet.response.Parameters[0].ToString().Split(':');
                                            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(IP[0]), Convert.ToInt32(IP[1]));
                                            client.P2PAllowList.Add(iPEndPoint);

                                            string[] IPs = (string[])packet.response.Parameters[1];
                                            void doing(int i)
                                            {
                                                P2PConnecting(IPs[i], () =>
                                                {
                                                    i++;
                                                    if (i < IPs.Length)
                                                    {
                                                        doing(i);
                                                    }
                                                    else
                                                    {
                                                        P2PConnecting(a, () =>
                                                        {
                                                            client.P2PConnectServer(new Response(2, new Dictionary<byte, object>() { { 0, a }, { 1, xx } }));
                                                        });
                                                    }
                                                });
                                            }

                                            foreach (string ip in IPs)
                                            {
                                                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip.Split(':')[0]), Convert.ToInt32(ip.Split(':')[1]));
                                                client.Send(stream, endPoint);
                                            }
                                            client.Send(stream, iPEndPoint);
                                            doing(0);
                                            break;
                                        }
                                    case 2:
                                        {
                                            string[] IPs = (string[])packet.response.Parameters[1];
                                            MyIPPort = packet.response.Parameters[2].ToString();
                                            string a = packet.response.Parameters[0].ToString();

                                            void doing(int i)
                                            {
                                                P2PConnecting(IPs[i], () =>
                                                {
                                                    i++;
                                                    if (i < IPs.Length)
                                                    {
                                                        doing(i);
                                                    }
                                                    else
                                                    {
                                                        P2PConnecting(a, () =>
                                                        {
                                                            client.P2PConnectServer(new Response(3, new Dictionary<byte, object>() { { 0, a } }));
                                                            string[] IP = a.Split(':');
                                                            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(IP[0]), Convert.ToInt32(IP[1]));
                                                            PeerForP2P peerForP2P = listener.P2PAddPeer(iPEndPoint, client, false);
                                                            networkManager.ToPeerUDP.Add(iPEndPoint, peerForP2P);
                                                            networkManager.ToPeerUDPIP.Add(a, peerForP2P);
                                                            networkManager._socketList.Add(peerForP2P);
                                                            Link.Add(peerForP2P, true);
                                                            peerForP2P.OnLink();
                                                        });
                                                    }
                                                });
                                            }
                                            doing(0);
                                            break;
                                        }
                                    case 3:
                                        {
                                            string[] IP = packet.response.Parameters[0].ToString().Split(':');
                                            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(IP[0]), Convert.ToInt32(IP[1]));
                                            PeerForP2P peerForP2P = listener.P2PAddPeer(iPEndPoint, client, false);
                                            networkManager.ToPeerUDP.Add(iPEndPoint, peerForP2P);
                                            networkManager.ToPeerUDPIP.Add(packet.response.Parameters[0].ToString(), peerForP2P);
                                            networkManager._socketList.Add(peerForP2P);
                                            Link.Add(peerForP2P, true);
                                            peerForP2P.OnLink();
                                            break;
                                        }
                                    case 4:
                                        {
                                            string[] IP = packet.response.Parameters[0].ToString().Split(':');
                                            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(IP[0]), Convert.ToInt32(IP[1]));
                                            NetBitStream stream = new NetBitStream();
                                            stream.BYTES = (byte[])packet.response.Parameters[1];
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
                                switch (packet.response.Code)
                                {
                                    case 0:
                                        {
                                            string[] IP = packet.response.Parameters[0].ToString().Split(':');
                                            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(IP[0]), Convert.ToInt32(IP[1]));
                                            client.P2PAllowList.Remove(iPEndPoint);
                                            client.P2PAllowList.Add(packet._peerUDP);
                                            PeerForP2P peerForP2P = listener.P2PAddPeer(packet._peerUDP, client, true);
                                            networkManager.ToPeerUDP.Add(packet._peerUDP, peerForP2P);
                                            networkManager.ToPeerUDPIP.Add(packet._peerUDP.ToString(), peerForP2P);
                                            networkManager._socketList.Add(peerForP2P);
                                            Link.Add(peerForP2P, true);
                                            peerForP2P.OnLink();

                                            NetBitStream stream = new NetBitStream();
                                            Response b = new Response(1, new Dictionary<byte, object>() { { 0, MyIPPort } });
                                            stream.BeginWrite((ushort)MessageIdentifiers.ID.P2P_CONNECTION);
                                            stream.WriteResponse2(b, key, false);
                                            stream.EncodeHeader();
                                            client.Send(stream, packet._peerUDP);
                                            break;
                                        }
                                    case 1:
                                        {
                                            string[] IP = packet.response.Parameters[0].ToString().Split(':');
                                            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(IP[0]), Convert.ToInt32(IP[1]));
                                            client.P2PAllowList.Remove(iPEndPoint);
                                            client.P2PAllowList.Add(packet._peerUDP);
                                            PeerForP2P peerForP2P = listener.P2PAddPeer(packet._peerUDP, client, true);
                                            networkManager.ToPeerUDP.Add(packet._peerUDP, peerForP2P);
                                            networkManager.ToPeerUDPIP.Add(packet._peerUDP.ToString(), peerForP2P);
                                            networkManager._socketList.Add(peerForP2P);
                                            Link.Add(peerForP2P, true);
                                            peerForP2P.OnLink();
                                            break;
                                        }
                                    default:
                                        {
                                            break;
                                        }
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