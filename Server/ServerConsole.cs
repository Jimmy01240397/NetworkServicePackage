using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using UnityNetwork;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace Server
{
    public partial class ServerConsole : UserControl
    {
        bool live = false;
        public bool Live { get; private set; }
        public System.Collections.Queue Packets { get; private set; }
        Dictionary<string, DataGridViewRow> people;
        List<string> PeopleKey;
        int Count = 0;

        public string ServiceName
        {
            get
            {
                return serviceController1.ServiceName;
            }
            set
            {
                serviceController1.ServiceName = value;
            }
        }

        public ServerConsole()
        {
            InitializeComponent();

            Live = false;

            comboBox1.SelectedIndex = 0;

            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;

            timer1.Start();
            timer2.Start();
            Packets = new System.Collections.Queue();
            people = new Dictionary<string, DataGridViewRow> { };
            PeopleKey = new List<string>();
        }

        public Response GetPacket()
        {
            if (Packets.Count == 0)
                return null;

            return (Response)Packets.Dequeue();
        }

        void AllSendMessage(string message)
        {
            Response response = new Response(252, new Dictionary<byte, object>() { { 0, 0 }, { 1, message } });
            MessageTell.SendMessage(response.AllToByte2(""), serviceController1.ServiceName);
        }

        void SendMessage(string IP, string message)
        {
            Response response = new Response(252, new Dictionary<byte, object>() { { 0, 1 }, { 1, IP }, { 2, message } });
            MessageTell.SendMessage(response.AllToByte2(""), serviceController1.ServiceName);
        }

        void TimeClose()
        {
            DateTime dateTime = DateTime.Parse(dateTimePicker1.Text);
            Response response = new Response(252, new Dictionary<byte, object>() { { 0, 2 },{ 1, new TimeSpan(dateTime.Ticks).TotalMilliseconds } });
            MessageTell.SendMessage(response.AllToByte2(""), serviceController1.ServiceName);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Response packet;
            for (packet = GetPacket(); packet != null; packet = GetPacket())
            {
                switch (packet.Code)
                {
                    case 0:
                        {
                            label1.Text = "你的IP和Port為：" + packet.DebugMessage;
                            break;
                        }
                    case 1:
                        {
                            DataGridViewRowCollection rows = dataGridView1.Rows;
                            string w = DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss");
                            richTextBox1.AppendText(w + " " + packet.DebugMessage + "  加入連線" + "\n");
                            File.AppendAllText("ServerLog.txt", w + " " + packet.DebugMessage + "  加入連線" + "\r\n");
                            people.Add(packet.DebugMessage, rows[rows.Add(new Object[] { w, packet.DebugMessage })]);
                            PeopleKey.Add(packet.DebugMessage);
                            break;
                        }
                    case 2:
                        {
                            string[] a = packet.DebugMessage.Split('+');
                            richTextBox1.AppendText(DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + " " + a[0] + "  " + a[1] + "\n");
                            File.AppendAllText("ServerLog.txt", DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + " " + a[0] + "  " + a[1] + "\r\n");
                            DataGridViewRowCollection rows = dataGridView1.Rows;
                            if (people.ContainsKey(a[0]))
                            {
                                rows.Remove(people[a[0]]);
                                people.Remove(a[0]);
                                PeopleKey.Remove(a[0]);
                            }
                            break;
                        }
                    case 3:
                        {
                            live = true;
                            string[] a = packet.DebugMessage.Split(' ');
                            label2.Text = "目前連線數 :" + a[0] + "\n" + "待處理封包數 :" + a[1] + "\n" + "上次更新到這次間取得的封包數 :" + a[2] + "\n" + a[3];
                            Count = Convert.ToInt32(a[2]);
                            if (label2.Size.Height - 80 > 0)
                            {
                                panel1.Location = new Point(10, 125 + label2.Size.Height - 80);
                                Size = new Size(1100, 580 + label2.Size.Height - 80);
                            }
                            else
                            {
                                panel1.Location = new Point(10, 125);
                                Size = new Size(1100, 580);
                            }
                            break;
                        }
                    case 4:
                        {
                            richTextBox1.AppendText(packet.DebugMessage + "\n");
                            File.AppendAllText("ServerLog.txt", packet.DebugMessage + "\r\n");
                            break;
                        }
                    case 5:
                        {
                            people.Clear();
                            PeopleKey.Clear();
                            DataGridViewRowCollection rows = dataGridView1.Rows;
                            rows.Clear();
                            label1.Text = "";
                            live = true;

                            richTextBox1.AppendText(packet.DebugMessage + "\n");
                            File.AppendAllText("ServerLog.txt", packet.DebugMessage + "\r\n");
                            break;
                        }
                    case 6:
                        {
                            try
                            {
                                string[] b = packet.DebugMessage.Split('\n');
                                Process a = new Process();
                                if (b.Length == 1)
                                {
                                    a.StartInfo = new ProcessStartInfo(b[0]);
                                }
                                else if (b.Length == 2)
                                {
                                    a.StartInfo = new ProcessStartInfo(b[0], b[1]);
                                }
                                a.StartInfo.UseShellExecute = false;
                                a.Start();
                            }
                            catch (Exception ee)
                            {
                                richTextBox1.AppendText(DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + " " + ee.Message + "\n");
                                File.AppendAllText("ServerLog.txt", DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + " " + ee.Message + "\r\n");
                            }
                            break;
                        }
                }
                packet = null;
            }
            if (Count > 17000000)
            {
                serviceController1.Stop();
                people.Clear();
                PeopleKey.Clear();
                DataGridViewRowCollection rows = dataGridView1.Rows;
                rows.Clear();
                label1.Text = "";
                live = true;
            }
        }

        public void Start_Click(object sender, EventArgs e)
        {
            try
            {
                serviceController1.Start();
                live = false;
            }
            catch (Exception ee)
            {
                richTextBox1.AppendText(DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + " " + ee.Message + "\n");
                File.AppendAllText("ServerLog.txt", DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + " " + ee.Message + "\r\n");
            }
        }

        public void Stop_Click(object sender, EventArgs e)
        {
            serviceController1.Stop();
            people.Clear();
            PeopleKey.Clear();
            DataGridViewRowCollection rows = dataGridView1.Rows;
            rows.Clear();
            label1.Text = "";
            live = true;
        }

        public void ReStart_Click(object sender, EventArgs e)
        {
            serviceController1.Stop();
            people.Clear();
            PeopleKey.Clear();
            DataGridViewRowCollection rows = dataGridView1.Rows;
            rows.Clear();
            label1.Text = "";
            live = false;
            bool a = true;
            Thread.Sleep(6000);
            while (a)
            {
                try
                {
                    serviceController1.Start();
                    a = false;
                }
                catch (Exception)
                {

                }
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (live)
            {
                button1.Enabled = false;
                button2.Enabled = true;
                button3.Enabled = true;
                button5.Enabled = true;
                button6.Enabled = true;
                button7.Enabled = true;
                live = false;
                Live = true;
            }
            else
            {
                button1.Enabled = true;
                button2.Enabled = false;
                button3.Enabled = false;
                button5.Enabled = false;
                button6.Enabled = false;
                button7.Enabled = false;
                Live = false;
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.ScrollToCaret();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    {
                        AllSendMessage(textBox1.Text);
                        break;
                    }
                case 1:
                    {
                        DataGridViewSelectedRowCollection a = dataGridView1.SelectedRows;
                        for (int i = 0; i < a.Count; i++)
                        {
                            if (people.ContainsValue(a[i]))
                            {
                                SendMessage(a[i].Cells[1].Value.ToString(), textBox1.Text);
                            }
                        }
                        List<DataGridViewRow> b = new List<DataGridViewRow>();
                        DataGridViewSelectedCellCollection c = dataGridView1.SelectedCells;
                        for (int i = 0; i < c.Count; i++)
                        {
                            DataGridViewRow row = c[i].OwningRow;
                            if (!a.Contains(row) && !b.Contains(row))
                            {
                                if (people.ContainsValue(row))
                                {
                                    SendMessage(row.Cells[1].Value.ToString(), textBox1.Text);
                                    b.Add(row);
                                }
                            }
                        }
                        break;
                    }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            TimeClose();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            dateTimePicker1.ShowUpDown = checkBox1.Checked;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            dateTimePicker1.Text = DateTime.Now.ToString();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Response response = new Response(252, new Dictionary<byte, object>() { { 0, 2 }, { 1, (double)-1 } });
            MessageTell.SendMessage(response.AllToByte2(""), serviceController1.ServiceName);
        }
    }
}
