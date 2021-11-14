using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using JimmikerNetwork;
using JimmikerNetwork.Server;

namespace TestNet
{
    public class Peer : PeerBase
    {
        Appllication appllication;
        string username = "";
        public Peer(object peer, INetServer _server, Appllication appllication) : base(peer, _server)
        {
            this.appllication = appllication;
        }

        public override void OnOperationRequest(SendData sendData)
        {
            switch(sendData.Code)
            {
                case 0:
                    {
                        username = sendData.Parameters.ToString();
                        if (!appllication.user.ContainsKey(username))
                        {
                            appllication.user.Add(username, this);

                            Dictionary<string, string> data = new Dictionary<string, string>();
                            List<KeyValuePair<string, Peer>> read = new List<KeyValuePair<string, Peer>>(appllication.user);
                            foreach(KeyValuePair<string, Peer> get in read)
                            {
                                data.Add(get.Key, ((EndPoint)get.Value.socket).ToString());
                            }
                            Reply(0, data, 0, "");
                        }
                        else
                        {
                            Close();
                        }
                        break;
                    }
                case 1:
                    {
                        string[] data = (string[])sendData.Parameters;
                        appllication.user[data[0]].Tell(0, username + ": " + data[1]);
                        break;
                    }
            }
        }

        public override void OnDisconnect()
        {
            appllication.user.Remove(username);
        }
    }
}