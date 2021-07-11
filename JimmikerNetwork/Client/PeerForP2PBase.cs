using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JimmikerNetwork.Client
{
    public abstract class PeerForP2PBase
    {
        public object socket { get; private set; }

        public object PublicIP { get; private set; }

        private INetClient client;

        protected SerializationData.RSAKeyPair Key
        {
            get
            {
                if (client == null || socket == null) return new SerializationData.RSAKeyPair();
                if (!client.P2PSocketToKey.ContainsKey(socket)) return new SerializationData.RSAKeyPair();
                return client.P2PSocketToKey[socket];
            }
        }

        public bool NAT { get; private set; } = false;

        public PeerForP2PBase(object peer, object publicIP, INetClient client, bool NAT)
        {
            socket = peer;
            PublicIP = publicIP;
            this.client = client;
            this.NAT = NAT;
        }

        public virtual void OnGetData(SendData sendData)
        {

        }

        public virtual void OnDisconnect()
        {

        }

        public virtual void Tell(byte Code, object Parameter, bool _Lock = true)
        {
            using (Packet packet = new Packet(socket))
            {
                packet.BeginWrite(PacketType.P2P_Tell);
                packet.WriteSendData(new SendData(Code, Parameter), Key.PublicKey, _Lock ? SerializationData.LockType.RSA : SerializationData.LockType.None);
                if (NAT)
                {
                    client.P2PNATPacketSend(packet);
                }
                else
                {
                    client.Send(packet);
                }
            }
        }

        public void Close()
        {
            using (Packet packet = new Packet(socket))
            {
                packet.BeginWrite(PacketType.P2P_CONNECTION_LOST);
                if (NAT)
                {
                    client.P2PNATPacketSend(packet);
                }
                else
                {
                    client.Send(packet);
                }
            }
            client.P2PPushPacket(PacketType.P2P_CONNECTION_LOST, "對等端端主動斷線", socket);
        }
    }
}