using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JimmikerNetwork.Client
{
    public enum P2PCode
    {
        CallConnect,
        TestCall,
        CallConnectComplete,
        ConnectCompleteWithNAT,
        ConnectCompleteWithNATCallback,
        NATP2PTell
    }
}