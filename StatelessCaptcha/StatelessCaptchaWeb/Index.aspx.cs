using StatelessCaptcha;
using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StatelessCaptchaWeb
{
    public partial class Index : Page
    {
        private string _identifier;
        private string _entry;

        protected void Page_Load(object sender, EventArgs e)
        {
            _identifier = HiddenField.Value;
            _entry = TextBox.Text;

            HiddenField.Value = StatelessCaptchaService.CreateIdentifier();
            Image.ImageUrl = "/Captcha.ashx?" + HiddenField.Value;

            TextBox.Text = string.Empty;
            TextBox.Focus();
        }

        protected void SubmitButton_Click(object sender, EventArgs e)
        {
            var success = StatelessCaptchaService.CheckEntry(_identifier, _entry, true);

            StartLabel.Visible = false;
            FailLabel.Visible = !success;
            SuccessLabel.Visible = success;
        }
    }
}
