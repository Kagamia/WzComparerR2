using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;
using DevComponents.DotNetBar;
using ICSharpCode.TextEditor.Document;
using NLua;

namespace WzComparerR2.LuaConsole
{
    public partial class FrmConsole : DevComponents.DotNetBar.Office2007Form
    {
        //NoOptimization防止Assembly.GetCallingAssembly因尾调用优化出错
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
        public FrmConsole()
        {
            InitializeComponent();
            HighlightingManager.Manager.AddSyntaxModeFileProvider(new AppSyntaxModeProvider());
            this.env = new LuaEnvironment(this);
            this.InitLuaEnv();
        }

        Lua lua;
        LuaEnvironment env;
        Thread executeThread;
        bool isRunning;

        private void InitLuaEnv()
        {
            lua = new Lua();
            lua.State.Encoding = Encoding.UTF8;
            lua.LoadCLRPackage();
            lua["env"] = env;

            lua.DoString(@"
local t_IEnumerable = {}
t_IEnumerable.typeRef = luanet.import_type('System.Collections.IEnumerable')
t_IEnumerable.GetEnumerator = luanet.get_method_bysig(t_IEnumerable.typeRef, 'GetEnumerator')

local t_IEnumerator = {}
t_IEnumerator.typeRef = luanet.import_type('System.Collections.IEnumerator')
t_IEnumerator.MoveNext = luanet.get_method_bysig(t_IEnumerator.typeRef, 'MoveNext')
t_IEnumerator.get_Current = luanet.get_method_bysig(t_IEnumerator.typeRef, 'get_Current')

function each(userData)
  if type(userData) == 'userdata' then
    local i = 0;
    local ienum = t_IEnumerable.GetEnumerator(userData)
    return function()
      if ienum and t_IEnumerator.MoveNext(ienum) then
        i = i + 1
        return i, t_IEnumerator.get_Current(ienum)
      end
      return nil, nil
    end
  end
end
");

            string dllPath = Assembly.GetCallingAssembly().Location;
            string baseDir = Path.GetDirectoryName(dllPath);
            string[] packageFile = new string[] { 
                "?.lua", 
                "?\\init.lua", 
                "Lua\\?.lua", 
                "Lua\\?\\init.lua" };
            string packageDir = string.Join(";", Array.ConvertAll(packageFile, s => Path.Combine(baseDir, s)));
            lua.DoString(string.Format("package.path = [[{0}]]..';'..package.path", packageDir));
            lua.RegisterLuaDelegateType(typeof(WzComparerR2.GlobalFindNodeFunction), typeof(LuaGlobalFindNodeFunctionHandler)); 
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
                    this.form.textBoxX2.AppendText(value.ToString());
                }
            }

            public void Write(string format, params object[] args)
            {
                if (format != null)
                {
                    string content = string.Format(format, args ?? new object[0]);
                    this.form.textBoxX2.AppendText(content);
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
                    this.form.textBoxX2.AppendText(value.ToString());
                }
                this.form.textBoxX2.AppendText(Environment.NewLine);
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
                    this.form.textBoxX2.AppendText(content);
                }
                this.form.textBoxX2.AppendText(Environment.NewLine);
            }

            public void Help()
            {
                this.WriteLine(@"-- 标准输出函数：
env:Write(object)
env:Write(string format, object[] args)
env:WriteLine(object)
env:WriteLine(string format, object[] args)");
            }

            private void AppendText(string text)
            {
                text = Regex.Replace(text, @"(?<!\r)\n", "\r\n", RegexOptions.Multiline);
                this.form.textBoxX2.AppendText(text);
            }
        }

        private void FrmConsole_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && this.isRunning)
            {
                if (DialogResult.Yes == MessageBoxEx.Show("还有未完成的任务，是否关闭？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information))
                {
                    e.Cancel = false;
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        private void FrmConsole_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.executeThread != null)
            {
                this.executeThread.Abort();
            }
        }

        private void buttonItem3_Click(object sender, EventArgs e)
        {
            /*
            LuaDocContainerItem tabItem = new LuaDocContainerItem("", "新文档嘿");
            this.bar3.Items.Add(tabItem);
            this.bar3.Controls.Add(tabItem.DockContainer);
            this.bar3.SelectedDockTab = this.bar3.Items.IndexOf(tabItem);
            this.bar3.Visible = true;*/
        }

        void tabItem_VisibleChanged(object sender, EventArgs e)
        {
            /*
            LuaDocContainerItem tabItem = sender as LuaDocContainerItem;
            this.bar3.Controls.Remove(tabItem.Control);
            this.bar3.Items.Remove(tabItem);*/
        }

        private void FrmConsole_MdiChildActivate(object sender, EventArgs e)
        {

        }

        private void menuReset_Click(object sender, EventArgs e)
        {
            if (!isRunning)
            {
                InitLuaEnv();
                textBoxX2.AppendText("===虚拟机已重置===\r\n");
            }
        }

        private void menuNew_Click(object sender, EventArgs e)
        {
            FrmLuaEditor frm = new FrmLuaEditor();
            frm.MdiParent = this;
            frm.WindowState = FormWindowState.Maximized;
            frm.Show();
        }

        private void menuRun_Click(object sender, EventArgs e)
        {
            FrmLuaEditor editor;
            if (tabStrip1.SelectedTab == null
               || (editor = tabStrip1.SelectedTab.AttachedControl as FrmLuaEditor) == null)
            {
                return;
            }

            try
            {
                lua.DoString(editor.CodeContent);
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

        private void menuOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "*.lua|*.lua|*.*|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                FrmLuaEditor frm = new FrmLuaEditor();
                frm.MdiParent = this;
                frm.WindowState = FormWindowState.Maximized;
                frm.LoadFile(dlg.FileName);
                frm.Show();
            }
        }

        private void menuSave_Click(object sender, EventArgs e)
        {
            FrmLuaEditor editor;
            if (tabStrip1.SelectedTab == null
               || (editor = tabStrip1.SelectedTab.AttachedControl as FrmLuaEditor) == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(editor.FileName))
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "*.lua|*.lua|*.*|*.*";
                dlg.FileName = editor.Text + ".lua";
                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                editor.FileName = dlg.FileName;
            }

            editor.SaveFile(editor.FileName);
            textBoxX2.AppendText($"====已经保存{editor.FileName}====");
        }

        class LuaGlobalFindNodeFunctionHandler : NLua.Method.LuaDelegate
        {
            Wz_Node CallFunction(string wzPath)
            {
                object[] args = { wzPath };
                object[] inArgs = { wzPath };
                int[] outArgs = { };

                object ret = CallFunction(args, inArgs, outArgs);

                return (Wz_Node)ret;
            }
        }
    }
}
