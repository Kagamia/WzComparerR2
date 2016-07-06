using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using DevComponents.AdvTree;

namespace WzComparerR2
{
    public partial class FrmAbout : DevComponents.DotNetBar.Office2007Form
    {
        public FrmAbout()
        {
            InitializeComponent();

            this.lblClrVer.Text = Environment.Version.ToString();
            this.lblAsmVer.Text = GetAsmVersion().ToString();
            this.lblFileVer.Text = GetFileVersion().ToString();
            this.lblCopyright.Text = GetAsmCopyright().ToString();
            GetPluginInfo();
        }

        private Version GetAsmVersion()
        {
            return this.GetType().Assembly.GetName().Version;
        }

        private string GetFileVersion()
        {
            return FileVersionInfo.GetVersionInfo(this.GetType().Assembly.Location).FileVersion;
        }

        private string GetAsmCopyright()
        {
            Assembly asm = this.GetType().Assembly;
            object[] attri = asm.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            return (attri == null || attri.Length <= 0) ? string.Empty : (attri[0] as AssemblyCopyrightAttribute).Copyright;
        }

        private void GetPluginInfo()
        {
            this.advTree1.Nodes.Clear();

            if (PluginBase.PluginManager.LoadedPlugins.Count > 0)
            {
                foreach (var plugin in PluginBase.PluginManager.LoadedPlugins)
                {
                    string nodeTxt = string.Format("{0} <font color=\"#808080\">{1}</font>", plugin.Instance.Name, plugin.Instance.Version);
                    Node node = new Node(nodeTxt);
                    this.advTree1.Nodes.Add(node);
                }
            }
            else
            {
                string nodeTxt = "<font color=\"#808080\">没有加载插件_(:з」∠)_</font>";
                Node node = new Node(nodeTxt);
                this.advTree1.Nodes.Add(node);
            }
        }

    }
}
