using StatelessCaptcha;
using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StatelessCaptchaWeb
{
    public partial class Index : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            HiddenField.Value = StatelessCaptchaService.CreateIdentifier();
            Image.ImageUrl = "/Captcha.ashx?" + StatelessCaptchaService.CreateImageName(
                HiddenField.Value, (int)Image.Width.Value, (int)Image.Height.Value);

            TextBox.Focus();
        }

        protected void SubmitButton_Click(object sender, EventArgs e)
        {
            var success = StatelessCaptchaService.CheckEntry(
                HiddenField.Value, TextBox.Text, true);

            StartLabel.Visible = false;
            FailLabel.Visible = !success;
            SuccessLabel.Visible = success;
        }
    }
}
