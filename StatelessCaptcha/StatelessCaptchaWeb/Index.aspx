<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="StatelessCaptchaWeb.Index" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Stateless Captcha</title>
    <style>
        div {
            font-family: Arial;
            margin: 10px;
        }
        #container {
            margin: 100px;
        }
    </style>
</head>
<body>
    <form id="MainForm" runat="server">
        <div id="container">
            <div>
                <h2>Stateless Captcha</h2>
                <asp:HiddenField ID="HiddenField" runat="server" />
            </div>
            <div>
                <asp:Image ID="Image" runat="server" Width="200" Height="100" />
            </div>
            <div>
                <asp:Label ID="StartLabel" runat="server" Text="Enter the characters shown." />
                <asp:Label ID="FailLabel" runat="server" Visible="False" Text="That is not correct." ForeColor="#CC0000" />
                <asp:Label ID="SuccessLabel" runat="server" Visible="False" Text="That is correct!" ForeColor="#009933" />
            </div>
            <div>
                <asp:TextBox ID="TextBox" runat="server" />
            </div>
            <div>
                <asp:Button ID="SubmitButton" runat="server" Text="Submit" OnClick="SubmitButton_Click" />
                <asp:Button ID="TryAgainButton" runat="server" Visible="False" Text="Try Again" OnClick="TryAgainButton_Click" />
            </div>
            <div>
                <asp:Label ID="DiagnosticLabel" runat="server" />
            </div>
        </div>
    </form>
</body>
</html>
