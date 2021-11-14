using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JimmikerNetwork.Client;
using JimmikerNetwork;

namespace TestClient
{
    public class Peer:PeerForP2PBase
    {
        Program client;
        string username;
        public Peer(object peer, object publicIP, INetClient client, bool NAT, Program listen):base(peer, publicIP, client, NAT)
        {
            this.client = listen;
            Tell(0, listen.username);
            Console.WriteLine("peer on " + NAT);
        }

        public override void OnGetData(SendData sendData)
        {
            switch(sendData.Code)
            {
                case 0:
                    {
                        username = sendData.Parameters.ToString();
                        client.userlist.Add(username, this);
                        Console.WriteLine(username + " NAT:" + NAT);
                        break;
                    }
                case 1:
                    {
                        Console.WriteLine(username + ": " + sendData.Parameters.ToString());
                        break;
                    }
            }
        }

        public override void OnDisconnect()
        {
            client.userlist.Remove(username);
        }
    }
}