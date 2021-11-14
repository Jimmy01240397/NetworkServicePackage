using JimmikerNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestNet
{
    class Program
    {
        static bool stop = false;
        static Appllication appllication = new Appllication(System.Net.Sockets.ProtocolType.Udp);
        static void Main(string[] args)
        {
            appllication.GetMessage += Appllication_GetMessage;
            appllication.Start();

            SetConsoleCtrlHandler(t =>
            {
                Console.WriteLine("Closing...");
                appllication.Disconnect();
                SpinWait.SpinUntil(() => stop);
                return false;
            }, true);

            SpinWait.SpinUntil(() => Console.ReadLine() == "exit");
            appllication.StopUpdateThread();
        }

        private static void Appllication_GetMessage(Appllication.MessageType type, string message)
        {
            if (type == Appllication.MessageType.ServerClose)
            {
                stop = true;
                appllication.GetMessage -= Appllication_GetMessage;
                appllication = null;
            }
            Console.WriteLine(type.ToString() + ": " + message);
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
    }
}
