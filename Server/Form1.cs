using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using UnityNetwork;
using System.Diagnostics;

namespace Server
{
    public partial class Form1 : Form
    {
        IPEndPoint ipep;
        UdpClient uc;
        Thread Run;
        bool run;

        TabPage MenuStripPage;

        Dictionary<string, TabPage> NameAndServer = new Dictionary<string, TabPage>();

        public Form1()
        {
            InitializeComponent();

            using (FileStream file = new FileStream("data.dat", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                byte[] bytes = new byte[file.Length];
                file.Read(bytes, 0, bytes.Length);
                if (bytes.Length != 0)
                {
                    Response response = new Response();
                    List<string> list = null;
                    try
                    {
                        response.ByteToAll2(bytes, 0, out int cont, "");
                        list = new List<string>((string[])response.Parameters[0]);
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    foreach(string name in list)
                    {
                        NewTab(name);
                    }
                }
            }

            run = true;
            ipep = new IPEndPoint(IPAddress.Any, 555);
            uc = new UdpClient(ipep.Port);
            Run = new Thread(new ThreadStart(Showwing));
            Run.Start();
            timer1.Start();
        }

        private void Showwing()
        {
            while (run)
            {
                Response x = new Response();
                int p;
                try
                {
                    x.ByteToAll(uc.Receive(ref ipep), 0, out p, "");
                }
                catch(SocketException e)
                {
                    File.AppendAllText("ServerLog.txt", DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToString("tt hh:mm:ss") + " "+ e.ToString() + "\r\n");
                }
                if (x.Parameters != null && x.DebugMessage != null)
                {
                    if (x.Parameters.Count > 0 && x.DebugMessage.Length > 0)
                    {
                        if (NameAndServer.ContainsKey(x.Parameters[0].ToString()))
                        {
                            ((ServerConsole)NameAndServer[x.Parameters[0].ToString()].Controls[0]).Packets.Enqueue(x);
                        }
                    }
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            notifyIcon1.Visible = true;
            e.Cancel = true;
        }

        private void 全部啟動ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach(TabPage tabPage in tabControl1.TabPages)
            {
                if(!((ServerConsole)tabPage.Controls[0]).Live)
                {
                    ((ServerConsole)tabPage.Controls[0]).Start_Click(sender, e);
                }
            }
        }

        private void 全部停止ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (TabPage tabPage in tabControl1.TabPages)
            {
                if (((ServerConsole)tabPage.Controls[0]).Live)
                {
                    ((ServerConsole)tabPage.Controls[0]).Stop_Click(sender, e);
                }
            }
        }

        private void 全部重新啟動ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (TabPage tabPage in tabControl1.TabPages)
            {
                if (((ServerConsole)tabPage.Controls[0]).Live)
                {
                    ((ServerConsole)tabPage.Controls[0]).ReStart_Click(sender, e);
                }
            }
        }

        private void 結束ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (TabPage tabPage in tabControl1.TabPages)
            {
                if (((ServerConsole)tabPage.Controls[0]).Live)
                {
                    ((ServerConsole)tabPage.Controls[0]).Stop_Click(sender, e);
                }
            }
            notifyIcon1.Visible = false;
            run = false;
            uc.Close();
            this.Close();
            Environment.Exit(Environment.ExitCode);
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void 開新伺服器ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (FileStream file = new FileStream("data.dat", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                bool loop = false;
                do
                {
                    loop = false;
                    Name f1 = new Name();
                    f1.ShowDialog();
                    if (f1.YesOrNo)
                    {
                        if (f1.NewName == "")
                        {
                            MessageBox.Show("名字不能為空。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            loop = true;
                        }
                        else
                        {
                            byte[] bytes = new byte[file.Length];
                            file.Read(bytes, 0, bytes.Length);
                            if (bytes.Length == 0)
                            {
                                List<string> list = new List<string>();
                                list.Add(f1.NewName);
                                Response response = new Response(0, new Dictionary<byte, object>() { { 0, list.ToArray() } });
                                bytes = response.AllToByte2("");
                                file.Write(bytes, 0, bytes.Length);
                                NewTab(f1.NewName);
                            }
                            else
                            {
                                Response response = new Response();
                                List<string> list = null;
                                try
                                {
                                    response.ByteToAll2(bytes, 0, out int cont, "");
                                    list = new List<string>((string[])response.Parameters[0]);
                                }
                                catch (Exception)
                                {
                                    list = new List<string>();
                                    list.Add(f1.NewName);
                                    response = new Response(0, new Dictionary<byte, object>() { { 0, list.ToArray() } });
                                    bytes = response.AllToByte2("");
                                    file.Write(bytes, 0, bytes.Length);
                                    NewTab(f1.NewName);
                                    return;
                                }
                                if (list.Contains(f1.NewName))
                                {
                                    MessageBox.Show("名字已被註冊。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    loop = true;
                                }
                                else
                                {
                                    list.Add(f1.NewName);
                                    response = new Response(0, new Dictionary<byte, object>() { { 0, list.ToArray() } });
                                    bytes = response.AllToByte2("");
                                    file.Write(bytes, 0, bytes.Length);
                                    NewTab(f1.NewName);
                                }
                            }
                        }
                    }
                } while (loop);
            }
        }

        private void NewTab(string name)
        {
            TabPage tabPageX = new TabPage();
            tabPageX.BackColor = System.Drawing.SystemColors.Control;
            tabPageX.Location = new System.Drawing.Point(4, 22);
            tabPageX.Name = name;
            tabPageX.Padding = new System.Windows.Forms.Padding(3);
            tabPageX.Size = new System.Drawing.Size(tabControl1.Size.Width - 8, tabControl1.Size.Height - 26);
            tabPageX.TabIndex = 0;
            tabPageX.Text = name;

            ServerConsole ServerX = new ServerConsole();
            ServerX.Location = new System.Drawing.Point(0, 0);
            ServerX.ServiceName = name;
            ServerX.Size = new System.Drawing.Size(1100, 580);
            ServerX.TabIndex = 0;

            tabPageX.Controls.Add(ServerX);
            tabControl1.Controls.Add(tabPageX);
            tabControl1.SelectedTab = tabPageX;
            NameAndServer.Add(name, tabPageX);
        }

        private void tabControl1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                for (int i = 0; i < tabControl1.TabPages.Count; i++)
                {
                    TabPage tp = tabControl1.TabPages[i];
                    if (tabControl1.GetTabRect(i).Contains(new Point(e.X, e.Y)))
                    {
                        MenuStripPage = tp;
                        break;
                    }
                }

                if (((ServerConsole)tabControl1.SelectedTab.Controls[0]).Live)
                {
                    toolStripMenuItem1.Enabled = false;
                    toolStripMenuItem2.Enabled = true;
                    toolStripMenuItem3.Enabled = true;
                }
                else
                {
                    toolStripMenuItem1.Enabled = true;
                    toolStripMenuItem2.Enabled = false;
                    toolStripMenuItem3.Enabled = false;
                }
                this.tabControl1.ContextMenuStrip = this.contextMenuStrip2;
            }
        }

        private void tabControl1_MouseLeave(object sender, EventArgs e)
        {
            this.tabControl1.ContextMenuStrip = null;
        }

        private void 重新命名伺服器ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (FileStream file = new FileStream("data.dat", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                bool loop = false;
                do
                {
                    loop = false;
                    Name f1 = new Name();
                    f1.ShowDialog();
                    if (f1.YesOrNo)
                    {
                        if (f1.NewName == "")
                        {
                            MessageBox.Show("名字不能為空。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            loop = true;
                        }
                        else
                        {
                            byte[] bytes = new byte[file.Length];
                            file.Read(bytes, 0, bytes.Length);
                            if (bytes.Length == 0)
                            {
                                List<string> list = new List<string>();
                                list.Add(f1.NewName);
                                Response response = new Response(0, new Dictionary<byte, object>() { { 0, list.ToArray() } });
                                bytes = response.AllToByte2("");
                                file.Write(bytes, 0, bytes.Length);
                                RenameTab(tabControl1.SelectedTab.Text, f1.NewName);
                            }
                            else
                            {
                                Response response = new Response();
                                List<string> list = null;
                                try
                                {
                                    response.ByteToAll2(bytes, 0, out int cont, "");
                                    list = new List<string>((string[])response.Parameters[0]);
                                }
                                catch (Exception)
                                {
                                    list = new List<string>();
                                    list.Add(f1.NewName);
                                    response = new Response(0, new Dictionary<byte, object>() { { 0, list.ToArray() } });
                                    bytes = response.AllToByte2("");
                                    file.Write(bytes, 0, bytes.Length);
                                    RenameTab(tabControl1.SelectedTab.Text, f1.NewName);
                                    return;
                                }
                                if (list.Contains(f1.NewName))
                                {
                                    MessageBox.Show("名字已被註冊。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    loop = true;
                                }
                                else
                                {
                                    list[list.IndexOf(tabControl1.SelectedTab.Text)] = f1.NewName;
                                    response = new Response(0, new Dictionary<byte, object>() { { 0, list.ToArray() } });
                                    bytes = response.AllToByte2("");
                                    file.Write(bytes, 0, bytes.Length);
                                    RenameTab(tabControl1.SelectedTab.Text, f1.NewName);
                                }
                            }
                        }
                    }
                } while (loop);
            }
        }

        private void RenameTab(string oldName, string name)
        {
            TabPage tabPageX = NameAndServer[oldName];
            tabPageX.Name = name;
            tabPageX.Text = name;

            ServerConsole ServerX = (ServerConsole)tabPageX.Controls[0];
            ServerX.Name = name;

            NameAndServer.Remove(oldName);
            NameAndServer.Add(name, tabPageX);
        }

        private void 關閉伺服器ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (FileStream file = new FileStream("data.dat", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                byte[] bytes = new byte[file.Length];
                file.Read(bytes, 0, bytes.Length);
                if (bytes.Length != 0)
                {
                    Response response = new Response();
                    List<string> list = null;
                    try
                    {
                        response.ByteToAll2(bytes, 0, out int cont, "");
                        list = new List<string>((string[])response.Parameters[0]);
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    list.Remove(tabControl1.SelectedTab.Text);
                    response = new Response(0, new Dictionary<byte, object>() { { 0, list.ToArray() } });
                    bytes = response.AllToByte2("");
                    file.Write(bytes, 0, bytes.Length);
                    NameAndServer.Remove(tabControl1.SelectedTab.Text);
                    tabControl1.TabPages.Remove(tabControl1.SelectedTab);
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == null)
            {
                重新命名伺服器ToolStripMenuItem.Enabled = false;
                關閉伺服器ToolStripMenuItem.Enabled = false;
            }
            else
            { 
                if (((ServerConsole)tabControl1.SelectedTab.Controls[0]).Live)
                {
                    重新命名伺服器ToolStripMenuItem.Enabled = false;
                    關閉伺服器ToolStripMenuItem.Enabled = false;
                }
                else
                {
                    重新命名伺服器ToolStripMenuItem.Enabled = true;
                    關閉伺服器ToolStripMenuItem.Enabled = true;
                }
                tabControl1.Size = new System.Drawing.Size(((ServerConsole)tabControl1.SelectedTab.Controls[0]).Size.Width + 15, ((ServerConsole)tabControl1.SelectedTab.Controls[0]).Size.Height + 35);
                tabControl1.SelectedTab.Size = new System.Drawing.Size(tabControl1.Size.Width - 8, tabControl1.Size.Height - 26);
                //Size = new Size(tabControl1.Size.Width + 18, tabControl1.Size.Height + 70);
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ServerConsole serverConsole = (ServerConsole)MenuStripPage.Controls[0];
            serverConsole.Start_Click(sender, e);
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            ServerConsole serverConsole = (ServerConsole)MenuStripPage.Controls[0];
            serverConsole.Stop_Click(sender, e);
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            ServerConsole serverConsole = (ServerConsole)MenuStripPage.Controls[0];
            serverConsole.ReStart_Click(sender, e);
        }
    }
}
