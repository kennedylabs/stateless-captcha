using System;
using System.Web;
using System.Web.UI;

namespace PblTestWeb
{
    public partial class Index : Page
    {
        public CaptchaState State { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            State = CaptchaManager.ValidateOrChallenge(HttpContext.Current);
        }
    }
}
