using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PblTestWeb
{
    public static class CaptchaService
    {
        private const string _imageContentType = "image/png";
        private const int _defaultWidth = 250;
        private const int _defaultHeight = 100;
        private const string _hashSecret = "qtsrgw";
        private const string _encryptionKey = "LqdJRHxrbhUzErC5FJJ5Db5EGjsOuwMg05AIrT/r90g=";
        private const string _encryptionIV = "IfwLX0zu1PAPLuQ28mSWtA==";
        private const string _encryptionSecret = "kufach";
        private static readonly Random _random = new Random();
        private static readonly Regex _identifierRegex = new Regex("^([a-z]{6})$");
        private static readonly Regex _fullImageNameRegex =
            new Regex("^([a-z]{6})([1-9][0-9]{1,2})x([1-9][0-9]{1,2})$");
        private static readonly ConcurrentBag<string> _usedIdentifiersBag =
            new ConcurrentBag<string>();

        public static string ImageContentType { get { return _imageContentType; } }

        public static string CreateIdentifier()
        {
            return new string(Enumerable.Repeat(Convert.ToByte('a'), 6)
                .Select(a => Convert.ToChar(_random.Next(a, a + 26)))
                .ToArray());
        }

        public static string CreateImageName(string identifier)
        {
            return CreateImageName(identifier, _defaultWidth, _defaultHeight);
        }

        public static string CreateImageName(string identifier, int width, int height)
        {
            return identifier + width + "x" + height;
        }

        public static MemoryStream GetImageFromName(string imageName)
        {
            var match = _fullImageNameRegex.Match(imageName);

            if (match.Success)
                return GetImageFromIdentifier(match.Groups[1].Value,
                    int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture.NumberFormat),
                    int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture.NumberFormat));
            else
                return GetImageFromIdentifier(imageName);
        }

        public static MemoryStream GetImageFromIdentifier(string identifier)
        {
            return GetImageFromIdentifier(identifier, _defaultWidth, _defaultHeight);
        }

        public static MemoryStream GetImageFromIdentifier(string identifier, int width, int height)
        {
            var value = DoHash(_identifierRegex.IsMatch(identifier) ?
                identifier : CreateIdentifier());
            var buffer = CaptchaImageCreator.CreatePngImage(value, width, height);

            return new MemoryStream(buffer, 0, buffer.Length, false, true);
        }

        public static bool CheckEntry(string identifier, string entry)
        {
            return identifier != null && entry != null && identifier.Length == 6 &&
                entry.Length == 6 && !CheckIsOverused(identifier) && DoHash(identifier) == entry;
        }

        public static string CreateToken(int expirationMinutes)
        {
            return Encrypt(DateTime.UtcNow.AddMinutes(expirationMinutes).ToString(
                CultureInfo.InvariantCulture.DateTimeFormat));
        }

        public static bool? CheckToken(string token)
        {
            var value = Decrypt(token);

            var expirationDateTime = default(DateTime);
            var parseSuceeded = value != null && DateTime.TryParse(
                value, CultureInfo.InvariantCulture.DateTimeFormat,
                DateTimeStyles.None, out expirationDateTime);

            return !parseSuceeded ? default(bool?) : expirationDateTime > DateTime.UtcNow;
        }

        private static bool CheckIsOverused(string identifier)
        {
            var tempString = default(string);
            while (_usedIdentifiersBag.Count >= 1000)
                _usedIdentifiersBag.TryTake(out tempString);

            var isOverused = _usedIdentifiersBag.Any(i => i == identifier);

            if (!isOverused) _usedIdentifiersBag.Add(identifier);

            return isOverused;
        }

        private static string DoHash(string identifier)
        {
            var bytes = Encoding.UTF8.GetBytes(identifier + _hashSecret);

            using (var sha256Managed = new SHA256Managed())
                bytes = sha256Managed.ComputeHash(bytes);

            return new string(bytes
                .Take(6)
                .Select(b => Convert.ToChar(b % 26 + Convert.ToByte('a')))
                .ToArray());
        }

        private static string Encrypt(string input)
        {
            var text = default(string);

            using (var symetricAlgorithm = new RijndaelManaged())
            {
                var key = Convert.FromBase64String(_encryptionKey);
                var iv = Convert.FromBase64String(_encryptionIV);
                var data = Encoding.UTF8.GetBytes(_encryptionSecret + input);

                using (var encryptor = symetricAlgorithm.CreateEncryptor(key, iv))
                {
                    try
                    {
                        var buffer = encryptor.TransformFinalBlock(data, 0, data.Length);
                        text = Convert.ToBase64String(buffer);
                    }
                    catch (CryptographicException)
                    {
                        text = string.Empty;
                    }
                }
            }

            return text;
        }

        private static string Decrypt(string input)
        {
            var text = default(string);

            using (var symetricAlgorithm = new RijndaelManaged())
            {
                var key = Convert.FromBase64String(_encryptionKey);
                var iv = Convert.FromBase64String(_encryptionIV);
                var data = Convert.FromBase64String(input);

                using (var decryptor = symetricAlgorithm.CreateDecryptor(key, iv))
                {
                    try
                    {
                        var buffer = decryptor.TransformFinalBlock(data, 0, data.Length);
                        text = Encoding.UTF8.GetString(buffer);
                    }
                    catch (CryptographicException)
                    {
                        text = string.Empty;
                    }
                }
            }

            return text.StartsWith(_encryptionSecret, StringComparison.Ordinal) ?
                text.Substring(_encryptionSecret.Length) : string.Empty;
        }

        private static class CaptchaImageCreator
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
                            FlattenAndSegmentWithNoise(
                                circlesPath, noiseAmplitude, noiseFrequency);
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

                var radiusMin = Math.Min(
                    Math.Min(width, height) / 4, Math.Max(width, height) / 16);
                var radiusMax = Math.Min(
                    Math.Min(width, height), Math.Max(width, height) / 2);

                var xMin = -2 * radiusMax;
                var xMax = width + 2 * radiusMax;

                var yMin = -radiusMax;
                var yMax = height + radiusMax;

                var circles = new List<RectangleF>();

                for (int countIndex = 0; countIndex < count; countIndex++)
                {
                    var radius = (float)(radiusMin +
                        _random.NextDouble() * (radiusMax - radiusMin));

                    for (int attempt = 0; attempt < 100; attempt++)
                    {
                        var x = (float)(xMin + _random.NextDouble() * (xMax - xMin));
                        var y = (float)(yMin + _random.NextDouble() * (yMax - yMin));
                        var circle = new RectangleF(
                            x - radius, y - radius, 2 * radius, 2 * radius);

                        var isOverlap = circles.Any(c => c.IntersectsWith(circle));

                        if (!isOverlap)
                        {
                            circles.Add(circle);
                            path.AddEllipse(circle);
                            break;
                        }
                    }
                }
            }

            private static void CreateText(
                GraphicsPath path, string text, float width, float height)
            {
                path.AddString(text, _fontFamily, 0, 1, PointF.Empty, StringFormat.GenericDefault);

                var bounds = path.GetBounds();
                var sourceRect = new RectangleF(bounds.X, _referenceHeight.Value.Y,
                    bounds.Width, _referenceHeight.Value.Height);

                var margin = Math.Min(width, height) / 8;
                var targetRect = new RectangleF(
                    margin, margin, width - 2 * margin, height - 2 * margin);

                var scale = targetRect.Width / targetRect.Height >
                    sourceRect.Width / sourceRect.Height ? targetRect.Height / sourceRect.Height :
                    targetRect.Width / sourceRect.Width;

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

            private static void AddNoise(
                List<List<PointF>> figures, float amplitude, float frequency)
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
                    path.AddString("|", _fontFamily, 0, 1, PointF.Empty,
                        StringFormat.GenericDefault);
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

            private class PerlinNoiseGenerator
            {
                private readonly Random _random = new Random();
                private readonly Tuple<float, float>[] _gradients = new Tuple<float, float>[514];
                private readonly int[] _permutations = new int[514];

                internal PerlinNoiseGenerator()
                {
                    for (int i = 0; i < 514; i++)
                    {
                        var x = 2 * (float)_random.NextDouble() - 1;
                        var y = 2 * (float)_random.NextDouble() - 1;

                        var magnitude = (float)Math.Sqrt(x * x + y * y);
                        _gradients[i] = Tuple.Create(x / magnitude, y / magnitude);
                    }

                    for (int i = 0; i < 256; i++)
                        _permutations[i] = i;

                    for (int i = 0; i < 256; i++)
                    {
                        var j = _random.Next(256);
                        var k = _permutations[i];

                        _permutations[i] = _permutations[j];
                        _permutations[j] = k;
                    }

                    for (int i = 0; i < 258; i++)
                        _permutations[i + 256] = _permutations[i];
                }

                internal float GetNoise(float x, float y)
                {
                    while (x < 0) x += 256;
                    var bx0 = (int)x % 256;
                    var bx1 = (bx0 + 1) % 256;
                    var rx0 = x - (int)x;
                    var rx1 = rx0 - 1;

                    while (y < 0) y += 256;
                    var by0 = (int)y % 256;
                    var by1 = (by0 + 1) % 256;
                    var ry0 = y - (int)y;
                    var ry1 = ry0 - 1;
                    var i = _permutations[bx0];
                    var j = _permutations[bx1];
                    var b00 = _permutations[i + by0];
                    var b10 = _permutations[j + by0];
                    var b01 = _permutations[i + by1];
                    var b11 = _permutations[j + by1];
                    var sx = rx0 * rx0 * (3 - 2 * rx0);
                    var sy = ry0 * ry0 * (3 - 2 * ry0);
                    var s = _gradients[b00].Item1 * rx0 + _gradients[b00].Item2 * ry0;
                    var t = _gradients[b10].Item1 * rx1 + _gradients[b10].Item2 * ry0;
                    var a = s + sx * (t - s);
                    var u = _gradients[b01].Item1 * rx0 + _gradients[b01].Item2 * ry1;
                    var v = _gradients[b11].Item1 * rx1 + _gradients[b11].Item2 * ry1;
                    var b = u + sx * (v - u);

                    return a + sy * (b - a);
                }
            }
        }
    }
}
