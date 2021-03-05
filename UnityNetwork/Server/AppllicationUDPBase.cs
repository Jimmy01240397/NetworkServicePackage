using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityNetwork;


namespace UnityNetwork.Server
{
    public class AppllicationUDPBase : NetworkManager
    {
        // 邏輯執行緒
        Thread NetThread;

        // 伺服器
        NetUDPServer _server;

        List<int> AuxiliaryServerList = new List<int>();
        Dictionary<int, bool> AuxiliaryServerListLink = new Dictionary<int, bool>();

        // 用戶端列表
        public Dictionary<PeerUDPBase, bool> Link;
        public List<PeerUDPBase> NewLink;
        bool run;
        bool Clean;
        public delegate void Message(byte t, string i);
        public event Message GetMessage;

        public void Start()
        {
            run = true;
            Clean = false;
            // 創建一個列表保存每個用戶端的Socket
            _socketList = new System.Collections.ArrayList();
            ToPeerUDP = new Dictionary<IPEndPoint, object>();
            ToPeerUDPIP = new Dictionary<string, object>();
            Link = new Dictionary<PeerUDPBase, bool>();
            NewLink = new List<PeerUDPBase>();

            _server = new NetUDPServer(this, -1);
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
                    if (_server.CreateUdpServer(GetIPv4List(), GetPort(), out w))
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
                    _server_GetMessage(e.ToString() + "From StartCreateUDpServer");
                }
                Setup();
            }
            catch (Exception e)
            {
                _server_GetMessage(e.ToString() + "From Start");
            }
        }
        public void Start(bool thread)
        {
            run = true;
            Clean = false;
            // 創建一個列表保存每個用戶端的Socket
            _socketList = new System.Collections.ArrayList();
            ToPeerUDPIP = new Dictionary<string, object>();
            ToPeerUDP = new Dictionary<IPEndPoint, object>();
            Link = new Dictionary<PeerUDPBase, bool>();
            NewLink = new List<PeerUDPBase>();


            _server = new NetUDPServer(this, -1);
            _server.GetMessage += _server_GetMessage;

            try
            {
                // 為邏輯部分建立新的執行緒
                NetThread = new Thread(new ParameterizedThreadStart(Update));
                NetThread.Start(thread);
                Thread checking = new Thread(new ThreadStart(check));
                checking.Start();
                Thread checking2 = new Thread(new ThreadStart(check2));
                checking2.Start();
                string w;
                try
                {
                    if (_server.CreateUdpServer(GetIPv4List(), GetPort(), out w))
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
                    _server_GetMessage(e.ToString() + "From StartCreateUDpServer");
                }
                Setup();
            }
            catch (Exception e)
            {
                _server_GetMessage(e.ToString() + "From Start");
            }
        }
        public void Start(bool thread, int maxConnections)
        {
            run = true;
            Clean = false;
            // 創建一個列表保存每個用戶端的Socket
            _socketList = new System.Collections.ArrayList();
            ToPeerUDPIP = new Dictionary<string, object>();
            ToPeerUDP = new Dictionary<IPEndPoint, object>();
            Link = new Dictionary<PeerUDPBase, bool>();
            NewLink = new List<PeerUDPBase>();


            _server = new NetUDPServer(this, maxConnections);
            _server.GetMessage += _server_GetMessage;

            try
            {
                // 為邏輯部分建立新的執行緒
                NetThread = new Thread(new ParameterizedThreadStart(Update));
                NetThread.Start(thread);
                Thread checking = new Thread(new ThreadStart(check));
                checking.Start();
                Thread checking2 = new Thread(new ThreadStart(check2));
                checking2.Start();
                string w;
                try
                {
                    if (_server.CreateUdpServer(GetIPv4List(), GetPort(), out w))
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
                    _server_GetMessage(e.ToString() + "From StartCreateUDpServer");
                }
                Setup();
            }
            catch (Exception e)
            {
                _server_GetMessage(e.ToString() + "From Start");
            }
        }
        public virtual void Setup()
        {

        }

        public override void Update()
        {
            NetPacket packet = null;
            while (run)
            {
                try
                {
                    if (_socketList.Count == 0 && NewLink.Count == 0 && Clean)
                    {
                        lock (Link)
                        {
                            Clean = false;
                            CleanPacket();
                            _socketList = null;
                            ToPeerUDPIP = null;
                            ToPeerUDP = null;
                            Link = null;
                            NewLink = null;
                            _socketList = new System.Collections.ArrayList();
                            ToPeerUDPIP = new Dictionary<string, object>();
                            ToPeerUDP = new Dictionary<IPEndPoint, object>();
                            Link = new Dictionary<PeerUDPBase, bool>();
                            NewLink = new List<PeerUDPBase>();
                            CleanUp();
                        }
                    }

                    SpinWait.SpinUntil(() => !run || this.PacketSize != 0);

                    for (packet = GetPacket(); packet != null; packet = GetPacket())
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
                                _server.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "不正確的標頭資訊", packet._peerUDP);
                            }
                            switch (msgid)
                            {
                                case (ushort)MessageIdentifiers.ID.NEW_INCOMING_CONNECTION:
                                    {
                                        Clean = true;
                                        GetMessage?.Invoke(1, packet._peerUDP.ToString());
                                        PeerUDPBase Peer = AddPeerBase(packet._peerUDP, this._server);
                                        if (Peer != null)
                                        {
                                            NewLink.Add(Peer);
                                            ToPeerUDPIP.Add(packet._peerUDP.ToString(), Peer);
                                            ToPeerUDP.Add(packet._peerUDP, Peer);
                                            lock (Link)
                                            {
                                                Link.Add(Peer, true);
                                            }
                                            string[] b = Guid.NewGuid().ToString().Split('-');
                                            string Key = "";
                                            for (int i = 0; i < b.Length; i++)
                                            {
                                                Key += b[i];
                                            }
                                            Peer.GetKey(Key);
                                            NetBitStream stream = new NetBitStream();
                                            Response bb = new Response(0, new Dictionary<byte, object>(), 0, Key);
                                            stream.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                                            stream.WriteResponse2(bb, "");
                                            stream.EncodeHeader();
                                            _server.Send(stream, packet._peerUDP);
                                        }
                                        break;
                                    }
                                case (ushort)MessageIdentifiers.ID.CONNECTION_LOST:
                                    {
                                        Thread a = new Thread(new ParameterizedThreadStart(Finding));
                                        a.IsBackground = true;
                                        a.Start(new Find(packet, 1));
                                        break;
                                    }
                                case (ushort)MessageIdentifiers.ID.ID_CHAT:
                                    {
                                        Thread a = new Thread(new ParameterizedThreadStart(Finding));
                                        a.IsBackground = true;
                                        a.Start(new Find(packet, 2));
                                        break;
                                    }
                                case (ushort)MessageIdentifiers.ID.CHECKING:
                                    {
                                        Thread a = new Thread(new ParameterizedThreadStart(Finding));
                                        a.IsBackground = true;
                                        a.Start(new Find(packet, 3));
                                        break;
                                    }
                                case (ushort)MessageIdentifiers.ID.KEY:
                                    {
                                        lock (Link)
                                        {
                                            Link[(PeerUDPBase)ToPeerUDP[packet._peerUDP]] = true;
                                        }
                                        NetBitStream stream = new NetBitStream();
                                        stream.BeginReadUDP2(packet);
                                        stream.ReadResponse2(((PeerUDPBase)ToPeerUDP[packet._peerUDP]).Key);
                                        stream.EncodeHeader();
                                        try
                                        {
                                            if (Convert.ToInt32(stream.thing.Parameters[0]) < 10000)
                                            {
                                                NetBitStream stream1 = new NetBitStream();
                                                Response bbbb = new Response(1, new Dictionary<byte, object>(), 0, "good");
                                                stream1.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                                                stream1.WriteResponse2(bbbb, "");
                                                stream1.EncodeHeader();

                                                _server.Send(stream1, packet._peerUDP);

                                                _socketList.Add(ToPeerUDP[packet._peerUDP]);
                                                NewLink.Remove((PeerUDPBase)ToPeerUDP[packet._peerUDP]);
                                            }
                                            else
                                            {
                                                NetBitStream stream2 = new NetBitStream();
                                                Response bb = new Response(0, new Dictionary<byte, object>(), 0, ((PeerUDPBase)ToPeerUDP[packet._peerUDP]).Key);
                                                stream2.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                                                stream2.WriteResponse2(bb, "");
                                                stream2.EncodeHeader();
                                                _server.Send(stream2, packet._peerUDP);
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            NetBitStream stream2 = new NetBitStream();
                                            Response bb = new Response(0, new Dictionary<byte, object>(), 0, ((PeerUDPBase)ToPeerUDP[packet._peerUDP]).Key);
                                            stream2.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                                            stream2.WriteResponse2(bb, "");
                                            stream2.EncodeHeader();
                                            _server.Send(stream2, packet._peerUDP);
                                        }
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
                            _server_GetMessage(ee.Message + " from UDPAppllicationUpdataFor");
                        }
                        packet = null;
                    }// end while
                }
                catch (Exception e)
                {
                    _server_GetMessage(e.Message + " from UDPAppllicationUpdataWhile");

                }
            }
        }
        public override void Update(object thread)
        {
            NetPacket packet = null;
            while (run)
            {
                try
                {
                    if (_socketList.Count == 0 && NewLink.Count == 0 && Clean)
                    {
                        lock (Link)
                        {
                            Clean = false;
                            CleanPacket();
                            _socketList = null;
                            ToPeerUDP = null;
                            ToPeerUDPIP = null;
                            Link = null;
                            _socketList = new System.Collections.ArrayList();
                            ToPeerUDPIP = new Dictionary<string, object>();
                            ToPeerUDP = new Dictionary<IPEndPoint, object>();
                            Link = new Dictionary<PeerUDPBase, bool>();
                            CleanUp();
                        }
                    }

                    SpinWait.SpinUntil(() => !run || this.PacketSize != 0);

                    for (packet = GetPacket(); packet != null; packet = GetPacket())
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
                                _server.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "不正確的標頭資訊", packet._peerUDP);
                            }
                            if (Convert.ToBoolean(thread))
                            {

                                switch (msgid)
                                {
                                    case (ushort)MessageIdentifiers.ID.NEW_INCOMING_CONNECTION:
                                        {
                                            Clean = true;
                                            GetMessage?.Invoke(1, packet._peerUDP.ToString());
                                            PeerUDPBase Peer = AddPeerBase(packet._peerUDP, this._server);

                                            if (Peer != null)
                                            {
                                                NewLink.Add(Peer);
                                                ToPeerUDPIP.Add(packet._peerUDP.ToString(), Peer);
                                                ToPeerUDP.Add(packet._peerUDP, Peer);
                                                lock (Link)
                                                {
                                                    Link.Add(Peer, true);
                                                }
                                                string[] b = Guid.NewGuid().ToString().Split('-');
                                                string Key = "";
                                                for (int i = 0; i < b.Length; i++)
                                                {
                                                    Key += b[i];
                                                }
                                                Peer.GetKey(Key);
                                                NetBitStream stream = new NetBitStream();
                                                Response bb = new Response(0, new Dictionary<byte, object>(), 0, Key);
                                                stream.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                                                stream.WriteResponse2(bb, "");
                                                stream.EncodeHeader();
                                                _server.Send(stream, packet._peerUDP);
                                            }
                                            break;
                                        }
                                    case (ushort)MessageIdentifiers.ID.CONNECTION_LOST:
                                        {
                                            Thread a = new Thread(new ParameterizedThreadStart(Finding));
                                            a.IsBackground = true;
                                            a.Start(new Find(packet, 1));
                                            break;
                                        }
                                    case (ushort)MessageIdentifiers.ID.ID_CHAT:
                                        {
                                            Thread a = new Thread(new ParameterizedThreadStart(Finding));
                                            a.IsBackground = true;
                                            a.Start(new Find(packet, 2));
                                            break;
                                        }
                                    case (ushort)MessageIdentifiers.ID.CHECKING:
                                        {
                                            Thread a = new Thread(new ParameterizedThreadStart(Finding));
                                            a.IsBackground = true;
                                            a.Start(new Find(packet, 3));
                                            break;
                                        }
                                    case (ushort)MessageIdentifiers.ID.KEY:
                                        {
                                            lock (Link)
                                            {
                                                Link[(PeerUDPBase)ToPeerUDP[packet._peerUDP]] = true;
                                            }
                                            NetBitStream stream = new NetBitStream();
                                            stream.BeginReadUDP2(packet);
                                            stream.ReadResponse2(((PeerUDPBase)ToPeerUDP[packet._peerUDP]).Key);
                                            stream.EncodeHeader();
                                            try
                                            {
                                                if (Convert.ToInt32(stream.thing.Parameters[0]) < 10000)
                                                {
                                                    NetBitStream stream1 = new NetBitStream();
                                                    Response bbbb = new Response(1, new Dictionary<byte, object>(), 0, "good");
                                                    stream1.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                                                    stream1.WriteResponse2(bbbb, "");
                                                    stream1.EncodeHeader();

                                                    _server.Send(stream1, packet._peerUDP);

                                                    _socketList.Add(ToPeerUDP[packet._peerUDP]);
                                                    NewLink.Remove((PeerUDPBase)ToPeerUDP[packet._peerUDP]);
                                                }
                                                else
                                                {
                                                    NetBitStream stream2 = new NetBitStream();
                                                    Response bb = new Response(0, new Dictionary<byte, object>(), 0, ((PeerUDPBase)ToPeerUDP[packet._peerUDP]).Key);
                                                    stream2.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                                                    stream2.WriteResponse2(bb, "");
                                                    stream2.EncodeHeader();
                                                    _server.Send(stream2, packet._peerUDP);
                                                }
                                            }
                                            catch (Exception)
                                            {
                                                NetBitStream stream2 = new NetBitStream();
                                                Response bb = new Response(0, new Dictionary<byte, object>(), 0, ((PeerUDPBase)ToPeerUDP[packet._peerUDP]).Key);
                                                stream2.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                                                stream2.WriteResponse2(bb, "");
                                                stream2.EncodeHeader();
                                                _server.Send(stream2, packet._peerUDP);
                                            }
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
                                switch (msgid)
                                {
                                    case (ushort)MessageIdentifiers.ID.NEW_INCOMING_CONNECTION:
                                        {
                                            Clean = true;
                                            GetMessage?.Invoke(1, packet._peerUDP.ToString());
                                            PeerUDPBase Peer = AddPeerBase(packet._peerUDP, this._server);
                                            if (Peer != null)
                                            {
                                                NewLink.Add(Peer);
                                                ToPeerUDPIP.Add(packet._peerUDP.ToString(), Peer);
                                                ToPeerUDP.Add(packet._peerUDP, Peer);
                                                lock (Link)
                                                {
                                                    Link.Add(Peer, true);
                                                }
                                                string[] b = Guid.NewGuid().ToString().Split('-');
                                                string Key = "";
                                                for (int i = 0; i < b.Length; i++)
                                                {
                                                    Key += b[i];
                                                }
                                                Peer.GetKey(Key);
                                                NetBitStream stream = new NetBitStream();
                                                Response bb = new Response(0, new Dictionary<byte, object>(), 0, Key);
                                                stream.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                                                stream.WriteResponse2(bb, "");
                                                stream.EncodeHeader();
                                                _server.Send(stream, packet._peerUDP);
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
                                    case (ushort)MessageIdentifiers.ID.KEY:
                                        {
                                            lock (Link)
                                            {
                                                Link[(PeerUDPBase)ToPeerUDP[packet._peerUDP]] = true;
                                            }
                                            NetBitStream stream = new NetBitStream();
                                            stream.BeginReadUDP2(packet);
                                            stream.ReadResponse2(((PeerUDPBase)ToPeerUDP[packet._peerUDP]).Key);
                                            stream.EncodeHeader();
                                            try
                                            {
                                                if (Convert.ToInt32(stream.thing.Parameters[0]) < 10000)
                                                {
                                                    NetBitStream stream1 = new NetBitStream();
                                                    Response bbbb = new Response(1, new Dictionary<byte, object>(), 0, "good");
                                                    stream1.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                                                    stream1.WriteResponse2(bbbb, "");
                                                    stream1.EncodeHeader();

                                                    _server.Send(stream1, packet._peerUDP);

                                                    _socketList.Add(ToPeerUDP[packet._peerUDP]);
                                                    NewLink.Remove((PeerUDPBase)ToPeerUDP[packet._peerUDP]);
                                                }
                                                else
                                                {
                                                    NetBitStream stream2 = new NetBitStream();
                                                    Response bb = new Response(0, new Dictionary<byte, object>(), 0, ((PeerUDPBase)ToPeerUDP[packet._peerUDP]).Key);
                                                    stream2.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                                                    stream2.WriteResponse2(bb, "");
                                                    stream2.EncodeHeader();
                                                    _server.Send(stream2, packet._peerUDP);
                                                }
                                            }
                                            catch (Exception)
                                            {
                                                NetBitStream stream2 = new NetBitStream();
                                                Response bb = new Response(0, new Dictionary<byte, object>(), 0, ((PeerUDPBase)ToPeerUDP[packet._peerUDP]).Key);
                                                stream2.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                                                stream2.WriteResponse2(bb, "");
                                                stream2.EncodeHeader();
                                                _server.Send(stream2, packet._peerUDP);
                                            }
                                            break;
                                        }
                                    default:
                                        {
                                            // 錯誤
                                            break;
                                        }
                                }
                            }
                        }// end fore
                        catch (Exception ee)
                        {
                            _server_GetMessage(ee.Message + " from UDPAppllicationUpdataFor");
                        }
                        packet = null;
                    }// end while
                }
                catch (Exception e)
                {
                    _server_GetMessage(e.Message + " from UDPAppllicationUpdataWhile");

                }
            }
        }
        public virtual int GetPort()
        {
            return 10001;
        }
        private string GetIPv4List()
        {
            IPAddress[] ipv4Addresses;
            ipv4Addresses = Array.FindAll(Dns.GetHostEntry(string.Empty).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
            return ipv4Addresses[0].ToString();
        }
        public void Finding(object _find)
        {
            Find c = (Find)_find;
            try
            {
                if (ToPeerUDP.ContainsKey(c.packet._peerUDP))
                {
                    PeerUDPBase a = (PeerUDPBase)ToPeerUDP[c.packet._peerUDP];
                    switch (c.cobe)
                    {
                        case 1:
                            {
                                GetMessage?.Invoke(2, a.socket.ToString() + "+" + c.packet._error);
                                if (_server != null && a.socket != null)
                                {
                                    NetBitStream stream2 = new NetBitStream();
                                    Response b = new Response();
                                    b.DebugMessage = "伺服器端主動關閉";
                                    stream2.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                                    stream2.WriteResponse2(b, "");
                                    stream2.EncodeHeader();
                                    this._server.Send(stream2, a.socket);
                                }
                                try
                                {
                                    a.OnDisconnect();
                                }
                                catch (Exception e)
                                {
                                    _server_GetMessage(a.socket.ToString() + " " + e.Message + "From OnDisconnect");
                                }
                                if (_socketList.Contains(a))
                                {
                                    _socketList.Remove(a);
                                }
                                if (NewLink.Contains(a))
                                {
                                    NewLink.Remove(a);
                                }
                                lock (Link)
                                {
                                    Link.Remove(a);
                                }
                                ToPeerUDP.Remove(c.packet._peerUDP);
                                ToPeerUDPIP.Remove(c.packet._peerUDP.ToString());
                                a.Close();
                                a = null;
                                break;
                            }
                        case 2:
                            {
                                if (a.Key != "")
                                {

                                    NetBitStream stream = new NetBitStream();
                                    stream.BeginReadUDP2(c.packet);
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
                    if (_server != null && c.packet._peerUDP != null)
                    {
                        NetBitStream stream2 = new NetBitStream();
                        Response b = new Response();
                        stream2.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                        stream2.WriteResponse2(b, "");
                        stream2.EncodeHeader();
                        _server.Send(stream2, c.packet._peerUDP);
                    }
                }
            }
            catch (Exception e)
            {
                _server_GetMessage(e.Message + " from UDPAppllicationFinding Cobe:" + c.cobe);
            }
        }

        public void check()
        {
            try
            {
                while (run)
                {
                    if (_socketList != null)
                    {
                        for (int i = 0; i < _socketList.Count; i++)
                        {
                            if (((PeerUDPBase)_socketList[i]) != null)
                            {
                                ((PeerUDPBase)_socketList[i]).check();
                            }
                        }
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception e)
            {
                _server_GetMessage(e.Message + " from UDPAppllicationCheck");
            }
        }

        public void check2()
        {
            try
            {
                while (run)
                {
                    if (_socketList != null)
                    {
                        for (int i = 0; i < _socketList.Count; i++)
                        {
                            lock (Link)
                            {
                                if (Link.ContainsKey((PeerUDPBase)_socketList[i]))
                                {
                                    Link[(PeerUDPBase)_socketList[i]] = false;
                                }
                            }
                        }
                        for (int i = 0; i < NewLink.Count; i++)
                        {
                            lock (Link)
                            {
                                if (Link.ContainsKey(NewLink[i]))
                                {
                                    Link[NewLink[i]] = false;
                                }
                            }
                        }
                        for (int i = 0; i < AuxiliaryServerList.Count; i++)
                        {
                            if (AuxiliaryServerListLink.ContainsKey(AuxiliaryServerList[i]))
                            {
                                AuxiliaryServerListLink[AuxiliaryServerList[i]] = false;
                            }
                        }
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();
                        while (stopwatch.ElapsedMilliseconds < 5000)
                        {
                            Thread.Sleep(5000);
                        }
                        stopwatch.Stop();
                        stopwatch.Reset();
                        for (int i = 0; i < _socketList.Count; i++)
                        {
                            lock (Link)
                            {
                                if (Link.ContainsKey((PeerUDPBase)_socketList[i]))
                                {
                                    if (!Link[(PeerUDPBase)_socketList[i]])
                                    {
                                        _server.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "超過5秒沒有回應", ((PeerUDPBase)_socketList[i]).socket);
                                    }
                                }
                                else
                                {
                                    _server.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "超過5秒沒有回應", ((PeerUDPBase)_socketList[i]).socket);
                                }
                            }
                        }
                        for (int i = 0; i < NewLink.Count; i++)
                        {
                            lock (Link)
                            {
                                if (Link.ContainsKey(NewLink[i]))
                                {
                                    if (!Link[NewLink[i]])
                                    {
                                        _server.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "超過5秒沒有回應", NewLink[i].socket);
                                    }
                                }
                                else
                                {
                                    _server.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "超過5秒沒有回應", NewLink[i].socket);
                                }
                            }
                        }
                        for (int i = 0; i < AuxiliaryServerList.Count; i++)
                        {
                            if (AuxiliaryServerListLink.ContainsKey(AuxiliaryServerList[i]))
                            {
                                if (!AuxiliaryServerListLink[AuxiliaryServerList[i]])
                                {
                                    AuxiliaryServerListLink.Remove(AuxiliaryServerList[i]);
                                    AuxiliaryServerList.RemoveAt(i);
                                }
                            }
                            else
                            {
                                AuxiliaryServerList.RemoveAt(i);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _server_GetMessage(e.Message + " from UDPAppllicationCheck2");
            }
        }


        public virtual PeerUDPBase AddPeerBase(IPEndPoint _peer, NetUDPServer server)
        {
            return new PeerUDPBase(_peer, server);
        }

        public virtual void TearDown()
        {

        }

        public virtual void CleanUp()
        {

        }

        public virtual string UpdateData()
        {
            return "";
        }

        public void Disconnect()
        {
            for (int i = 0; i < _socketList.Count; i++)
            {
                ((PeerUDPBase)_socketList[i]).OffLine();
            }
            Thread a = new Thread(new ThreadStart(exit));
            a.Start();
        }
        public void exit()
        {
            Thread.Sleep(4000);
            TearDown();
            _server.close();
            _server.GetMessage -= _server_GetMessage;
            _socketList.Clear();
            ToPeerUDP.Clear();
            ToPeerUDPIP.Clear();
            Link.Clear();
            AuxiliaryServerList.Clear();
            _socketList = null;
            ToPeerUDPIP = null;
            ToPeerUDP = null;
            Link = null;
            run = false;
#if EnableMessageTell
            MessageTell.StopRead();
#endif
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