using ColorAmbience.Capturing;
using System.Drawing;

namespace ColorAmbience
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var cReg = Capturer.FromWindowName(Config.Capture.CaptureName);

            while (true)
            {
                var image = cReg.Capture();
                DspCol(image.GetCenterColor(), "CCol");
                DspCol(image.GetDominantColor(), "DCol");
                DspCol(image.GetAverageColor(), "ACol"); //todo: color picking modes
                image.Dispose();

                Thread.Sleep(Config.Capture.CaptureInterval); //todo: saturate?
            }
        }

        //todo: remove cuz debug
        internal static void DspCol(Color clr, string label)
            => Console.WriteLine($"{label,-5}: \u001b[38;2;{clr.R};{clr.G};{clr.B}m\u2591\u2592\u2593\u2588\u2588\u2588 {clr.R,3} {clr.G,3} {clr.B,3} \u2588\u2588\u2588\u2593\u2592\u2591\u001b[39;49m");
    }
}