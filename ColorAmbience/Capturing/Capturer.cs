using ColorAmbience.Interop;
using System.Diagnostics;
using System.Drawing;

namespace ColorAmbience.Capturing
{
    internal class Capturer
    {
        private readonly IntPtr _winHandle;

        #region Constructor
        internal Capturer(IntPtr wHandle)
        {
            _winHandle = wHandle;
        }

        /// <summary>
        /// Generates a CaptureRegion by specifying a window name
        /// </summary>
        /// <param name="wName">Name of window</param>
        /// <param name="percent">Percentage that is recorded</param>
        /// <returns>CaptureRegion from window or full screen</returns>
        internal static Capturer FromWindowName(string? wName)
        {
            if (string.IsNullOrWhiteSpace(wName))
                return new(IntPtr.Zero);

            //if (int.TryParse(wName, out var monNum))
            //{
            //    var mons = Screen;
            //}

            var processes = Process.GetProcesses().Where(x => !string.IsNullOrWhiteSpace(x.MainWindowTitle));

            //Check for starting
            foreach (var p in processes)
                if (p.MainWindowTitle.StartsWith(wName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.PInfo($"Now observing \"{p.MainWindowTitle}\"");
                    return new(p.MainWindowHandle);
                }

            //Check for containing
            foreach (var p in processes)
                if (p.MainWindowTitle.Contains(wName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.PInfo($"Now observing \"{p.MainWindowTitle}\"");
                    return new(p.MainWindowHandle);
                }

            Logger.PInfo("Now observing Desktop");
            return new(IntPtr.Zero);
        }
        #endregion

        #region Getting Bounds
        /// <summary>
        /// Gets bounds of window or screen
        /// </summary>
        /// <returns>Bounds</returns>
        internal Rectangle GetBounds()
        {
            var bounds = _winHandle == IntPtr.Zero
                ? GetBoundsScreen()
                : GetBoundsWindow();

            var wOff = (int)(bounds.Width - bounds.Width * Config.Capture.CapturePercentage);
            var hOff = (int)(bounds.Height - bounds.Height * Config.Capture.CapturePercentage);

            return new Rectangle(
                bounds.Left + wOff / 2,
                bounds.Top + hOff / 2,
                bounds.Width - wOff,
                bounds.Height - hOff
            );
        }

        /// <summary>
        /// Gets the bounds of a window
        /// </summary>
        /// <returns>Bounds</returns>
        private Rectangle GetBoundsWindow()
        {
            var boundsRect = new Rect();
            return User32.GetWindowRect(_winHandle, ref boundsRect)
                ? new(boundsRect.Left, boundsRect.Top, boundsRect.Right - boundsRect.Left, boundsRect.Bottom - boundsRect.Top)
                : GetBoundsScreen();
        }

        /// <summary>
        /// Gets the bounds of the screen
        /// </summary>
        /// <returns>Bounds</returns>
        private static Rectangle GetBoundsScreen() //todo: allow selection of monitor
        {
            if (Config.Capture.UseVirtualScreen)
            {
                return new(
                    User32.GetSystemMetrics(SysMet.VScreenLeft),
                    User32.GetSystemMetrics(SysMet.VScreenTop),
                    User32.GetSystemMetrics(SysMet.VScreenWidth),
                    User32.GetSystemMetrics(SysMet.VScreenHeight)
                );
            }

            return new(
                0,
                0,
                User32.GetSystemMetrics(SysMet.PScreenWidth),
                User32.GetSystemMetrics(SysMet.PScreenHeight)
            );
        }
        #endregion

        #region Capturing
        internal Bitmap Capture() //todo: only works in foreground
        {
            var bounds = GetBounds();
            Console.WriteLine($"CBnds: {bounds.Left}x {bounds.Top}y {bounds.Width}w {bounds.Height}h");

            //Generating main image
            var bmp = new Bitmap(bounds.Width, bounds.Height);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);

            var newBmp = bmp.Downscale();
            bmp.Dispose();

            return newBmp;
        }
        #endregion
    }
}
