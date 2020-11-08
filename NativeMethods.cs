using System;
using System.Runtime.InteropServices;

namespace Display
{
    internal static class NativeMethods
    {
        public const int SWP_NOOWNERZORDER = 0x200;
        public const int SWP_NOREDRAW = 0x8;
        public const int SWP_NOZORDER = 0x4;
        public const int SWP_SHOWWINDOW = 0x0040;
        public const int SWP_FRAMECHANGED = 0x20;
        public const int SWP_NOACTIVATE = 0x10;
        public const int SWP_ASYNCWINDOWPOS = 0x4000;
        public const int SWP_NOMOVE = 0x2;
        public const int SWP_NOSIZE = 0x1;
        
        public const int GWL_STYLE = (-16);
        
        public const int WS_CHILD = 0x40000000;
        public const int WS_EX_MDICHILD = 0x40;
        public const int WS_VISIBLE = 0x10000000;

        public const uint WM_KEYUP = 0x0101;
        public const uint WM_KEYDOWN = 0x0100;
        public const uint WM_SETFOCUS = 0x0007;

        #region user32.dll
        [DllImport("user32.dll", SetLastError = true)]
        public static extern long SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongA", SetLastError = true)]
        public static extern int SetWindowLongA([InAttribute] IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hwnd, int x, int y, int cx, int cy, bool repaint);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, UIntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr PostMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, UIntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        #endregion
    }
}
