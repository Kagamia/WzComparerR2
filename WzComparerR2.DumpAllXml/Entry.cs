using DevComponents.DotNetBar;
using System;
using WzComparerR2.Config;
using WzComparerR2.PluginBase;

namespace WzComparerR2.DumpAllXml {
    public class Entry : PluginEntry {
        public Entry(PluginContext context)
            : base(context) {
            Instance = this;
        }

        internal static Entry Instance { get; private set; }

        protected override void OnLoad() {
            var bar = this.Context.AddRibbonBar("Tools", "一键导出");
            ButtonItem btnItem = new ButtonItem("", "一键导出所有XML");

            btnItem.Click += btnItem_Click;
            bar.Items.Add(btnItem);
            ConfigManager.RegisterAllSection(this.GetType().Assembly);

            // 初始化配置文件
            WzComparerR2.Config.ConfigManager.RegisterAllSection();
        }

        FrmConsole frm;
        void btnItem_Click(object sender, EventArgs e) {
            if (frm == null || frm.IsDisposed) {
                frm = new FrmConsole();
                frm.Owner = Context.MainForm;

            }
            frm.Show();
            frm.Focus();
        }

        protected override void OnUnload() {
            base.OnUnload();
        }
    }
}
