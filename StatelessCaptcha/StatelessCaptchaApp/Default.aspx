<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="StatelessCaptchaApp.Default" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Stateless Captcha</title>
</head>
<body>
    <form id="DefaultForm" runat="server">
        <div style="margin: 100px;">
            <div style="margin: 10px;">
                <h3>Stateless Captcha</h3>
                <asp:HiddenField ID="HiddenField" runat="server" />
            </div>
            <div style="margin: 10px;">
                <asp:Image ID="Image" runat="server" Width="200" Height="100" />
            </div>
            <div style="margin: 10px;">
                <asp:Label ID="StartLabel" runat="server" Text="Enter the characters shown." />
                <asp:Label ID="FailLabel" runat="server" Visible="False" Text="That is correct!" ForeColor="#009933" />
                <asp:Label ID="SuccessLabel" runat="server" Visible="False" Text="That is not correct." ForeColor="#CC0000" />
            </div>
            <div style="margin: 10px;">
                <asp:TextBox ID="TextBox" runat="server" />
            </div>
            <div style="margin: 10px;">
                <asp:Button ID="SubmitButton" runat="server" Text="Submit" OnClick="SubmitButton_Click" />
                <asp:Button ID="TryAgainButton" runat="server" Visible="False" Text="Try Again" />
            </div>
            <div style="margin: 10px;">
                <asp:Label ID="DiagnosticLabel" runat="server" />
            </div>
        </div>
    </form>
</body>
</html>
