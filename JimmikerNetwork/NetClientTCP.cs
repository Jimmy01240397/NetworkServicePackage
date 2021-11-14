using JimmikerNetwork.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace JimmikerNetwork
{
    class NetClientTCP : NetTCPBase, INetClient
    {
        Socket socket;

        public bool EnableP2P
        {
            get
            {
                return false;
            }
            set
            {
                throw new P2PException("TCP do not allow P2P.");
            }
        }

        public Dictionary<object, string> P2PSocketToKey { get; private set; }

        public EncryptAndCompress.RSAKeyPair P2PRSAkey { get; private set; }

        public List<PeerForP2PBase> P2PSocketList { get; private set; }
        public Dictionary<EndPoint, PeerForP2PBase> P2PToPeer { get; private set; }

        public EncryptAndCompress.RSAKeyPair RSAkey { get; private set; }

        public string AESkey { get; private set; }

        public List<Packet> Packets { get; private set; }

        public EndPoint LocalEndPoint
        {
            get
            {
                return socket.LocalEndPoint;
            }
        }

        public EndPoint RemoteEndPoint
        {
            get
            {
                return socket.RemoteEndPoint;
            }
        }

        public NetClientTCP()
        {
            P2PSocketList = new List<PeerForP2PBase>();
            Packets = new List<Packet>();
        }

        public NetClientTCP(bool enableP2P)
        {
            P2PSocketList = new List<PeerForP2PBase>();
            Packets = new List<Packet>();
            EnableP2P = enableP2P;
        }

        public event Action<string> GetMessage;

        public bool Connect(string serverhost, int remotePort)
        {
            if (socket != null && socket.Connected)
                return true;
            else if(socket != null)
            {
                socket.Close();
                socket = null;
            }

            socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            try
            {
                IPAddress address;
                if(!IPAddress.TryParse(serverhost, out address))
                {
                    address = Dns.GetHostEntry(serverhost).AddressList[0];
                }
                IPEndPoint ipe = new IPEndPoint(address, remotePort);

                DebugMessage("正在嘗試連線IP:" + ipe.ToString());
                socket.BeginConnect(ipe, ConnectionCallback, socket);
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
                socket.EndConnect(ar);

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

                        byte[] data = new byte[Packet.header_length];
                        if (socket.Receive(data) <= 0)
                        {
                            PushPacket(PacketType.CONNECTION_ATTEMPT_FAILED, "CONNECTION Shutdown");
                            Disconnect();
                            return new SendData();
                        }

                        socket.ReceiveTimeout = defReadTimeout;

                        int len = BitConverter.ToInt32(data, 0);
                        data = ReadSocketData(len, socket);
                        using (Packet packet = new Packet(socket, data, null, true))
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
                void onSend(PacketType sendType, string key, EncryptAndCompress.LockType lockType, SendData send)
                {
                    using (Packet packet = new Packet(socket))
                    {
                        packet.BeginWrite(sendType);
                        packet.WriteSendData(send, key, lockType);
                        Send(packet);
                    }
                }
                #endregion

                #region Start Get Public Key
                SendData sendData = onRead(PacketType.RSAKEY, "", (a) => true);
                if (sendData == new SendData())
                    return;

                RSAkey = new EncryptAndCompress.RSAKeyPair((byte[])sendData.Parameters);
                #endregion

                #region Generate And Send AES Key
                AESkey = EncryptAndCompress.GenerateAESKey();
                onSend(PacketType.AESKEY, RSAkey.PublicKey, EncryptAndCompress.LockType.RSA, new SendData(0, AESkey));
                #endregion

                #region Check AES Key
                sendData = onRead(PacketType.AESKEY, AESkey, (a) => a.Parameters.ToString() == "Connect check");
                if (sendData == new SendData())
                    return;
                #endregion

                #region Send CONNECT_SUCCESSFUL
                onSend(PacketType.CONNECT_SUCCESSFUL, AESkey, EncryptAndCompress.LockType.AES, new SendData(0, "Connect successful"));
                #endregion

                #region On CONNECT
                sendData = onRead(PacketType.CONNECT_SUCCESSFUL, AESkey, (a) => a.Parameters.ToString() == "On Connect");
                if (sendData == new SendData())
                    return;

                PushPacket(PacketType.CONNECT_SUCCESSFUL, "");


                byte[] bytes = new byte[Packet.header_length];
                socket.BeginReceive(bytes, 0, Packet.header_length, SocketFlags.None, Receive, new object[] { bytes });

                #endregion
            }
            catch (Exception e)
            {
                PushPacket(PacketType.CONNECTION_ATTEMPT_FAILED, e.ToString());
                DebugMessage(e.ToString());
                //Disconnect(0);
            }
        }

        private void Receive(IAsyncResult ar)
        {
            object[] state = (object[])ar.AsyncState;
            byte[] data = (byte[])state[0];
            PacketType msgid = (PacketType)(-1);
            Packet packet = null;
            try
            {
                if(socket == null)
                {
                    PushPacket(PacketType.CONNECTION_LOST, "連線已關閉");
                    return;
                }


                int len = socket.EndReceive(ar);
                // 伺服器斷開連接
                if (len < 1)
                {
                    PushPacket(PacketType.CONNECTION_LOST, "遠端已斷線");
                    return;
                }


                data = ReadSocketData(BitConverter.ToInt32(data, 0), socket);
                packet = new Packet(socket, data, null, true);
                msgid = packet.BeginRead();
                packet.ResetPosition();

                data = new byte[Packet.header_length];
                socket.BeginReceive(data, 0, Packet.header_length, SocketFlags.None, Receive, new object[] { data });
            }
            catch(Exception e)
            {
                PushPacket(PacketType.CONNECTION_LOST, e.ToString());
                return;
            }
            if (msgid == PacketType.CONNECTION_LOST)
            {
                packet.BeginRead();
                SendData sendData = packet.ReadSendData("");
                PushPacket(PacketType.CONNECTION_LOST, sendData.DebugMessage);
                packet.CloseStream();
            }
            else if (msgid > PacketType.SendAllowTypeTop && msgid < PacketType.SendAllowTypeEnd)
            {
                PushPacket(packet);
            }
            else
            {
                PushPacket(PacketType.CONNECTION_LOST, "不正確的標頭資訊 Receive");
                packet.CloseStream();
            }
        }

        public void ConnectSuccessful(Packet packet)
        {
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

        public void Disconnect()
        {
            Disconnect(-1);
        }

        public void Disconnect(int timeout)
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

            GetMessage = null;

            Packets.Clear();
            AESkey = null;
            RSAkey = new EncryptAndCompress.RSAKeyPair();
        }

        public void PushPacket(PacketType msgid, string exception)
        {
            Packet packet = new Packet(socket, null, exception);
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

        public bool Send(Packet bts)
        {
            if (!socket.Connected)
            {
                DebugMessage("沒有連線至伺服器");
                return false;
            }

            lock (socket)
            {
                try
                {
                    socket.BeginSend(bts.Bytes, 0, bts.Length, SocketFlags.None, SendCallback, socket);
                    return true;
                }
                catch (System.Exception e)
                {
                    PushPacket(PacketType.CONNECTION_LOST, e.ToString());
                    return false;
                }
            }
        }

        private void SendCallback(System.IAsyncResult ar)
        {
            //Socket ns = (Socket)ar.AsyncState;
            try
            {
                socket.EndSend(ar);
            }
            catch (System.Exception e)
            {
                PushPacket(PacketType.CONNECTION_LOST, "寄信回檔期間出問題：" + e.ToString());
            }
        }

        public void DebugMessage(string message)
        {
            GetMessage?.Invoke(message);
        }

        public void StartP2PConnect(IPEndPoint IPPort, Action<EndPoint, EndPoint, PeerForP2PBase, bool> callback)
        {
            throw new P2PException("TCP do not allow P2P.");
        }

        public void WaitP2PConnect(IPEndPoint IPPort, Action<EndPoint, EndPoint, PeerForP2PBase, bool> callback)
        {
            throw new P2PException("TCP do not allow P2P.");
        }

        public void P2PNATPacketSend(Packet packet)
        {
            throw new P2PException("TCP do not allow P2P.");
        }

        public void P2PConnectSuccessful(Func<object, object, INetClient, bool, PeerForP2PBase> P2PAddPeer, Packet packet)
        {
            throw new P2PException("TCP do not allow P2P.");
        }

        public void P2PDisconnect(object socket)
        {
            throw new P2PException("TCP do not allow P2P.");
        }

        public void P2PDisconnect(object socket, int timeout)
        {
            throw new P2PException("TCP do not allow P2P.");
        }

        public void P2PPushPacket(PacketType msgid, string Key, object remote, object remotePublic, bool NAT)
        {
            throw new P2PException("TCP do not allow P2P.");
        }

        public void P2PPushPacket(PacketType msgid, string exception, object remote)
        {
            throw new P2PException("TCP do not allow P2P.");
        }
    }
}