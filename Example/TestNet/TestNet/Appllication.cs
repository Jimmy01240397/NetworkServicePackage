using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using System.Net;
using JimmikerNetwork;
using JimmikerNetwork.Server;

namespace TestNet
{
    public class Appllication : AppllicationBase
    {
        public Dictionary<string, Peer> user;

        public Appllication(ProtocolType type): base(IPAddress.IPv6Any, 6565 ,type)
        {
            
        }

        protected override void Setup()
        {
            user = new Dictionary<string, Peer>();
            RunUpdateThread();
        }

        protected override PeerBase AddPeerBase(object _peer, INetServer server)
        {
            return new Peer(_peer, server, this);
        }

        protected override void TearDown()
        {
            StopUpdateThread();
            user.Clear();
        }
    }
}