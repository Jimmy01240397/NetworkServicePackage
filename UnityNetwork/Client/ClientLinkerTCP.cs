using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace UnityNetwork.Client
{
    public class ClientLinkerTCP
    {
        NetTCPClient client;
        ClientListenTCP listener;


        bool getcheck = true;
        private int cantlink = 0;

        private NetworkManager networkManager;

        string _key = "";

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

        object locker = new object();

        private List<string> SendKey = new List<string>();
        private Dictionary<string, NetBitStream> Sendthing = new Dictionary<string, NetBitStream>();

        public ClientLinkerTCP(ClientListenTCP a)
        {
            listener = (ClientListenTCP)a;
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
            client = new NetTCPClient(networkManager);
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

        private void OnCheck(ushort ID)
        {
            if (ID == (ushort)MessageIdentifiers.ID.KEY || ID == (ushort)MessageIdentifiers.ID.CHECKING || ID == (ushort)MessageIdentifiers.ID.ID_CHAT || ID == (ushort)MessageIdentifiers.ID.ID_CHAT2 || ID == (ushort)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT || ID == (ushort)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT2)
            {
                lock (locker)
                {
                    getcheck = true;
                }
                if (ID == (ushort)MessageIdentifiers.ID.CHECKING)
                {
                    check();
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
            if (listener != null)
            {
                listener.DebugReturn("Disconnect");
            }
            client.Cheak -= OnCheck;
            client.GetMessage -= OnGetMessage;
            client.Disconnect(0);
            client = null;
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
                    stream.WriteResponse2(b, key);
                    stream.EncodeHeader();
                    client.Send(stream);
                }
                catch (Exception e)
                {
                    client.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message);
                }
            }
        }

        private void check()
        {
            try
            {
                NetBitStream stream = new NetBitStream();
                stream.BeginWrite((ushort)MessageIdentifiers.ID.CHECKING);
                stream.EncodeHeader();
                client.Send(stream);
                cantlink = 0;
            }
            catch (Exception e)
            {
                if (client != null && listener != null)
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
                                Thread aa = new Thread(new ThreadStart(Checker));
                                aa.Start();
                                Thread a = new Thread(new ThreadStart(check2));
                                a.Start();
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.CONNECTION_LOST:
                            {
                                if (packet._error == "")
                                {
                                    NetBitStream stream = new NetBitStream();
                                    stream.BeginReadTCP2(packet);
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
                                stream.BeginReadTCP2(packet);
                                if (listener != null)
                                {
                                    listener.OnOperationResponse(packet.response);
                                }
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.ID_CHAT2:
                            {
                                NetBitStream stream = new NetBitStream();
                                stream.BeginReadTCP2(packet);
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
                                check();
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.KEY:
                            {
                                NetBitStream stream = new NetBitStream();
                                stream.BeginReadTCP2(packet);
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

        void Checker()
        {
            try
            {
                while (client != null)
                {
                    Thread.Sleep(2000);
                    check();
                }
            }
            catch(Exception)
            {

            }
        }

        void check2()
        {
            bool Re = true;
            while (getcheck || Re)
            {
                Re = false;
                lock (locker)
                {
                    getcheck = false;
                }
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                while (stopwatch.ElapsedMilliseconds < 5000)
                {
                    Thread.Sleep(5000);
                }
                stopwatch.Stop();
                stopwatch.Reset();
                stopwatch.Start();
                SpinWait.SpinUntil(() => getcheck, 10000);
                stopwatch.Stop();
                if(stopwatch.ElapsedMilliseconds < 9500)
                {
                    Re = true;
                    getcheck = true;
                }
                stopwatch.Reset();
            }
            if (client != null)
            {
                client.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "超過5秒沒有回應");
            }
            getcheck = true;
        }
    }
}
