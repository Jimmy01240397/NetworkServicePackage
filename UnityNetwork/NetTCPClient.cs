using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace UnityNetwork
{
    public class NetTCPClient
    {
        public int _sendTimeout = 1000;
        public int _revTimeout = 1000;

        public delegate void Checking(ushort ID);
        public event Checking Cheak;
        //bool run = true;

        private NetworkManager _netMgr = null;

        private TcpClient _socket = new TcpClient();

        public delegate void Message(string i);
        public event Message GetMessage;

        public string key = "";

        public NetTCPClient(NetworkManager network)
        {
            _netMgr = network;
        }


        // 連接伺服器
        public bool Connect(string address, int remotePort)
        {
            _socket = new TcpClient();

            if (_socket != null && _socket.Connected)
                return true;

            try
            {
                IPEndPoint ipe = new IPEndPoint(Array.FindAll(Dns.GetHostEntry(address).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork)[0], remotePort);

                PushPacket((ushort)MessageIdentifiers.ID.LOADING_NOW, "正在嘗試連線IP:" + ipe.ToString());
                // 開始連接
                _socket.BeginConnect(IPAddress.Parse(address), remotePort, new System.AsyncCallback(ConnectionCallback), _socket);
                PushPacket((ushort)MessageIdentifiers.ID.LOADING_NOW, "等待回檔...");
            }
            catch (System.Exception e)
            {
                // 連接失敗
                PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_ATTEMPT_FAILED, e.ToString());
                return false;
            }
            return true;
        }

        // 非同步連接回檔
        void ConnectionCallback(System.IAsyncResult ar)
        {
            NetBitStream stream = new NetBitStream();

            // 獲得伺服器socket
            stream._socketTCP = (TcpClient)ar.AsyncState;
            stream.BYTES = new byte[NetBitStream.header_length];
            try
            {
                // 與伺服器取得連接
                _socket.EndConnect(ar);

                _socket.SendTimeout = _sendTimeout;
                _socket.ReceiveTimeout = _revTimeout;

                // 向Network Manager傳遞消息
                PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_REQUEST_ACCEPTED, "");

                NetworkStream ns = stream._socketTCP.GetStream();

                ns.BeginRead(stream.BYTES, 0, NetBitStream.header_length, new System.AsyncCallback(Receive), new object[] { ns, stream.BYTES });


            }
            catch (System.Exception e)
            {
                if (e.GetType() == typeof(SocketException))
                {
                    if (((SocketException)e).SocketErrorCode == SocketError.ConnectionRefused)
                    {
                        PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_ATTEMPT_FAILED, e.ToString());
                    }
                    else
                    {
                        PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.ToString());
                    }
                }
                GetMessage?.Invoke(e.ToString());
                Disconnect(0);
            }
        }

        void Receive(System.IAsyncResult ar)
        {
            object[] ar2 = (object[])ar.AsyncState;
            NetworkStream ns = (NetworkStream)ar2[0];
            byte[] bytes = (byte[])ar2[1];
            NetBitStream stream = new NetBitStream();

            try
            {
                int read = ns.EndRead(ar);

                if (read < 1)
                {
                    PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "只讀到" + read + "位元?");
                    return;
                }

                stream._socketTCP = _socket;

                stream.BYTES = bytes;

                stream.DecodeHeader();


                Array.Resize(ref bytes, NetBitStream.header_length + stream.BodyLength);
                stream.BYTES = bytes;

                for (int iIndex = 0; iIndex < stream.BodyLength;)
                {
                    byte[] buffer = new byte[stream.BodyLength];
                    SpinWait.SpinUntil(() => ns.CanRead && ns.DataAvailable, 2000);
                    if (ns.CanRead && ns.DataAvailable)
                    {
                        int j = ns.Read(buffer, 0, stream.BodyLength - iIndex);
                        Array.Copy(buffer, 0, stream.BYTES, NetBitStream.header_length + iIndex, j);
                        iIndex += j;
                    }
                    else
                    {
                        return;
                    }
                }

                ushort ID = System.BitConverter.ToUInt16(stream.BYTES, NetBitStream.header_length); ;

                Cheak(ID);

                NetBitStream stream3 = new NetBitStream();
                stream3._socketTCP = _socket;
                stream3.BYTES = new byte[NetBitStream.header_length];
                ns.BeginRead(stream3.BYTES, 0, NetBitStream.header_length, new System.AsyncCallback(Receive), new object[] { ns, stream3.BYTES });
                if (ID != (ushort)MessageIdentifiers.ID.CHECKING)
                {
                    PushPacket2(stream);
                }
            }
            catch (System.Exception e)
            {
                PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.ToString() + " From Receive");
            }
        }

        // 發送消息
        public void Send(NetBitStream bts)
        {
            SpinWait.SpinUntil(() => _socket.Connected, 200);

            if (!_socket.Connected)
            {
                PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "沒有連線至伺服器");
                return;
            }

            NetworkStream ns;
            lock (_socket)
            {
                ns = _socket.GetStream();
            }

            if (ns.CanWrite)
            {
                try
                {
                    ns.BeginWrite(bts.BYTES, 0, bts.Length, new System.AsyncCallback(SendCallback), ns);
                    //ns.Write(bts.BYTES, 0, bts.Length);
                }
                catch (System.Exception e)
                {
                    PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.ToString());
                }
            }
        }

        private void SendCallback(System.IAsyncResult ar)
        {
            NetworkStream ns = (NetworkStream)ar.AsyncState;
            try
            {
                ns.EndWrite(ar);
            }
            catch (System.Exception e)
            {
                PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "寄信回檔期間出問題：" + e.ToString());
            }
        }

        // 關閉連接
        public void Disconnect(int timeout)
        {
            if (_socket != null)
            {
                if (_socket.Connected)
                {
                    _socket.Client.Shutdown(SocketShutdown.Receive);
                    _socket.Client.Close(timeout);
                }
                else
                {
                    _socket.Client.Close();
                }
                _socket.Close();
            }
            _socket = null;
            //run = false;
        }

        // 向Network Manager的佇列傳遞內部消息
        public void PushPacket(ushort msgid, string exception)
        {

            NetPacket packet = new NetPacket(NetBitStream.header_length + NetBitStream.max_body_length);
            packet.SetIDOnly(msgid);
            packet._error = exception;
            packet._peerTCP = _socket;

            _netMgr.AddPacket(_netMgr.AddPacketKey(), packet);
        }

        // 向Network Manager的佇列傳遞資料
        void PushPacket2(NetBitStream stream)
        {
            NetPacket packet = new NetPacket(stream.BYTES.Length);
            stream.BYTES.CopyTo(packet._bytes, 0);
            packet._peerTCP = stream._socketTCP;
            ushort msgid = 0;
            packet.TOID(out msgid);
            try
            {
                string packetkey = _netMgr.AddPacketKey();
                if (msgid == (short)MessageIdentifiers.ID.ID_CHAT || msgid == (short)MessageIdentifiers.ID.ID_CHAT2 || msgid == (short)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT || msgid == (short)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT2)
                {
                    NetBitStream stream2 = new NetBitStream();
                    stream2.BeginReadTCP2(packet);
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

