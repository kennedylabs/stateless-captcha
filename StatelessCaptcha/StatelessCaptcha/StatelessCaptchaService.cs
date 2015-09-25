using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace StatelessCaptcha
{
    public static class StatelessCaptchaService
    {
        private static string _secret = "qtsrgw";
        private static readonly Random _random = new Random();
        private static readonly Regex _fullImageNameRegex =
            new Regex("^([a-z]{6})([1-9][0-9]{1,2})x([1-9][0-9]{1,2})$");
        private static readonly Regex _identifierRegex = new Regex("^([a-z]{6,8})$");
        private static readonly SHA256Managed _sha256 = new SHA256Managed();
        private static readonly ConcurrentBag<string> _usedIdentifiersBag =
            new ConcurrentBag<string>();

        public static string Secret
        {
            get { return _secret; }
            set { _secret = value; }
        }

        public static string GetRandomIdentifier()
        {
            var stringBuilder = new StringBuilder();

            for (int index = 0; index < 6; index++)
                stringBuilder.Append(Convert.ToChar(_random.Next(97, 123)));

            return stringBuilder.ToString();
        }

        public static byte[] GetPngImageFromName(string imageName)
        {
            var match = _fullImageNameRegex.Match(imageName);

            if (match.Success)
                return GetPngImage(match.Groups[1].Value,
                    int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value));
            else
                return GetPngImage(imageName);
        }

        public static byte[] GetPngImage(string identifier, int width = 200, int height = 100)
        {
            var value = DoHash(_identifierRegex.IsMatch(identifier) ?
                identifier : GetRandomIdentifier());
            return CaptchaImageCreator.CreatePngImage(value, width, height);
        }

        public static bool CheckEntry(string combinedIndentiferAndEntry)
        {
            var identifier = combinedIndentiferAndEntry.Substring(
                0, combinedIndentiferAndEntry.Length / 2);
            var entry = combinedIndentiferAndEntry.Substring(
                identifier.Length, combinedIndentiferAndEntry.Length - identifier.Length);

            return CheckEntry(identifier, entry);
        }
        
        public static bool CheckEntry(string identifier, string entry)
        {
            return identifier.Length == 6 && entry.Length == 6 && !CheckIsOverused(identifier) &&
                DoHash(identifier) == entry;
        }

        private static string DoHash(string identifier)
        {
            var bytes = Encoding.UTF8.GetBytes(identifier + _secret);
            bytes = _sha256.ComputeHash(bytes);

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
