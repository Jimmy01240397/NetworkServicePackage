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

        /*// 開始監聽
        public bool CreateTcpServer(string ip, int listenPort, out string a)
        {
            _port = listenPort;
            int i = 0;
            a = "";
            foreach (IPAddress address in Dns.GetHostEntry(ip).AddressList)
            {
                try
                {
                    
                    IPEndPoint ipe = new IPEndPoint(Dns.GetHostAddresses(ip)[0], _port);
                    a += ipe.Address + ":" + ipe.Port + " ";
                    _listener = new TcpListener(ipe);
                    _listener.Start();
                    Thread myThread = new Thread(new ThreadStart(ListenTcpClient));
                    myThread.IsBackground = true;
                    myThread.Start();
                    Thread myThread2 = new Thread(new ThreadStart(Receive));
                    myThread2.IsBackground = true;
                    myThread2.Start();
                    ip = ipe.ToString();
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
        }*/


        public bool CreateTcpServer(string ip, int listenPort, out string a)
        {
            _port = listenPort;

            a = "";
            try
            {

                IPEndPoint ipe = new IPEndPoint(IPAddress.Any, _port);
                _listener = new TcpListener(ipe);
                _listener.Start(5000);

                _listener.BeginAcceptTcpClient(new System.AsyncCallback(ListenTcpClient), _listener);
                a = ipe.Address + ":" + ipe.Port + " ";
            }
            catch (System.Exception e)
            {
                a += e.ToString() + "  無法建立伺服器";
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
            while (stopwatch1.ElapsedMilliseconds < 2000 && go)
            {
                go = false;
                try
                {
                    NetBitStream stream = new NetBitStream();
                    Response bb = new Response(0, new Dictionary<byte, object>(), 0, Key);
                    stream.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                    stream.WriteResponse2(bb, "");
                    stream.EncodeHeader();

                    TimeStopWatch stopwatch = new TimeStopWatch();
                    stopwatch.Start();
                    while (!ns.CanWrite || stopwatch.ElapsedMilliseconds > 200) { }
                    stopwatch.Stop();
                    stopwatch.Reset();
                    stopwatch = null;
                    if (ns.CanWrite)
                    {
                            ns.WriteTimeout = 500;
                            Stopwatch stopwatch2 = new Stopwatch();
                            stopwatch2.Start();
                            while (true)
                            {
                                try
                                {
                                    ns.Write(stream.BYTES, 0, stream.Length);
                                    stopwatch2.Stop();
                                    stopwatch2.Reset();
                                    stopwatch2 = null;
                                    break;
                                }
                                catch (Exception e)
                                {
                                    if (stopwatch2.ElapsedMilliseconds > 2000)
                                    {
                                        stopwatch2.Stop();
                                        stopwatch2.Reset();
                                        stopwatch2 = null;
                                        throw e;
                                    }
                                }
                            }
                    }
                    stream = null;
                    stream = new NetBitStream();
                    ns.ReadTimeout = 1000;
                    stream.BYTES = new byte[NetBitStream.header_length];
                    Stopwatch stopwatch3 = new Stopwatch();
                    stopwatch3.Start();
                    while (true)
                    {
                        if (ns.DataAvailable)
                        {
                            ns.Read(stream.BYTES, 0, NetBitStream.header_length);
                            stopwatch3.Stop();
                            stopwatch3.Reset();
                            stopwatch3 = null;
                            break;
                        }
                        else
                        {
                            if (stopwatch3.ElapsedMilliseconds > 2000)
                            {
                                stopwatch3.Stop();
                                stopwatch3.Reset();
                                stopwatch3 = null;
                                throw new Exception("為於時間內讀取完畢");
                            }
                        }
                    }
                    stream.DecodeHeader();
                    stream.BYTES = new byte[NetBitStream.header_length + stream.BodyLength];
                    Stopwatch stopwatch4 = new Stopwatch();
                    stopwatch4.Start();
                    for (int iIndex = 0; iIndex < stream.BodyLength;)
                    {
                        byte[] buffer = new byte[stream.BodyLength];
                        if (ns.CanRead)
                        {
                            if (ns.DataAvailable)
                            {
                                int j = ns.Read(buffer, 0, stream.BodyLength - iIndex);
                                Array.Copy(buffer, 0, stream.BYTES, NetBitStream.header_length + iIndex, j);
                                iIndex += j;
                            }
                            else if (stopwatch4.ElapsedMilliseconds > 4000)
                            {
                                stopwatch4.Stop();
                                stopwatch4.Reset();
                                stopwatch4 = null;
                                throw new Exception("為於時間內讀取完畢");
                            }
                        }
                    }
                    stopwatch4.Stop();
                    stopwatch4.Reset();
                    stopwatch4 = null;
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

                                NetBitStream stream1 = new NetBitStream();
                                Response bbbb = new Response(1, new Dictionary<byte, object>(), 0, "good");
                                stream1.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                                stream1.WriteResponse2(bbbb, "");
                                stream1.EncodeHeader();
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
                        NetBitStream stream3 = new NetBitStream();
                        Response bbb = new Response();
                        bbb.DebugMessage = e.ToString();
                        stream3.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                        stream3.WriteResponse2(bbb, "");
                        stream3.EncodeHeader();
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
                    NetBitStream stream3 = new NetBitStream();
                    Response bbb = new Response();
                    bbb.DebugMessage = "驗證碼錯誤";
                    stream3.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                    stream3.WriteResponse2(bbb, "");
                    stream3.EncodeHeader();
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

        /*// 接受一個新的連接
        void ListenTcpClient()
        {
            // bit stream
            while (run)
            {
                if (_netMgr._socketList.Count < _maxConnections || _maxConnections == -1)
                {
                    try
                    {
                        //建立與客戶端的連線
                        TcpClient client = _listener.AcceptTcpClient();
                        client.ReceiveTimeout = 1000;
                        client.SendTimeout = 500;
                        Thread.Sleep(100);
                        string[] b = Guid.NewGuid().ToString().Split('-');
                        string Key = "";
                        for (int i = 0; i < b.Length; i++)
                        {
                            Key += b[i];
                        }
                        if (client.Connected)
                        {
                            try
                            {
                                NetBitStream stream = new NetBitStream();
                                Response bb = new Response(0, new Dictionary<byte, object>(), 0, Key);
                                stream.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                                stream.WriteResponse2(bb, "");
                                stream.EncodeHeader();

                                NetworkStream ns2;
                                lock (client)
                                {
                                    ns2 = client.GetStream();
                                }
                                TimeStopWatch stopwatch = new TimeStopWatch();
                                stopwatch.Start();
                                while (!ns2.CanWrite || stopwatch.ElapsedMilliseconds > 200) { }
                                stopwatch.Stop();
                                stopwatch.Reset();
                                stopwatch = null;
                                if (ns2.CanWrite)
                                {
                                    try
                                    {
                                        ns2.WriteTimeout = 500;
                                        bool get2 = false;
                                        Thread thread = new Thread(new ThreadStart(() =>
                                        {
                                            try
                                            {
                                                Thread.Sleep(3000);
                                                if (!get2)
                                                {
                                                    ns2.Close();
                                                }
                                            }
                                            catch(Exception)
                                            {

                                            }
                                        }));
                                        thread.Start();
                                        Stopwatch stopwatch2 = new Stopwatch();
                                        stopwatch2.Start();
                                        while (true)
                                        {
                                            try
                                            {
                                                ns2.Write(stream.BYTES, 0, stream.Length);
                                                stopwatch2.Stop();
                                                stopwatch2.Reset();
                                                stopwatch2 = null;
                                                break;
                                            }
                                            catch(Exception e)
                                            {
                                                if(stopwatch2.ElapsedMilliseconds > 2000)
                                                {
                                                    stopwatch2.Stop();
                                                    stopwatch2.Reset();
                                                    stopwatch2 = null;
                                                    throw e;
                                                }
                                            }
                                        }
                                        get2 = true;
                                        thread.Abort();
                                    }
                                    catch (System.Exception e)
                                    {
                                        throw e;
                                    }
                                }

                                stream = null;
                                stream = new NetBitStream();

                                NetworkStream ns = client.GetStream();
                                ns.ReadTimeout = 1000;
                                stream.BYTES = new byte[NetBitStream.header_length];
                                bool get = false;
                                bool nostop = true;
                                Thread thread1 = new Thread(new ThreadStart(() =>
                                {
                                    try
                                    {
                                        Thread.Sleep(3000);
                                        if (!get)
                                        {
                                            nostop = false;
                                            ns.Close();
                                        }
                                    }
                                    catch(Exception)
                                    {

                                    }
                                }));
                                thread1.Start();
                                Stopwatch stopwatch3 = new Stopwatch();
                                stopwatch3.Start();
                                while (true)
                                {
                                    try
                                    {
                                        ns.Read(stream.BYTES, 0, NetBitStream.header_length);
                                        stopwatch3.Stop();
                                        stopwatch3.Reset();
                                        stopwatch3 = null;
                                        break;
                                    }
                                    catch(Exception e)
                                    {
                                        if(stopwatch3.ElapsedMilliseconds > 2000)
                                        {
                                            stopwatch3.Stop();
                                            stopwatch3.Reset();
                                            stopwatch3 = null;
                                            throw e;
                                        }
                                    }
                                }
                                stream.DecodeHeader();
                                if (stream.BodyLength == 109)
                                {
                                    stream.BYTES = new byte[NetBitStream.header_length + stream.BodyLength];
                                    for (int iIndex = 0; iIndex < stream.BodyLength && nostop;)
                                    {
                                        byte[] buffer = new byte[stream.BodyLength];
                                        if (ns.CanRead)
                                        {
                                            int j = ns.Read(buffer, 0, stream.BodyLength - iIndex);
                                            Array.Copy(buffer, 0, stream.BYTES, NetBitStream.header_length + iIndex, j);
                                            iIndex += j;
                                        }
                                    }
                                    get = true;
                                    thread1.Abort();
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
                                        if (Convert.ToInt32(stream.thing.Parameters[0]) < 10000)
                                        {
                                            PushPacket((ushort)MessageIdentifiers.ID.NEW_INCOMING_CONNECTION, Key, client);
                                        }
                                        else
                                        {
                                            GetMessage(Convert.ToInt32(stream.thing.Parameters[0]).ToString());
                                            client.Close();
                                            client = null;
                                        }
                                    }
                                }
                                else
                                {
                                    GetMessage(stream.BodyLength.ToString());
                                    client.Close();
                                    client = null;
                                }
                            }
                            catch (Exception e)
                            {
                                GetMessage(e.ToString());
                                client.Close();
                                client = null;
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        try
                        {
                            GetMessage(e.Message);
                        }
                        catch(Exception)
                        {

                        }
                    }
                }
            }
            // 繼續接受其它連接 
        }*/

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

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                for (int iIndex = 0; iIndex < stream.BodyLength;)
                {
                    byte[] buffer = new byte[stream.BodyLength];
                    if (ns.CanRead && ns.DataAvailable)
                    {
                        int j = ns.Read(buffer, 0, stream.BodyLength - iIndex);
                        Array.Copy(buffer, 0, stream.BYTES, NetBitStream.header_length + iIndex, j);
                        iIndex += j;
                    }
                    else if (stopwatch.ElapsedMilliseconds > 4000)
                    {
                        stopwatch.Stop();
                        stopwatch.Reset();
                        stopwatch = null;
                        return;
                    }
                }
                stopwatch.Stop();
                stopwatch.Reset();
                stopwatch = null;
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


        /*// 接收頭訊息
        void Receive()
        {
            while (run)
            {
                try
                {
                    if (_netMgr != null && _netMgr._socketList != null)
                    {
                        for (int i = 0; i < _netMgr._socketList.Count; i++)
                        {
                            TcpClient client = null;
                            NetBitStream stream = new NetBitStream();
                            try
                            {
                                client = ((UnityNetwork.Server.PeerBase)_netMgr._socketList[i]).socket;
                            }
                            catch (Exception)
                            {

                            }
                            try
                            {
                                if (client != null && client.Connected && !_netMgr._blackList.Contains(_netMgr._socketList[i]))
                                {
                                    NetworkStream ns = client.GetStream();
                                    if (ns.CanRead && ns.DataAvailable)
                                    {
                                        stream.BYTES = new byte[NetBitStream.header_length];

                                        bool get = false;
                                        bool nostop = true;
                                        Thread thread1 = new Thread(new ThreadStart(() =>
                                        {
                                            try
                                            {
                                                Thread.Sleep(1000);
                                                if (!get)
                                                {
                                                    nostop = false;
                                                }
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }));
                                        thread1.Start();

                                        ns.Read(stream.BYTES, 0, NetBitStream.header_length);
                                        stream.DecodeHeader();
                                        stream.BYTES = new byte[NetBitStream.header_length + stream.BodyLength];

                                        for (int iIndex = 0; iIndex < stream.BodyLength && nostop;)
                                        {
                                            byte[] buffer = new byte[stream.BodyLength];
                                            if (ns.CanRead && ns.DataAvailable)
                                            {
                                                int j = ns.Read(buffer, 0, stream.BodyLength - iIndex);
                                                Array.Copy(buffer, 0, stream.BYTES, NetBitStream.header_length + iIndex, j);
                                                iIndex += j;
                                            }
                                        }

                                        get = true;
                                        thread1.Abort();

                                        stream._socketTCP = client;
                                        try
                                        {
                                            ushort msgid = PushPacket2(stream);
                                            if (msgid <= 0 || msgid >= 10)
                                            {
                                                try
                                                {
                                                    NetBitStream stream2 = new NetBitStream();
                                                    Response b = new Response();
                                                    b.DebugMessage = "不正確的標頭資訊";
                                                    stream2.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                                                    stream2.WriteResponse2(b, "");
                                                    stream2.EncodeHeader();
                                                    Send(stream2, client);
                                                }
                                                catch (System.Exception ee)
                                                {
                                                    GetMessage(ee.Message);
                                                }
                                                finally
                                                {
                                                    _netMgr._blackList.Add(_netMgr._socketList[i]);
                                                }
                                            }
                                        }
                                        catch (System.Exception e)
                                        {
                                            try
                                            {
                                                NetBitStream stream2 = new NetBitStream();
                                                Response b = new Response();
                                                b.DebugMessage = e.Message;
                                                GetMessage(e.Message);
                                                stream2.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                                                stream2.WriteResponse2(b, "");
                                                stream2.EncodeHeader();
                                                Send(stream2, client);
                                            }
                                            catch (System.Exception ee)
                                            {
                                                GetMessage(ee.Message);

                                            }
                                            finally
                                            {
                                                _netMgr._blackList.Add(_netMgr._socketList[i]);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (System.Exception ee)
                            {
                                try
                                {
                                    NetBitStream stream2 = new NetBitStream();
                                    Response b = new Response();
                                    b.DebugMessage = ee.Message;
                                    GetMessage(ee.Message);
                                    stream2.BeginWrite((ushort)MessageIdentifiers.ID.CONNECTION_LOST);
                                    stream2.WriteResponse2(b, "");
                                    stream2.EncodeHeader();
                                    Send(stream2, client);
                                }
                                catch (System.Exception eee)
                                {
                                    GetMessage(eee.Message);
                                }
                                finally
                                {
                                    _netMgr._blackList.Add(_netMgr._socketList[i]);
                                }
                            }
                        }
                    }
                }
                catch(Exception)
                {

                }
            }
        }*/

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

