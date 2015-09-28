using StatelessCaptcha;
using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace StatelessCaptchaWeb
{
    public partial class Index : Page
    {
        private string _validationString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(HiddenField.Value))
                _validationString = HiddenField.Value + TextBox.Text;

            var identifier = StatelessCaptchaService.CreateRandomIdentifier();

            var imageName = identifier;
            if (Image.Width.Value > 0 && Image.Height.Value > 0)
                imageName += Image.Width.Value + "x" + Image.Height.Value;

            Image.ImageUrl = "/Captcha.ashx?" + imageName;
            HiddenField.Value = identifier;

            TextBox.Focus();
        }

        protected void SubmitButton_Click(object sender, EventArgs e)
        {
            var success = StatelessCaptchaService.CheckEntry(_validationString);

            StartLabel.Visible = false;
            FailLabel.Visible = !success;
            SuccessLabel.Visible = success;
        }
    }
}
