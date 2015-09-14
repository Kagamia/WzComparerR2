using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;

namespace WzComparerR2.Updater
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                ProcessStart(args);
            }
            
            if (!willExit)
            {
                if (CreateMutex())
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Form1());
                }
                else
                {
                    MessageBox.Show("Updater程序只能同时运行一个的说...");
                }
            }
            if (onExit != null)
            {
                onExit(null, EventArgs.Empty);
            }
        }

        static bool willExit = false;
        static EventHandler onExit;
        static Mutex mutex;

        static bool CreateMutex()
        {
            bool createNew;
            mutex = new Mutex(true,"WzComparerR2.Updater",out createNew);
            return createNew;
        }

        static void ProcessStart(string[] args)
        {
            Match m;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if ((m = Regex.Match(arg, @"^/w:(\d+)$")).Success)//等待上一个程序退出
                {
                    Process p;
                    try
                    {
                        p = Process.GetProcessById(Convert.ToInt32(m.Result("$1")));
                    }
                    catch (ArgumentException)
                    {
                        p = null; //程序已经成功退出
                    }

                    if (p != null)
                    {
                        //等待退出
                        bool exit = false;
                        for (int retry = 1; retry <= 5; retry++)
                        {
                            exit = p.WaitForExit(1000);
                            if (exit)
                            {
                                break;
                            }
                        }

                        if (!exit) //过期不退出 终止处理
                        {
                            return;
                        }
                    }
                }

                else if ((m = Regex.Match(arg, @"^/s:(.+)$")).Success)//执行自身复制
                {
                    string fileSrc = typeof(Program).Assembly.Location;
                    string fileDst = m.Result("$1");
                    File.Copy(fileSrc, fileDst, true);
                    if (i + 1 < args.Length && args[i + 1] == "/o")
                    {
                        i++;
                        willExit = true;
                        string argNext = string.Format("/w:{0} \"/d:{1}\"", Process.GetCurrentProcess().Id, fileSrc);
                        onExit += (o, e) => Process.Start(fileDst, argNext);
                    }
                }

                else if ((m = Regex.Match(arg, @"^/d:(.+)$")).Success)//执行删除操作
                {
                    File.Delete(m.Result("$1"));
                }
            }
        }
    }
}
