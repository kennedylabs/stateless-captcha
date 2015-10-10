<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Index.aspx.cs"
    Inherits="PblTestWeb.Index" %>

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
            <% if (!State.IsValidated) %>
            <% { %>
            <section>
                <input type="hidden" name="captcha-identifier" autofocus
                    value="<% Response.Write(State.Identifier); %>" />
                <img src="<% Response.Write("Captcha.ashx?" + State.ImageName); %>" />
            </section>
            <header>Enter Characters:</header>
            <% if (State.ShowFailMessage) %>
            <% { %>
            <header class="fail">That was incorrect...</header>
            <% } %>
            <section>
                <input type="text" name="captcha-entry" value="" autocomplete="off" />
                <input type="submit" value="Submit" />
            </section>
            <aside>Prove you're not a robot by entering text above.</aside>
            <% } %>
            <% else %>
            <% { %>
            <section class="spacer"></section>
            <% if (State.ShowSuccessMessage) %>
            <% { %>
            <section>
                <span class="success">You are not a robot!</span>
            </section>
            <% } %>
            <% } %>
            <footer>
                <input type="text" name="sample-entry"
                    value="<% Response.Write(HttpUtility.HtmlEncode(Request["sample-entry"])); %>"
                    <% if (!State.IsValidated) Response.Write("disabled"); %> />
                <input type="submit" value="Save"
                    <% if (!State.IsValidated) Response.Write("disabled"); %>
                    <% else Response.Write("autofocus"); %> />
            </footer>
        </div>
    </form>
</body>
</html>
