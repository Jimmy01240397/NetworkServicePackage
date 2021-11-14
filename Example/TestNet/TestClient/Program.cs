using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using JimmikerNetwork;
using JimmikerNetwork.Client;

namespace TestClient
{
    public class Program : ClientListen
    {
        static bool stop = false;

        static void Main(string[] args)
        {
            Program program = new Program(ProtocolType.Udp);

            program.Connect("vpn2.chummy.site", 6565);
            //program.Connect("192.168.100.100", 6565);

            SetConsoleCtrlHandler(t =>
            {
                Console.WriteLine("Closing...");
                program.linker.Disconnect();
                SpinWait.SpinUntil(() => stop);
                Console.WriteLine("OK...");
                return false;
            }, true);

            SpinWait.SpinUntil(() => program.linker.linkstate != LinkCobe.None);

            if(program.linker.linkstate == LinkCobe.Connect)
            {
                program.username = Console.ReadLine();
                if (program.username != null)
                {
                    program.linker.Ask(0, program.username);
                }
                for (string data = Console.ReadLine(); data != "exit"; data = Console.ReadLine())
                {
                    if (data == null)
                    {
                        break;
                    }
                    string[] all = data.Split(':');
                    program.userlist[all[0]].Tell(1, all[1]);
                    //program.linker.Ask(1, all);
                }
            }
            else
            {
                Console.ReadKey();
            }

            program.linker.Disconnect();
        }

        public ClientLinker linker { get; private set; }

        public Dictionary<string, Peer> userlist = new Dictionary<string, Peer>();

        public string username;

        public Program(ProtocolType type)
        {
            linker = new ClientLinker(this, type, true);
        }

        public void Connect(string serverhost, int port)
        {
            linker.Connect(serverhost, port);
            linker.RunUpdateThread();
        }

        public void DebugReturn(string message)
        {
            Console.WriteLine(message);
        }

        public void OnEvent(SendData sendData)
        {
            /*switch(sendData.Code)
            {
                case 0:
                    {
                        Console.WriteLine(sendData.Parameters.ToString());
                        break;
                    }
            }*/
        }

        public void OnOperationResponse(SendData sendData)
        {
            switch (sendData.Code)
            {
                case 0:
                    {
                        List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>((Dictionary<string, string>)sendData.Parameters);
                        foreach(KeyValuePair<string, string> now in data)
                        {
                            linker.StartP2PConnect(TraceRoute.IPEndPointParse(now.Value, AddressFamily.InterNetworkV6), (remote, publicremote, peer, success) =>
                            {
                                Console.WriteLine(now.Key + " " + success);
                            });
                        }
                        break;
                    }
            }
        }

        public void OnStatusChanged(LinkCobe connect)
        {
            switch(connect)
            {
                case LinkCobe.Connect:
                    {
                        Console.WriteLine("connect Success");
                        break;
                    }
                case LinkCobe.Failed:
                    {
                        Console.WriteLine("connect Failed");
                        linker.StopUpdateThread();
                        stop = true;
                        break;
                    }
                case LinkCobe.Lost:
                    {
                        Console.WriteLine("connect Lost");
                        linker.StopUpdateThread();
                        stop = true;
                        break;
                    }
            }
        }


        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handler, bool add);

        public delegate bool ConsoleCtrlDelegate(CtrlTypes ctrlType);

        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }


        static void OnGetError(string message)
        {
            Console.WriteLine(message);
        }

        public PeerForP2PBase P2PAddPeer(object _peer, object publicIP, INetClient client, bool NAT)
        {
            return new Peer(_peer, publicIP, client, NAT, this);
        }
    }
}
