using System;
using System.Collections.Generic;
using System.Text;
using DevComponents.DotNetBar;
using ICSharpCode.TextEditor;
using System.Windows.Forms;

namespace WzComparerR2.LuaConsole
{
    public class LuaDocContainerItem : DockContainerItem
    {
        public LuaDocContainerItem()
            : base()
        {
            if (!inited)
            {
                inited = true;
                this.InitializeComponent();
            }
        }

        public LuaDocContainerItem(string name, string text)
            : base(name, text)
        {
            if (!inited)
            {
                inited = true;
                this.InitializeComponent();
            }
        }

        bool inited;

        private void InitializeComponent()
        {
            this.DockContainer = new PanelDockContainer();
            this.TextEditor = new TextEditorControl();

            this.TextEditor.Dock = DockStyle.Fill;
            this.TextEditor.IsReadOnly = false;
            this.TextEditor.Text = "require \'CLRPackage\'\r\nimport \'WzComparerR2.PluginBase\'\r\nimport \'WzComparerR2.WzLi" +
    "b\'\r\n\r\nlocal baseWz = PluginManager.FindWz(Wz_Type.Base)\r\nenv:WriteLine(baseWz an" +
    "d \'已加载BaseWz\' or \'未加载BaseWz\')";
            this.DockContainer.Controls.Add(this.TextEditor);
            this.Control = this.DockContainer;


        }

        public PanelDockContainer DockContainer { get; private set; }
        public TextEditorControl TextEditor { get; private set; }
    }
}
