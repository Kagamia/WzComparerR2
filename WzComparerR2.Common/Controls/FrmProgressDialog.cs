using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WzComparerR2.Controls
{
    public partial class FrmProgressDialog : DevComponents.DotNetBar.Office2007Form
    {
        public FrmProgressDialog()
        {
            InitializeComponent();
        }

        public string Message
        {
            get { return this.labelX1.Text; }
            set { this.labelX1.Text = value; }
        }

        public int Progress
        {
            get { return this.progressBarX1.Value; }
            set { this.progressBarX1.Value = value; }
        }

        public int ProgressMin
        {
            get { return this.progressBarX1.Minimum; }
            set { this.progressBarX1.Minimum = value; }
        }

        public int ProgressMax
        {
            get { return this.progressBarX1.Maximum; }
            set { this.progressBarX1.Maximum = value; }
        }
    }
}
