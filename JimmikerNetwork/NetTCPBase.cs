using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace JimmikerNetwork
{
    abstract class NetTCPBase
    {
        protected byte[] ReadSocketData(int len, Socket socket)
        {
            int defReadTimeout = socket.ReceiveTimeout;
            socket.ReceiveTimeout = 1000;

            byte[] data = new byte[len];
            for (int iIndex = 0; iIndex < len;)
            {
                int j = socket.Receive(data, iIndex, len - iIndex, SocketFlags.None);
                iIndex += j;
            }
            socket.ReceiveTimeout = defReadTimeout;
            return data;
        }
    }
}