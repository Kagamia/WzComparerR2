using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

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
            Program.StartMainForm();
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

        static void StartMainForm()
        {
            using (Form frm = new MainForm())
            {
                Application.Run(frm);
            }
        }
    }
}
