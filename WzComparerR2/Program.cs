using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WzComparerR2
{
    public class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (Form frm = new MainForm())
            {
                Application.Run(frm);
            }
        }
    }
}
