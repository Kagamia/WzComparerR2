using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;

namespace WzComparerR2.Controls
{
    public interface IProgressDialogContext
    {
        string Message { get; set; }
        int Progress { get; set; }
        int ProgressMin { get; set; }
        int ProgressMax { get; set; }
    }

    internal class ProgressDialogContext : IProgressDialogContext
    {
        internal ProgressDialogContext(
            string text,
            string caption,
            bool closeOnComplete,
            bool closeOnError,
            Func<ProgressDialogContext, CancellationToken, Task> factory)
        {
            this.dialog = new FrmProgressDialog();
            if (caption != null)
            {
                this.dialog.Text = caption;
            }
            if (text != null)
            {
                this.dialog.Message = text;
            }
            this.cancellationTokenSource = new CancellationTokenSource();
            this.closeOnComplete = closeOnComplete;
            this.closeOnError = closeOnError;
            this.factory = factory;
        }

        public string Message
        {
            get { return this.dialog.Message; }
            set { this.dialog.Message = value; }
        }

        public int Progress
        {
            get { return this.dialog.Progress; }
            set { this.dialog.Progress = value; }
        }

        public int ProgressMin
        {
            get { return this.dialog.ProgressMin; }
            set { this.dialog.ProgressMin = value; }
        }

        public int ProgressMax
        {
            get { return this.dialog.ProgressMax; }
            set { this.dialog.ProgressMax = value; }
        }

        private readonly FrmProgressDialog dialog;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly bool closeOnComplete;
        private readonly bool closeOnError;
        private readonly Func<ProgressDialogContext, CancellationToken, Task> factory;

        private DialogResult dialogResult;

        internal DialogResult ShowDialog(IWin32Window owner)
        {
            this.dialog.Load += Dialog_Load;
            this.dialog.FormClosing += Dialog_FormClosing;
            this.dialogResult = DialogResult.None;
            dialog.ShowDialog(owner);
            return this.dialogResult;
        }

        private async void Dialog_Load(object sender, EventArgs e)
        {
            try
            {
                if (this.factory != null)
                {
                    await this.factory(this, this.cancellationTokenSource.Token);
                }
                this.dialog.FormClosing -= Dialog_FormClosing;
                this.dialogResult = DialogResult.OK;
                if (this.closeOnComplete)
                {
                    this.dialog.Close();
                }
            }
            catch
            {
                this.OnCancel();
                if (this.closeOnError)
                {
                    this.dialog.Close();
                }
            }
        }

        private void Dialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.OnCancel();
        }

        private void OnCancel()
        {
            this.dialogResult = DialogResult.Cancel;
            if (!this.cancellationTokenSource.IsCancellationRequested)
            {
                this.cancellationTokenSource.Cancel();
            }
        }
    }
}
