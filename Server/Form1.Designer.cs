namespace Server
{
    partial class Form1
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置 Managed 資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器
        /// 修改這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.啟動ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.停止ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.重新啟動ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.結束ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.伺服器ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.開新伺服器ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.重新命名伺服器ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.關閉伺服器ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.控制ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.全部啟動ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.全部停止ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.全部重新啟動ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.結束ToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip2 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.contextMenuStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.contextMenuStrip2.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "伺服器";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_Click);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.啟動ToolStripMenuItem,
            this.停止ToolStripMenuItem,
            this.重新啟動ToolStripMenuItem,
            this.結束ToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(147, 92);
            this.contextMenuStrip1.Text = "伺服器";
            // 
            // 啟動ToolStripMenuItem
            // 
            this.啟動ToolStripMenuItem.Name = "啟動ToolStripMenuItem";
            this.啟動ToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.啟動ToolStripMenuItem.Text = "全部啟動";
            this.啟動ToolStripMenuItem.Click += new System.EventHandler(this.全部啟動ToolStripMenuItem_Click);
            // 
            // 停止ToolStripMenuItem
            // 
            this.停止ToolStripMenuItem.Name = "停止ToolStripMenuItem";
            this.停止ToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.停止ToolStripMenuItem.Text = "全部停止";
            this.停止ToolStripMenuItem.Click += new System.EventHandler(this.全部停止ToolStripMenuItem_Click);
            // 
            // 重新啟動ToolStripMenuItem
            // 
            this.重新啟動ToolStripMenuItem.Name = "重新啟動ToolStripMenuItem";
            this.重新啟動ToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.重新啟動ToolStripMenuItem.Text = "全部重新啟動";
            this.重新啟動ToolStripMenuItem.Click += new System.EventHandler(this.全部重新啟動ToolStripMenuItem_Click);
            // 
            // 結束ToolStripMenuItem
            // 
            this.結束ToolStripMenuItem.Name = "結束ToolStripMenuItem";
            this.結束ToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.結束ToolStripMenuItem.Text = "結束";
            this.結束ToolStripMenuItem.Click += new System.EventHandler(this.結束ToolStripMenuItem_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Location = new System.Drawing.Point(0, 30);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1115, 615);
            this.tabControl1.TabIndex = 1;
            this.tabControl1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tabControl1_MouseDown);
            this.tabControl1.MouseLeave += new System.EventHandler(this.tabControl1_MouseLeave);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.伺服器ToolStripMenuItem,
            this.控制ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1117, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 伺服器ToolStripMenuItem
            // 
            this.伺服器ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.開新伺服器ToolStripMenuItem,
            this.重新命名伺服器ToolStripMenuItem,
            this.關閉伺服器ToolStripMenuItem});
            this.伺服器ToolStripMenuItem.Name = "伺服器ToolStripMenuItem";
            this.伺服器ToolStripMenuItem.Size = new System.Drawing.Size(55, 20);
            this.伺服器ToolStripMenuItem.Text = "伺服器";
            // 
            // 開新伺服器ToolStripMenuItem
            // 
            this.開新伺服器ToolStripMenuItem.Name = "開新伺服器ToolStripMenuItem";
            this.開新伺服器ToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.開新伺服器ToolStripMenuItem.Text = "開新伺服器";
            this.開新伺服器ToolStripMenuItem.Click += new System.EventHandler(this.開新伺服器ToolStripMenuItem_Click);
            // 
            // 重新命名伺服器ToolStripMenuItem
            // 
            this.重新命名伺服器ToolStripMenuItem.Name = "重新命名伺服器ToolStripMenuItem";
            this.重新命名伺服器ToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.重新命名伺服器ToolStripMenuItem.Text = "重新命名伺服器";
            this.重新命名伺服器ToolStripMenuItem.Click += new System.EventHandler(this.重新命名伺服器ToolStripMenuItem_Click);
            // 
            // 關閉伺服器ToolStripMenuItem
            // 
            this.關閉伺服器ToolStripMenuItem.Name = "關閉伺服器ToolStripMenuItem";
            this.關閉伺服器ToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.關閉伺服器ToolStripMenuItem.Text = "關閉伺服器";
            this.關閉伺服器ToolStripMenuItem.Click += new System.EventHandler(this.關閉伺服器ToolStripMenuItem_Click);
            // 
            // 控制ToolStripMenuItem
            // 
            this.控制ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.全部啟動ToolStripMenuItem,
            this.全部停止ToolStripMenuItem,
            this.全部重新啟動ToolStripMenuItem,
            this.結束ToolStripMenuItem1});
            this.控制ToolStripMenuItem.Name = "控制ToolStripMenuItem";
            this.控制ToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
            this.控制ToolStripMenuItem.Text = "控制";
            // 
            // 全部啟動ToolStripMenuItem
            // 
            this.全部啟動ToolStripMenuItem.Name = "全部啟動ToolStripMenuItem";
            this.全部啟動ToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.全部啟動ToolStripMenuItem.Text = "全部啟動";
            this.全部啟動ToolStripMenuItem.Click += new System.EventHandler(this.全部啟動ToolStripMenuItem_Click);
            // 
            // 全部停止ToolStripMenuItem
            // 
            this.全部停止ToolStripMenuItem.Name = "全部停止ToolStripMenuItem";
            this.全部停止ToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.全部停止ToolStripMenuItem.Text = "全部停止";
            this.全部停止ToolStripMenuItem.Click += new System.EventHandler(this.全部停止ToolStripMenuItem_Click);
            // 
            // 全部重新啟動ToolStripMenuItem
            // 
            this.全部重新啟動ToolStripMenuItem.Name = "全部重新啟動ToolStripMenuItem";
            this.全部重新啟動ToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.全部重新啟動ToolStripMenuItem.Text = "全部重新啟動";
            this.全部重新啟動ToolStripMenuItem.Click += new System.EventHandler(this.全部重新啟動ToolStripMenuItem_Click);
            // 
            // 結束ToolStripMenuItem1
            // 
            this.結束ToolStripMenuItem1.Name = "結束ToolStripMenuItem1";
            this.結束ToolStripMenuItem1.Size = new System.Drawing.Size(146, 22);
            this.結束ToolStripMenuItem1.Text = "結束";
            this.結束ToolStripMenuItem1.Click += new System.EventHandler(this.結束ToolStripMenuItem_Click);
            // 
            // contextMenuStrip2
            // 
            this.contextMenuStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem2,
            this.toolStripMenuItem3});
            this.contextMenuStrip2.Name = "contextMenuStrip1";
            this.contextMenuStrip2.Size = new System.Drawing.Size(123, 70);
            this.contextMenuStrip2.Text = "伺服器";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(122, 22);
            this.toolStripMenuItem1.Text = "啟動";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(122, 22);
            this.toolStripMenuItem2.Text = "停止";
            this.toolStripMenuItem2.Click += new System.EventHandler(this.toolStripMenuItem2_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(122, 22);
            this.toolStripMenuItem3.Text = "重新啟動";
            this.toolStripMenuItem3.Click += new System.EventHandler(this.toolStripMenuItem3_Click);
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1117, 646);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.tabControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "伺服器";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.contextMenuStrip1.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.contextMenuStrip2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 啟動ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 停止ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 重新啟動ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 結束ToolStripMenuItem;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 伺服器ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 開新伺服器ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 重新命名伺服器ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 關閉伺服器ToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem 控制ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 全部啟動ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 全部停止ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 全部重新啟動ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 結束ToolStripMenuItem1;
        private System.Windows.Forms.Timer timer1;
    }
}

