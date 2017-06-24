using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WzComparerR2.PluginBase;

namespace WzComparerR2
{
    public class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Program.SetDllDirectory();
            Program.StartMainForm();
        }

        public static string LibPath { get; private set; }

        /// <summary>
        /// 这是程序入口无雾。
        /// </summary>
        static void StartMainForm()
        {
            //创建主窗体
            var frm = new MainForm();
            //加载插件
            LoadPlugins(frm);
            //加载配置文件并初始化插件
            var cng = Config.ConfigManager.ConfigFile;
            frm.PluginOnLoad();
            PluginManager.PluginOnLoad();
            //走你
            Application.Run(frm);
        }

        static void LoadPlugins(PluginContextProvider provider)
        {
            var asmList = PluginManager.GetPluginFiles().Select(asmFile =>
            {
                try
                {
                    var asmName = AssemblyName.GetAssemblyName(asmFile);
                    return Assembly.Load(asmName);
                }
                catch(Exception ex)
                {
                    return null;
                }
            }).OfType<Assembly>().ToList();

            var context = new PluginContext(provider);
            asmList.SelectMany(asm => PluginManager.LoadPlugin(asm, context)).ToList();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                string logFile = Path.Combine(Application.StartupPath, "error.log");
                try
                {
                    string content = DateTime.Now.ToString() + "\r\n" + ex.ToString() + "\r\n";
                    File.AppendAllText(logFile, content);
                }
                catch
                {
                }
            }
        }

        static void SetDllDirectory()
        {
            LibPath = Path.Combine(Application.StartupPath, "Lib", Environment.Is64BitProcess ? "x64" : "x86");
            SetDllDirectory(LibPath);

            foreach (var dllName in Directory.GetFiles(LibPath, "*.dll"))
            {
                var handle = LoadLibrary(dllName);
            }
        }

        [DllImport("kernel32.dll")]
        static extern bool SetDllDirectory(string path);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string path);
    }
}
