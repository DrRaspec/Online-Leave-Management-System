<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="OnlineLeaveManagementSystem.Login" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Sign In &mdash; LeaveHub</title>
    <link rel="icon" type="image/png" href="<%= ResolveUrl("~/Images/DR_tab_logo.png") %>" />
    <link rel="stylesheet" href="<%= ResolveUrl("~/Content/site.css?v=20260425c") %>" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />
</head>
<body class="login-body-clean">
    <form id="form1" runat="server">
        <div class="login-clean-wrap">
            <!-- Left panel -->
            <div class="login-clean-left">
                <div class="login-clean-left-inner">
                    <img src="<%= ResolveUrl("~/Images/logo.png") %>" alt="LeaveHub Logo" class="login-clean-logo" />
                    <h1 class="login-clean-heading">Online Leave<br/>Management System</h1>
                    <p class="login-clean-tagline">Submit, track, and manage leave requests &mdash; all in one place.</p>

                    <ul class="login-clean-list">
                        <li>Submit leave requests in seconds</li>
                        <li>Track approval status in real time</li>
                        <li>Approve or reject with one click</li>
                        <li>Export reports to CSV, PDF, or Word</li>
                    </ul>
                </div>
            </div>

            <!-- Right panel -->
            <div class="login-clean-right">
                <div class="login-clean-card">
                    <h2 class="login-clean-title">Welcome back</h2>
                    <p class="login-clean-sub">Sign in to your account to continue.</p>

                    <div class="login-clean-field">
                        <label for="<%= txtUsername.ClientID %>" class="login-clean-label">Username</label>
                        <asp:TextBox ID="txtUsername" runat="server" CssClass="login-clean-input" placeholder="Enter your username" />
                    </div>

                    <div class="login-clean-field">
                        <label for="<%= txtPassword.ClientID %>" class="login-clean-label">Password</label>
                        <div class="password-input-wrap">
                            <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="login-clean-input password-input" placeholder="Enter your password" />
                            <button type="button" class="password-toggle" data-password-toggle="true" data-target-id="<%= txtPassword.ClientID %>" aria-label="Show password" title="Show password">
                                <span class="password-toggle-show" aria-hidden="true">
                                    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" d="M2.036 12.322a1.012 1.012 0 0 1 0-.644C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.43 0 .644C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.964-7.178Z" />
                                        <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z" />
                                    </svg>
                                </span>
                                <span class="password-toggle-hide" aria-hidden="true">
                                    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" d="M3 3l18 18" />
                                        <path stroke-linecap="round" stroke-linejoin="round" d="M10.477 10.487A3 3 0 0 0 12 15a3 3 0 0 0 2.516-1.37" />
                                        <path stroke-linecap="round" stroke-linejoin="round" d="M9.88 5.09A10.97 10.97 0 0 1 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.43 0 .644a10.963 10.963 0 0 1-4.02 5.17M6.228 6.24C4.215 7.558 2.665 9.617 2.036 11.678a1.012 1.012 0 0 0 0 .644C3.423 16.49 7.36 19.5 12 19.5a10.98 10.98 0 0 0 5.097-1.249" />
                                    </svg>
                                </span>
                            </button>
                        </div>
                    </div>

                    <asp:Label ID="lblMessage" runat="server" CssClass="error-label" EnableViewState="false" />

                    <div class="login-clean-actions">
                        <asp:Button ID="btnLogin" runat="server" Text="Sign In" CssClass="login-clean-btn-primary" OnClick="btnLogin_Click" />
                        <asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="login-clean-btn-ghost" CausesValidation="false" OnClick="btnClear_Click" />
                    </div>
                </div>
            </div>
        </div>
    </form>
    <script src="<%= ResolveUrl("~/Scripts/site.js?v=20260425") %>"></script>
</body>
</html>
