namespace WzComparerR2
{
    partial class FrmUpdater
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmUpdater));
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.labelX2 = new DevComponents.DotNetBar.LabelX();
            this.labelX3 = new DevComponents.DotNetBar.LabelX();
            this.lblCurrentVer = new DevComponents.DotNetBar.LabelX();
            this.lblLatestVer = new DevComponents.DotNetBar.LabelX();
            this.lblUpdateContent = new DevComponents.DotNetBar.LabelX();
            this.buttonX1 = new DevComponents.DotNetBar.ButtonX();
            this.richTextBoxEx1 = new DevComponents.DotNetBar.Controls.RichTextBoxEx();
            this.chkEnableAutoUpdate = new DevComponents.DotNetBar.Controls.CheckBoxX();
            this.elementStyle1 = new DevComponents.DotNetBar.ElementStyle();
            this.SuspendLayout();
            // 
            // labelX1
            // 
            this.labelX1.AutoSize = true;
            // 
            // 
            // 
            this.labelX1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX1.Location = new System.Drawing.Point(12, 9);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(56, 16);
            this.labelX1.TabIndex = 0;
            this.labelX1.Text = "当前版本:";
            // 
            // labelX2
            // 
            this.labelX2.AutoSize = true;
            // 
            // 
            // 
            this.labelX2.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX2.Location = new System.Drawing.Point(11, 33);
            this.labelX2.Name = "labelX2";
            this.labelX2.Size = new System.Drawing.Size(81, 16);
            this.labelX2.TabIndex = 1;
            this.labelX2.Text = "最新版本:";
            // 
            // labelX3
            // 
            this.labelX3.AutoSize = true;
            // 
            // 
            // 
            this.labelX3.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX3.Location = new System.Drawing.Point(11, 57);
            this.labelX3.Name = "labelX3";
            this.labelX3.Size = new System.Drawing.Size(50, 16);
            this.labelX3.TabIndex = 2;
            this.labelX3.Text = "版本信息:";
            // 
            // lblCurrentVer
            // 
            this.lblCurrentVer.AutoSize = true;
            // 
            // 
            // 
            this.lblCurrentVer.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lblCurrentVer.Location = new System.Drawing.Point(110, 9);
            this.lblCurrentVer.Name = "lblCurrentVer";
            this.lblCurrentVer.Size = new System.Drawing.Size(13, 16);
            this.lblCurrentVer.TabIndex = 4;
            this.lblCurrentVer.Text = "-";
            // 
            // lblLatestVer
            // 
            this.lblLatestVer.AutoSize = true;
            // 
            // 
            // 
            this.lblLatestVer.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lblLatestVer.Location = new System.Drawing.Point(110, 33);
            this.lblLatestVer.Name = "lblLatestVer";
            this.lblLatestVer.Size = new System.Drawing.Size(13, 16);
            this.lblLatestVer.TabIndex = 5;
            this.lblLatestVer.Text = "-";
            // 
            // lblUpdateContent
            // 
            this.lblUpdateContent.AutoSize = true;
            // 
            // 
            // 
            this.lblUpdateContent.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lblUpdateContent.Location = new System.Drawing.Point(110, 57);
            this.lblUpdateContent.Name = "lblUpdateContent";
            this.lblUpdateContent.Size = new System.Drawing.Size(13, 16);
            this.lblUpdateContent.TabIndex = 6;
            this.lblUpdateContent.Text = "正在检查更新...";
            // 
            // buttonX1
            // 
            this.buttonX1.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.buttonX1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonX1.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            // this.buttonX1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonX1.Location = new System.Drawing.Point(110, 187);
            this.buttonX1.Name = "buttonX1";
            this.buttonX1.Size = new System.Drawing.Size(85, 23);
            this.buttonX1.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.buttonX1.TabIndex = 8;
            this.buttonX1.Enabled = false;
            this.buttonX1.Text = "更新";
            this.buttonX1.Click += new System.EventHandler(this.buttonX1_Click);
            // 
            // richTextBoxEx1
            // 
            // 
            // 
            // 
            this.richTextBoxEx1.BackgroundStyle.Class = "RichTextBoxBorder";
            this.richTextBoxEx1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.richTextBoxEx1.Location = new System.Drawing.Point(12, 81);
            this.richTextBoxEx1.Name = "richTextBoxEx1";
            this.richTextBoxEx1.Rtf = "{\\rtf1\\ansi\\ansicpg936\\deff0\\deflang1033\\deflangfe1042{\\fonttbl{\\f0\\fnil\\fcharset" +
    "129 \\\'b5\\\'b8\\\'bf\\\'f2;}}\r\n\\viewkind4\\uc1\\pard\\lang1042\\f0\\fs18\\par\r\n}\r\n";
            this.richTextBoxEx1.Size = new System.Drawing.Size(280, 100);
            this.richTextBoxEx1.ReadOnly = true;
            this.richTextBoxEx1.TabIndex = 9;
            // 
            // elementStyle1
            // 
            this.elementStyle1.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.elementStyle1.Name = "elementStyle1";
            this.elementStyle1.TextColor = System.Drawing.SystemColors.ControlText;
            // 
            // chkEnableAutoUpdate
            // 
            this.chkEnableAutoUpdate.AutoSize = true;
            this.chkEnableAutoUpdate.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.chkEnableAutoUpdate.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.chkEnableAutoUpdate.Location = new System.Drawing.Point(12, 189);
            this.chkEnableAutoUpdate.Name = "chkEnableAutoUpdate";
            this.chkEnableAutoUpdate.Size = new System.Drawing.Size(170, 23);
            this.chkEnableAutoUpdate.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.chkEnableAutoUpdate.TabIndex = 10;
            this.chkEnableAutoUpdate.Text = "自动检查更新";
            this.chkEnableAutoUpdate.CheckedChanged += new System.EventHandler(this.chkEnableAutoUpdate_CheckedChanged);
            // 
            // FrmUpdater
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(304, 221);
            this.Controls.Add(this.richTextBoxEx1);
            this.Controls.Add(this.buttonX1);
            this.Controls.Add(this.lblUpdateContent);
            this.Controls.Add(this.lblLatestVer);
            this.Controls.Add(this.lblCurrentVer);
            this.Controls.Add(this.labelX3);
            this.Controls.Add(this.labelX2);
            this.Controls.Add(this.labelX1);
            this.Controls.Add(this.chkEnableAutoUpdate);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmUpdater";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "更新程序";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmUpdater_FormClosed);
            this.Load += new System.EventHandler(this.FrmUpdater_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevComponents.DotNetBar.LabelX labelX1;
        private DevComponents.DotNetBar.LabelX labelX2;
        private DevComponents.DotNetBar.LabelX labelX3;
        private DevComponents.DotNetBar.LabelX lblCurrentVer;
        private DevComponents.DotNetBar.LabelX lblLatestVer;
        private DevComponents.DotNetBar.LabelX lblUpdateContent;
        private DevComponents.DotNetBar.ButtonX buttonX1;
        private DevComponents.DotNetBar.Controls.CheckBoxX chkEnableAutoUpdate;
        private DevComponents.DotNetBar.Controls.RichTextBoxEx richTextBoxEx1;
        private DevComponents.DotNetBar.ElementStyle elementStyle1;
    }
}