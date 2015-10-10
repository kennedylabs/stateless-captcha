using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace PblTestWeb
{
    public class CaptchaState
    {
        private static readonly Lazy<CaptchaState> _authenticated = new Lazy<CaptchaState>(
            () => new CaptchaState { IsAuthenticated = true });

        public bool IsAuthenticated { get; internal set; }

        public bool IsValidated { get; internal set; }

        public bool ShowSuccessMessage { get; internal set; }

        public bool ShowFailMessage { get; internal set; }

        public bool IsTokenExpired { get; internal set; }

        public string Identifier { get; internal set; }

        public string ImageName { get; internal set; }

        public static CaptchaState Authenticated { get { return _authenticated.Value; } }

        public string AsXml()
        {
            var element = new XElement("CaptchaState",
                new XAttribute("Authenticated", IsAuthenticated));

            if (!IsAuthenticated)
                element.Add(
                    new XAttribute("IsValidated", IsValidated),
                    new XAttribute("ShowSuccessMessage", ShowSuccessMessage),
                    new XAttribute("ShowFailMessage", ShowFailMessage),
                    new XAttribute("IsTokenExpired", IsTokenExpired));

            if (!string.IsNullOrEmpty(Identifier))
                element.Add(
                    new XAttribute("Identifier", Identifier),
                    new XAttribute("ImageName", ImageName));

            return element.ToString();
        }
    }

    public static class CaptchaManager
    {
        private const string _anonymousUploadCookieName = "CMXUPLOADAUTH";

        public static string ImageContentType
        {
            get { return CaptchaService.ImageContentType; }
        }

        public static bool CheckIsValidated(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var captchaToken = context.Request.Cookies[_anonymousUploadCookieName];
            return captchaToken != null &&
                CaptchaService.CheckToken(captchaToken.Value) == true;
        }

        public static CaptchaState ValidateOrChallenge(
            HttpContext context, string identifier, string entry)
        {
            return ValidateOrChallenge(context, identifier, entry, 0, 0);
        }

        public static CaptchaState ValidateOrChallenge(
            HttpContext context, string identifier, string entry, int width, int height)
        {
            if (context == null) throw new ArgumentNullException("context");

            var state = new CaptchaState();

            var captchaToken = context.Request.Cookies[_anonymousUploadCookieName];
            var tokenState = captchaToken != null ?
                CaptchaService.CheckToken(captchaToken.Value) : null;

            if (tokenState == false) state.IsTokenExpired = true;

            if (tokenState == true)
            {
                state.IsValidated = true;
            }
            else if (CaptchaService.CheckEntry(identifier, entry))
            {
                state.IsValidated = true;
                state.ShowSuccessMessage = true;

                var tokenCookie = new HttpCookie(_anonymousUploadCookieName,
                    CaptchaService.CreateToken(60));

                HttpContext.Current.Response.Cookies.Add(tokenCookie);
            }
            else
            {
                if (!string.IsNullOrEmpty(entry)) state.ShowFailMessage = true;

                state.Identifier = CaptchaService.CreateIdentifier();
                state.ImageName = width == 0 || height == 0 ?
                    CaptchaService.CreateImageName(state.Identifier) :
                    CaptchaService.CreateImageName(state.Identifier, width, height);
            }

            return state;
        }

        public static MemoryStream CreateImageStream(Uri requestUri)
        {
            if (requestUri == null) throw new ArgumentNullException("requestUri");

            var imageName = requestUri.AbsoluteUri.Split(
                new char[] { '?', '/' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

            return CaptchaService.GetImageFromName(imageName);
        }
    }
}
