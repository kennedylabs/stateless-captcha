using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace StatelessCaptcha
{
    public static class StatelessCaptchaService
    {
        private static string _secret = "qtsrgwoe";
        private static readonly Random _random = new Random();
        private static readonly Regex _imageNameRegex =
            new Regex("^([a-z]{6,8})([0-9]{2,3})x([0-9]{2,3})$");
        private static readonly Regex _identifierRegex = new Regex("^([a-z]{6,8})$");
        private static readonly SHA256Managed _sha256 = new SHA256Managed();

        public static string Secret
        {
            get { return _secret; }
            set { _secret = value; }
        }

        public static string GetRandomIdentifier()
        {
            var stringBuilder = new StringBuilder();

            for (int index = 0; index < 8; index++)
                stringBuilder.Append(Convert.ToChar(_random.Next(97, 123)));

            return stringBuilder.ToString();
        }

        public static byte[] GetPngImageFromName(string imageName)
        {
            var match = _imageNameRegex.Match(imageName);
            return match.Success ? GetPngImage(match.Captures[0].Value,
                int.Parse(match.Captures[1].Value), int.Parse(match.Captures[2].Value)) : null;
        }

        public static byte[] GetPngImage(string identifier, int width = 200, int height = 100)
        {
            return _identifierRegex.IsMatch(identifier) ?
                CaptchaImageCreator.CreatePngImage(DoHash(identifier), width, height) : null;
        }

        public static bool CheckEntry(string combinedIndentiferAndEntry)
        {
            var identifier = combinedIndentiferAndEntry.Substring(
                0, combinedIndentiferAndEntry.Length / 2 + combinedIndentiferAndEntry.Length % 2);
            var entry = combinedIndentiferAndEntry.Substring(identifier.Length,
                combinedIndentiferAndEntry.Length - identifier.Length);

            return CheckEntry(identifier, entry);
        }
        
        public static bool CheckEntry(string identifier, string entry)
        {
            return DoHash(identifier) == entry;
        }

        private static string DoHash(string identifier)
        {
            var bytes = Encoding.UTF8.GetBytes(identifier + _secret);
            bytes = _sha256.ComputeHash(bytes);

            var stringBuilder = new StringBuilder();

            for (int index = 0; index < 8; index++)
                stringBuilder.Append(Convert.ToChar(bytes[index % bytes.Length]));
            
            return stringBuilder.ToString();
        }
    }
}
