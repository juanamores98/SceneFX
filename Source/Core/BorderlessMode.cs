using System;
using System.Runtime.InteropServices;

namespace SceneFX.Core
{
    /// <summary>
    /// Borderless windowed mode for the game window, implemented with the
    /// plain Win32 window-style API. Fully reversible: the original window
    /// rect and styles are captured before the change.
    /// </summary>
    internal static class BorderlessMode
    {
        private const int GWL_STYLE = -16;
        private const long WS_CAPTION = 0x00C00000L;
        private const long WS_THICKFRAME = 0x00040000L;
        private const long WS_MINIMIZEBOX = 0x00020000L;
        private const long WS_MAXIMIZEBOX = 0x00010000L;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOZORDER = 0x0004;

        private static bool _applied;
        private static long _originalStyle;
        private static Rect _originalRect;

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern long GetWindowLong(IntPtr hWnd, int index);

        [DllImport("user32.dll")]
        private static extern long SetWindowLong(IntPtr hWnd, int index, long value);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

        internal static bool IsApplied
        {
            get { return _applied; }
        }

        internal static bool Apply()
        {
            try
            {
                IntPtr hwnd = GetActiveWindow();
                if (hwnd == IntPtr.Zero || _applied)
                {
                    return _applied;
                }

                _originalStyle = GetWindowLong(hwnd, GWL_STYLE);
                var rect = new Rect();
                GetWindowRect(hwnd, out rect);
                _originalRect = rect;

                long style = _originalStyle & ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
                SetWindowLong(hwnd, GWL_STYLE, style);

                int width = GetSystemMetrics(0);  // SM_CXSCREEN
                int height = GetSystemMetrics(1); // SM_CYSCREEN
                SetWindowPos(hwnd, IntPtr.Zero, 0, 0, width, height, SWP_FRAMECHANGED | SWP_NOZORDER);

                _applied = true;
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[SceneFX] borderless mode failed: " + e.Message);
                return false;
            }
        }

        internal static void Restore()
        {
            if (!_applied)
            {
                return;
            }

            try
            {
                IntPtr hwnd = GetActiveWindow();
                if (hwnd != IntPtr.Zero)
                {
                    SetWindowLong(hwnd, GWL_STYLE, _originalStyle);
                    int width = _originalRect.Right - _originalRect.Left;
                    int height = _originalRect.Bottom - _originalRect.Top;
                    SetWindowPos(hwnd, IntPtr.Zero, _originalRect.Left, _originalRect.Top, width, height, SWP_FRAMECHANGED | SWP_NOZORDER);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[SceneFX] borderless restore failed: " + e.Message);
            }
            finally
            {
                _applied = false;
            }
        }
    }
}
