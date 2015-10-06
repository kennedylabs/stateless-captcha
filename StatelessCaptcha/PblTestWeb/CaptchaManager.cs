using System;
using System.Linq;
using System.Web;

namespace PblTestWeb
{
    public class CaptchaState
    {
        public bool IsValidated { get; internal set; }

        public bool ShowSuccessMessage { get; internal set; }

        public bool ShowFailMessage { get; internal set; }

        public string Identifier { get; internal set; }

        public string ImageName { get; internal set; }

        public bool IsTokenExpired { get; internal set; }
    }

    public static class CaptchaManager
    {
        private static string _anonymousUploadCookieName = "CMXUPLOADAUTH";
        private static string _identifierElementName = "captcha-identifier";
        private static string _entryElementName = "captch-entry";
        private static Func<string, string> _extractImageNameFunc = u => u.Split(
            new char[] { '?', '/' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

        public static string AnonymousUploadCookieName
        {
            get { return _anonymousUploadCookieName; }
            set { _anonymousUploadCookieName = value; }
        }

        public static string IdentifierElementName
        {
            get { return _identifierElementName; }
            set { _identifierElementName = value; }
        }

        public static string EntryElementName
        {
            get { return _entryElementName; }
            set { _entryElementName = value; }
        }

        public static Func<string, string> ExtractImageNameFunc
        {
            get { return _extractImageNameFunc; }
            set { _extractImageNameFunc = value; }
        }

        public static bool CheckIsValidated(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var captchaToken = context.Request.Cookies[AnonymousUploadCookieName];
            return captchaToken != null &&
                StatelessCaptchaService.CheckToken(captchaToken.Value) == true;
        }

        public static CaptchaState ValidateOrChallenge(HttpContext context)
        {
            return ValidateOrChallenge(context, 0, 0);
        }

        public static CaptchaState ValidateOrChallenge(HttpContext context, int width, int height)
        {
            if (context == null) throw new ArgumentNullException("context");

            return ValidateOrChallenge(context, context.Request[IdentifierElementName],
                context.Request[EntryElementName], width, height);
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

            var captchaToken = context.Request.Cookies[AnonymousUploadCookieName];
            var tokenState = captchaToken != null ?
                StatelessCaptchaService.CheckToken(captchaToken.Value) : null;

            if (tokenState == false) state.IsTokenExpired = true;

            if (tokenState == true)
            {
                state.IsValidated = true;
            }
            else if (StatelessCaptchaService.CheckEntry(identifier, entry))
            {
                state.IsValidated = true;
                state.ShowSuccessMessage = true;

                var tokenCookie = new HttpCookie(AnonymousUploadCookieName,
                    StatelessCaptchaService.CreateToken(60));

                HttpContext.Current.Response.Cookies.Add(tokenCookie);
            }
            else
            {
                if (!string.IsNullOrEmpty(entry)) state.ShowFailMessage = true;

                state.Identifier = StatelessCaptchaService.CreateIdentifier();
                state.ImageName = width == 0 || height == 0 ?
                    StatelessCaptchaService.CreateImageName(state.Identifier) :
                    StatelessCaptchaService.CreateImageName(state.Identifier, width, height);
            }

            return state;
        }

        public static void WriteImageToResponse(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var imageName = ExtractImageNameFunc(context.Request.Url.AbsoluteUri);

            context.Response.ContentType = "image/png";
            context.Response.BinaryWrite(StatelessCaptchaService.GetImageFromName(imageName));
        }
    }
}
