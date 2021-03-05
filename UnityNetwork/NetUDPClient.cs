using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace UnityNetwork
{
    public class NetUDPClient
    {
        public delegate void Checking(ushort ID, NetBitStream stream);
        public event Checking Cheak;
        //bool run = true;

        private NetworkManager _netMgr = null;

        private IPEndPoint ipe = null;

        private UdpClient _socket;

        public delegate void Message(string i);
        public event Message GetMessage;

        public List<IPEndPoint> P2PAllowList = null;

        public string key = "";
        public bool enableP2P = false;

        object SendLock = new object();

        public NetUDPClient(NetworkManager network)
        {
            _netMgr = network;
            P2PAllowList = new List<IPEndPoint>();
        }


        // 連接伺服器
        public bool Connect(string address, int remotePort)
        {

            if (_socket != null)
                return true;

            _socket = new UdpClient();

            try
            {
                ipe = new IPEndPoint(IPAddress.Parse(address), remotePort);
                PushPacket((ushort)MessageIdentifiers.ID.LOADING_NOW, "正在嘗試連線IP:" + ipe.ToString());
                // 開始連接

                try
                {
                    NetBitStream stream = new NetBitStream();
                    stream.BeginWrite((ushort)MessageIdentifiers.ID.NEW_INCOMING_CONNECTION);
                    stream.EncodeHeader();
                    _socket.Send(stream.BYTES, stream.BYTES.Length, ipe);

                    // 向Network Manager傳遞消息

                    _socket.BeginReceive(new AsyncCallback(Receive), _socket);
                    PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_REQUEST_ACCEPTED, "");
                }
                catch (System.Exception e)
                {
                    PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_ATTEMPT_FAILED, e.ToString());
                    Disconnect(0);
                }
            }
            catch (System.Exception e)
            {
                // 連接失敗
                PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_ATTEMPT_FAILED, e.ToString());
                return false;
            }
            return true;
        }

        public void P2PConnectServer(Response response)
        {
            if (key != "")
            {
                try
                {
                    NetBitStream stream = new NetBitStream();
                    stream.BeginWrite((ushort)MessageIdentifiers.ID.P2P_SERVER_CALL);
                    stream.WriteResponse2(response, key, true);
                    stream.EncodeHeader();
                    Send(stream);
                }
                catch (Exception e)
                {
                    OnGetMessage(e.ToString());
                }
            }
        }

        void Receive(System.IAsyncResult ar)
        {
            UdpClient uc = (UdpClient)ar.AsyncState;

            NetBitStream stream = new NetBitStream();

            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                byte[] bytes = uc.EndReceive(ar, ref iPEndPoint);

                stream._socketUDP = iPEndPoint;

                stream.BYTES = bytes;

                stream.DecodeHeader();

                _socket.BeginReceive(new AsyncCallback(Receive), uc);
                ushort ID = System.BitConverter.ToUInt16(stream.BYTES, NetBitStream.header_length);

                if (!enableP2P && (ID >= (ushort)MessageIdentifiers.ID.P2P_SERVER_CALL && ID <= (ushort)MessageIdentifiers.ID.P2P_ID_CHAT))
                {
                    return;
                }

                if ((ID >= (ushort)MessageIdentifiers.ID.P2P_CONNECTION && ID <= (ushort)MessageIdentifiers.ID.P2P_ID_CHAT) && !P2PAllowList.Contains(iPEndPoint))
                {
                    return;
                }

                Cheak(ID, stream);
                if (ID != (ushort)MessageIdentifiers.ID.CHECKING)
                {
                    PushPacket2(stream);
                }
            }
            catch (System.Exception e)
            {
                PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.ToString());
            }
        }
        // 發送消息

        public void Send(NetBitStream bts)
        {
            if (SendLock != null)
            {
                lock (SendLock)
                {
                    try
                    {
                        _socket.Send(bts.BYTES, bts.Length, ipe);
                        //ns.Write(bts.BYTES, 0, bts.Length);
                    }
                    catch (System.Exception e)
                    {
                        GetMessage?.Invoke(e.ToString());
                    }
                }
            }
        }

        public bool Send(NetBitStream bts, IPEndPoint iPEndPoint)
        {
            if (SendLock != null)
            {
                lock (SendLock)
                {
                    try
                    {
                        return _socket.Send(bts.BYTES, bts.Length, iPEndPoint) != 0;
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

        public void SetAuxiliaryServer(NetBitStream bts, int Port)
        {
            if (SendLock != null)
            {
                lock (SendLock)
                {
                    try
                    {
                        _socket.Send(bts.BYTES, bts.Length, new IPEndPoint(ipe.Address, Port));
                        //ns.Write(bts.BYTES, 0, bts.Length);
                    }
                    catch (System.Exception e)
                    {
                        GetMessage?.Invoke(e.ToString());
                    }
                }
            }
        }

        // 關閉連接
        public void Disconnect(int timeout)
        {
            _socket.Close();
            _socket = null;
            ipe = null;
            //run = false;
        }

        public void OnGetMessage(string message)
        {
            GetMessage?.Invoke(message);
        }

        // 向Network Manager的佇列傳遞內部消息
        public void P2PPushPacket(ushort msgid, string exception, IPEndPoint peer)
        {

            NetPacket packet = new NetPacket(NetBitStream.header_length + NetBitStream.max_body_length);
            packet.SetIDOnly(msgid);
            packet._error = exception;
            packet._peerUDP = peer;

            _netMgr.AddPacket(_netMgr.AddPacketKey(), packet);
        }

        // 向Network Manager的佇列傳遞內部消息
        public void PushPacket(ushort msgid, string exception)
        {

            NetPacket packet = new NetPacket(NetBitStream.header_length + NetBitStream.max_body_length);
            packet.SetIDOnly(msgid);
            packet._error = exception;
            packet._peerUDP = ipe;

            _netMgr.AddPacket(_netMgr.AddPacketKey(), packet);
        }

        // 向Network Manager的佇列傳遞資料
        public void PushPacket2(NetBitStream stream)
        {
            NetPacket packet = new NetPacket(stream.BYTES.Length);
            stream.BYTES.CopyTo(packet._bytes, 0);
            packet._peerUDP = stream._socketUDP;
            ushort msgid = 0;
            packet.TOID(out msgid);
            try
            {
                string packetkey = _netMgr.AddPacketKey();
                if (msgid >= (ushort)MessageIdentifiers.ID.ID_CHAT && msgid <= (ushort)MessageIdentifiers.ID.P2P_ID_CHAT)
                {
                    NetBitStream stream2 = new NetBitStream();
                    stream2.BeginReadUDP2(packet);
                    stream2.ReadResponse2(key);
                    stream2.EncodeHeader();
                    packet.response = stream2.thing;
                    _netMgr.AddPacket(packetkey, packet);
                }
                else
                {
                    _netMgr.AddPacket(packetkey, packet);
                }
            }
            catch (Exception e)
            {
                GetMessage(e.ToString() + "PushPacket2");
            }
        }
    }
}