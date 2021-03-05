using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace UnityNetwork
{
    public class NetTCPServer
    {
        // 最大連接數
        private int _maxConnections = -1;
        public int _sendTimeout = 500;
        public int _revTimeout = 1000;

        TcpListener _listener;
        public delegate void Message(string i);
        public event Message GetMessage;

        // 埠號
        int _port = 0;

        // 網路管理器 處理訊息和邏輯
        private NetworkManager _netMgr = null;

        public NetTCPServer(NetworkManager network, int maxConnections)
        {
            _maxConnections = maxConnections;
            _netMgr = network;
        }

        public bool CreateTcpServer(string ip, int listenPort, out string a)
        {
            _port = listenPort;


            int i = 0;
            a = "";
            foreach (IPAddress address in Dns.GetHostEntry(ip).AddressList)
            {
                try
                {

                    IPEndPoint ipe = new IPEndPoint(IPAddress.Any, _port);
                    a += ipe.Address + ":" + ipe.Port + " ";
                    _listener = new TcpListener(ipe);
                    _listener.Start(5000);

                    _listener.BeginAcceptTcpClient(new System.AsyncCallback(ListenTcpClient), _listener);

                    ip = Dns.GetHostAddresses(ip)[0].ToString() + ":" + _port;
                    i = 1;
                    break;

                }
                catch (System.Exception)
                {

                }
            }
            if (i == 1)
            {
                a = ip;
            }
            else
            {
                a += "無法建立伺服器";
                return false;
            }
            return true;
        }

        void ListenTcpClient(System.IAsyncResult ar)
        {
            // bit stream
            try
            {
                // 取得伺服器端的Socket
                // Socket listener = (Socket)ar.AsyncState;

                // 取得用戶端的socket
                TcpClient client = _listener.EndAcceptTcpClient(ar);

                try
                {
                    _listener.BeginAcceptTcpClient(new System.AsyncCallback(ListenTcpClient), _listener);
                }
                catch (Exception)
                {

                }

                // 設置 timeout時間
                client.SendTimeout = _sendTimeout;
                client.ReceiveTimeout = _revTimeout;


                string[] b = Guid.NewGuid().ToString().Split('-');
                string Key = "";
                for (int i = 0; i < b.Length; i++)
                {
                    Key += b[i];
                }
                if (client.Connected)
                {
                    SendAndCheckKey(client, Key);
                }
                else
                {
                    try
                    {
                        NetBitStream stream3 = new NetBitStream();
                        Response bbb = new Response();
                        bbb.DebugMessage = "未連接";
                        stream3.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                        stream3.WriteResponse2(bbb, "");
                        stream3.EncodeHeader();
                        client.GetStream().WriteTimeout = 500;
                        client.GetStream().Write(stream3.BYTES, 0, stream3.Length);
                    }
                    catch (Exception e)
                    {
                        GetMessage?.Invoke(e.ToString());
                    }
                    client.GetStream().Flush();
                    client.GetStream().Close();
                    client.Close();
                    client = null;
                }
            }
            catch (System.Exception e)
            {
                    GetMessage?.Invoke(e.Message + " from ListenTcpClient");
            }
            // 繼續接受其它連接
        }

        public void SendAndCheckKey(TcpClient client, string Key)
        {
            bool go = true;
            bool good = false;
            Stopwatch stopwatch1 = new Stopwatch();
            stopwatch1.Start();
            NetworkStream ns = null;
            lock (client)
            {
                ns = client.GetStream();
            }

            NetBitStream SendOut(MessageIdentifiers.ID use, Response response)
            {
                NetBitStream stream = new NetBitStream();
                stream.BeginWrite((ushort)use);
                stream.WriteResponse2(response, "");
                stream.EncodeHeader();
                return stream;
            }

            while (stopwatch1.ElapsedMilliseconds < 2000 && go)
            {
                go = false;
                try
                {
                    NetBitStream stream = SendOut(MessageIdentifiers.ID.KEY, new Response(0, new Dictionary<byte, object>(), 0, Key));

                    SpinWait.SpinUntil(() => ns.CanWrite, 200);
                    if (ns.CanWrite)
                    {
                        ns.WriteTimeout = 500;
                        SpinWait.SpinUntil(() =>
                        {
                            try
                            {
                                ns.Write(stream.BYTES, 0, stream.Length);
                                return true;
                            }
                            catch (Exception)
                            {
                                return false;
                            }
                        }, 2000);
                    }
                    stream = null;
                    stream = new NetBitStream();
                    ns.ReadTimeout = 1000;
                    stream.BYTES = new byte[NetBitStream.header_length];
                    SpinWait.SpinUntil(() => ns.DataAvailable, 2000);
                    if (ns.DataAvailable)
                    {
                        ns.Read(stream.BYTES, 0, NetBitStream.header_length);
                    }
                    else
                    {
                        throw new Exception("為於時間內讀取完畢");
                    }
                    stream.DecodeHeader();
                    stream.BYTES = new byte[NetBitStream.header_length + stream.BodyLength];
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
                            throw new Exception("為於時間內讀取完畢");
                        }
                    }
                    stream._socketTCP = client;
                    ushort msgid = System.BitConverter.ToUInt16(stream.BYTES, NetBitStream.header_length);
                    NetPacket packet = new NetPacket(stream.BYTES.Length);
                    stream.BYTES.CopyTo(packet._bytes, 0);
                    packet._peerTCP = stream._socketTCP;
                    stream = null;
                    stream = new NetBitStream();
                    stream.BeginReadTCP2(packet);
                    if (msgid == (ushort)MessageIdentifiers.ID.KEY)
                    {
                        stream.ReadResponse2(Key);
                        stream.EncodeHeader();
                        try
                        {
                            if (Convert.ToInt32(stream.thing.Parameters[0]) < 10000)
                            {
                                good = true;

                                NetBitStream stream1 = SendOut(MessageIdentifiers.ID.KEY, new Response(1, new Dictionary<byte, object>(), 0, "good"));
                                ns.WriteTimeout = 3000;
                                ns.Write(stream1.BYTES, 0, stream1.Length);

                                // 接收從伺服器返回的頭資訊
                                NetBitStream stream2 = new NetBitStream();
                                stream2._socketTCP = client;
                                stream2.BYTES = new byte[NetBitStream.header_length];
                                ns.BeginRead(stream2.BYTES, 0, NetBitStream.header_length, new System.AsyncCallback(Receive), new object[] { ns, client, stream2.BYTES });

                                // 發送訊息 建立一個新的連接
                                PushPacket((ushort)MessageIdentifiers.ID.NEW_INCOMING_CONNECTION, Key, client);
                            }
                            else
                            {
                                go = true;
                            }
                        }
                        catch (Exception)
                        {
                            go = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        GetMessage?.Invoke(e.ToString() + " SendAndCheckKey");
                        NetBitStream stream3 = SendOut(MessageIdentifiers.ID.CONNECTION_LOST, new Response(e.ToString()));
                        ns.WriteTimeout = 500;
                        ns.Write(stream3.BYTES, 0, stream3.Length);
                    }
                    catch (Exception ee)
                    {
                        GetMessage?.Invoke(ee.ToString());
                    }
                    ns.Flush();
                    ns.Close();
                    client.Close();
                    client = null;
                }
            }
            stopwatch1.Stop();
            stopwatch1.Reset();
            stopwatch1 = null;
            if (!good)
            {
                try
                {
                    GetMessage?.Invoke("驗證碼錯誤");
                    NetBitStream stream3 = SendOut(MessageIdentifiers.ID.CONNECTION_LOST, new Response("驗證碼錯誤"));
                    ns.WriteTimeout = 500;
                    ns.Write(stream3.BYTES, 0, stream3.Length);
                }
                catch (Exception e)
                {
                    GetMessage?.Invoke(e.ToString() + " SendAndCheckKey !good");
                }
                ns.Flush();
                ns.Close();
                client.Close();
                client = null;
            }
        }

        public void close()
        {
            _listener.Stop();
        }

        void Receive(System.IAsyncResult ar)
        {
            NetBitStream stream = new NetBitStream();
            try
            {
                object[] ar2 = (object[])ar.AsyncState;
                NetworkStream ns = (NetworkStream)ar2[0];
                TcpClient client = (TcpClient)ar2[1];
                byte[] bytes = (byte[])ar2[2];

                stream._socketTCP = client;
                int read = ns.EndRead(ar);
                // 伺服器斷開連接
                if (read < 1)
                {
                    // 發送訊息 失去一個連接
                    GetMessage?.Invoke("只讀到" + read + "位元?");
                    //PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "", stream._socketTCP);
                    return;
                }



                stream.BYTES = bytes;

                // 獲得訊息體長度
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
                try
                {
                    NetBitStream stream3 = new NetBitStream();
                    stream3._socketTCP = client;

                    stream3.BYTES = new byte[NetBitStream.header_length];

                    ushort msgid = PushPacket2(stream);

                    ns.BeginRead(stream3.BYTES, 0, NetBitStream.header_length, new System.AsyncCallback(Receive), new object[] { ns, stream3._socketTCP, stream3.BYTES });

                    if (msgid <= 0 || msgid >= 12)
                    {
                        try
                        {
                            GetMessage?.Invoke("不正確的標頭資訊 Receive msgid <= 0 || msgid >= 12 try");
                            NetBitStream stream2 = new NetBitStream();
                            Response b = new Response();
                            b.DebugMessage = "不正確的標頭資訊";
                            stream2.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                            stream2.WriteResponse2(b, "");
                            stream2.EncodeHeader();
                            Send(stream2, stream._socketTCP);
                        }
                        catch (System.Exception ee)
                        {
                            GetMessage?.Invoke(ee.Message + " Receive msgid <= 0 || msgid >= 12 catch");
                        }
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    try
                    {
                        GetMessage?.Invoke(e.Message + " Receive catch try");
                        NetBitStream stream2 = new NetBitStream();
                        Response b = new Response();
                        b.DebugMessage = e.Message;
                        GetMessage?.Invoke(e.Message);
                        stream2.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                        stream2.WriteResponse2(b, "");
                        stream2.EncodeHeader();
                        Send(stream2, stream._socketTCP);
                    }
                    catch (System.Exception ee)
                    {
                        GetMessage?.Invoke(ee.Message + " Receive catch catch");

                    }
                    return;
                }

                // 下一個讀取
            }
            catch (System.Exception e)
            {
                PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message + " Receive", stream._socketTCP);
            }
        }

        public void Send(NetBitStream bts, TcpClient peer)
        {
            NetworkStream ns;
            lock (peer)
            {
                ns = peer.GetStream();
            }

            if (ns.CanWrite)
            {
                    ns.WriteTimeout = 500;
                    ns.BeginWrite(bts.BYTES, 0, bts.Length, new System.AsyncCallback(SendCallback), ns);
            }
        }

        private void SendCallback(System.IAsyncResult ar)
        {

            NetworkStream ns = (NetworkStream)ar.AsyncState;
            try
            {
                ns.EndWrite(ar);
            }
            catch (System.Exception)
            {
                //錯誤
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
        public void PushPacket(ushort msgid, string exception, TcpClient peer)
        {

            NetPacket packet = new NetPacket(NetBitStream.header_length + NetBitStream.max_body_length);
            packet.SetIDOnly(msgid);
            if (msgid == (short)MessageIdentifiers.ID.CONNECTION_LOST)
            {
                packet._error = DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + " " + exception;
            }
            else
            {
                packet._error = exception;
            }
            packet._peerTCP = peer;

            _netMgr.AddPacket(_netMgr.AddPacketKey(), packet);
        }

        // 向Network Manager的佇列傳遞資料
        public ushort PushPacket2(NetBitStream stream)
        {
            NetPacket packet = new NetPacket(stream.BYTES.Length);
            stream.BYTES.CopyTo(packet._bytes, 0);
            packet._peerTCP = stream._socketTCP;
            ushort msgid = 0;
            packet.TOID(out msgid);
            ThreadPool.QueueUserWorkItem((aa) =>
            {
                try
                {
                    string packetkey = _netMgr.AddPacketKey();
                    if (msgid == (short)MessageIdentifiers.ID.ID_CHAT || msgid == (short)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT)
                    {
                        NetBitStream stream2 = new NetBitStream();
                        stream2.BeginReadTCP2(packet);
                        if (_netMgr.ToPeerTCP.ContainsKey(stream2._socketTCP))
                        {
                            stream2.ReadResponse2(((UnityNetwork.Server.PeerTCPBase)_netMgr.ToPeerTCP[stream2._socketTCP]).Key);
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
                catch (Exception e)
                {
                    GetMessage(e.ToString() + "PushPacket2");
                }
            });
            return msgid;
        }
    }
}

