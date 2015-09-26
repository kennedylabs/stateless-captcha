using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace StatelessCaptcha
{
    internal static class CaptchaImageCreator
    {
        private static readonly Random _random = new Random();
        private static readonly FontFamily _fontFamily = new FontFamily("Times New Roman");
        private static readonly Lazy<RectangleF> _referenceHeight =
            new Lazy<RectangleF>(GetReferenceHeight);

        private static RectangleF GetReferenceHeight()
        {
            using (var path = new GraphicsPath())
            {
                path.AddString("|", _fontFamily, 0, 1, PointF.Empty, StringFormat.GenericDefault);
                return path.GetBounds();
            }
        }

        internal static byte[] CreatePngImage(string value, int width, int height)
        {
            using (var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            {
                var graphics = Graphics.FromImage(bitmap);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                graphics.Clear(Color.Gainsboro);

                foreach (var path in CreateCirclePaths((float)width, (float)height))
                    graphics.FillPath(Brushes.Silver, path);

                graphics.FillPath(Brushes.Gray,
                    CreateTextPath(value, (float)width, (float)height));

                using (var memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    return memoryStream.GetBuffer();
                }
            }
        }

        private static IEnumerable<GraphicsPath> CreateCirclePaths(float width, float height)
        {
            var count = _random.Next(20, 40);

            var radiusMin = Math.Min(Math.Min(width, height) / 4, Math.Max(width, height) / 16);
            var radiusMax = Math.Min(Math.Min(width, height) / 2, Math.Max(width, height) / 4);

            var xMin = -2 * radiusMax;
            var xMax = width + 2 * radiusMax;

            var yMin = -radiusMax;
            var yMax = height + radiusMax;

            for (int index = 0; index < count; index++)
            {
                var radius = (float)(radiusMin + _random.NextDouble() * (radiusMax - radiusMin));
                var x = (float)(xMin + _random.NextDouble() * (xMax - xMin));
                var y = (float)(yMin + _random.NextDouble() * (yMax - yMin));

                var path = new GraphicsPath();
                path.AddEllipse(new RectangleF(x - radius, y - radius, 2 * radius, 2 * radius));
                FlattenAndSegment(path);

                yield return path;
            }
        }

        private static GraphicsPath CreateTextPath(string text, float width, float height)
        {
            var path = new GraphicsPath();
            path.AddString(text, _fontFamily, 0, 1, PointF.Empty, StringFormat.GenericDefault);

            var bounds = path.GetBounds();
            var sourceRect = new RectangleF(
                bounds.X, _referenceHeight.Value.Y, bounds.Width, _referenceHeight.Value.Height);

            var margin = Math.Min(width, height) / 8;
            var layoutRect = new RectangleF(
                margin, margin, width - 2 * margin, height - 2 * margin);

            path.Transform(GetScaleFitTransform(sourceRect, layoutRect));

            return path;
        }

        private static Matrix GetScaleFitTransform(RectangleF source, RectangleF target)
        {
            var scale = target.Width / target.Height > source.Width / source.Height ?
                target.Height / source.Height : target.Width / source.Width;

            var x = target.X + (target.Width - scale * source.Width) / 2;
            var y = target.Y + (target.Height - scale * source.Height) / 2;

            var x1y1 = new PointF(x, y);
            var x2y1 = new PointF(x + scale * source.Width, y);
            var x1y2 = new PointF(x, y + scale * source.Height);

            return new Matrix(source, new PointF[] { x1y1, x2y1, x1y2 });
        }

        private static void FlattenAndSegment(GraphicsPath path)
        {
            path.Flatten();

            var figures = new List<List<PointF>>();
            var currentFigure = default(List<PointF>);

            var sourcePointCount = path.PointCount;
            for (int index = 0; index < sourcePointCount; index++)
            {
                var currentPoint = path.PathPoints[index];
                var nextPoint = path.PathPoints[(index + 1) % sourcePointCount];

                if ((path.PathTypes[index] & 0x7) == 0)
                    currentFigure = null;
                if ((path.PathTypes[(index + 1) % sourcePointCount] & 0x7) == 0)
                    nextPoint = currentPoint;
                if ((path.PathTypes[index] & 0x80) == 0x80)
                    nextPoint = currentFigure.Count > 0 ? currentFigure[0] : currentPoint;

                if (currentFigure == null)
                {
                    currentFigure = new List<PointF>();
                    figures.Add(currentFigure);
                }

                currentFigure.AddRange(GetSegmentPoints(currentPoint, nextPoint));

                if ((path.PathTypes[index] & 0x80) == 0x80)
                    currentFigure = null;
            }

            path.Reset();

            foreach (var figure in figures) path.AddPolygon(figure.ToArray());
        }

        private static IEnumerable<PointF> GetSegmentPoints(PointF point1, PointF point2)
        {
            var distance = (float)Math.Sqrt(Math.Pow(point2.X - point1.X, 2) +
                Math.Pow(point2.Y - point1.Y, 2));

            yield return point1;

            var count = (int)(distance / 5);
            for (int index = 1; index <= count; index++)
            {
                var t = (float)index / (count + 1);
                var x = (1 - t) * point1.X + t * point2.X;
                var y = (1 - t) * point1.Y + t * point2.Y;

                yield return new PointF(x, y);
            }
        }
    }
}
