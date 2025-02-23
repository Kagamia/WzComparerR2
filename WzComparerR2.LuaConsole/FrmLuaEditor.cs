using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using ICSharpCode.TextEditor.Document;

namespace WzComparerR2.LuaConsole
{
    public partial class FrmLuaEditor : DevComponents.DotNetBar.OfficeForm
    {
        static bool globalInit = false;

        public FrmLuaEditor()
        {
            if (!globalInit)
            {
                HighlightingManager.Manager.AddSyntaxModeFileProvider(new AppSyntaxModeProvider());
                globalInit = true;
            }

            InitializeComponent();
            textEditorControl1.SetHighlighting("Lua");
            this.BaseFileName = "untitled";
        }

        private string _baseFileName;
        private bool _isContentModified;

        public void LoadFile(string fileName)
        {
            this.textEditorControl1.LoadFile(fileName, false, true);
            this.BaseFileName = Path.GetFileName(fileName);
            this.IsContentModified = false;
        }

        public void SaveFile(string fileName)
        {
            this.textEditorControl1.SaveFile(fileName);
            this.BaseFileName = Path.GetFileName(fileName);
            this.IsContentModified = false;
        }

        public string FileName
        {
            get { return this.textEditorControl1.FileName; }
            set { this.textEditorControl1.FileName = value; }
        }

        public string CodeContent
        {
            get { return this.textEditorControl1.Text; }
        }

        public bool IsContentModified
        {
            get => this._isContentModified;
            private set
            {
                if (this._isContentModified != value)
                {
                    this._isContentModified = value;
                    this.UpdateTitle();
                }
            }
        }

        public string BaseFileName
        {
            get => this._baseFileName;
            private set
            {
                if (this._baseFileName != value)
                {
                    this._baseFileName = value;
                    this.UpdateTitle();
                }
            }
        }

        public event EventHandler FileNameChanged;

        private void UpdateTitle()
        {
            this.Text = (this._baseFileName ?? "(null)") + (this._isContentModified ? "*" : null);
        }

        private void textEditorControl1_TextChanged(object sender, EventArgs e)
        {
            this.IsContentModified = true;
        }

        private void textEditorControl1_FileNameChanged(object sender, EventArgs e)
        {
            this.FileNameChanged?.Invoke(this, e);
        }
    }
}