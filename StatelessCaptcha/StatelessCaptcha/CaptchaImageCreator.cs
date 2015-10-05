using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace StatelessCaptcha
{
    internal static class CaptchaImageCreator
    {
        private static readonly Random _random = new Random();
        private static readonly FontFamily _fontFamily = new FontFamily("Times New Roman");
        private static readonly Lazy<RectangleF> _referenceHeight =
            new Lazy<RectangleF>(GetReferenceHeight);
        private static readonly Lazy<PerlinNoiseGenerator> _perlinNoiseGenerator =
            new Lazy<PerlinNoiseGenerator>(() => new PerlinNoiseGenerator());

        internal static byte[] CreatePngImage(string value, int width, int height)
        {
            using (var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            {
                var graphics = Graphics.FromImage(bitmap);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Silver);

                var noiseAmplitude = Math.Min(width, height) / 8f;
                var noiseFrequency = Math.Min(width, height) / 2f;

                using (var path = new GraphicsPath())
                {
                    using (var circlesPath = new GraphicsPath())
                    {
                        CreateCircles(circlesPath, width, height);
                        FlattenAndSegmentWithNoise(circlesPath, noiseAmplitude, noiseFrequency);
                        path.AddPath(circlesPath, false);
                    }

                    using (var textPath = new GraphicsPath())
                    {
                        CreateText(textPath, value, width, height);
                        FlattenAndSegmentWithNoise(textPath, noiseAmplitude, noiseFrequency);
                        path.AddPath(textPath, false);
                    }

                    graphics.FillPath(Brushes.DimGray, path);
                }

                using (var memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    return memoryStream.GetBuffer();
                }
            }
        }

        private static void CreateCircles(GraphicsPath path, float width, float height)
        {
            var count = _random.Next(20, 40);

            var radiusMin = Math.Min(Math.Min(width, height) / 4, Math.Max(width, height) / 16);
            var radiusMax = Math.Min(Math.Min(width, height), Math.Max(width, height) / 2);

            var xMin = -2 * radiusMax;
            var xMax = width + 2 * radiusMax;

            var yMin = -radiusMax;
            var yMax = height + radiusMax;

            var circles = new List<RectangleF>();

            for (int countIndex = 0; countIndex < count; countIndex++)
            {
                var radius = (float)(radiusMin + _random.NextDouble() * (radiusMax - radiusMin));

                var isOverlapping = true;
                for (int attempt = 0; isOverlapping && attempt < 100; attempt++)
                {
                    var x = (float)(xMin + _random.NextDouble() * (xMax - xMin));
                    var y = (float)(yMin + _random.NextDouble() * (yMax - yMin));
                    var circle = new RectangleF(x - radius, y - radius, 2 * radius, 2 * radius);

                    isOverlapping = circles.Any(c => c.IntersectsWith(circle));

                    if (!isOverlapping)
                    {
                        circles.Add(circle);
                        path.AddEllipse(circle);
                    }
                }
            }
        }

        private static void CreateText(GraphicsPath path, string text, float width, float height)
        {
            path.AddString(text, _fontFamily, 0, 1, PointF.Empty, StringFormat.GenericDefault);

            var bounds = path.GetBounds();
            var sourceRect = new RectangleF(
                bounds.X, _referenceHeight.Value.Y, bounds.Width, _referenceHeight.Value.Height);

            var margin = Math.Min(width, height) / 8;
            var targetRect = new RectangleF(
                margin, margin, width - 2 * margin, height - 2 * margin);

            var scale = targetRect.Width / targetRect.Height > sourceRect.Width / sourceRect.Height
                ? targetRect.Height / sourceRect.Height : targetRect.Width / sourceRect.Width;

            var x = targetRect.X + (targetRect.Width - scale * sourceRect.Width) / 2;
            var y = targetRect.Y + (targetRect.Height - scale * sourceRect.Height) / 2;

            var x1y1 = new PointF(x, y);
            var x2y1 = new PointF(x + scale * sourceRect.Width, y);
            var x1y2 = new PointF(x, y + scale * sourceRect.Height);

            using (var matrix = new Matrix(sourceRect, new PointF[] { x1y1, x2y1, x1y2 }))
            {
                path.Transform(matrix);
            }
        }

        private static void FlattenAndSegmentWithNoise(
            GraphicsPath path, float amplitude, float frequency)
        {
            path.Flatten();

            var sourceData = path.PathData;

            var figures = new List<List<PointF>>();
            var currentFigure = default(List<PointF>);

            var sourcePointCount = sourceData.Points.Length;
            for (int index = 0; index < sourcePointCount; index++)
            {
                var currentPoint = sourceData.Points[index];
                var currentPathType = sourceData.Types[index];
                var nextPoint = sourceData.Points[(index + 1) % sourcePointCount];
                var nextPathType = sourceData.Types[(index + 1) % sourcePointCount];

                if ((currentPathType & 0x7) == 0)
                    currentFigure = null;
                if ((nextPathType & 0x7) == 0)
                    nextPoint = currentPoint;
                if ((currentPathType & 0x80) == 0x80)
                    nextPoint = currentFigure.Count > 0 ? currentFigure[0] : currentPoint;

                if (currentFigure == null)
                {
                    currentFigure = new List<PointF>();
                    figures.Add(currentFigure);
                }

                currentFigure.AddRange(GetSegmentPoints(currentPoint, nextPoint, 5));

                if ((path.PathTypes[index] & 0x80) == 0x80)
                    currentFigure = null;
            }

            path.Reset();

            AddNoise(figures, amplitude, frequency);

            foreach (var figure in figures)
                path.AddPolygon(figure.ToArray());
        }

        private static IEnumerable<PointF> GetSegmentPoints(
            PointF point1, PointF point2, float threshold)
        {
            yield return point1;

            if (GetDistanceSquared(point1, point2) > threshold * threshold)
            {
                var count = 1 + (int)(GetDistance(point1, point2) / threshold);
                {
                    for (int index = 1; index < count; index++)
                    {
                        var t = (float)index / count;
                        var x = (1 - t) * point1.X + t * point2.X;
                        var y = (1 - t) * point1.Y + t * point2.Y;

                        yield return new PointF(x, y);
                    }
                }
            }
        }

        private static void AddNoise(List<List<PointF>> figures, float amplitude, float frequency)
        {
            var noiseGenerator = _perlinNoiseGenerator.Value;

            var x1 = _random.Next(256);
            var x2 = _random.Next(256);
            var y1 = _random.Next(256);
            var y2 = _random.Next(256);

            for (int i = 0; i < figures.Count; i++)
            {
                for (int j = 0; j < figures[i].Count; j++)
                {
                    var p = figures[i][j];

                    var x = p.X + amplitude * noiseGenerator.GetNoise(
                        p.X / frequency + x1, p.Y / frequency + y1);
                    var y = p.Y + amplitude * noiseGenerator.GetNoise(
                        p.X / frequency + x2, p.Y / frequency + y2);

                    figures[i][j] = new PointF(x, y);
                }
            }
        }

        private static RectangleF GetReferenceHeight()
        {
            using (var path = new GraphicsPath())
            {
                path.AddString("|", _fontFamily, 0, 1, PointF.Empty, StringFormat.GenericDefault);
                return path.GetBounds();
            }
        }

        private static float GetDistance(PointF p1, PointF p2)
        {
            return (float)Math.Sqrt(GetDistanceSquared(p1, p2));
        }

        private static float GetDistanceSquared(PointF p1, PointF p2)
        {
            return (p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y);
        }
    }
}
