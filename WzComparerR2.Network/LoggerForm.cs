using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using DevComponents.DotNetBar.Controls;

namespace WzComparerR2.Network
{
    public partial class LoggerForm : Form
    {
        public LoggerForm()
        {
            InitializeComponent();
        }

        public event EventHandler<CommandEventArgs> OnCommand;

        public void AttachDockBar(DockSite dockSite)
        {
            this.bar1.Controls.Remove(this.panelDockContainer1);
            this.bar1.Items.Remove(this.dockContainerItem1);

            var bar = dockSite.Controls[0] as Bar;
            bar.Controls.Add(this.panelDockContainer1);
            bar.Items.Add(this.dockContainerItem1);
        }

        internal LogPrinter GetLogger()
        {
            return new LogPrinter(this.richTextBoxEx1);
        }

        public class LogPrinter : ILogger
        {
            public LogPrinter(RichTextBoxEx textbox)
            {
                this.textbox = textbox;
            }

            public LogLevel Level { get; set; }
            RichTextBoxEx textbox;

            void ILogger.Write(LogLevel logLevel, string format, params object[] args)
            {
                if (logLevel >= this.Level)
                {
                    var color = GetLogColor(logLevel);

                    if (logLevel < LogLevel.None)
                    {
                        this.AppendText($"[{logLevel}]", color);
                    }
                   
                    this.AppendText($"[{DateTime.Now:HH:mm:ss}]", Color.Blue);
                    if (args == null || args.Length <= 0)
                    {
                        this.AppendText(format, color);
                    }
                    else
                    {
                        this.AppendText(string.Format(format, args), color);
                    }
                    this.textbox.AppendText(Environment.NewLine);
                    this.textbox.ScrollToCaret();
                }
            }

            private void AppendText(string text, Color color)
            {
                this.textbox.SelectionStart = this.textbox.TextLength;
                this.textbox.SelectionLength = 0;

                this.textbox.SelectionColor = color;
                this.textbox.AppendText(text);
                this.textbox.SelectionColor = this.textbox.ForeColor;
            }

            private Color GetLogColor(LogLevel level)
            {
                switch (level)
                {
                    case LogLevel.Debug: return Color.Gray;
                    case LogLevel.Info: return Color.Black;
                    case LogLevel.Warn: return Color.Orange;
                    case LogLevel.Error: return Color.Red;
                    default: return Color.DarkBlue;
                }
            }
        }

        private void textBoxX1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string txt = textBoxX1.Text;
                if (!string.IsNullOrWhiteSpace(txt))
                {
                    var ev = new CommandEventArgs(txt);
                    this.OnCommand?.Invoke(this, ev);
                }
                textBoxX1.Clear();
            }
        }
    }

    public sealed class CommandEventArgs : EventArgs
    {
        public CommandEventArgs(string command)
        {
            this.Command = command;
        }

        public string Command { get; private set; }
    }
}
