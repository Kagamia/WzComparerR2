namespace WzComparerR2
{
    partial class FrmAbout
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmAbout));
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.labelX2 = new DevComponents.DotNetBar.LabelX();
            this.labelX3 = new DevComponents.DotNetBar.LabelX();
            this.lblAsmVer = new DevComponents.DotNetBar.LabelX();
            this.lblFileVer = new DevComponents.DotNetBar.LabelX();
            this.lblCopyright = new DevComponents.DotNetBar.LabelX();
            this.buttonX1 = new DevComponents.DotNetBar.ButtonX();
            this.advTree1 = new DevComponents.AdvTree.AdvTree();
            this.elementStyle1 = new DevComponents.DotNetBar.ElementStyle();
            ((System.ComponentModel.ISupportInitialize)(this.advTree1)).BeginInit();
            this.SuspendLayout();
            // 
            // labelX1
            // 
            this.labelX1.AutoSize = true;
            // 
            // 
            // 
            this.labelX1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX1.Location = new System.Drawing.Point(12, 12);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(68, 18);
            this.labelX1.TabIndex = 0;
            this.labelX1.Text = "程序版本：";
            // 
            // labelX2
            // 
            this.labelX2.AutoSize = true;
            // 
            // 
            // 
            this.labelX2.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX2.Location = new System.Drawing.Point(12, 36);
            this.labelX2.Name = "labelX2";
            this.labelX2.Size = new System.Drawing.Size(68, 18);
            this.labelX2.TabIndex = 1;
            this.labelX2.Text = "文件版本：";
            // 
            // labelX3
            // 
            this.labelX3.AutoSize = true;
            // 
            // 
            // 
            this.labelX3.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX3.Location = new System.Drawing.Point(12, 60);
            this.labelX3.Name = "labelX3";
            this.labelX3.Size = new System.Drawing.Size(68, 18);
            this.labelX3.TabIndex = 2;
            this.labelX3.Text = "版权所有：";
            // 
            // lblAsmVer
            // 
            this.lblAsmVer.AutoSize = true;
            // 
            // 
            // 
            this.lblAsmVer.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lblAsmVer.Location = new System.Drawing.Point(74, 12);
            this.lblAsmVer.Name = "lblAsmVer";
            this.lblAsmVer.Size = new System.Drawing.Size(13, 16);
            this.lblAsmVer.TabIndex = 4;
            this.lblAsmVer.Text = "-";
            // 
            // lblFileVer
            // 
            this.lblFileVer.AutoSize = true;
            // 
            // 
            // 
            this.lblFileVer.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lblFileVer.Location = new System.Drawing.Point(74, 36);
            this.lblFileVer.Name = "lblFileVer";
            this.lblFileVer.Size = new System.Drawing.Size(13, 16);
            this.lblFileVer.TabIndex = 5;
            this.lblFileVer.Text = "-";
            // 
            // lblCopyright
            // 
            this.lblCopyright.AutoSize = true;
            // 
            // 
            // 
            this.lblCopyright.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lblCopyright.Location = new System.Drawing.Point(74, 60);
            this.lblCopyright.Name = "lblCopyright";
            this.lblCopyright.Size = new System.Drawing.Size(13, 16);
            this.lblCopyright.TabIndex = 6;
            this.lblCopyright.Text = "-";
            // 
            // buttonX1
            // 
            this.buttonX1.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.buttonX1.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.buttonX1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonX1.Location = new System.Drawing.Point(115, 166);
            this.buttonX1.Name = "buttonX1";
            this.buttonX1.Size = new System.Drawing.Size(75, 23);
            this.buttonX1.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.buttonX1.TabIndex = 8;
            this.buttonX1.Text = "关掉我";
            // 
            // advTree1
            // 
            this.advTree1.AccessibleRole = System.Windows.Forms.AccessibleRole.Outline;
            this.advTree1.AllowDrop = true;
            this.advTree1.BackColor = System.Drawing.SystemColors.Window;
            // 
            // 
            // 
            this.advTree1.BackgroundStyle.Class = "TreeBorderKey";
            this.advTree1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.advTree1.DoubleClickTogglesNode = false;
            this.advTree1.DragDropEnabled = false;
            this.advTree1.DragDropNodeCopyEnabled = false;
            this.advTree1.ExpandWidth = 4;
            this.advTree1.HideSelection = true;
            this.advTree1.Location = new System.Drawing.Point(12, 82);
            this.advTree1.Name = "advTree1";
            this.advTree1.NodeStyle = this.elementStyle1;
            this.advTree1.PathSeparator = ";";
            this.advTree1.Size = new System.Drawing.Size(280, 78);
            this.advTree1.Styles.Add(this.elementStyle1);
            this.advTree1.TabIndex = 9;
            this.advTree1.Text = "advTree1";
            // 
            // elementStyle1
            // 
            this.elementStyle1.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.elementStyle1.Name = "elementStyle1";
            this.elementStyle1.TextColor = System.Drawing.SystemColors.ControlText;
            // 
            // FrmAbout
            // 
            this.AcceptButton = this.buttonX1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonX1;
            this.ClientSize = new System.Drawing.Size(304, 201);
            this.Controls.Add(this.advTree1);
            this.Controls.Add(this.buttonX1);
            this.Controls.Add(this.lblCopyright);
            this.Controls.Add(this.lblFileVer);
            this.Controls.Add(this.lblAsmVer);
            this.Controls.Add(this.labelX3);
            this.Controls.Add(this.labelX2);
            this.Controls.Add(this.labelX1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmAbout";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "关于";
            ((System.ComponentModel.ISupportInitialize)(this.advTree1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevComponents.DotNetBar.LabelX labelX1;
        private DevComponents.DotNetBar.LabelX labelX2;
        private DevComponents.DotNetBar.LabelX labelX3;
        private DevComponents.DotNetBar.LabelX lblAsmVer;
        private DevComponents.DotNetBar.LabelX lblFileVer;
        private DevComponents.DotNetBar.LabelX lblCopyright;
        private DevComponents.DotNetBar.ButtonX buttonX1;
        private DevComponents.AdvTree.AdvTree advTree1;
        private DevComponents.DotNetBar.ElementStyle elementStyle1;
    }
}