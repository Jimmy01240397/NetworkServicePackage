using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace UnityNetwork
{
    public class TimeStopWatch
    {
        bool go = false;
        int q = -1;
        public int ElapsedMilliseconds { get; private set; }
        public TimeStopWatch()
        {
            ElapsedMilliseconds = 0;
            go = false;
        }
        public void Start()
        {
            if (!go)
            {
                go = true;
                Thread thread = new Thread(new ThreadStart(() =>
                {
                    while (go)
                    {
                        if (q == -1)
                        {
                            q = DateTime.Now.Millisecond;
                        }
                        else
                        {
                            int a = DateTime.Now.Millisecond;
                            ElapsedMilliseconds += a >= q ? a - q : a + 1000 - q;
                            q = a;
                        }
                    }
                }));
                thread.IsBackground = true;
                thread.Start();
            }
        }
        public void Stop()
        {
            go = false;
        }

        public void Reset()
        {
            go = false;
            ElapsedMilliseconds = 0;
        }
    }
}