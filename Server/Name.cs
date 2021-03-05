using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;

namespace Server
{
    public partial class Name : Form
    {
        public bool YesOrNo { get; private set; }
        public string NewName { get; private set; }
        public Name()
        {
            InitializeComponent();

            var serviceControllers = ServiceController.GetServices();
            //遍历服务集合，打印服务名和服务状态
            foreach (var service in serviceControllers)
            {
                textBox1.Items.Add(service.ServiceName);
            }
            YesOrNo = false;
            NewName = "";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            YesOrNo = true;
            NewName = textBox1.Text;
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            YesOrNo = false;
            this.Hide();
        }
    }
}
