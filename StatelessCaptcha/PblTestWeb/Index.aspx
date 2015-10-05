<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="PblTestWeb.Index" %>

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
            </section>
            <% if (!CanUpload) %>
            <% { %>
            <section>
                <asp:HiddenField ID="IdentifierField" runat="server" />
                <asp:Image ID="CaptchaImage" runat="server" />
            </section>
            <header>Enter Characters:</header>
            <% if (ShowFailMessage) %>
            <% { %>
            <header class="fail">That was incorrect...</header>
            <% } %>
            <section>
                <asp:TextBox ID="EntryTextBox" runat="server" />
                <asp:Button ID="SubmitButton" runat="server" Text="Submit" />
            </section>
            <aside>Prove you're not a robot by entering text above.</aside>
            <% } %>
            <% else %>
            <% { %>
            <section class="spacer"></section>
            <% if (ShowSuccessMessage) %>
            <% { %>
            <section>
                <asp:TextBox ID="SuccessTextBox" runat="server" CssClass="success" ReadOnly="true" Text="You are not a robot!" />
            </section>
            <% } %>
            <% } %>
            <footer>
            <% EditTextBox.Enabled = CanUpload; %>
            <% SaveButton.Enabled = CanUpload; %>
                <asp:TextBox ID="EditTextBox" runat="server" Text="Foo" />
                <asp:Button ID="SaveButton" runat="server" Text="Save" />
            </footer>
        </div>
    </form>
</body>
</html>
