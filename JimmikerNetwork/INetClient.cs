using JimmikerNetwork.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;


namespace JimmikerNetwork
{
    public interface INetClient
    {
        bool EnableP2P { get; set; }

        SerializationData.RSAKeyPair RSAkey { get; }
        string AESkey { get; }

        SerializationData.RSAKeyPair P2PRSAkey { get; }

        Dictionary<object, SerializationData.RSAKeyPair> P2PSocketToKey { get; }
        List<PeerForP2PBase> P2PSocketList { get; }
        Dictionary<EndPoint, PeerForP2PBase> P2PToPeer { get; }


        List<Packet> Packets { get; }
        EndPoint LocalEndPoint { get; }
        EndPoint RemoteEndPoint { get; }
        event Action<string> GetMessage;
        bool Connect(string serverhost, int remotePort);

        void ConnectSuccessful(Packet packet);

        /// <summary>
        /// Start P2P Connect
        /// </summary>
        /// <param name="IPPort">Connect Target</param>
        /// <param name="callback">Connect Callback(Connect IP, Connect Public IP, Successful)</param>
        void StartP2PConnect(IPEndPoint IPPort, Action<EndPoint, EndPoint, bool> callback);

        void P2PNATPacketSend(Packet packet);

        void P2PConnectSuccessful(Func<object, object, INetClient, bool, PeerForP2PBase> P2PAddPeer, Packet packet);

        bool Send(Packet bts);

        Packet GetPacket();

        void Disconnect();
        void Disconnect(int timeout);

        void P2PDisconnect(object socket);
        void P2PDisconnect(object socket, int timeout);

        void PushPacket(PacketType msgid, string exception);
        void PushPacket(Packet stream);

        void P2PPushPacket(PacketType msgid, SerializationData.RSAKeyPair Key, object remote, object remotePublic, bool NAT);

        void P2PPushPacket(PacketType msgid, string exception, object remote);

        void DebugMessage(string message);
    }
}