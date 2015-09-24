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
            if (string.IsNullOrEmpty(HiddenField.Value))
                ShowStartState();
        }

        protected void SubmitButton_Click(object sender, EventArgs e)
        {
            var validationString = HiddenField.Value + TextBox.Text;

            SetVisibilities(StatelessCaptchaService.CheckEntry(validationString));
        }

        protected void TryAgainButton_Click(object sender, EventArgs e)
        {
            ShowStartState();
        }

        private void ShowStartState()
        {
            SetVisibilities(null);
            TextBox.Text = string.Empty;

            var identifier = StatelessCaptchaService.GetRandomIdentifier();
            var width = Image.Width;
            var height = Image.Height;

            Image.ImageUrl = "/Captcha.ashx?image=" + identifier + width + "x" + height;
            HiddenField.Value = identifier;
        }

        private void SetVisibilities(bool? succeeded)
        {
            StartLabel.Visible = succeeded == null;
            FailLabel.Visible = succeeded == false;
            SuccessLabel.Visible = succeeded == true;

            SubmitButton.Visible = succeeded != true;
            TryAgainButton.Visible = succeeded == true;
        }
    }
}
