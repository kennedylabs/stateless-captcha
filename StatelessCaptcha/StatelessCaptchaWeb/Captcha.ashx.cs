using StatelessCaptcha;
using System.Web;

namespace StatelessCaptchaWeb
{
    public class Captcha : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "image/png";
            context.Response.BinaryWrite(StatelessCaptchaService.GetPngImageFromName(
                context.Request.QueryString.ToString()));
        }

        public bool IsReusable { get { return true; } }
    }
}
