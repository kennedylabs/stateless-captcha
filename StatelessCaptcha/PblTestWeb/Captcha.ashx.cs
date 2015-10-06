using System.Web;

namespace PblTestWeb
{
    public class Captcha : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            CaptchaManager.WriteImageToResponse(context);
        }

        public bool IsReusable { get { return true; } }
    }
}
