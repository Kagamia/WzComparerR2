using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX.Win32;
using SharpDX.RawInput;
using SharpDX.Multimedia;
using Microsoft.Xna.Framework;
using System.Windows.Forms;

namespace WzComparerR2.MapRender
{
    static class GameExt
    {
        public static void FixKeyboard(Game game)
        {
            IntPtr hWnd = game.Window.Handle;
            Device.RegisterDevice(UsagePage.Generic, UsageId.GenericKeyboard, DeviceFlags.None, hWnd, RegisterDeviceOptions.Default);
            var filter = new RawInputMessageFilter();
            filterCache[hWnd] = filter;
            MessageFilterHook.AddMessageFilter(hWnd, filter);
        }


        public static void ReleaseKeyboard(Game game)
        {
            RawInputMessageFilter filter;
            IntPtr hWnd = game.Window.Handle;
            if (filterCache.TryGetValue(hWnd, out filter))
            {
                MessageFilterHook.RemoveMessageFilter(hWnd, filter);
                filterCache.Remove(hWnd);
            }
        }

        private static Dictionary<IntPtr, RawInputMessageFilter> filterCache = new Dictionary<IntPtr, RawInputMessageFilter>();

        class RawInputMessageFilter : IMessageFilter
        {
            public virtual bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == 0xff)
                    Device.HandleMessage(m.LParam);
                return false;
            }
        }
    }
}
