using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace StatelessCaptcha
{
    internal static class CaptchaImageCreator
    {
        internal static byte[] CreatePngImage(string value, int width, int height)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            var graphics = Graphics.FromImage(bitmap);
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

            graphics.Clear(Color.Gainsboro);

            graphics.FillPath(Brushes.Gray, CreateContentPath(value, width, height));

            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Png);
                return memoryStream.GetBuffer();
            }
        }

        private static GraphicsPath CreateContentPath(string value, int width, int height)
        {
            var path = new GraphicsPath();

            path.AddPath(CreateCirclesPath(width, height), true);
            path.AddPath(CreateTextPath(value, width, height), true);

            return path;
        }

        private static GraphicsPath CreateCirclesPath(int width, int height)
        {
            var path = new GraphicsPath();

            var radius = Math.Min(width, height) * 3 / 8;
            path.AddEllipse(new Rectangle(
                width / 2 - radius, height / 2 - radius, 2 * radius, 2 * radius));

            return path;
        }

        private static GraphicsPath CreateTextPath(string value, int width, int height)
        {
            var path = new GraphicsPath();

            path.AddRectangle(new Rectangle(width / 4, height / 4, width / 2, height / 2));

            return path;
        }
    }
}
