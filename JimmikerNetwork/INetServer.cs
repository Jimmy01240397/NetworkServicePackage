using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using JimmikerNetwork.Server;

namespace JimmikerNetwork
{
    public interface INetServer
    {
        int MaxConnections { get; set; }

        ProtocolType type { get; }

        EncryptAndCompress.RSAKeyPair RSAkey { get; }
        List<PeerBase> SocketList { get; }
        List<Packet> Packets { get; }
        Dictionary<EndPoint, PeerBase> ToPeer { get; }
        Dictionary<object, string> SocketToKey { get; }

        EndPoint ListenerIP { get; }

        event Action<string> GetMessage;

        bool CreateServer(IPAddress ip, int listenPort, out string a);

        void ConnectSuccessful(Func<object, INetServer, PeerBase> AddPeerFunc, Packet packet);

        void Close();

        bool Send(Packet bts, object peer);

        Packet GetPacket();

        void Disconnect(object socket);
        void Disconnect(object socket, int timeout);

        void PushPacket(PacketType msgid, string exception, object peer);
        void PushPacket(Packet stream);

        void DebugMessage(string message);
    }
}
