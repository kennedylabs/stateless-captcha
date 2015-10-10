using System;
using System.Web;

namespace PblTestWeb
{
    public class Captcha : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            context.Response.ContentType = CaptchaManager.ImageContentType;
            using (var imageStream = CaptchaManager.CreateImageStream(context.Request.Url))
                context.Response.BinaryWrite(imageStream.GetBuffer());
        }

        public bool IsReusable { get { return true; } }
    }
}
