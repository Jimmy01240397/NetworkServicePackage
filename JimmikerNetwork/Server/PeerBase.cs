using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace JimmikerNetwork.Server
{
    public abstract class PeerBase
    {
        private INetServer server;

        public object socket { get; private set; }

        protected string Key
        {
            get
            {
                if (server == null || socket == null) return "";
                if(!server.SocketToKey.ContainsKey(socket)) return "";
                return server.SocketToKey[socket];
            }
        }

        public PeerBase(object peer, INetServer _server)
        {
            socket = peer;
            this.server = _server;

        }

        public virtual void OnOperationRequest(SendData sendData)
        {

        }

        public virtual void OnDisconnect()
        {

        }
        
        public virtual void Tell(byte Code, object Parameter, bool _Lock = true)
        {
            using (Packet packet = new Packet(socket))
            {
                packet.BeginWrite(PacketType.ServerTell);
                packet.WriteSendData(new SendData(Code, Parameter), Key, _Lock ? EncryptAndCompress.LockType.AES : EncryptAndCompress.LockType.None);
                server.Send(packet, socket);
            }
        }

        public virtual void Reply(byte Code, object Parameter, short ReturnCode, string DebugMessage, bool _Lock = true)
        {
            using (Packet packet = new Packet(socket))
            {
                packet.BeginWrite(PacketType.Response);
                packet.WriteSendData(new SendData(Code, Parameter, ReturnCode, DebugMessage), Key, _Lock ? EncryptAndCompress.LockType.AES : EncryptAndCompress.LockType.None);
                server.Send(packet, socket);
            }
        }

        public void Close()
        {
            if (server.type == ProtocolType.Udp)
            {
                using (Packet packet = new Packet(socket))
                {
                    packet.BeginWrite(PacketType.CONNECTION_LOST);
                    bool get = server.Send(packet, socket);
                }
            }
            server.PushPacket(PacketType.CONNECTION_LOST, "伺服器端主動斷線", socket);

            //server.Disconnect(socket);
            //socket = null;
        }
    }
}
