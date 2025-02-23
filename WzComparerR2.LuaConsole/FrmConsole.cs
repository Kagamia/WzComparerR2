using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using NLua;

using WzComparerR2.Config;
using WzComparerR2.LuaConsole.Config;
using WzComparerR2.PluginBase;

namespace WzComparerR2.LuaConsole
{
    public partial class FrmConsole : DevComponents.DotNetBar.Office2007Form
    {
        public FrmConsole()
        {
            InitializeComponent();
            this.refreshRecentDocItems();
            this.env = new LuaEnvironment(this);
        }

        private FrmLuaEditor SelectedLuaEditor => tabStrip1.SelectedTab?.AttachedControl as FrmLuaEditor;

        private readonly LuaEnvironment env;
        private Task runningTask;
        private CancellationTokenSource cancellationTokenSource;

        private void FrmConsole_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && this.runningTask?.IsCompleted == false)
            {
                if (DialogResult.Yes != MessageBoxEx.Show(this, "任务还在运行中，是否关闭？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning))
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void FrmConsole_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.runningTask?.IsCompleted == false && this.cancellationTokenSource != null)
            {
                this.cancellationTokenSource.Cancel();
                try
                {
                    this.runningTask.Wait();
                }
                catch
                {
                    // ignore any error
                }
            }

            foreach (var frm in this.MdiChildren)
            {
                if (frm is FrmLuaEditor editor && editor.Tag is LuaSandbox sandbox)
                {
                    sandbox.Dispose();
                }
            }
        }

        private void FrmConsole_MdiChildActivate(object sender, EventArgs e)
        {
        }

        private void menuReset_Click(object sender, EventArgs e)
        {
            if (this.runningTask?.IsCompleted == false)
            {
                ToastNotification.Show(this, "已有任务正在运行", 1000, eToastPosition.TopCenter);
                return;
            }

            var selectedEditor = this.SelectedLuaEditor;
            if (selectedEditor == null || selectedEditor.Tag is not LuaSandbox sandbox)
            {
                MessageBoxEx.Show(this, "获取当前所选窗体失败。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string baseDir = null;
            if (selectedEditor.FileName != null)
            {
                baseDir = Path.GetDirectoryName(selectedEditor.FileName);
            }
            sandbox.InitLuaEnv(baseDir, true);
            this.env.WriteLine("运行时已重置");
        }

        private void menuNew_Click(object sender, EventArgs e)
        {
            this.CreateNewTab();
        }

        private async void menuRun_Click(object sender, EventArgs e)
        {
            if (this.runningTask?.IsCompleted == false)
            {
                ToastNotification.Show(this, "已有任务正在运行", 1000, eToastPosition.TopCenter);
                return;
            }

            var selectedEditor = this.SelectedLuaEditor;
            if (selectedEditor == null || selectedEditor.Tag is not LuaSandbox sandbox)
            {
                MessageBoxEx.Show(this, "获取当前所选窗体失败。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string baseDir = null;
                if (selectedEditor.FileName != null)
                {
                    baseDir = Path.GetDirectoryName(selectedEditor.FileName);
                }

                this.env.WriteLine("开始执行{0}...", selectedEditor.BaseFileName);
                sandbox.InitLuaEnv(baseDir);
                this.cancellationTokenSource = new CancellationTokenSource();
                this.runningTask = sandbox.DoStringAsync(selectedEditor.CodeContent, cancellationTokenSource.Token);
                await this.runningTask;
            }
            catch (NLua.Exceptions.LuaScriptException ex)
            {
                env.WriteLine(ex);
                if (ex.IsNetException && ex.InnerException != null)
                {
                    env.WriteLine(ex.InnerException);
                }
            }
            catch (Exception ex)
            {
                env.WriteLine(ex);
            }
        }

        private void menuStopRun_Click(object sender, EventArgs e)
        {
            if (this.runningTask?.IsCompleted == false && this.cancellationTokenSource != null)
            {
                this.cancellationTokenSource.Cancel();
                ToastNotification.Show(this, "正在中止运行...", 1000, eToastPosition.TopCenter);
            }
        }

        private void menuOpen_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dlg = new();
            dlg.Filter = "*.lua|*.lua|*.*|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this.OpenFile(dlg.FileName);
            }
        }

        private void menuSave_Click(object sender, EventArgs e)
        {
            if (tabStrip1.SelectedTab?.AttachedControl is FrmLuaEditor editor)
            {
                this.SaveFile(editor, false);
            }
        }

        private void menuSaveAs_Click(object sender, EventArgs e)
        {
            if (tabStrip1.SelectedTab?.AttachedControl is FrmLuaEditor editor)
            {
                this.SaveFile(editor, true);
            }
        }

        private void refreshRecentDocItems()
        {
            this.menuRecent.SubItems.Clear();
            foreach (var doc in LuaConsoleConfig.Default.RecentDocuments)
            {
                ButtonItem item = new ButtonItem() { Text = "&" + (this.menuRecent.SubItems.Count + 1) + ". " + Path.GetFileName(doc), Tooltip = doc, Tag = doc };
                item.Click += RecentDocumentItem_Click;
                this.menuRecent.SubItems.Add(item);
            }
        }

        private void RecentDocumentItem_Click(object sender, EventArgs e)
        {
            if (sender is ButtonItem item && item.Tag is string fileName)
            {
                OpenFile(fileName);
            }
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private bool SaveFile(FrmLuaEditor editor, bool saveAs = false)
        {
            if (saveAs || string.IsNullOrEmpty(editor.FileName))
            {
                using SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "*.lua|*.lua|*.*|*.*";
                if (editor.BaseFileName != null)
                {
                    dlg.FileName = editor.BaseFileName;
                }
                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    return false;
                }
                editor.FileName = dlg.FileName;
            }

            editor.SaveFile(editor.FileName);
            this.env.WriteLine($"====已经保存{editor.FileName}====");
            return true;
        }

        private void OpenFile(string fileName)
        {
            try
            {
                this.CreateNewTab(fileName);
            }
            catch (Exception ex)
            {
                MessageBoxEx.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ConfigManager.Reload();
            var config = LuaConsoleConfig.Default;
            config.RecentDocuments.Remove(fileName);
            config.RecentDocuments.Insert(0, fileName);
            for (int i = config.RecentDocuments.Count - 1; i >= 10; i--)
            {
                config.RecentDocuments.RemoveAt(i);
            }
            ConfigManager.Save();
            refreshRecentDocItems();
        }

        private void CreateNewTab(string fileName = null)
        {
            FrmLuaEditor frm = new FrmLuaEditor();
            frm.FileNameChanged += this.FrmLuaEditor_FileNameChanged;
            frm.FormClosing += this.FrmLuaEditor_FormClosing;
            frm.FormClosed += this.FrmLuaEditor_FormClosed;
            frm.Tag = new LuaSandbox(this.env);
            try
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    frm.LoadFile(fileName);
                }

                frm.MdiParent = this;
                frm.WindowState = FormWindowState.Maximized;
                frm.Show();
            }
            catch
            {
                frm.Dispose();
                throw;
            }
        }

        private void FrmLuaEditor_FileNameChanged(object sender, EventArgs e)
        {
            if (sender is FrmLuaEditor frm)
            {
                foreach (TabItem tab in this.tabStrip1.Tabs)
                {
                    if (tab.AttachedControl == frm)
                    {
                        tab.Tooltip = frm.FileName;
                        break;
                    }
                }
            }
        }

        private void FrmLuaEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sender is FrmLuaEditor editor && editor.IsContentModified)
            {
                switch(MessageBoxEx.Show(this, "关闭窗口将丢失所有未保存的修改，是否保存？", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning))
                {
                    case DialogResult.Yes:
                        e.Cancel = !this.SaveFile(editor, false);
                        break;
                    case DialogResult.No:
                        e.Cancel = false;
                        break;
                    default:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private void FrmLuaEditor_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (sender is FrmLuaEditor frm && frm.Tag is Lua lua)
            {
                lua.Dispose();
            }
        }

        private Lua GetOrCreateLuaVM(FrmLuaEditor frm)
        {
            if (frm.Tag is not Lua lua)
            {
                lua = null;
            }
            return lua;
        }

        public class LuaEnvironment
        {
            internal LuaEnvironment(FrmConsole form)
            {
                this.form = form;
            }

            private FrmConsole form;

            public PluginContext Context
            {
                get
                {
                    return Entry.Instance?.Context;
                }
            }

            public void Write(object value)
            {
                if (value != null)
                {
                    this.AppendText(value.ToString());
                }
            }

            public void Write(string format, params object[] args)
            {
                if (format != null)
                {
                    string content = string.Format(format, args ?? new object[0]);
                    this.AppendText(content);
                }
            }

            public void WriteLine()
            {
                this.WriteLine(null);
            }

            public void WriteLine(object value)
            {
                if (value != null)
                {
                    this.AppendText(value.ToString());
                }
                this.AppendText(Environment.NewLine);
            }

            public void WriteLine(string format, object arg0)
            {
                this.WriteLine(format, new object[] { arg0 });
            }

            public void WriteLine(string format, object arg0, object arg1)
            {
                this.WriteLine(format, new object[] { arg0, arg1 });
            }

            public void WriteLine(string format, object arg0, object arg1, object arg2)
            {
                this.WriteLine(format, new object[] { arg0, arg1, arg2 });
            }

            public void WriteLine(string format, params object[] args)
            {
                if (format != null)
                {
                    string content;
                    if (args == null || args.Length <= 0)
                    {
                        content = format;
                    }
                    else
                    {
                        content = string.Format(format, args ?? new object[0]);
                    }
                    this.AppendText(content);
                }
                this.AppendText(Environment.NewLine);
            }

            public void Help()
            {
                this.WriteLine(@"-- 标准输出函数：
env:Write(object)
env:Write(string format, object[] args)
env:WriteLine(object)
env:WriteLine(string format, object[] args)
");
            }

            private void AppendText(string text)
            {
                if (!this.form.textBoxX2.IsDisposed)
                {
                    text = Regex.Replace(text, @"(?<!\r)\n", "\n", RegexOptions.Multiline);
                    this.form.textBoxX2.AppendText(text);
                }
            }
        }
    }
}
