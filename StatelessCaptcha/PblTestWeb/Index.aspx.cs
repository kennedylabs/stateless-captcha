using System;
using System.Web;
using System.Web.UI;

namespace PblTestWeb
{
    public partial class Index : Page
    {
        const string _anonymousUploadCookieName = "CMXUPLOADAUTH";

        public bool CanUpload { get; private set; }

        public bool ShowSuccessMessage { get; private set; }

        public bool ShowFailMessage { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var captchaToken = HttpContext.Current.Request.Cookies[_anonymousUploadCookieName];

            if (captchaToken != null &&
                StatelessCaptchaService.CheckToken(captchaToken.Value) == true)
            {
                CanUpload = true;
            }
            else if (StatelessCaptchaService.CheckEntry(IdentifierField.Value, EntryTextBox.Text))
            {
                CanUpload = true;
                ShowSuccessMessage = true;
                EntryTextBox.Enabled = false;

                var tokenCookie = new HttpCookie(_anonymousUploadCookieName,
                    StatelessCaptchaService.CreateToken(60));

                HttpContext.Current.Response.Cookies.Add(tokenCookie);
            }
            else
            {
                if (!string.IsNullOrEmpty(EntryTextBox.Text)) ShowFailMessage = true;

                IdentifierField.Value = StatelessCaptchaService.CreateIdentifier();
                CaptchaImage.ImageUrl = "/Captcha.ashx?" + IdentifierField.Value;

                EntryTextBox.Enabled = true;
                EntryTextBox.Text = string.Empty;
                EntryTextBox.Focus();
            }

            EditTextBox.Enabled = CanUpload;
            SaveButton.Enabled = CanUpload;
        }
    }
}
