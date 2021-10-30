using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using JimmikerNetwork.Server;

namespace JimmikerNetwork
{
    class NetServerTCP : NetTCPBase, INetServer
    {
        public int MaxConnections { get; set; } = -1;

        public ProtocolType type { get; private set; } = ProtocolType.Tcp;

        public SerializationData.RSAKeyPair RSAkey { get; private set; }

        public List<PeerBase> SocketList { get; private set; }
        public List<Packet> Packets { get; private set; }
        public Dictionary<EndPoint, PeerBase> ToPeer { get; private set; }
        public Dictionary<object, string> SocketToKey { get; private set; }

        public event Action<string> GetMessage;

        Socket listener;

        public EndPoint ListenerIP
        {
            get
            {
                return listener.LocalEndPoint;
            }
        }

        public NetServerTCP()
        {
            //_maxConnections = maxConnections;

            SocketList = new List<PeerBase>();
            Packets = new List<Packet>();
            ToPeer = new Dictionary<EndPoint, PeerBase>();
            SocketToKey = new Dictionary<object, string>();
        }

        public bool CreateServer(IPAddress ip, int listenPort, out string a)
        {
            IPEndPoint ipe = new IPEndPoint(ip, listenPort);
            try
            {
                RSAkey = SerializationData.GenerateRSAKeys(2048);
                listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                listener.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                listener.Bind(ipe);
                listener.Listen(MaxConnections);
                listener.BeginAccept(ListenClient, listener);
                a = ipe.ToString() + " 成功建立伺服器";
            }
            catch(SocketException)
            {
                RSAkey = new SerializationData.RSAKeyPair();
                a = ipe.ToString() + "無法建立伺服器";
                return false;
            }
            return true;
        }

