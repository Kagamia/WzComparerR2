using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace WzComparerR2.Updater
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
            string newFile = CopySelf(); //模拟下载新文件
            OpenCopy(newFile);
            this.Close();
        }

        private string CopySelf()
        {
            string fileSrc = this.GetType().Assembly.Location;
            string updateDir = Path.Combine(Path.GetDirectoryName(fileSrc), "update");
            if (!Directory.Exists(updateDir))
            {
                Directory.CreateDirectory(updateDir);
            }

            string fileDest = Path.Combine(updateDir, Path.GetFileName(fileSrc));
            while (File.Exists(fileDest))
            {
                fileDest = Path.Combine(updateDir, Path.GetRandomFileName() + ".exe");
            }
            File.Copy(fileSrc, fileDest);
            return fileDest;
        }

        private void OpenCopy(string tempExeFileName)
        {
            int pid = Process.GetCurrentProcess().Id;

            ProcessStartInfo startInfo = new ProcessStartInfo(tempExeFileName);
            StringBuilder sb = new StringBuilder();
            string[] args = new[] {
                "/w:"+pid, 
                "/s:"+this.GetType().Assembly.Location,
                "/o"
                };
            foreach (var arg in args)
            {
                if (arg.Contains(" "))
                {
                    sb.AppendFormat("\"{0}\"", arg);
                }
                else
                {
                    sb.Append(arg);
                }
                sb.Append(" ");
            }
            startInfo.Arguments = sb.ToString();
            Process p = Process.Start(startInfo);
        }
    }
}
