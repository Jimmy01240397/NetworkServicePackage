using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace UnityNetwork.Client
{
    public interface ClientListenUDP
    {
        void DebugReturn(string message);
        void Loading(string message);

        void OnEvent(Response response);
        void OnOperationResponse(Response response);
        void OnStatusChanged(LinkCobe connect);
        PeerForP2P P2PAddPeer(IPEndPoint _peer, NetUDPClient client, bool NATPass);
    }
}