using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace UnityNetwork
{
    public class NetUDPServer
    {
        // 最大連接數
        private int _maxConnections = -1;
        private int _revTimeout = 1000;

        UdpClient _listener;
        public delegate void Message(string i);
        public event Message GetMessage;

        public bool enableP2P = false;
        // 埠號
        int _port = 0;

        private IPEndPoint listenerIP = null;
        public IPEndPoint ListenerIP
        {
            get
            {
                if (listenerIP == null)
                {
                    listenerIP = (IPEndPoint)_listener.Client.LocalEndPoint;
                }
                return listenerIP;
            }
        }

        // 網路管理器 處理訊息和邏輯
        private NetworkManager _netMgr = null;

        public NetUDPServer(NetworkManager network, int maxConnections)
        {
            _maxConnections = maxConnections;
            _netMgr = network;
        }
        public bool CreateUdpServer(string ip, int listenPort, out string a)
        {
            _port = listenPort;


            a = "";
            try
            {

                IPEndPoint ipe = new IPEndPoint(IPAddress.Any, _port);
                a += ipe.Address + ":" + ipe.Port + " ";
                _listener = new UdpClient(ipe.Port);
                _listener.BeginReceive(new AsyncCallback(Receive), _listener);
                ip = Dns.GetHostAddresses(ip)[0].ToString() + ":" + _port;
                a = ip;
            }
            catch (System.Exception)
            {
                a += "無法建立伺服器";
                return false;
            }
            return true;
        }


        public void close()
        {
            _listener.Close();
            _listener = null;
        }

        private void Receive(System.IAsyncResult ar)
        {
            try
            {
                UdpClient uc = (UdpClient)ar.AsyncState;
                IPEndPoint ipe = new IPEndPoint(IPAddress.Any, _port);

                NetBitStream stream = new NetBitStream();
                byte[] bytes = null;
                try
                {
                    bytes = uc.EndReceive(ar, ref ipe);
                }
                catch (Exception e)
                {
                    if (_listener != null)
                    {
                        _listener.BeginReceive(new AsyncCallback(Receive), uc);
                        return;
                    }
                    else
                    {
                        throw e;
                    }
                }
                stream._socketUDP = ipe;

                stream.BYTES = bytes;

                // 獲得訊息體長度
                stream.DecodeHeader();
                ushort msgid = System.BitConverter.ToUInt16(stream.BYTES, NetBitStream.header_length);

                if (!enableP2P && (msgid >= (ushort)MessageIdentifiers.ID.P2P_SERVER_CALL && msgid <= (ushort)MessageIdentifiers.ID.P2P_GET_PUBLIC_ENDPOINT))
                {
                    return;
                }

                if (msgid == (ushort)MessageIdentifiers.ID.P2P_GET_PUBLIC_ENDPOINT)
                {
                    stream = new NetBitStream();
                    Response response = new Response(0, new Dictionary<byte, object>() { { 0, ipe.ToString() } });
                    stream.BeginWrite((ushort)MessageIdentifiers.ID.P2P_GET_PUBLIC_ENDPOINT);
                    stream.WriteResponse2(response, "", false);
                    stream.EncodeHeader();
                    _listener.Send(stream.BYTES, stream.Length, ipe);
                }
                else
                {
                    if (!((msgid == (ushort)MessageIdentifiers.ID.ID_CHAT || msgid == (ushort)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT || msgid == (ushort)MessageIdentifiers.ID.CHECKING || msgid == (ushort)MessageIdentifiers.ID.CONNECTION_LOST || msgid == (ushort)MessageIdentifiers.ID.KEY) && !_netMgr.ToPeerUDP.ContainsKey(ipe)))
                    {
                        PushPacket2(stream);
                    }
                    else
                    {
                        CatchMessage(((MessageIdentifiers.ID)msgid).ToString());
                    }
                }
                uc.BeginReceive(new AsyncCallback(Receive), uc);
                // 下一個讀取
            }
            catch (SocketException e)
            {
                if (_listener != null)
                {
                    _listener.Close();
                    _listener = new UdpClient(listenerIP);
                    _listener.BeginReceive(new AsyncCallback(Receive), _listener);
                }
            }
            catch (System.Exception e)
            {
                CatchMessage(e.ToString());
            }
        }
        public void Send(NetBitStream bts, IPEndPoint peer)
        {
            if (peer != null && _netMgr!=null)
            {
                if (_netMgr.ToPeerUDP != null)
                {
                    if (_netMgr.ToPeerUDP.ContainsKey(peer))
                    {
                        if (((UnityNetwork.Server.PeerUDPBase)_netMgr.ToPeerUDP[peer]).Lock != null)
                        {
                            lock (((UnityNetwork.Server.PeerUDPBase)_netMgr.ToPeerUDP[peer]).Lock)
                            {
                                _listener.Send(bts.BYTES, bts.Length, peer);
                            }
                        }
                    }
                }
            }
        }

        public void Send(byte[] bytes, IPEndPoint peer)
        {
            if (peer != null && _netMgr != null)
            {
                if (_netMgr.ToPeerUDP != null)
                {
                    if (_netMgr.ToPeerUDP.ContainsKey(peer))
                    {
                        if (((UnityNetwork.Server.PeerUDPBase)_netMgr.ToPeerUDP[peer]).Lock != null)
                        {
                            lock (((UnityNetwork.Server.PeerUDPBase)_netMgr.ToPeerUDP[peer]).Lock)
                            {
                                _listener.Send(bytes, bytes.Length, peer);
                            }
                        }
                    }
                }
            }
        }

        public void CatchMessage(string message)
        {
            if (GetMessage != null)
            {
                GetMessage(message);
            }
        }

        // 向Network Manager的佇列傳遞內部訊息
        public void PushPacket(ushort msgid, string exception, IPEndPoint peer)
        {

            NetPacket packet = new NetPacket(NetBitStream.header_length + NetBitStream.max_body_length);
            packet.SetIDOnly(msgid);
            packet._error = exception;
            packet._peerUDP = peer;

            _netMgr.AddPacket(_netMgr.AddPacketKey(), packet);
        }

        // 向Network Manager的佇列傳遞資料
        private ushort PushPacket2(NetBitStream stream)
        {

            NetPacket packet = new NetPacket(stream.BYTES.Length);
            stream.BYTES.CopyTo(packet._bytes, 0);
            packet._peerUDP = stream._socketUDP;
            ushort msgid = 0;
            packet.TOID(out msgid);
            ThreadPool.QueueUserWorkItem((aa) =>
            {
                try
                {
                    string packetkey = _netMgr.AddPacketKey();
                    if (msgid == (short)MessageIdentifiers.ID.ID_CHAT || msgid == (short)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT || msgid == (short)MessageIdentifiers.ID.P2P_SERVER_CALL)
                    {
                        NetBitStream stream2 = new NetBitStream();
                        stream2.BeginReadUDP2(packet);
                        if (_netMgr.ToPeerUDP.ContainsKey(stream2._socketUDP))
                        {
                            stream2.ReadResponse2(((UnityNetwork.Server.PeerUDPBase)_netMgr.ToPeerUDP[stream2._socketUDP]).Key);
                        }
                        stream2.EncodeHeader();
                        packet.response = stream2.thing;
                        _netMgr.AddPacket(packetkey, packet);
                    }
                    else
                    {
                        _netMgr.AddPacket(packetkey, packet);
                    }
                }
                catch(Exception e)
                {
                    GetMessage(e.ToString() + "PushPacket2");
                }
            });
            return msgid;
        }
    }
}