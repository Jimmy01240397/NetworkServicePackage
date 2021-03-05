using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace UnityNetwork.Client
{
    public class ClientLinkerUDP : NetworkManager
    {
        NetUDPClient client;
        ClientListenUDP listener;
        bool getcheck = true;
        private int cantlink = 0;
        public string key { get; private set; } = "";
        public int UnLockTime = 0;
        public int LockTime = 0;

        object locker = new object();

        List<string> SendKey = new List<string>();
        Dictionary<string, NetBitStream> Sendthing = new Dictionary<string, NetBitStream>();

        public ClientLinkerUDP(ClientListenUDP a) : base()
        {
            listener = (ClientListenUDP)a;
        }
        public bool Connect(string ip, int port)
        {
            if (client != null)
            {
                client.Cheak -= OnCheck;
                client.GetMessage -= OnGetMessage;
            }
            client = null;
            while (!getcheck) { }
            client = new NetUDPClient(this);
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

        private void OnCheck(ushort ID, NetBitStream stream)
        {
            if (ID == (ushort)MessageIdentifiers.ID.KEY || ID == (ushort)MessageIdentifiers.ID.CHECKING || ID == (ushort)MessageIdentifiers.ID.ID_CHAT || ID == (ushort)MessageIdentifiers.ID.ID_CHAT2 || ID == (ushort)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT || ID == (ushort)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT2)
            {
                lock (locker)
                {
                    getcheck = true;
                }
                if (ID == (ushort)MessageIdentifiers.ID.CHECKING)
                {
                    NetPacket packet = new NetPacket(stream.BYTES.Length);
                    stream.BYTES.CopyTo(packet._bytes, 0);

                    NetBitStream stream2 = new NetBitStream();
                    stream2.BeginReadUDP2(packet);

                    stream2.ReadResponse2("");
                    stream2.EncodeHeader();
                    check((int[])stream2.thing.Parameters[0]);
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
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
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
                    finally
                    {
                        stopwatch.Stop();
                        LockTime = (int)stopwatch.ElapsedMilliseconds;
                        stopwatch.Reset();
                    }
                }
            });
        }

        public void NatImportask(byte Code, Dictionary<byte, Object> Parameter, bool _Lock = true)
        {
            ThreadPool.QueueUserWorkItem((aa) =>
            {
                if (key != "")
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
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
                    finally
                    {
                        stopwatch.Stop();
                        LockTime = (int)stopwatch.ElapsedMilliseconds;
                        stopwatch.Reset();
                    }
                }
            });
        }

        public void CheckKey()
        {
            if (key != "")
            {
                try
                {
                    NetBitStream stream = new NetBitStream();
                    Response b = new Response(0, new Dictionary<byte, object>() { { 0, (new Random(Guid.NewGuid().GetHashCode())).Next(0, 10000) } });
                    stream.BeginWrite((ushort)MessageIdentifiers.ID.KEY);
                    stream.WriteResponse2(b, key, true);
                    stream.EncodeHeader();
                    client.Send(stream);
                }
                catch (Exception e)
                {
                    client.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, e.Message);
                }
            }
        }

        public void check(int[] ints)
        {
            try
            {
                NetBitStream stream = new NetBitStream();
                stream.BeginWrite((ushort)MessageIdentifiers.ID.CHECKING);
                stream.EncodeHeader();
                client.Send(stream);
                for (int i = 0; i < ints.Length; i++)
                {
                    client.SetAuxiliaryServer(stream, ints[i]);
                }
                cantlink = 0;
            }
            catch (Exception e)
            {
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

        public override void Update()
        {
            NetPacket packet = null;
            for (packet = GetPacket(); packet != null; packet = GetPacket())
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
                                Thread a = new Thread(new ThreadStart(check2));
                                a.Start();
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.CONNECTION_LOST:
                            {
                                if (packet._error == "")
                                {
                                    NetBitStream stream = new NetBitStream();
                                    stream.BeginReadUDP2(packet);
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
                                stream.BeginReadUDP2(packet);
                                if (listener != null)
                                {
                                    listener.OnOperationResponse(packet.response);
                                }
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.ID_CHAT2:
                            {
                                NetBitStream stream = new NetBitStream();
                                stream.BeginReadUDP2(packet);
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
                                NetBitStream stream = new NetBitStream();
                                stream.BeginReadUDP2(packet);
                                stream.ReadResponse2("");
                                stream.EncodeHeader();
                                check((int[])(stream.thing.Parameters[0]));
                                break;
                            }
                        case (ushort)MessageIdentifiers.ID.KEY:
                            {
                                NetBitStream stream = new NetBitStream();
                                stream.BeginReadUDP2(packet);
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

        public void Doing(object thing)
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

        public void check2()
        {
            while (getcheck)
            {
                lock (locker)
                {
                    getcheck = false;
                }
                Thread.Sleep(5000);
            }
            if (client != null)
            {
                client.PushPacket((ushort)MessageIdentifiers.ID.CONNECTION_LOST, "超過5秒沒有回應");
            }
            getcheck = true;
        }
    }
}