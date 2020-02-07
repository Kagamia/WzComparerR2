namespace WzComparerR2.Avatar.UI
{
    partial class EffectForm
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
            this.ItemEffectListBox1 = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.ItemDescBox = new System.Windows.Forms.TextBox();
            this.EffectMoveBox = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.EffectComboBox = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.EffectTextBox = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.MixHairComboBox = new System.Windows.Forms.ComboBox();
            this.MixHairOpacityText = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.BaseHairText = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // ItemEffectListBox1
            // 
            this.ItemEffectListBox1.FormattingEnabled = true;
            this.ItemEffectListBox1.ItemHeight = 12;
            this.ItemEffectListBox1.Location = new System.Drawing.Point(6, 32);
            this.ItemEffectListBox1.Name = "ItemEffectListBox1";
            this.ItemEffectListBox1.Size = new System.Drawing.Size(120, 124);
            this.ItemEffectListBox1.TabIndex = 0;
            this.ItemEffectListBox1.SelectedIndexChanged += new System.EventHandler(this.ItemEffectListBox1_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "道具選擇";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.ItemDescBox);
            this.groupBox1.Controls.Add(this.EffectMoveBox);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.EffectComboBox);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.EffectTextBox);
            this.groupBox1.Controls.Add(this.ItemEffectListBox1);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(316, 164);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "特效管理";
            // 
            // ItemDescBox
            // 
            this.ItemDescBox.Location = new System.Drawing.Point(132, 135);
            this.ItemDescBox.Name = "ItemDescBox";
            this.ItemDescBox.ReadOnly = true;
            this.ItemDescBox.Size = new System.Drawing.Size(170, 21);
            this.ItemDescBox.TabIndex = 8;
            // 
            // EffectMoveBox
            // 
            this.EffectMoveBox.AutoSize = true;
            this.EffectMoveBox.Location = new System.Drawing.Point(132, 81);
            this.EffectMoveBox.Name = "EffectMoveBox";
            this.EffectMoveBox.Size = new System.Drawing.Size(72, 16);
            this.EffectMoveBox.TabIndex = 7;
            this.EffectMoveBox.Text = "移動";
            this.EffectMoveBox.UseVisualStyleBackColor = true;
            this.EffectMoveBox.CheckedChanged += new System.EventHandler(this.EffectMoveBox_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(237, 17);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(65, 12);
            this.label3.TabIndex = 5;
            this.label3.Text = "特效編號";
            // 
            // EffectComboBox
            // 
            this.EffectComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.EffectComboBox.FormattingEnabled = true;
            this.EffectComboBox.Location = new System.Drawing.Point(239, 32);
            this.EffectComboBox.Name = "EffectComboBox";
            this.EffectComboBox.Size = new System.Drawing.Size(63, 20);
            this.EffectComboBox.TabIndex = 4;
            this.EffectComboBox.SelectedIndexChanged += new System.EventHandler(this.EffectComboBox_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(135, 17);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "動作";
            // 
            // EffectTextBox
            // 
            this.EffectTextBox.Location = new System.Drawing.Point(132, 32);
            this.EffectTextBox.Name = "EffectTextBox";
            this.EffectTextBox.ReadOnly = true;
            this.EffectTextBox.Size = new System.Drawing.Size(100, 21);
            this.EffectTextBox.TabIndex = 2;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(531, 13);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 164);
            this.button1.TabIndex = 3;
            this.button1.Text = "關閉";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Button1_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.MixHairComboBox);
            this.groupBox2.Controls.Add(this.MixHairOpacityText);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.BaseHairText);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.button2);
            this.groupBox2.Location = new System.Drawing.Point(335, 13);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(190, 163);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "混染";
            // 
            // MixHairComboBox
            // 
            this.MixHairComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.MixHairComboBox.FormattingEnabled = true;
            this.MixHairComboBox.Location = new System.Drawing.Point(6, 71);
            this.MixHairComboBox.Name = "MixHairComboBox";
            this.MixHairComboBox.Size = new System.Drawing.Size(166, 20);
            this.MixHairComboBox.TabIndex = 7;
            // 
            // MixHairOpacityText
            // 
            this.MixHairOpacityText.Location = new System.Drawing.Point(6, 114);
            this.MixHairOpacityText.Name = "MixHairOpacityText";
            this.MixHairOpacityText.Size = new System.Drawing.Size(166, 21);
            this.MixHairOpacityText.TabIndex = 6;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(8, 98);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(176, 12);
            this.label6.TabIndex = 5;
            this.label6.Text = "混染透明度設定(0 ~ 100)";
            // 
            // BaseHairText
            // 
            this.BaseHairText.Location = new System.Drawing.Point(6, 30);
            this.BaseHairText.Name = "BaseHairText";
            this.BaseHairText.ReadOnly = true;
            this.BaseHairText.Size = new System.Drawing.Size(166, 21);
            this.BaseHairText.TabIndex = 3;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 55);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(57, 12);
            this.label5.TabIndex = 2;
            this.label5.Text = "副顏色";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 16);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(69, 12);
            this.label4.TabIndex = 1;
            this.label4.Text = "主顏色";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(6, 134);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(166, 23);
            this.button2.TabIndex = 0;
            this.button2.Text = "套用";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.Button2_Click);
            // 
            // EffectForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(616, 184);
            this.ControlBox = false;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.groupBox1);
            this.Name = "EffectForm";
            this.Text = "特效管理/混染";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        public System.Windows.Forms.ListBox ItemEffectListBox1;
        public System.Windows.Forms.CheckBox EffectMoveBox;
        public System.Windows.Forms.ComboBox EffectComboBox;
        public System.Windows.Forms.TextBox EffectTextBox;
        public System.Windows.Forms.TextBox ItemDescBox;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button2;
        public System.Windows.Forms.TextBox BaseHairText;
        public System.Windows.Forms.ComboBox MixHairComboBox;
        public System.Windows.Forms.TextBox MixHairOpacityText;
    }
}