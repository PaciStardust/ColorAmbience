using ColorAmbience.DominantColorFinder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorAmbience.Capturing
{
    internal static class BitmapExtentions
    {
        /// <summary>
        /// Grabs all the colors from an image
        /// </summary>
        internal static List<Color> GetAllColors(this Bitmap bmp)
        {
            BitmapData data = bmp.LockBits(new(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var colors = new List<Color>();

            unsafe
            {
                // example assumes 24bpp image.  You need to verify your pixel depth
                // loop by row for better data locality
                for (int y = 0; y < data.Height; ++y)
                {
                    byte* pRow = (byte*)data.Scan0 + y * data.Stride;
                    for (int x = 0; x < data.Width; ++x)
                    {
                        if (!(Config.Capture.IgnoreBlackPixels && pRow[0] == 0 && pRow[1] == 0 && pRow[2] == 0))
                            colors.Add(Color.FromArgb(pRow[2], pRow[1], pRow[0]));

                        // next pixel in the row
                        pRow += 3;
                    }
                }
            }
            bmp.UnlockBits(data);

            return colors;
        }

        /// <summary>
        /// Gets the dominant color from an image
        /// </summary>
        internal static Color GetDominantColor(this Bitmap bmp)
        {
            var colors = bmp.GetAllColors();
            return KMeansClusteringCalculator.Calculate(1, colors, Config.Capture.KMeansThreshold)[0];
        }

        /// <summary>
        /// Gets the center color from an image
        /// </summary>
        internal static Color GetCenterColor(this Bitmap bmp)
            => bmp.GetPixel(bmp.Width / 2, bmp.Height / 2);

        /// <summary>
        /// Grans the average color from an image
        /// </summary>
        internal static Color GetAverageColor(this Bitmap bmp)
        {
            BitmapData data = bmp.LockBits(new(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var colors = 0;
            var average = new long[] { 0, 0, 0 };

            unsafe
            {
                // example assumes 24bpp image.  You need to verify your pixel depth
                // loop by row for better data locality
                for (int y = 0; y < data.Height; ++y)
                {
                    byte* pRow = (byte*)data.Scan0 + y * data.Stride;
                    for (int x = 0; x < data.Width; ++x)
                    {
                        if (!(Config.Capture.IgnoreBlackPixels && pRow[0] == 0 && pRow[1] == 0 && pRow[2] == 0))
                        {
                            for (int i = 0; i < 3; i++)
                                average[i] += pRow[i];
                            colors++;
                        }

                        // next pixel in the row
                        pRow += 3;
                    }
                }
            }
            bmp.UnlockBits(data);

            if (colors == 0)
                return Color.Black;

            return Color.FromArgb((int)(average[2] / colors), (int)(average[1] / colors), (int)(average[0] / colors));
        }

        internal static Bitmap Downscale(this Bitmap bmp)
        {
            var outputSize = bmp.GetOptimalSize();
            var newBmp = new Bitmap(outputSize.Width, outputSize.Height);
            using var g2 = Graphics.FromImage(newBmp);
            g2.InterpolationMode = InterpolationMode.NearestNeighbor;

            using var wrapMode = new ImageAttributes();
            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
            g2.DrawImage(bmp, new(0, 0, outputSize.Width, outputSize.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, wrapMode);
            return newBmp;
        }

        /// <summary>
        /// Calculates the reduced image size for performance
        /// </summary>
        private static Size GetOptimalSize(this Bitmap bmp)
        {
            var difHeight = bmp.Height / (float)Config.Capture.ResolutionHeight;
            var difWidth = bmp.Width / (float)Config.Capture.ResolutionWidth;

            if (difHeight <= 1 && difWidth <= 1)
                return new(bmp.Width, bmp.Height);

            if (difHeight > difWidth)
                return new((int)(bmp.Width / difHeight), Config.Capture.ResolutionHeight);

            return new(Config.Capture.ResolutionWidth, (int)(bmp.Height / difWidth));
        }
    }
}
