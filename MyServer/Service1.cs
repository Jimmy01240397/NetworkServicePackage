using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityNetwork;
using UnityNetwork.Server;
using Servers;
using System.Timers;
using System.Threading;
using Timer = System.Timers.Timer;

namespace MyServer
{
    public partial class Service1 : ServiceBase
    {
        AppllicationTCPBase server;
        IPEndPoint ipep;
        UdpClient uc;
        Timer timer1;
        bool run = true;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            run = true;
            timer1 = new Timer();
            timer1.Elapsed += new ElapsedEventHandler(timer1_Tick);
            timer1.Interval = 1000;
            ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 555);
            uc = new UdpClient();
            timer1.Start();
            server = new Appllication();
            server.GetMessage += server_GetMessage;
            try
            {
                server.Start();
            }
            catch(Exception e)
            {
                server_GetMessage(4, DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + " " + "錯誤：" + e.ToString());
            }
            new Thread(() =>
            {
                while (run)
                {
                    SpinWait.SpinUntil(() => MessageTell.CanRead(this.ServiceName));
                    byte[] a = MessageTell.GetMessage(this.ServiceName);
                    if (a != null)
                    {
                        Response response = new Response();
                        response.ByteToAll2(a, 0, out int cont, "");
                        switch (response.Code)
                        {
                            case 252:
                                {
                                    switch (Convert.ToInt32(response.Parameters[0]))
                                    {
                                        case 0:
                                            {
                                                foreach (PeerTCPBase peerTCP in server.SocketList)
                                                {
                                                    peerTCP.NotImportTell(response.Code, new Dictionary<byte, object>() { { 0, response.Parameters[1] } });
                                                }
                                                break;
                                            }
                                        case 1:
                                            {
                                                if (server.ToPeerTCPIP.ContainsKey(response.Parameters[1].ToString()))
                                                {
                                                    ((PeerTCPBase)server.ToPeerTCPIP[response.Parameters[1].ToString()]).NotImportTell(response.Code, new Dictionary<byte, object>() { { 0, response.Parameters[2] } });
                                                }
                                                break;
                                            }
                                        case 2:
                                            {
                                                if ((double)response.Parameters[1] != -1)
                                                {
                                                    int q = (int)(((double)response.Parameters[1] - new TimeSpan(DateTime.Now.Ticks).TotalMilliseconds) / 60000);
                                                    if (q > 0)
                                                    {
                                                        foreach (PeerTCPBase peerTCP in server.SocketList)
                                                        {
                                                            peerTCP.NotImportTell(response.Code, new Dictionary<byte, object>() { { 0, "伺服器將於" + q + "分鐘關閉" } });
                                                        }
                                                    }
                                                }
                                                server.CloseTime = (double)response.Parameters[1];
                                                break;
                                            }
                                    }
                                    break;
                                }
                        }
                    }
                }
            }).Start();
        }

        protected override void OnStop()
        {
            try
            {
                server.Disconnect();
            }
            catch (Exception e)
            {
                server_GetMessage(4, DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + " " + "錯誤：" + e.ToString());
            }
        }

        private void server_GetMessage(byte Cobe, string thing)
        {
            if(Cobe == 5)
            {
                run = false;
                MessageTell.StopRead();
                timer1.Stop();
                server.GetMessage -= server_GetMessage;
                server = null;
            }
            Response a = new Response(Cobe, new Dictionary<byte, object>() { { 0, ServiceName} }, 0, thing);
            byte[] b = a.AllToByte("");
            uc.Send(b, b.Length, ipep);
        }

        private void timer1_Tick(object sender, ElapsedEventArgs e)
        {
            Response aa = new Response(3, new Dictionary<byte, object>() { { 0, ServiceName } }, 0, server.SocketList.Count + " " + server.PacketSize.ToString() + " " + server.PacketCount.ToString() + " " + server.UpdateData());
            server.PacketCount = 0;
            byte[] bb = aa.AllToByte("");
            uc.Send(bb, bb.Length, ipep);
        }
    }
}
