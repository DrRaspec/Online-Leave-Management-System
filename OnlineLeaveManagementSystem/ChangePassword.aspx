<%@ Page Title="Change Password" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ChangePassword.aspx.cs" Inherits="OnlineLeaveManagementSystem.ChangePassword" %>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="stack-lg password-page">
        <div>
            <div class="page-section-label">
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 1 0-9 0v3.75m-1.5 0h12A2.25 2.25 0 0 1 20.25 12.75v6A2.25 2.25 0 0 1 18 21H6a2.25 2.25 0 0 1-2.25-2.25v-6A2.25 2.25 0 0 1 6 10.5Z" />
                </svg>
                Security
            </div>
            <h2 class="section-title">Update Your Password</h2>
            <p class="page-copy">Use a unique password that protects your account and keeps your leave history, approvals, and profile access secure.</p>
        </div>

        <div class="password-layout">
            <div class="card password-card">
                <div class="card-head">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 1 0-9 0v3.75m-1.5 0h12A2.25 2.25 0 0 1 20.25 12.75v6A2.25 2.25 0 0 1 18 21H6a2.25 2.25 0 0 1-2.25-2.25v-6A2.25 2.25 0 0 1 6 10.5Z" />
                    </svg>
                    Account Password
                </div>
                <div class="card-body">
                    <asp:Label ID="lblIntroMessage" runat="server" CssClass="password-banner" Visible="false" />
                    <asp:Label ID="lblPasswordMessage" runat="server" CssClass="error-label" Visible="false" />
                    <asp:Label ID="lblPasswordSuccess" runat="server" CssClass="success-label" Visible="false" />

                    <div class="password-account-meta">
                        <div class="password-meta-chip">
                            <span class="password-meta-label">Account</span>
                            <span class="password-meta-value"><%= CurrentUser == null ? string.Empty : Server.HtmlEncode(CurrentUser.Username) %></span>
                        </div>
                        <div class="password-meta-chip">
                            <span class="password-meta-label">Role</span>
                            <span class="password-meta-value"><%= CurrentUser == null ? string.Empty : Server.HtmlEncode(CurrentUser.Role) %></span>
                        </div>
                    </div>

                    <div class="form-group">
                        <label for="<%= txtCurrentPassword.ClientID %>" class="form-label">Current Password</label>
                        <div class="password-input-wrap">
                            <asp:TextBox ID="txtCurrentPassword" runat="server" TextMode="Password" CssClass="password-input" placeholder="Enter your current or temporary password" />
                            <button type="button" class="password-toggle" data-password-toggle="true" data-target-id="<%= txtCurrentPassword.ClientID %>" aria-label="Show current password" title="Show current password">
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

                    <div class="form-group mt-md">
                        <label for="<%= txtNewPassword.ClientID %>" class="form-label">New Password</label>
                        <div class="password-input-wrap">
                            <asp:TextBox ID="txtNewPassword" runat="server" TextMode="Password" CssClass="password-input" placeholder="Use at least 12 characters" />
                            <button type="button" class="password-toggle" data-password-toggle="true" data-target-id="<%= txtNewPassword.ClientID %>" aria-label="Show new password" title="Show new password">
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

                    <div class="form-group mt-md">
                        <label for="<%= txtConfirmPassword.ClientID %>" class="form-label">Confirm New Password</label>
                        <div class="password-input-wrap">
                            <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password" CssClass="password-input" placeholder="Re-enter the new password" />
                            <button type="button" class="password-toggle" data-password-toggle="true" data-target-id="<%= txtConfirmPassword.ClientID %>" aria-label="Show confirm password" title="Show confirm password">
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
                        <div class="form-hint">Use uppercase, lowercase, a number, and a symbol.</div>
                    </div>

                    <div class="btn-row password-actions">
                        <asp:Button ID="btnUpdatePassword" runat="server" Text="Update Password" CssClass="btn-primary" OnClick="btnUpdatePassword_Click" />
                        <asp:HyperLink ID="lnkCancelPasswordChange" runat="server" NavigateUrl="~/Dashboard.aspx" CssClass="btn-secondary">Cancel</asp:HyperLink>
                    </div>
                </div>
            </div>

            <div class="card password-help-card">
                <div class="card-head">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M11.25 9.75h1.5v4.5h-1.5m.75 3h.008v.008H12V17.25Zm9-5.25a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                    </svg>
                    Password Rules
                </div>
                <div class="card-body">
                    <div class="password-rule-list">
                        <div class="password-rule-item">
                            <span class="password-rule-icon">1</span>
                            <div>
                                <div class="password-rule-title">Minimum length</div>
                                <div class="page-copy">Use at least 12 characters.</div>
                            </div>
                        </div>
                        <div class="password-rule-item">
                            <span class="password-rule-icon">2</span>
                            <div>
                                <div class="password-rule-title">Mix character types</div>
                                <div class="page-copy">Include uppercase, lowercase, a number, and a symbol.</div>
                            </div>
                        </div>
                        <div class="password-rule-item">
                            <span class="password-rule-icon">3</span>
                            <div>
                                <div class="password-rule-title">Choose something new</div>
                                <div class="page-copy">Do not reuse your current or temporary password.</div>
                            </div>
                        </div>
                        <div class="password-rule-item">
                            <span class="password-rule-icon">4</span>
                            <div>
                                <div class="password-rule-title">Keep it private</div>
                                <div class="page-copy">Avoid sharing admin or employee passwords between users.</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
<asp:Content ID="ScriptsContent" ContentPlaceHolderID="ScriptsContent" runat="server">
</asp:Content>
