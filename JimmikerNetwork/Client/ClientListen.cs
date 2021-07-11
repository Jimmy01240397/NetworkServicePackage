using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace JimmikerNetwork.Client
{
    public interface ClientListen
    {
        void DebugReturn(string message);

        void OnEvent(SendData sendData);
        void OnOperationResponse(SendData sendData);
        void OnStatusChanged(LinkCobe connect);

        PeerForP2PBase P2PAddPeer(object _peer, object publicIP, INetClient client, bool NAT);
    }
}