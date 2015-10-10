<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Index.aspx.cs"
    Inherits="StatelessCaptchaWeb.Index" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Stateless Captcha</title>
    <link rel="stylesheet" type="text/css" href="Styles.css" />
</head>
<body>
    <form id="MainForm" runat="server">
        <div id="container">
            <section>
                <h1>Stateless Captcha</h1>
                <asp:HiddenField ID="HiddenField" runat="server" />
            </section>
            <section>
                <asp:Image ID="Image" runat="server" Width="250" Height="100" />
            </section>
            <section>
                <asp:Label ID="StartLabel" runat="server"
                    Text="Enter the characters shown." />
                <asp:Label ID="FailLabel" runat="server" Visible="False" CssClass="fail"
                    Text="That was not correct..." />
                <asp:Label ID="SuccessLabel" runat="server" Visible="False" CssClass="success"
                    Text="That was correct!" />
            </section>
            <section>
                <asp:TextBox ID="TextBox" runat="server" />
            </section>
            <section>
                <asp:Button ID="SubmitButton" runat="server" Text="Submit"
                    OnClick="SubmitButton_Click" />
            </section>
        </div>
    </form>
</body>
</html>
