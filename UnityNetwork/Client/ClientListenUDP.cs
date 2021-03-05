﻿using System;
using System.Collections.Generic;
using System.IO;
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
    }
}