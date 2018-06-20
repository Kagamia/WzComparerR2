namespace WzComparerR2
{
    partial class FrmOptions
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
            this.panelEx1 = new DevComponents.DotNetBar.PanelEx();
            this.buttonX2 = new DevComponents.DotNetBar.ButtonX();
            this.buttonX1 = new DevComponents.DotNetBar.ButtonX();
            this.superTabControl1 = new DevComponents.DotNetBar.SuperTabControl();
            this.superTabControlPanel1 = new DevComponents.DotNetBar.SuperTabControlPanel();
            this.chkAutoCheckExtFiles = new DevComponents.DotNetBar.Controls.CheckBoxX();
            this.cmbWzEncoding = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.chkWzAutoSort = new DevComponents.DotNetBar.Controls.CheckBoxX();
            this.superTabItem1 = new DevComponents.DotNetBar.SuperTabItem();
            this.chkWzSortByImgID = new DevComponents.DotNetBar.Controls.CheckBoxX();
            this.chkImgCheckDisabled = new DevComponents.DotNetBar.Controls.CheckBoxX();
            this.panelEx1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.superTabControl1)).BeginInit();
            this.superTabControl1.SuspendLayout();
            this.superTabControlPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelEx1
            // 
            this.panelEx1.CanvasColor = System.Drawing.SystemColors.Control;
            this.panelEx1.ColorSchemeStyle = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.panelEx1.Controls.Add(this.buttonX2);
            this.panelEx1.Controls.Add(this.buttonX1);
            this.panelEx1.DisabledBackColor = System.Drawing.Color.Empty;
            this.panelEx1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelEx1.Location = new System.Drawing.Point(0, 171);
            this.panelEx1.Name = "panelEx1";
            this.panelEx1.Size = new System.Drawing.Size(304, 30);
            this.panelEx1.Style.Alignment = System.Drawing.StringAlignment.Center;
            this.panelEx1.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground;
            this.panelEx1.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground2;
            this.panelEx1.Style.Border = DevComponents.DotNetBar.eBorderType.SingleLine;
            this.panelEx1.Style.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBorder;
            this.panelEx1.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelText;
            this.panelEx1.Style.GradientAngle = 90;
            this.panelEx1.TabIndex = 0;
            // 
            // buttonX2
            // 
            this.buttonX2.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.buttonX2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonX2.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.buttonX2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonX2.Location = new System.Drawing.Point(235, 4);
            this.buttonX2.Name = "buttonX2";
            this.buttonX2.Size = new System.Drawing.Size(60, 23);
            this.buttonX2.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.buttonX2.TabIndex = 1;
            this.buttonX2.Text = "取消";
            // 
            // buttonX1
            // 
            this.buttonX1.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.buttonX1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonX1.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.buttonX1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonX1.Location = new System.Drawing.Point(168, 4);
            this.buttonX1.Name = "buttonX1";
            this.buttonX1.Size = new System.Drawing.Size(60, 23);
            this.buttonX1.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.buttonX1.TabIndex = 0;
            this.buttonX1.Text = "确定";
            // 
            // superTabControl1
            // 
            this.superTabControl1.AutoCloseTabs = false;
            // 
            // 
            // 
            // 
            // 
            // 
            this.superTabControl1.ControlBox.CloseBox.Name = "";
            // 
            // 
            // 
            this.superTabControl1.ControlBox.MenuBox.Name = "";
            this.superTabControl1.ControlBox.Name = "";
            this.superTabControl1.ControlBox.SubItems.AddRange(new DevComponents.DotNetBar.BaseItem[] {
            this.superTabControl1.ControlBox.MenuBox,
            this.superTabControl1.ControlBox.CloseBox});
            this.superTabControl1.Controls.Add(this.superTabControlPanel1);
            this.superTabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.superTabControl1.Location = new System.Drawing.Point(0, 0);
            this.superTabControl1.Name = "superTabControl1";
            this.superTabControl1.ReorderTabsEnabled = true;
            this.superTabControl1.SelectedTabFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Bold);
            this.superTabControl1.SelectedTabIndex = 0;
            this.superTabControl1.Size = new System.Drawing.Size(304, 171);
            this.superTabControl1.TabAlignment = DevComponents.DotNetBar.eTabStripAlignment.Left;
            this.superTabControl1.TabFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.superTabControl1.TabIndex = 4;
            this.superTabControl1.Tabs.AddRange(new DevComponents.DotNetBar.BaseItem[] {
            this.superTabItem1});
            this.superTabControl1.Text = "superTabControl1";
            // 
            // superTabControlPanel1
            // 
            this.superTabControlPanel1.Controls.Add(this.chkImgCheckDisabled);
            this.superTabControlPanel1.Controls.Add(this.chkWzSortByImgID);
            this.superTabControlPanel1.Controls.Add(this.chkAutoCheckExtFiles);
            this.superTabControlPanel1.Controls.Add(this.cmbWzEncoding);
            this.superTabControlPanel1.Controls.Add(this.labelX1);
            this.superTabControlPanel1.Controls.Add(this.chkWzAutoSort);
            this.superTabControlPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.superTabControlPanel1.Location = new System.Drawing.Point(82, 0);
            this.superTabControlPanel1.Name = "superTabControlPanel1";
            this.superTabControlPanel1.Size = new System.Drawing.Size(222, 171);
            this.superTabControlPanel1.TabIndex = 1;
            this.superTabControlPanel1.TabItem = this.superTabItem1;
            // 
            // chkAutoCheckExtFiles
            // 
            this.chkAutoCheckExtFiles.AutoSize = true;
            this.chkAutoCheckExtFiles.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.chkAutoCheckExtFiles.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.chkAutoCheckExtFiles.Location = new System.Drawing.Point(14, 86);
            this.chkAutoCheckExtFiles.Name = "chkAutoCheckExtFiles";
            this.chkAutoCheckExtFiles.Size = new System.Drawing.Size(193, 18);
            this.chkAutoCheckExtFiles.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.chkAutoCheckExtFiles.TabIndex = 4;
            this.chkAutoCheckExtFiles.Text = "自动检测扩展wz文件(map2...)";
            // 
            // cmbWzEncoding
            // 
            this.cmbWzEncoding.DisplayMember = "Text";
            this.cmbWzEncoding.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbWzEncoding.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbWzEncoding.FormattingEnabled = true;
            this.cmbWzEncoding.ItemHeight = 15;
            this.cmbWzEncoding.Location = new System.Drawing.Point(86, 59);
            this.cmbWzEncoding.Name = "cmbWzEncoding";
            this.cmbWzEncoding.Size = new System.Drawing.Size(121, 21);
            this.cmbWzEncoding.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cmbWzEncoding.TabIndex = 3;
            // 
            // labelX1
            // 
            this.labelX1.AutoSize = true;
            this.labelX1.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX1.Location = new System.Drawing.Point(14, 61);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(68, 18);
            this.labelX1.TabIndex = 2;
            this.labelX1.Text = "wz默认编码";
            // 
            // chkWzAutoSort
            // 
            this.chkWzAutoSort.AutoSize = true;
            this.chkWzAutoSort.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.chkWzAutoSort.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.chkWzAutoSort.Location = new System.Drawing.Point(14, 13);
            this.chkWzAutoSort.Name = "chkWzAutoSort";
            this.chkWzAutoSort.Size = new System.Drawing.Size(125, 18);
            this.chkWzAutoSort.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.chkWzAutoSort.TabIndex = 0;
            this.chkWzAutoSort.Text = "Wz加载时自动排序";
            // 
            // superTabItem1
            // 
            this.superTabItem1.AttachedControl = this.superTabControlPanel1;
            this.superTabItem1.GlobalItem = false;
            this.superTabItem1.Name = "superTabItem1";
            this.superTabItem1.Text = "WzLoading";
            // 
            // chkWzSortByImgID
            // 
            this.chkWzSortByImgID.AutoSize = true;
            this.chkWzSortByImgID.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.chkWzSortByImgID.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.chkWzSortByImgID.Location = new System.Drawing.Point(31, 36);
            this.chkWzSortByImgID.Name = "chkWzSortByImgID";
            this.chkWzSortByImgID.Size = new System.Drawing.Size(107, 18);
            this.chkWzSortByImgID.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.chkWzSortByImgID.TabIndex = 5;
            this.chkWzSortByImgID.Text = "按照ImgID排序";
            // 
            // chkImgCheckDisabled
            // 
            this.chkImgCheckDisabled.AutoSize = true;
            this.chkImgCheckDisabled.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.chkImgCheckDisabled.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.chkImgCheckDisabled.Location = new System.Drawing.Point(14, 110);
            this.chkImgCheckDisabled.Name = "chkImgCheckDisabled";
            this.chkImgCheckDisabled.Size = new System.Drawing.Size(132, 18);
            this.chkImgCheckDisabled.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.chkImgCheckDisabled.TabIndex = 6;
            this.chkImgCheckDisabled.Text = "跳过img校验和检测";
            // 
            // FrmOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(304, 201);
            this.Controls.Add(this.superTabControl1);
            this.Controls.Add(this.panelEx1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FrmOptions";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "通用设置";
            this.panelEx1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.superTabControl1)).EndInit();
            this.superTabControl1.ResumeLayout(false);
            this.superTabControlPanel1.ResumeLayout(false);
            this.superTabControlPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private DevComponents.DotNetBar.PanelEx panelEx1;
        private DevComponents.DotNetBar.SuperTabControl superTabControl1;
        private DevComponents.DotNetBar.SuperTabControlPanel superTabControlPanel1;
        private DevComponents.DotNetBar.SuperTabItem superTabItem1;
        private DevComponents.DotNetBar.ButtonX buttonX2;
        private DevComponents.DotNetBar.ButtonX buttonX1;
        private DevComponents.DotNetBar.Controls.CheckBoxX chkWzAutoSort;
        private DevComponents.DotNetBar.LabelX labelX1;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cmbWzEncoding;
        private DevComponents.DotNetBar.Controls.CheckBoxX chkAutoCheckExtFiles;
        private DevComponents.DotNetBar.Controls.CheckBoxX chkWzSortByImgID;
        private DevComponents.DotNetBar.Controls.CheckBoxX chkImgCheckDisabled;
    }
}