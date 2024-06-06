using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ColorAmbience.Interop
{
    internal static class User32
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hWnd, ref Rect rect);

        [DllImport("user32.dll")]
        internal static extern int GetSystemMetrics(SysMet metric);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    internal enum SysMet
    {
        PScreenWidth = 0,
        PScreenHeight = 1,

        VScreenLeft = 76,
        VScreenTop = 77,
        VScreenWidth = 78,
        VScreenHeight = 79
    }
}
