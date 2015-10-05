using System;
using System.Web;

namespace PblTestWeb
{
    public class Captcha : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            context.Response.ContentType = "image/png";
            context.Response.BinaryWrite(StatelessCaptchaService.GetImageFromName(
                context.Request.QueryString.ToString()));
        }

        public bool IsReusable { get { return true; } }
    }
}
