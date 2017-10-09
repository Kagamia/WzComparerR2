using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WzComparerR2.Network
{
    static class NativeMethods
    {
        public static bool FlashWindowEx(Form form)
        {
            FLASHWINFO fInfo = new FLASHWINFO();

            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = form.Handle;
            fInfo.dwFlags = FlashType.FLASHW_TIMERNOFG | FlashType.FLASHW_ALL;
            fInfo.uCount = 3;
            fInfo.dwTimeout = 0;

            return FlashWindowEx(ref fInfo);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public FlashType dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

        public enum FlashType : uint
        {
            /// <summary>
                /// Stop flashing. The system restores the window to its original state. 
                /// </summary>    
            FLASHW_STOP = 0,

            /// <summary>
                /// Flash the window caption 
                /// </summary>
            FLASHW_CAPTION = 1,

            /// <summary>
                /// Flash the taskbar button. 
                /// </summary>
            FLASHW_TRAY = 2,

            /// <summary>
                /// Flash both the window caption and taskbar button.
                /// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags. 
                /// </summary>
            FLASHW_ALL = 3,

            FLASHW_PARAM1 = 4,
            FLASHW_PARAM2 = 12,
            FLASHW_TIMER = FLASHW_TRAY | FLASHW_PARAM1,
            FLASHW_TIMERNOFG = FLASHW_TRAY | FLASHW_PARAM2
        }
    }
}
