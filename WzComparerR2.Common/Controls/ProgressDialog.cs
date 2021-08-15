using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WzComparerR2.Controls
{
    public static class ProgressDialog
    {
        public static DialogResult Show(IWin32Window owner, string text, string caption, bool closeOnComplete, bool closeOnError, Func<IProgressDialogContext, CancellationToken, Task> factory)
        {
            return new ProgressDialogContext(text, caption, closeOnComplete, closeOnError, factory).ShowDialog(owner);
        }
    }
}
