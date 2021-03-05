using System;
using System.Collections.Generic;
using System.IO;
using System.Messaging;
using System.Text;
using System.Threading;

namespace UnityNetwork
{
    public static class MessageTell
    {
        static bool stop = false;
        static bool read = false;

        public static bool CanRead(string path)
        {
            string queuePath = @".\private$\" + path;//使用本機方式指定訊息佇列位置
            bool a = false;
            try
            {
                using (MessageQueue myQueue = new MessageQueue(queuePath))
                {
                    a = myQueue.CanRead;
                }
            }
            catch (Exception)
            {
                a = false;
            }
            return a;
        }

        public static void StopRead()
        {
            if (read)
            {
                stop = true;
            }
        }
        public static void SendMessage(byte[] bytes, string path)
        {
            //string queuePath = @"FormatName:DIRECT=TCP:192.168.1.1\private$\myqueue";// 使用遠程IP指定訊息佇列位置
            string queuePath = @".\private$\" + path;//使用本機方式指定訊息佇列位置

            if (!MessageQueue.Exists(queuePath))//判斷 myqueue訊息佇列是否存在
            {
                using (MessageQueue message = MessageQueue.Create(queuePath))//建立用來接受/發送的訊息佇列
                {
                    AccessControlList list = new AccessControlList();

                    // Create a new trustee to represent the "Everyone" user group.
                    Trustee[] tr = new Trustee[3] { new Trustee("Everyone"), new Trustee("SYSTEM"), new Trustee("ANONYMOUS LOGON") };

                    // Create an AccessControlEntry, granting the trustee read access to
                    // the queue.
                    foreach (Trustee trustee in tr)
                    {
                        AccessControlEntry entry = new AccessControlEntry(
                            trustee, GenericAccessRights.All,
                            StandardAccessRights.All,
                            AccessControlEntryType.Allow);
                        list.Add(entry);
                    }
                    message.SetPermissions(list);
                }
            }
            using (MessageQueue myQueue = new MessageQueue(queuePath))
            {
                //要發送的內容

                //發送訊息
                myQueue.Send(bytes);
            }
        }

        public static byte[] GetMessage(string path)
        {
            stop = false;
            read = true;
            string queuePath = @".\private$\" + path;//使用本機方式指定訊息佇列位置
            if (!MessageQueue.Exists(queuePath))//判斷 myqueue訊息佇列是否存在
            {
                using (MessageQueue message = MessageQueue.Create(queuePath))//建立用來接受/發送的訊息佇列
                {
                    AccessControlList list = new AccessControlList();

                    // Create a new trustee to represent the "Everyone" user group.
                    Trustee[] tr = new Trustee[3] { new Trustee("Everyone"), new Trustee("SYSTEM"), new Trustee("ANONYMOUS LOGON") };

                    // Create an AccessControlEntry, granting the trustee read access to
                    // the queue.
                    foreach (Trustee trustee in tr)
                    {
                        AccessControlEntry entry = new AccessControlEntry(
                            trustee, GenericAccessRights.All,
                            StandardAccessRights.All,
                            AccessControlEntryType.Allow);
                        list.Add(entry);
                    }
                    message.SetPermissions(list);
                }
            }
            try
            {
                using (MessageQueue myQueue = new MessageQueue(queuePath))
                {
                    myQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(byte[]) });//設定接收訊息內容的型別
                    new Thread(() =>
                    {
                        SpinWait.SpinUntil(() => stop || !read);
                        if (stop)
                        {
                            myQueue.Close();
                            myQueue.Dispose();
                        }
                    }).Start();
                    byte[] data = null;
                    try
                    {
                        Message message = myQueue.Receive(new TimeSpan(0,0,1));//接收訊息佇列內的訊息
                        data = (byte[])message.Body;//將訊息內容轉成正確型
                    }
                    catch (Exception)
                    {

                    }
                    stop = false;
                    read = false;
                    return data;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}