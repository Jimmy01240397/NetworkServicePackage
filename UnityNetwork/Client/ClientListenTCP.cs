using System;
using System.Collections.Generic;
using System.Text;

namespace UnityNetwork.Client
{
    public interface ClientListenTCP
    {
        void DebugReturn(string message);
        void Loading(string message);

        void OnEvent(Response response);
        void OnOperationResponse(Response response);
        void OnStatusChanged(LinkCobe connect);
    }
}
