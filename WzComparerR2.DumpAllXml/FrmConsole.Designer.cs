namespace WzComparerR2.DumpAllXml {
    partial class FrmConsole {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.styleManager1 = new DevComponents.DotNetBar.StyleManager(this.components);
            this.panelExMain = new DevComponents.DotNetBar.PanelEx();
            this.label导出文件夹路径 = new DevComponents.DotNetBar.LabelX();
            this.textBox导出文件夹路径 = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new DevComponents.DotNetBar.ButtonX();
            this.label过滤数据类型 = new DevComponents.DotNetBar.LabelX();
            this.textBox过滤数据类型 = new System.Windows.Forms.TextBox();
            this.label待导出WZ类型 = new DevComponents.DotNetBar.LabelX();
            this.textBox待导出WZ类型 = new System.Windows.Forms.TextBox();
            this.label导出并发数 = new DevComponents.DotNetBar.LabelX();
            this.numericUpDown导出并发数 = new System.Windows.Forms.NumericUpDown();
            this.lineSeparator = new DevComponents.DotNetBar.Controls.Line();
            this.buttonStart = new DevComponents.DotNetBar.ButtonX();
            this.label日志 = new DevComponents.DotNetBar.LabelX();
            this.logBox = new System.Windows.Forms.RichTextBox();
            this.uiTimer = new System.Windows.Forms.Timer(this.components);
            this.panelExMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown导出并发数)).BeginInit();
            this.SuspendLayout();
            // 
            // styleManager1
            // 
            this.styleManager1.ManagerStyle = DevComponents.DotNetBar.eStyle.Office2007VistaGlass;
            // 
            // panelExMain
            // 
            this.panelExMain.CanvasColor = System.Drawing.SystemColors.Control;
            this.panelExMain.ColorSchemeStyle = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.panelExMain.Controls.Add(this.logBox);
            this.panelExMain.Controls.Add(this.label日志);
            this.panelExMain.Controls.Add(this.buttonStart);
            this.panelExMain.Controls.Add(this.lineSeparator);
            this.panelExMain.Controls.Add(this.numericUpDown导出并发数);
            this.panelExMain.Controls.Add(this.label导出并发数);
            this.panelExMain.Controls.Add(this.textBox待导出WZ类型);
            this.panelExMain.Controls.Add(this.label待导出WZ类型);
            this.panelExMain.Controls.Add(this.textBox过滤数据类型);
            this.panelExMain.Controls.Add(this.label过滤数据类型);
            this.panelExMain.Controls.Add(this.buttonBrowse);
            this.panelExMain.Controls.Add(this.textBox导出文件夹路径);
            this.panelExMain.Controls.Add(this.label导出文件夹路径);
            this.panelExMain.DisabledBackColor = System.Drawing.Color.Empty;
            this.panelExMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelExMain.Location = new System.Drawing.Point(0, 0);
            this.panelExMain.Name = "panelExMain";
            this.panelExMain.Size = new System.Drawing.Size(620, 560);
            this.panelExMain.Style.Alignment = System.Drawing.StringAlignment.Center;
            this.panelExMain.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground;
            this.panelExMain.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground2;
            this.panelExMain.Style.Border = DevComponents.DotNetBar.eBorderType.SingleLine;
            this.panelExMain.Style.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBorder;
            this.panelExMain.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelText;
            this.panelExMain.Style.GradientAngle = 90;
            this.panelExMain.TabIndex = 0;
            // 
            // label导出文件夹路径
            // 
            this.label导出文件夹路径.AutoSize = true;
            this.label导出文件夹路径.BackColor = System.Drawing.Color.Transparent;
            this.label导出文件夹路径.Location = new System.Drawing.Point(16, 20);
            this.label导出文件夹路径.Name = "label导出文件夹路径";
            this.label导出文件夹路径.Size = new System.Drawing.Size(89, 20);
            this.label导出文件夹路径.TabIndex = 0;
            this.label导出文件夹路径.Text = "导出文件夹路径";
            // 
            // textBox导出文件夹路径
            // 
            this.textBox导出文件夹路径.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox导出文件夹路径.Location = new System.Drawing.Point(120, 19);
            this.textBox导出文件夹路径.Name = "textBox导出文件夹路径";
            this.textBox导出文件夹路径.Size = new System.Drawing.Size(372, 21);
            this.textBox导出文件夹路径.TabIndex = 1;
            // 
            // buttonBrowse
            // 
            this.buttonBrowse.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.buttonBrowse.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.buttonBrowse.Location = new System.Drawing.Point(500, 17);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(96, 26);
            this.buttonBrowse.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.buttonBrowse.TabIndex = 2;
            this.buttonBrowse.Text = "浏览...";
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            // 
            // label过滤数据类型
            // 
            this.label过滤数据类型.AutoSize = true;
            this.label过滤数据类型.BackColor = System.Drawing.Color.Transparent;
            this.label过滤数据类型.Location = new System.Drawing.Point(16, 54);
            this.label过滤数据类型.Name = "label过滤数据类型";
            this.label过滤数据类型.Size = new System.Drawing.Size(77, 20);
            this.label过滤数据类型.TabIndex = 3;
            this.label过滤数据类型.Text = "过滤数据类型";
            // 
            // textBox过滤数据类型
            // 
            this.textBox过滤数据类型.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox过滤数据类型.Location = new System.Drawing.Point(120, 53);
            this.textBox过滤数据类型.Name = "textBox过滤数据类型";
            this.textBox过滤数据类型.Size = new System.Drawing.Size(476, 21);
            this.textBox过滤数据类型.TabIndex = 4;
            this.textBox过滤数据类型.Text = "Wz_Png,Wz_Uol,Wz_Sound,Wz_Convex,Wz_RawData,Wz_Video,canvas";
            // 
            // label待导出WZ类型
            // 
            this.label待导出WZ类型.AutoSize = true;
            this.label待导出WZ类型.BackColor = System.Drawing.Color.Transparent;
            this.label待导出WZ类型.Location = new System.Drawing.Point(16, 88);
            this.label待导出WZ类型.Name = "label待导出WZ类型";
            this.label待导出WZ类型.Size = new System.Drawing.Size(77, 20);
            this.label待导出WZ类型.TabIndex = 5;
            this.label待导出WZ类型.Text = "待导出WZ类型";
            // 
            // textBox待导出WZ类型
            // 
            this.textBox待导出WZ类型.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox待导出WZ类型.Location = new System.Drawing.Point(120, 87);
            this.textBox待导出WZ类型.Name = "textBox待导出WZ类型";
            this.textBox待导出WZ类型.Size = new System.Drawing.Size(476, 21);
            this.textBox待导出WZ类型.TabIndex = 6;
            this.textBox待导出WZ类型.Text = "Character,Effect,Etc,Item,Map,Mob,Morph,Npc,Quest,Reactor,Skill,Sound,String,Tami" +
    "ngMob,UI";
            // 
            // label导出并发数
            // 
            this.label导出并发数.AutoSize = true;
            this.label导出并发数.BackColor = System.Drawing.Color.Transparent;
            this.label导出并发数.Location = new System.Drawing.Point(16, 122);
            this.label导出并发数.Name = "label导出并发数";
            this.label导出并发数.Size = new System.Drawing.Size(65, 20);
            this.label导出并发数.TabIndex = 7;
            this.label导出并发数.Text = "导出并发数";
            // 
            // numericUpDown导出并发数
            // 
            this.numericUpDown导出并发数.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.numericUpDown导出并发数.Location = new System.Drawing.Point(120, 121);
            this.numericUpDown导出并发数.Maximum = new decimal(new int[] { 256, 0, 0, 0});
            this.numericUpDown导出并发数.Minimum = new decimal(new int[] { 1, 0, 0, 0});
            this.numericUpDown导出并发数.Name = "numericUpDown导出并发数";
            this.numericUpDown导出并发数.Size = new System.Drawing.Size(90, 21);
            this.numericUpDown导出并发数.TabIndex = 8;
            this.numericUpDown导出并发数.Value = new decimal(new int[] { 64, 0, 0, 0});
            // 
            // lineSeparator
            // 
            this.lineSeparator.Location = new System.Drawing.Point(16, 158);
            this.lineSeparator.Name = "lineSeparator";
            this.lineSeparator.Size = new System.Drawing.Size(580, 2);
            this.lineSeparator.TabIndex = 9;
            this.lineSeparator.Text = "line1";
            // 
            // buttonStart
            // 
            this.buttonStart.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.buttonStart.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.buttonStart.Location = new System.Drawing.Point(476, 118);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(120, 30);
            this.buttonStart.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.buttonStart.TabIndex = 10;
            this.buttonStart.Text = "开始一键解析";
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // label日志
            // 
            this.label日志.AutoSize = true;
            this.label日志.BackColor = System.Drawing.Color.Transparent;
            this.label日志.Location = new System.Drawing.Point(16, 168);
            this.label日志.Name = "label日志";
            this.label日志.Size = new System.Drawing.Size(29, 20);
            this.label日志.TabIndex = 11;
            this.label日志.Text = "日志";
            // 
            // logBox
            // 
            this.logBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logBox.BackColor = System.Drawing.Color.White;
            this.logBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.logBox.Location = new System.Drawing.Point(16, 192);
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.Size = new System.Drawing.Size(588, 350);
            this.logBox.TabIndex = 12;
            this.logBox.Text = "";
            this.logBox.WordWrap = false;
            // 
            // uiTimer
            // 
            this.uiTimer.Tick += new System.EventHandler(this.uiTimer_Tick);
            // 
            // FrmConsole
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(620, 560);
            this.Controls.Add(this.panelExMain);
            this.DoubleBuffered = true;
            this.MinimumSize = new System.Drawing.Size(560, 460);
            this.Name = "FrmConsole";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "一键解析XML";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmConsole_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmConsole_FormClosed);
            this.panelExMain.ResumeLayout(false);
            this.panelExMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown导出并发数)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevComponents.DotNetBar.StyleManager styleManager1;
        private DevComponents.DotNetBar.PanelEx panelExMain;
        private DevComponents.DotNetBar.LabelX label导出文件夹路径;
        private System.Windows.Forms.TextBox textBox导出文件夹路径;
        private DevComponents.DotNetBar.ButtonX buttonBrowse;
        private DevComponents.DotNetBar.LabelX label过滤数据类型;
        private System.Windows.Forms.TextBox textBox过滤数据类型;
        private DevComponents.DotNetBar.LabelX label待导出WZ类型;
        private System.Windows.Forms.TextBox textBox待导出WZ类型;
        private DevComponents.DotNetBar.LabelX label导出并发数;
        private System.Windows.Forms.NumericUpDown numericUpDown导出并发数;
        private DevComponents.DotNetBar.Controls.Line lineSeparator;
        private DevComponents.DotNetBar.ButtonX buttonStart;
        private DevComponents.DotNetBar.LabelX label日志;
        private System.Windows.Forms.RichTextBox logBox;
        private System.Windows.Forms.Timer uiTimer;
    }
}
