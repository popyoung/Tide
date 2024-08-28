using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.ApplicationServices;
using System.Windows.Input;
using Vanara.PInvoke;
using System.Diagnostics;

namespace 潮汐2
{
    public class GlobalInputMonitor
    {
        private readonly User32.HookProc keyboardHookProc, mouseHookProc;
        private readonly User32.SafeHHOOK keyboardHookId, mouseHookId;

        public event Action<KeyMsgReceivedEventArgs>? KeyMsgReceived;
        public event Action<MouseMsgReceivedEventArgs>? MouseMsgReceived;

        public class MouseMsgReceivedEventArgs(User32.WindowMessage msg, int x, int y) : EventArgs
        {
            public User32.WindowMessage Msg { get; set; } = msg;
            public int X { get; set; } = x;
            public int Y { get; set; } = y;
        }
        public class KeyMsgReceivedEventArgs(User32.WindowMessage msg, Key key) : EventArgs
        {
            public User32.WindowMessage Msg { get; set; } = msg;
            public Key Key { get; set; } = key;
        }
        public GlobalInputMonitor()
        {
            keyboardHookProc = KeyboardHookProc;
            mouseHookProc = MouseHookProc;

            //测试时注释
            keyboardHookId = User32.SetWindowsHookEx(User32.HookType.WH_KEYBOARD_LL, keyboardHookProc);
            mouseHookId = User32.SetWindowsHookEx(User32.HookType.WH_MOUSE_LL, mouseHookProc);
        }

        ~GlobalInputMonitor()
        {
            User32.UnhookWindowsHookEx(keyboardHookId);
            User32.UnhookWindowsHookEx(mouseHookId);
        }

        private IntPtr KeyboardHookProc(int nCode, nint wParam, nint lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                User32.WindowMessage op = (User32.WindowMessage)wParam;
                //Trace.WriteLine($"{"MouseHookProc"} {nCode} {op} {(User32.VK)vkCode}");
                KeyMsgReceived?.Invoke(new KeyMsgReceivedEventArgs(op, key));
            }
            return User32.CallNextHookEx(keyboardHookId, nCode, wParam, lParam);
        }

        public int MouseMinMovement = 0;
        private int lastX, lastY;
        private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                User32.WindowMessage op = (User32.WindowMessage)wParam;
                int x = Marshal.ReadInt32(lParam);
                int y = Marshal.ReadInt32(lParam + 4);
                if (op != User32.WindowMessage.WM_MOUSEMOVE || (MouseMinMovement <= 0 || (Math.Abs(x - lastX) + Math.Abs(y - lastY)) > MouseMinMovement))
                {
                    //Trace.WriteLine("OK: " + (x - lastX) + "  " + (y - lastY) + "  " + (Math.Abs(x - lastX) + Math.Abs(y - lastY)));
                    lastX = x;
                    lastY = y;
                    MouseMsgReceived?.Invoke(new MouseMsgReceivedEventArgs(op, x, y));
                }

                //Trace.WriteLine($"{"MouseHookProc"} {DateTime.Now} {op} {(x - lastX)} {y - lastY}");
            }
            return User32.CallNextHookEx(mouseHookId, nCode, wParam, lParam);
        }
    }

}
