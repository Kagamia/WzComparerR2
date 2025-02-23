namespace WzComparerR2.LuaConsole
{
    partial class FrmLuaEditor
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
            this.textEditorControl1 = new ICSharpCode.TextEditor.TextEditorControl();
            this.SuspendLayout();
            // 
            // textEditorControl1
            // 
            this.textEditorControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textEditorControl1.IsReadOnly = false;
            this.textEditorControl1.Location = new System.Drawing.Point(0, 0);
            this.textEditorControl1.Name = "textEditorControl1";
            this.textEditorControl1.Size = new System.Drawing.Size(284, 261);
            this.textEditorControl1.TabIndex = 0;
            this.textEditorControl1.TextChanged += new System.EventHandler(this.textEditorControl1_TextChanged);
            this.textEditorControl1.FileNameChanged += new System.EventHandler(this.textEditorControl1_FileNameChanged);
            // 
            // FrmLuaEditor
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.textEditorControl1);
            this.DoubleBuffered = true;
            this.Name = "FrmLuaEditor";
            this.Text = "FrmLuaEditor";
            this.ResumeLayout(false);

        }

        #endregion

        private ICSharpCode.TextEditor.TextEditorControl textEditorControl1;
    }
}