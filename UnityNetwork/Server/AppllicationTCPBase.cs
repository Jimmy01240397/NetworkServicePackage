using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace UnityNetwork.Server
{
    public class AppllicationTCPBase
    {
        // 邏輯執行緒
        Thread NetThread;

        // 伺服器
        NetTCPServer _server;

        public double CloseTime { get; set; }

        // 用戶端列表
        private Dictionary<PeerTCPBase, bool> Link;
        bool run;
        bool Clean;
        public delegate void Message(byte t, string i);
        public event Message GetMessage;

        public System.Collections.ArrayList SocketList
        {
            get
            {
                return networkManager._socketList;
            }
        }
        public Dictionary<string, object> ToPeerTCPIP
        {
            get
            {
                return networkManager.ToPeerTCPIP;
            }
        }
        public Dictionary<TcpClient, object> ToPeerTCP
        {
            get
            {
                return networkManager.ToPeerTCP;
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

        private NetworkManager networkManager;

        public void Start(int maxConnections = -1)
        {
            CloseTime = -1;
            run = true;
            Clean = false;
            // 創建一個列表保存每個用戶端的Socket
            Link = new Dictionary<PeerTCPBase, bool>();

            networkManager = new NetworkManager();

            _server = new NetTCPServer(networkManager, maxConnections);
            _server.GetMessage += _server_GetMessage;

            try
            {

                // 為邏輯部分建立新的執行緒
                NetThread = new Thread(new ThreadStart(Update));
                NetThread.Start();
                Thread checking = new Thread(new ThreadStart(check));
                checking.Start();
                Thread checking2 = new Thread(new ThreadStart(check2));
                checking2.Start();
                string w;
                try
                {
                    if (_server.CreateTcpServer(GetIPv4List(), GetPort(), out w))
                    {
                        GetMessage?.Invoke(0, w);
                    }
                    else
                    {
                        GetMessage?.Invoke(4, DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + " " + "錯誤：" + w);
                    }
                }
                catch (Exception e)
                {
                    _server_GetMessage(e.ToString() + "From StartCreateTcpServer");
                }
                Setup();
            }
            catch (Exception e)
            {
                _server_GetMessage(e.ToString() + "From Start");
            }
        }
        protected virtual void Setup()
        {

        }

        private void Update()
        {
            NetPacket packet = null;
            while (run)
            {
                try
                {
                    if (networkManager._socketList.Count == 0 && Clean)
                    {
                        lock (Link)
                        {
                            Clean = false;
                            networkManager.Clear();
                            Link.Clear();
                            CleanUp();
                        }
                    }

                    SpinWait.SpinUntil(() => !run || networkManager.PacketSize != 0);

                    for (packet = networkManager.GetPacket(); packet != null; packet = networkManager.GetPacket())
                    {
                        try
                        {
                            // 獲得訊息ID
                            ushort msgid = 0;
                            try
                            {
                                packet.TOID(out msgid);
                            }
                            catch (Exception)
                            {
                                _server.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "不正確的標頭資訊", packet._peerTCP);
                            }
                            switch (msgid)
                            {
                                case (ushort)MessageIdentifiers.ID.NEW_INCOMING_CONNECTION:
                                    {
                                        Clean = true;
                                        GetMessage?.Invoke(1, packet._peerTCP.Client.RemoteEndPoint.ToString());
                                        PeerTCPBase Peer = AddPeerBase(packet._peerTCP, this._server);
                                        if (Peer != null)
                                        {
                                            Peer.GetKey(packet._error);
                                            networkManager._socketList.Add(Peer);
                                            networkManager.ToPeerTCPIP.Add(packet._peerTCP.Client.RemoteEndPoint.ToString(), Peer);
                                            networkManager.ToPeerTCP.Add(packet._peerTCP, Peer);
                                            lock (Link)
                                            {
                                                Link.Add(Peer, true);
                                            }
                                        }
                                        break;
                                    }
                                case (ushort)MessageIdentifiers.ID.CONNECTION_LOST:
                                    {
                                        Finding(new Find(packet, 1));
                                        break;
                                    }
                                case (ushort)MessageIdentifiers.ID.ID_CHAT:
                                    {
                                        Finding(new Find(packet, 2));
                                        break;
                                    }
                                case (ushort)MessageIdentifiers.ID.CHECKING:
                                    {
                                        Finding(new Find(packet, 3));
                                        break;
                                    }
                                default:
                                    {
                                        // 錯誤
                                        break;
                                    }
                            }
                        }// end fore
                        catch (Exception ee)
                        {
                            _server_GetMessage(ee.Message + " Update for");
                        }
                        packet = null;
                    }// end while
                    if (CloseTime != -1 && CloseTime < new TimeSpan(DateTime.Now.Ticks).TotalMilliseconds)
                    {
                        try
                        {
                            Disconnect();
                        }
                        catch (Exception e)
                        {
                            GetMessage(4, DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + " " + "錯誤：" + e.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    _server_GetMessage(e.Message + " Update while");

                }
            }
        }
        protected virtual int GetPort()
        {
            return 10001;
        }
        private string GetIPv4List()
        {
            IPAddress[] ipv4Addresses;
            ipv4Addresses = Array.FindAll(Dns.GetHostEntry(string.Empty).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
            return ipv4Addresses[0].ToString();
        }
        protected void Finding(object _find)
        {
            try
            {
                Find c = (Find)_find;
                if (networkManager.ToPeerTCP.ContainsKey(c.packet._peerTCP))
                {
                    PeerTCPBase a = (PeerTCPBase)networkManager.ToPeerTCP[c.packet._peerTCP];
                    switch (c.cobe)
                    {
                        case 1:
                            {
                                if (c.packet._error == "")
                                {
                                    NetBitStream stream = new NetBitStream();
                                    stream.BeginReadTCP2(c.packet);
                                    stream.ReadResponse2("");
                                    stream.EncodeHeader();
                                    c.packet._error = stream.thing.DebugMessage;
                                }
                                GetMessage?.Invoke(2, a.socket.Client.RemoteEndPoint.ToString() + "+" + c.packet._error);
                                if (_server != null && a.socket != null)
                                {
                                    if (a.socket.Connected)
                                    {
                                        NetBitStream stream2 = new NetBitStream();
                                        Response b = new Response();
                                        b.DebugMessage = "伺服器端主動關閉";
                                        stream2.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                                        stream2.WriteResponse2(b, "");
                                        stream2.EncodeHeader();
                                        this._server.Send(stream2, a.socket);
                                    }
                                }
                                try
                                {
                                    a.OnDisconnect();
                                }
                                catch (Exception e)
                                {
                                    _server_GetMessage(a.socket.Client.RemoteEndPoint.ToString() + " " + e.Message + "From OnDisconnect");
                                }
                                networkManager._socketList.Remove(a);
                                lock (Link)
                                {
                                    Link.Remove(a);
                                }
                                networkManager.ToPeerTCP.Remove(c.packet._peerTCP);
                                networkManager.ToPeerTCPIP.Remove(c.packet._peerTCP.Client.RemoteEndPoint.ToString());
                                a.Close();
                                a = null;
                                break;
                            }
                        case 2:
                            {
                                if (a.Key != "")
                                {
                                    NetBitStream stream = new NetBitStream();
                                    stream.BeginReadTCP2(c.packet);
                                    try
                                    {
                                        a.OnOperationRequest(c.packet.response);
                                    }
                                    catch (Exception e)
                                    {
                                        _server_GetMessage(a.socket.ToString() + " " + e.Message + "From OnOperationRequest");
                                    }
                                    lock (Link)
                                    {
                                        Link[a] = true;
                                    }
                                }
                                break;
                            }
                        case 3:
                            {
                                lock (Link)
                                {
                                    Link[a] = true;
                                }
                                //a.check();
                                break;
                            }
                        default:
                            {
                                // 錯誤
                                break;
                            }
                    }
                }
                else
                {
                    if (_server != null && c.packet._peerTCP != null)
                    {
                        if (c.packet._peerTCP.Connected)
                        {
                            NetBitStream stream2 = new NetBitStream();
                            Response b = new Response();
                            stream2.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                            stream2.WriteResponse2(b, "");
                            stream2.EncodeHeader();
                            _server.Send(stream2, c.packet._peerTCP);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _server_GetMessage(e.Message + " from Find");

            }
        }

        protected void check()
        {
            try
            {
                if (networkManager._socketList != null)
                {
                    while (run)
                    {
                        for (int i = 0; i < networkManager._socketList.Count; i++)
                        {
                            ((PeerTCPBase)networkManager._socketList[i]).check();
                        }
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception e)
            {
                _server_GetMessage(e.ToString() + "From check");
            }
        }

        protected void check2()
        {
            try
            {
                while (run)
                {
                    if (networkManager._socketList != null)
                    {
                        for (int i = 0; i < networkManager._socketList.Count; i++)
                        {
                            lock (Link)
                            {
                                if (Link.ContainsKey((PeerTCPBase)networkManager._socketList[i]))
                                {
                                    Link[(PeerTCPBase)networkManager._socketList[i]] = false;
                                }
                            }
                        }
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();
                        while (stopwatch.ElapsedMilliseconds < 5000)
                        {
                            Thread.Sleep(5000);
                        }
                        stopwatch.Stop();
                        string x = stopwatch.ElapsedMilliseconds.ToString();
                        stopwatch.Reset();
                        for (int i = 0; i < networkManager._socketList.Count; i++)
                        {
                            lock (Link)
                            {
                                if (Link.ContainsKey((PeerTCPBase)networkManager._socketList[i]))
                                {
                                    if (!Link[(PeerTCPBase)networkManager._socketList[i]])
                                    {
                                        _server.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "超過5秒沒有回應:Link == false " + x, ((PeerTCPBase)networkManager._socketList[i]).socket);
                                    }
                                }
                                else
                                {
                                    _server.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "超過5秒沒有回應:無Link" + x, ((PeerTCPBase)networkManager._socketList[i]).socket);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _server_GetMessage(e.ToString() + "From check2");
            }
        }


        protected virtual PeerTCPBase AddPeerBase(TcpClient _peer, NetTCPServer server)
        {
            return new PeerTCPBase(_peer, server);
        }

        protected virtual void TearDown()
        {

        }

        protected virtual void CleanUp()
        {

        }

        public virtual string UpdateData()
        {
            return "";
        }

        public void Disconnect()
        {
            for (int i = 0; i < networkManager._socketList.Count; i++)
            {
                ((PeerTCPBase)networkManager._socketList[i]).OffLine();
            }
            Thread a = new Thread(new ThreadStart(exit));
            a.Start();
        }
        protected void exit()
        {
            Thread.Sleep(4000);
            TearDown();
            _server.close();
            _server.GetMessage -= _server_GetMessage;
            networkManager.Clear();
            Link.Clear();
            networkManager = null;
            Link = null;
            run = false;
            NetThread = null;
            _server = null;
            try
            {
                GetMessage?.Invoke(5, DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + " " + "成功關閉伺服器");
            }
            catch (Exception)
            {

            }
        }
        public void _server_GetMessage(string w)
        {
            try
            {
                GetMessage?.Invoke(4, DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + " " + "錯誤：" + w);
            }
            catch (Exception)
            {

            }
        }

        public void StartProcess(string Path)
        {
            try
            {
                GetMessage?.Invoke(6, Path);
            }
            catch (Exception)
            {

            }
        }

        public void StartProcess(string Path, string args)
        {
            try
            {
                GetMessage?.Invoke(6, Path + "\n" + args);
            }
            catch (Exception)
            {

            }
        }

        class Find
        {
            public NetPacket packet;
            public int cobe;
            public Find(NetPacket a, int b)
            {
                packet = a;
                cobe = b;
            }
        }
    }
}