        private void ListenClient(System.IAsyncResult ar)
        {
            Socket _listener = (Socket)ar.AsyncState;
            Socket client = null;
            try
            {
                client = _listener.EndAccept(ar);
                _listener.BeginAccept(ListenClient, _listener);
            }
            catch (Exception e)
            {
                DebugMessage(e.ToString());
            }

            if (client != null)
            {
                #region Set def var
                int defReadTimeout = client.ReceiveTimeout;
                #endregion

                #region Set Read func
                SendData onRead(PacketType checkType, string key, Func<SendData, bool> datacheck)
                {
                    SendData ReadData = new SendData();
                    try
                    {
                        client.ReceiveTimeout = 1000;

                        byte[] data = new byte[Packet.header_length];
                        if (client.Receive(data) <= 0)
                        {
                            Disconnect(client);
                            return new SendData();
                        }

                        client.ReceiveTimeout = defReadTimeout;

                        int len = BitConverter.ToInt32(data, 0);
                        data = ReadSocketData(len, client);

                        using (Packet packet = new Packet(client, data, null, true))
                        {
                            if (packet.BeginRead() != checkType)
                            {
                                Disconnect(client);
                                return new SendData();
                            }
                            ReadData = packet.ReadSendData(key);
                        }
                    }
                    catch (Exception)
                    {
                        Disconnect(client);
                        return new SendData();
                    }

                    if (!datacheck(ReadData))
                    {
                        Disconnect(client);
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
                PushPacket(PacketType.CONNECT_SUCCESSFUL, AESkey, client);

                byte[] bytes = new byte[Packet.header_length];
                client.BeginReceive(bytes, 0, Packet.header_length, SocketFlags.None, Receive, new object[] { client, bytes });
                #endregion
            }
        }

        public void ConnectSuccessful(Func<object, INetServer, PeerBase> AddPeerFunc, Packet packet)
        {
            SocketToKey.Add(packet.peer, (string)packet.state);
            PeerBase peer = AddPeerFunc(packet.peer, this);
            SocketList.Add(peer);
            ToPeer.Add(((Socket)packet.peer).RemoteEndPoint, peer);

            using (Packet newpacket = new Packet(packet.peer))
            {
                packet.BeginWrite(PacketType.CONNECT_SUCCESSFUL);
                packet.WriteSendData(new SendData(0, "On Connect"), (string)packet.state, SerializationData.LockType.AES);
                Send(packet, packet.peer);
            }
        }

        public void Close()
        {
            if (listener != null)
            {
                try
                {
                    listener.Shutdown(SocketShutdown.Both);
                }
                catch (Exception) { }
                finally
                {
                    listener.Close();
                    listener = null;
                }
            }

            SocketList.Clear();
            Packets.Clear();
            ToPeer.Clear();
            SocketToKey.Clear();

            GetMessage = null;

            RSAkey = new SerializationData.RSAKeyPair();
        }

        private void Receive(IAsyncResult ar)
        {
            object[] state = (object[])ar.AsyncState;
            Socket client = (Socket)state[0];
            byte[] data = (byte[])state[1];
            PacketType msgid = (PacketType)(-1);
            Packet packet = null;
            try
            {
                int len = client.EndReceive(ar);

                if (!SocketList.Contains(ToPeer[client.RemoteEndPoint]))
                {
                    PushPacket(PacketType.CONNECTION_LOST, "連線已關閉", client);
                    return;
                }

                // 伺服器斷開連接
                if (len < 1)
                {
                    PushPacket(PacketType.CONNECTION_LOST, "遠端已斷線", client);
                    return;
                }

                byte[] databody = ReadSocketData(BitConverter.ToInt32(data, 0), client);
                packet = new Packet(client, databody, null, true);
                msgid = packet.BeginRead();
                packet.ResetPosition();

                if (data.Length != Packet.header_length)
                {
                    data = new byte[Packet.header_length];
                }
                client.BeginReceive(data, 0, Packet.header_length, SocketFlags.None, Receive, new object[] { client, data });
            }
            catch (Exception e)
            {
                PushPacket(PacketType.CONNECTION_LOST, e.ToString(), client);
                return;
            }
            if (msgid > PacketType.SendAllowTypeTop && msgid < PacketType.SendAllowTypeEnd)
            {
                PushPacket(packet);
            }
            else
            {
                switch (msgid)
                {
                    case PacketType.CONNECTION_LOST:
                        {
                            packet.BeginRead();
                            SendData sendData = packet.ReadSendData("");
                            PushPacket(PacketType.CONNECTION_LOST, sendData.DebugMessage, client);
                            packet.CloseStream();
                            break;
                        }
                    case PacketType.ClientDebugMessage:
                        {
                            packet.BeginRead();
                            SendData sendData = packet.ReadSendData(SocketToKey[packet.peer]);
                            DebugMessage("ClientDebugMessage:" + packet.peer.ToString() + " " + sendData.DebugMessage);

                            packet.CloseStream();
                            break;
                        }
                    default:
                        {
                            PushPacket(PacketType.CONNECTION_LOST, "不正確的標頭資訊 Receive", client);
                            packet.CloseStream();
                            break;
                        }
                }
            }
        }

        public bool Send(Packet bts, object peer)
        {
            Socket client = (Socket)peer;
            if (client.Connected)
            {
                lock (client)
                {
                    client.BeginSend(bts.Bytes, 0, bts.Length, SocketFlags.None, SendCallback, client);
                    return true;
                }
            }
            return false;
        }

        private void SendCallback(System.IAsyncResult ar)
        {
            Socket ns = (Socket)ar.AsyncState;
            try
            {
                ns.EndSend(ar);
            }
            catch (System.Exception)
            {
                //錯誤
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

        public void Disconnect(object socket)
        {
            Disconnect(socket , -1);
        }

        public void Disconnect(object socket, int timeout)
        {
            if (socket != null)
            {
                if (ToPeer.ContainsKey(((Socket)socket).RemoteEndPoint))
                {
                    if (SocketList.Contains(ToPeer[((Socket)socket).RemoteEndPoint]))
                    {
                        SocketList.Remove(ToPeer[((Socket)socket).RemoteEndPoint]);
                    }
                }
                if (SocketToKey.ContainsKey(socket))
                {
                    SocketToKey.Remove(socket);
                }
                if (((Socket)socket).RemoteEndPoint != null)
                {
                    if (ToPeer.ContainsKey(((Socket)socket).RemoteEndPoint))
                    {
                        ToPeer.Remove(((Socket)socket).RemoteEndPoint);
                    }
                }

                try
                {
                    ((Socket)socket).Shutdown(SocketShutdown.Both);
                }
                catch(Exception e)
                {
                    GetMessage(e.Message);
                }
                finally
                {
                    if (timeout == -1)
                    {
                        ((Socket)socket).Close();
                    }
                    else
                    {
                        ((Socket)socket).Close(timeout);
                    }
                    socket = null;
                }
            }
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

        public void DebugMessage(string message)
        {
            GetMessage?.Invoke(message);
        }
    }
}
