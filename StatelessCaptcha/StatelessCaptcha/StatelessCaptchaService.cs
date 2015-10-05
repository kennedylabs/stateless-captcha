using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace StatelessCaptcha
{
    public static class StatelessCaptchaService
    {
        private static Tuple<int, int> _defaultImageSize = Tuple.Create(250, 100);
        private static string _hashSecret = "qtsrgw";
        private static readonly Random _random = new Random();
        private static readonly Regex _fullImageNameRegex =
            new Regex("^([a-z]{6})([1-9][0-9]{1,2})x([1-9][0-9]{1,2})$");
        private static readonly Regex _identifierRegex = new Regex("^([a-z]{6,8})$");
        private static readonly Lazy<SHA256Managed> _sha256 = new Lazy<SHA256Managed>();
        private static readonly ConcurrentBag<string> _usedIdentifiersBag =
            new ConcurrentBag<string>();

        public static Tuple<int, int> DefaultImageSize
        {
            get { return _defaultImageSize; }
            set { _defaultImageSize = value; }
        }

        public static string HashSecret
        {
            get { return _hashSecret; }
            set { _hashSecret = value; }
        }

        public static string CreateIdentifier()
        {
            var stringBuilder = new StringBuilder();

            for (int index = 0; index < 6; index++)
                stringBuilder.Append(Convert.ToChar(_random.Next(97, 123)));

            return stringBuilder.ToString();
        }

        public static string CreateImageName(string identifier, int width, int height)
        {
            var size = width > 0 && height > 0 ? Tuple.Create(width, height) : DefaultImageSize;
            return identifier + size.Item1 + "x" + size.Item2;
        }

        public static byte[] GetImageFromName(string imageName)
        {
            var match = _fullImageNameRegex.Match(imageName);

            if (match.Success)
                return GetImage(match.Groups[1].Value,
                    int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture.NumberFormat),
                    int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture.NumberFormat));
            else
                return GetImage(imageName);
        }

        public static byte[] GetImage(string identifier)
        {
            return GetImage(identifier, DefaultImageSize.Item1, DefaultImageSize.Item2);
        }

        public static byte[] GetImage(string identifier, int width, int height)
        {
            var value = DoHash(_identifierRegex.IsMatch(identifier) ?
                identifier : CreateIdentifier());
            return CaptchaImageCreator.CreatePngImage(value, width, height);
        }

        public static bool CheckEntry(string identifier, string entry, bool checkOverused)
        {
            return identifier != null && entry != null && identifier.Length == 6 &&
                entry.Length == 6 && !(checkOverused && CheckIsOverused(identifier)) &&
                DoHash(identifier) == entry;
        }

        private static string DoHash(string identifier)
        {
            var bytes = Encoding.UTF8.GetBytes(identifier + HashSecret);
            bytes = _sha256.Value.ComputeHash(bytes);

            var stringBuilder = new StringBuilder();

            for (int index = 0; index < 6; index++)
                stringBuilder.Append(Convert.ToChar(bytes[index] % 26 + 97));
            
            return stringBuilder.ToString();
        }

        private static bool CheckIsOverused(string identifier)
        {
            var tempString = default(string);
            while (_usedIdentifiersBag.Count >= 1000)
                _usedIdentifiersBag.TryTake(out tempString);

            _usedIdentifiersBag.Add(identifier);

            return _usedIdentifiersBag.Count(i => i == identifier) > 2;
        }
    }
}
