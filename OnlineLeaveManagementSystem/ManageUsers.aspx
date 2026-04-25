<%@ Page Title="Manage Users" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ManageUsers.aspx.cs" Inherits="OnlineLeaveManagementSystem.ManageUsers" %>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="stack-lg">
        <div>
            <div class="page-section-label">
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M18 18.72a9.094 9.094 0 0 0 3.742-.479 3 3 0 0 0-4.682-2.72m.94 3.198-.001.031c0 .225-.018.447-.053.664M18 18.72a8.966 8.966 0 0 1-5.054-1.189M18 18.72a8.966 8.966 0 0 0-5.054-1.189m0 0a8.966 8.966 0 0 0-5.054 1.189m5.054-1.189V14.25m0 3.281c-.225 0-.447.018-.664.053m.664-.053a8.966 8.966 0 0 0-5.054 1.189M15 6.75a3 3 0 1 1-6 0 3 3 0 0 1 6 0Zm6 3a2.25 2.25 0 1 1-4.5 0 2.25 2.25 0 0 1 4.5 0ZM7.5 9.75a2.25 2.25 0 1 1-4.5 0 2.25 2.25 0 0 1 4.5 0Z" />
                </svg>
                Administration
            </div>
            <h2 class="section-title">User Management</h2>
            <p class="page-copy">Create accounts, control access, reset passwords, and keep admins from getting locked out.</p>
        </div>

        <asp:Label ID="lblUsersMessage" runat="server" CssClass="error-label" Visible="false" />
        <asp:Label ID="lblUsersSuccess" runat="server" CssClass="success-label" Visible="false" />
        <asp:Label ID="lblAdminCoverageWarning" runat="server" CssClass="error-label" Visible="false" />

        <div class="dash-grid">
            <div class="dash-card">
                <div class="dash-card-top">
                    <div>
                        <div class="dashboard-summary-label">Total Users</div>
                        <div class="dashboard-summary-value"><asp:Label ID="lblTotalUsers" runat="server" /></div>
                    </div>
                </div>
                <div class="page-copy">All accounts in the workspace.</div>
            </div>
            <div class="dash-card">
                <div class="dash-card-top">
                    <div>
                        <div class="dashboard-summary-label">Active Users</div>
                        <div class="dashboard-summary-value"><asp:Label ID="lblActiveUsers" runat="server" /></div>
                    </div>
                </div>
                <div class="page-copy">Accounts that can currently sign in.</div>
            </div>
            <div class="dash-card">
                <div class="dash-card-top">
                    <div>
                        <div class="dashboard-summary-label">Active Admins</div>
                        <div class="dashboard-summary-value"><asp:Label ID="lblActiveAdmins" runat="server" /></div>
                    </div>
                </div>
                <div class="page-copy">Admins available to manage the system.</div>
            </div>
            <div class="dash-card">
                <div class="dash-card-top">
                    <div>
                        <div class="dashboard-summary-label">Reset Pending</div>
                        <div class="dashboard-summary-value"><asp:Label ID="lblPasswordResetsPending" runat="server" /></div>
                    </div>
                </div>
                <div class="page-copy">Accounts that must change password on next sign-in.</div>
            </div>
        </div>

        <div class="card">
            <div class="card-head">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                </svg>
                Create Account
            </div>
            <div class="card-body">
                <div class="form-grid">
                    <div class="form-group">
                        <label for="<%= txtNewUsername.ClientID %>" class="form-label">Username</label>
                        <asp:TextBox ID="txtNewUsername" runat="server" MaxLength="50" placeholder="e.g. sreyneang" />
                    </div>
                    <div class="form-group">
                        <label for="<%= txtNewFullName.ClientID %>" class="form-label">Full Name</label>
                        <asp:TextBox ID="txtNewFullName" runat="server" MaxLength="200" placeholder="Employee full name" />
                    </div>
                    <div class="form-group">
                        <label for="<%= ddlNewDepartment.ClientID %>" class="form-label">Department</label>
                        <asp:DropDownList ID="ddlNewDepartment" runat="server" />
                    </div>
                    <div class="form-group">
                        <label for="<%= ddlNewRole.ClientID %>" class="form-label">Role</label>
                        <asp:DropDownList ID="ddlNewRole" runat="server" />
                    </div>
                </div>
                <div class="form-group mt-md">
                    <label class="checkbox-inline">
                        <asp:CheckBox ID="chkNewIsActive" runat="server" Checked="true" />
                        <span>Create this account as active</span>
                    </label>
                    <div class="form-hint">New accounts receive a temporary password and must change it on first login.</div>
                </div>
                <div class="btn-row">
                    <asp:Button ID="btnCreateUser" runat="server" Text="Create User" CssClass="btn-primary" OnClick="btnCreateUser_Click" />
                </div>
            </div>
        </div>

        <div class="card">
            <div class="card-head" style="justify-content: space-between;">
                <div style="display:flex;align-items:center;gap:8px;">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
                    </svg>
                    All Accounts
                </div>
                <asp:Label ID="lblLockedUsers" runat="server" CssClass="status-badge status-warning" />
            </div>
            <div class="card-body">
                <div class="toolbar-inline">
                    <div class="form-grid" style="width:100%;">
                        <div class="form-group">
                            <label for="<%= txtUserSearch.ClientID %>" class="form-label">Search</label>
                            <asp:TextBox ID="txtUserSearch" runat="server" placeholder="Name, username, department" />
                        </div>
                        <div class="form-group">
                            <label for="<%= ddlDepartmentFilter.ClientID %>" class="form-label">Department</label>
                            <asp:DropDownList ID="ddlDepartmentFilter" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlDepartmentFilter_SelectedIndexChanged" />
                        </div>
                        <div class="form-group" style="justify-content:flex-end;">
                            <label class="form-label" style="visibility:hidden;">Apply</label>
                            <asp:Button ID="btnApplyFilters" runat="server" Text="Apply Filters" CssClass="btn-secondary" OnClick="btnApplyFilters_Click" />
                        </div>
                    </div>
                </div>

                <div class="table-wrap">
                    <table class="data-table user-admin-table">
                        <thead>
                            <tr>
                                <th>User</th>
                                <th>Department</th>
                                <th>Role</th>
                                <th>Status</th>
                                <th>Last Login</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            <asp:Repeater ID="rptUsers" runat="server" OnItemCommand="rptUsers_ItemCommand" OnItemDataBound="rptUsers_ItemDataBound">
                                <ItemTemplate>
                                    <tr>
                                        <td>
                                            <div class="user-cell">
                                                <div class="user-title"><%# Eval("Username") %></div>
                                                <div class="user-meta">
                                                    <asp:TextBox ID="txtFullName" runat="server" Text='<%# Eval("FullName") %>' MaxLength="200" />
                                                    <span>Created <%# FormatDate(Eval("CreatedAt")) %></span>
                                                </div>
                                            </div>
                                        </td>
                                        <td>
                                            <asp:DropDownList ID="ddlDepartment" runat="server" />
                                        </td>
                                        <td>
                                            <asp:DropDownList ID="ddlRole" runat="server" />
                                        </td>
                                        <td>
                                            <div class="user-status-stack">
                                                <span class='<%# GetAccountStatusCss(Eval("IsActive")) %>'><%# GetAccountStatusText(Eval("IsActive")) %></span>
                                                <span class='<%# GetPasswordStatusCss(Eval("MustChangePassword")) %>'><%# GetPasswordStatusText(Eval("MustChangePassword")) %></span>
                                                <asp:PlaceHolder ID="phLockedOut" runat="server" Visible='<%# IsLockedOut(Eval("LockoutEndUtc")) %>'>
                                                    <span class="status-badge status-warning">Locked</span>
                                                </asp:PlaceHolder>
                                            </div>
                                            <label class="checkbox-inline mt-sm">
                                                <asp:CheckBox ID="chkIsActive" runat="server" />
                                                <span>Enabled</span>
                                            </label>
                                        </td>
                                        <td>
                                            <div class="user-meta"><%# FormatLastLogin(Eval("LastLoginUtc")) %></div>
                                        </td>
                                        <td>
                                            <div class="request-actions">
                                                <asp:TextBox ID="txtResetPassword" runat="server" TextMode="Password" CssClass="password-input" MaxLength="128" placeholder="Temporary password" />
                                                <asp:Button ID="btnSaveUser" runat="server" Text="Save" CssClass="btn-secondary" CommandName="UpdateUser" CommandArgument='<%# Eval("Id") %>' />
                                                <asp:Button ID="btnResetPassword" runat="server" Text="Reset Password" CssClass="btn-primary" CommandName="ResetPassword" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" />
                                            </div>
                                        </td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                    <asp:Label ID="lblUsersEmpty" runat="server" CssClass="empty-state" Visible="false" Text="No user accounts found." />
                </div>
            </div>
        </div>
    </div>
</asp:Content>
<asp:Content ID="ScriptsContent" ContentPlaceHolderID="ScriptsContent" runat="server">
</asp:Content>
