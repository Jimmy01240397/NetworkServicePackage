using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace JimmikerNetwork.Server
{
    public class AppllicationBase
    {
        public enum MessageType
        {
            ServerStartSuccess,
            ServerClose,
            ConnectSuccess,
            ConnectLost,
            DebugMessage
        }

        public ProtocolType type { get; private set; }

        public IPAddress IP { get; private set; }


        public int Port { get; private set; }

        public event Action<MessageType, string> GetMessage;

        Thread NetThread;
        INetServer server;
        bool run;

        public List<PeerBase> SocketList
        {
            get
            {
                return server.SocketList;
            }
        }
        public Dictionary<EndPoint, PeerBase> ToPeer
        {
            get
            {
                return server.ToPeer;
            }
        }

        public AppllicationBase(IPAddress IP, int port, ProtocolType protocol)
        {
            type = protocol;
            this.IP = IP;
            this.Port = port;
            switch (type)
            {
                case ProtocolType.Tcp:
                    {
                        server = new NetServerTCP();
                        break;
                    }
                case ProtocolType.Udp:
                    {
                        server = new NetServerUDP();
                        break;
                    }
            }
        }

        public void Start(int maxConnections = -1)
        {
            server.MaxConnections = maxConnections;
            server.GetMessage += server_GetMessage;
            if (server.CreateServer(IP, Port, out string w))
            {
                GetMessage?.Invoke(MessageType.ServerStartSuccess, w);
            }
            else
            {
                GetMessage?.Invoke(MessageType.DebugMessage, DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + "：" + w);
            }
            Setup();
        }

        public void RunUpdateThread()
        {
            if (!run)
            {
                run = true;
                NetThread = new Thread(() =>
                {
                    while (run)
                    {
                        Update();
                        Thread.Sleep(10);
                    }
                });
                NetThread.Start();
            }
        }

        public void StopUpdateThread()
        {
            run = false;
            NetThread = null;
        }

        protected virtual void Setup()
        {

        }

        public void Update()
        {
            for (Packet packet = server.GetPacket(); packet != null; packet = server.GetPacket())
            {
                PacketType packetType = packet.BeginRead();
                EndPoint remote = null;
                switch (type)
                {
                    case ProtocolType.Tcp:
                        {
                            remote = ((Socket)packet.peer).RemoteEndPoint;
                            break;
                        }
                    case ProtocolType.Udp:
                        {
                            remote = (EndPoint)packet.peer;
                            break;
                        }
                }
                switch (packetType)
                {
                    case PacketType.CONNECT_SUCCESSFUL:
                        {
                            if (!ToPeer.ContainsKey(remote))
                            {
                                GetMessage?.Invoke(MessageType.ConnectSuccess, remote.ToString());
                                server.ConnectSuccessful(AddPeerBase, packet);
                            }
                            break;
                        }
                    case PacketType.CONNECTION_LOST:
                        {
                            if (ToPeer.ContainsKey(remote))
                            {
                                PeerBase peer = ToPeer[remote];
                                GetMessage?.Invoke(MessageType.ConnectLost, remote.ToString() + ", error: " + packet.state);
                                peer.OnDisconnect();
                                server.Disconnect(packet.peer);
                                peer = null;
                            }
                            break;
                        }
                    case PacketType.Request:
                        {
                            if (ToPeer.ContainsKey(remote))
                            {
                                PeerBase peer = ToPeer[remote];
                                string key = server.SocketToKey[packet.peer];
                                if (!string.IsNullOrEmpty(key))
                                {
                                    SendData sendData = packet.ReadSendData(key);
                                    peer.OnOperationRequest(sendData);
                                }
                            }
                            break;
                        }
                }
                packet.CloseStream();
            }
        }

        protected virtual PeerBase AddPeerBase(object _peer, INetServer server)
        {
            return null;
        }

        protected virtual void TearDown()
        {
            
        }

        public void Disconnect()
        {
            for (int i = 0; i < SocketList.Count; i++)
            {
                SocketList[i].Close();
            }
            Thread a = new Thread(new ThreadStart(exit));
            a.Start();
        }

        private void exit()
        {
            SpinWait.SpinUntil(() => SocketList.Count == 0);
            TearDown();
            server.Close();
            run = false;
            NetThread = null;
            GetMessage?.Invoke(MessageType.ServerClose, DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + " " + "成功關閉伺服器");
        }

        private void server_GetMessage(string w)
        {
            GetMessage?.Invoke(MessageType.DebugMessage, DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + "：" + w);
        }
    }
}