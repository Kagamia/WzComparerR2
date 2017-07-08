using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX.Win32;
using SharpDX.RawInput;
using SharpDX.Multimedia;
using Microsoft.Xna.Framework;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WzComparerR2.MapRender
{
    static class GameExt
    {
        public static void FixKeyboard(Game game)
        {
            IntPtr hWnd = game.Window.Handle;
            Device.RegisterDevice(UsagePage.Generic, UsageId.GenericKeyboard, DeviceFlags.None, hWnd, RegisterDeviceOptions.Default);
            
            if(!filterCache.ContainsKey(hWnd))
            {
                var filter = new RawInputMessageFilter();
                filterCache[hWnd] = filter;
                MessageFilterHook.AddMessageFilter(hWnd, filter);
            }
        }


        public static void ReleaseKeyboard(Game game)
        {
            if (game == null || game.Window == null)
            {
                return;
            }

            RawInputMessageFilter filter;
            IntPtr hWnd = game.Window.Handle;

            if (filterCache.TryGetValue(hWnd, out filter))
            {
                MessageFilterHook.RemoveMessageFilter(hWnd, filter);
                filterCache.Remove(hWnd);
            }
        }

        public static void RemoveKeyboardEvent(Game game)
        {
            if (game == null || game.Window == null)
            {
                return;
            }
            var fieldInfo = typeof(Device).GetField("KeyboardInput", BindingFlags.Static | BindingFlags.NonPublic);
            var value = (EventHandler<KeyboardInputEventArgs>)fieldInfo.GetValue(null);
            var methodInfo = game.Window.GetType().GetMethod("OnRawKeyEvent", BindingFlags.Instance | BindingFlags.NonPublic);
            if (value != null)
            {
                var fn = (EventHandler<KeyboardInputEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<KeyboardInputEventArgs>), game.Window, methodInfo);
                value -= fn;
                fieldInfo.SetValue(null, value);
            }
        }

        public static void RemoveMouseStateCache()
        {
            var fieldInfo = typeof(Microsoft.Xna.Framework.Input.Mouse).GetField("PrimaryWindow", BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(null, null);
            }

            fieldInfo = typeof(Microsoft.Xna.Framework.Input.Mouse).GetField("Window", BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(null, null);
            }

            fieldInfo = typeof(Microsoft.Xna.Framework.Input.Touch.TouchPanel).GetField("PrimaryWindow", BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(null, null);
            }
        }

        public static void EnsureGameExit(Game game)
        {
            var tid = GetCurrentThreadId();
            bool success = PostThreadMessage(tid, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
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

        [DllImport("kernel32")]
        static extern int GetCurrentThreadId();

        [DllImport("user32")]
        static extern bool PostThreadMessage(int tid, int msg, IntPtr wparam, IntPtr lparam);

        const int WM_QUIT = 0x12;
    }
}
