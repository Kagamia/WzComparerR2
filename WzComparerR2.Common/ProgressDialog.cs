using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace WzComparerR2.Common
{
    public class ProgressDialog
    {
        private IntPtr _parentHandle;

        private Win32IProgressDialog pd = null;

        public ProgressDialog(IntPtr parentHandle)
        {
            this._parentHandle = parentHandle;
        }

        public void ShowDialog(params PROGDLG[] flags)
        {
            if (pd == null)
            {
                pd = (Win32IProgressDialog)new Win32ProgressDialog();

                pd.SetTitle(this._Title);
                pd.SetCancelMsg(this._CancelMessage, null);
                pd.SetLine(1, this._Line1, false, IntPtr.Zero);
                pd.SetLine(2, this._Line2, false, IntPtr.Zero);
                pd.SetLine(3, this._Line3, false, IntPtr.Zero);

                PROGDLG dialogFlags = PROGDLG.Normal;
                if (flags.Length != 0)
                {
                    dialogFlags = flags[0];
                    for (var i = 1; i < flags.Length; i++)
                    {
                        dialogFlags = dialogFlags | flags[i];
                    }
                }

                pd.StartProgressDialog(this._parentHandle, null, dialogFlags, IntPtr.Zero);
            }
        }

        public void CloseDialog()
        {
            if (pd != null)
            {
                pd.StopProgressDialog();
                //Marshal.ReleaseComObject(pd);
                pd = null;
            }
        }

        private string _Title = string.Empty;
        public string Title
        {
            get
            {
                return this._Title;
            }
            set
            {
                this._Title = value;
                if (pd != null)
                {
                    pd.SetTitle(this._Title);
                }
            }
        }

        private string _CancelMessage = string.Empty;
        public string CancelMessage
        {
            get
            {
                return this._CancelMessage;
            }
            set
            {
                this._CancelMessage = value;
                if (pd != null)
                {
                    pd.SetCancelMsg(this._CancelMessage, null);
                }
            }
        }

        private string _Line1 = string.Empty;
        public string Line1
        {
            get
            {
                return this._Line1;
            }
            set
            {
                this._Line1 = value;
                if (pd != null)
                {
                    pd.SetLine(1, this._Line1, false, IntPtr.Zero);
                }
            }
        }

        private string _Line2 = string.Empty;
        public string Line2
        {
            get
            {
                return this._Line2;
            }
            set
            {
                this._Line2 = value;
                if (pd != null)
                {
                    pd.SetLine(2, this._Line2, false, IntPtr.Zero);
                }
            }
        }

        private string _Line3 = string.Empty;
        public string Line3
        {
            get
            {
                return this._Line3;
            }
            set
            {
                this._Line3 = value;
                if (pd != null)
                {
                    pd.SetLine(3, this._Line3, false, IntPtr.Zero);
                }
            }
        }

        private uint _value = 0;
        public uint Value
        {
            get
            {
                return this._value;
            }
            set
            {
                this._value = value;
                if (pd != null)
                {
                    pd.SetProgress(this._value, this._maximum);
                }
            }
        }

        private uint _maximum = 100;
        public uint Maximum
        {
            get
            {
                return this._maximum;
            }
            set
            {
                this._maximum = value;
                if (pd != null)
                {
                    pd.SetProgress(this._value, this._maximum);
                }
            }
        }
        public bool HasUserCancelled
        {
            get
            {
                if (pd != null)
                {
                    return pd.HasUserCancelled();
                }
                else
                    return false;
            }
        }

        #region "Win32 Stuff"
        // The below was copied from: http://pinvoke.net/default.aspx/Interfaces/IProgressDialog.html

        public static class shlwapi
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
            static extern bool PathCompactPath(IntPtr hDC, [In, Out] StringBuilder pszPath, int dx);
        }

        [ComImport]
        [Guid("EBBC7C04-315E-11d2-B62F-006097DF5BD4")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface Win32IProgressDialog
        {
            /// <summary>
            /// Starts the progress dialog box.
            /// </summary>
            /// <param name="hwndParent">A handle to the dialog box's parent window.</param>
            /// <param name="punkEnableModless">Reserved. Set to null.</param>
            /// <param name="dwFlags">Flags that control the operation of the progress dialog box. </param>
            /// <param name="pvResevered">Reserved. Set to IntPtr.Zero</param>
            void StartProgressDialog(
                IntPtr hwndParent, //HWND
                [MarshalAs(UnmanagedType.IUnknown)]    object punkEnableModless, //IUnknown
                PROGDLG dwFlags,  //DWORD
                IntPtr pvResevered //LPCVOID
                );

            /// <summary>
            /// Stops the progress dialog box and removes it from the screen.
            /// </summary>
            void StopProgressDialog();

            /// <summary>
            /// Sets the title of the progress dialog box.
            /// </summary>
            /// <param name="pwzTitle">A pointer to a null-terminated Unicode string that contains the dialog box title.</param>
            void SetTitle(
                [MarshalAs(UnmanagedType.LPWStr)] string pwzTitle //LPCWSTR
                );

            /// <summary>
            /// Specifies an Audio-Video Interleaved (AVI) clip that runs in the dialog box. Note: Note  This method is not supported in Windows Vista or later versions.
            /// </summary>
            /// <param name="hInstAnimation">An instance handle to the module from which the AVI resource should be loaded.</param>
            /// <param name="idAnimation">An AVI resource identifier. To create this value, use the MAKEINTRESOURCE macro. The control loads the AVI resource from the module specified by hInstAnimation.</param>
            void SetAnimation(
                IntPtr hInstAnimation, //HINSTANCE
                ushort idAnimation //UINT
                );

            /// <summary>
            /// Checks whether the user has canceled the operation.
            /// </summary>
            /// <returns>TRUE if the user has cancelled the operation; otherwise, FALSE.</returns>
            /// <remarks>
            /// The system does not send a message to the application when the user clicks the Cancel button.
            /// You must periodically use this function to poll the progress dialog box object to determine
            /// whether the operation has been canceled.
            /// </remarks>
            [PreserveSig]
            [return: MarshalAs(UnmanagedType.Bool)]
            bool HasUserCancelled();

            /// <summary>
            /// Updates the progress dialog box with the current state of the operation.
            /// </summary>
            /// <param name="dwCompleted">An application-defined value that indicates what proportion of the operation has been completed at the time the method was called.</param>
            /// <param name="dwTotal">An application-defined value that specifies what value dwCompleted will have when the operation is complete.</param>
            void SetProgress(
                uint dwCompleted, //DWORD
                uint dwTotal //DWORD
                );

            /// <summary>
            /// Updates the progress dialog box with the current state of the operation.
            /// </summary>
            /// <param name="ullCompleted">An application-defined value that indicates what proportion of the operation has been completed at the time the method was called.</param>
            /// <param name="ullTotal">An application-defined value that specifies what value ullCompleted will have when the operation is complete.</param>
            void SetProgress64(
                ulong ullCompleted, //ULONGLONG
                ulong ullTotal //ULONGLONG
                );

            /// <summary>
            /// Displays a message in the progress dialog.
            /// </summary>
            /// <param name="dwLineNum">The line number on which the text is to be displayed. Currently there are three lines—1, 2, and 3. If the PROGDLG_AUTOTIME flag was included in the dwFlags parameter when IProgressDialog::StartProgressDialog was called, only lines 1 and 2 can be used. The estimated time will be displayed on line 3.</param>
            /// <param name="pwzString">A null-terminated Unicode string that contains the text.</param>
            /// <param name="fCompactPath">TRUE to have path strings compacted if they are too large to fit on a line. The paths are compacted with PathCompactPath.</param>
            /// <param name="pvResevered"> Reserved. Set to IntPtr.Zero.</param>
            /// <remarks>This function is typically used to display a message such as "Item XXX is now being processed." typically, messages are displayed on lines 1 and 2, with line 3 reserved for the estimated time.</remarks>
            void SetLine(
                uint dwLineNum, //DWORD
                [MarshalAs(UnmanagedType.LPWStr)] string pwzString, //LPCWSTR
                [MarshalAs(UnmanagedType.VariantBool)] bool fCompactPath, //BOOL
                IntPtr pvResevered //LPCVOID
                );

            /// <summary>
            /// Sets a message to be displayed if the user cancels the operation.
            /// </summary>
            /// <param name="pwzCancelMsg">A pointer to a null-terminated Unicode string that contains the message to be displayed.</param>
            /// <param name="pvResevered">Reserved. Set to NULL.</param>
            /// <remarks>Even though the user clicks Cancel, the application cannot immediately call
            /// IProgressDialog::StopProgressDialog to close the dialog box. The application must wait until the
            /// next time it calls IProgressDialog::HasUserCancelled to discover that the user has canceled the
            /// operation. Since this delay might be significant, the progress dialog box provides the user with
            /// immediate feedback by clearing text lines 1 and 2 and displaying the cancel message on line 3.
            /// The message is intended to let the user know that the delay is normal and that the progress dialog
            /// box will be closed shortly.
            /// It is typically is set to something like "Please wait while ...". </remarks>
            void SetCancelMsg(
                [MarshalAs(UnmanagedType.LPWStr)] string pwzCancelMsg, //LPCWSTR
                object pvResevered //LPCVOID
                );

            /// <summary>
            /// Resets the progress dialog box timer to zero.
            /// </summary>
            /// <param name="dwTimerAction">Flags that indicate the action to be taken by the timer.</param>
            /// <param name="pvResevered">Reserved. Set to NULL.</param>
            /// <remarks>
            /// The timer is used to estimate the remaining time. It is started when your application
            /// calls IProgressDialog::StartProgressDialog. Unless your application will start immediately,
            /// it should call Timer just before starting the operation.
            /// This practice ensures that the time estimates will be as accurate as possible. This method
            /// should not be called after the first call to IProgressDialog::SetProgress.</remarks>
            void Timer(
                PDTIMER dwTimerAction, //DWORD
                object pvResevered //LPCVOID
                );

        }

        [ComImport]
        [Guid("F8383852-FCD3-11d1-A6B9-006097DF5BD4")]
        public class Win32ProgressDialog
        {
        }

        /// <summary>
        /// Flags that indicate the action to be taken by the ProgressDialog.SetTime() method.
        /// </summary>
        public enum PDTIMER : uint //DWORD
        {
            /// <summary>Resets the timer to zero. Progress will be calculated from the time this method is called.</summary>
            Reset = (0x01),
            /// <summary>Progress has been suspended.</summary>
            Pause = (0x02),
            /// <summary>Progress has been resumed.</summary>
            Resume = (0x03)
        }

        [Flags]
        public enum PROGDLG : uint //DWORD
        {
            /// <summary>Normal progress dialog box behavior.</summary>
            Normal = 0x00000000,
            /// <summary>The progress dialog box will be modal to the window specified by hwndParent. By default, a progress dialog box is modeless.</summary>
            Modal = 0x00000001,
            /// <summary>Automatically estimate the remaining time and display the estimate on line 3. </summary>
            /// <remarks>If this flag is set, IProgressDialog::SetLine can be used only to display text on lines 1 and 2.</remarks>
            AutoTime = 0x00000002,
            /// <summary>Do not show the "time remaining" text.</summary>
            NoTime = 0x00000004,
            /// <summary>Do not display a minimize button on the dialog box's caption bar.</summary>
            NoMinimize = 0x00000008,
            /// <summary>Do not display a progress bar.</summary>
            /// <remarks>Typically, an application can quantitatively determine how much of the operation remains and periodically pass that value to IProgressDialog::SetProgress. The progress dialog box uses this information to update its progress bar. This flag is typically set when the calling application must wait for an operation to finish, but does not have any quantitative information it can use to update the dialog box.</remarks>
            NoProgressBar = 0x00000010
        }
        #endregion
    }
}