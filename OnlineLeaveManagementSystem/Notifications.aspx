<%@ Page Title="Notifications" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Notifications.aspx.cs" Inherits="OnlineLeaveManagementSystem.Notifications" %>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="stack-lg">
        <div>
            <div class="page-section-label">
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M14.857 17.082a23.848 23.848 0 0 1-5.714 0A8.967 8.967 0 0 1 6 16.139V11.25a6 6 0 1 1 12 0v4.889a8.967 8.967 0 0 1-3.143.943ZM9.75 17.25h4.5a2.25 2.25 0 0 1-4.5 0Z" />
                </svg>
                Inbox
            </div>
            <h2 class="section-title">Notifications</h2>
            <p class="page-copy">Track leave activity updates and system messages for your account.</p>
        </div>

        <div class="card">
            <div class="card-head" style="justify-content:space-between;">
                <div>My Notifications</div>
                <asp:Button ID="btnMarkAllRead" runat="server" Text="Mark All Read" CssClass="btn-secondary" OnClick="btnMarkAllRead_Click" CausesValidation="false" />
            </div>
            <div class="card-body">
                <asp:Label ID="lblNotificationMessage" runat="server" CssClass="empty-state" Visible="false" />
                <asp:Repeater ID="rptNotifications" runat="server">
                    <ItemTemplate>
                        <div class='notification-card <%# Convert.ToBoolean(Eval("IsRead")) ? string.Empty : "notification-card-unread" %>'>
                            <div class="notification-title"><%# Eval("Title") %></div>
                            <div class="notification-message"><%# Eval("Message") %></div>
                            <div class="notification-meta">
                                <span><%# Eval("CreatedAt", "{0:dd MMM yyyy HH:mm}") %></span>
                                <asp:PlaceHolder ID="phLink" runat="server" Visible='<%# !string.IsNullOrWhiteSpace(Convert.ToString(Eval("LinkUrl"))) %>'>
                                    <a href="<%# ResolveUrl(Convert.ToString(Eval("LinkUrl"))) %>">Open</a>
                                </asp:PlaceHolder>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
                <asp:Label ID="lblNotificationsEmpty" runat="server" CssClass="empty-state" Visible="false" Text="No notifications yet." />
            </div>
        </div>
    </div>
</asp:Content>
<asp:Content ID="ScriptsContent" ContentPlaceHolderID="ScriptsContent" runat="server">
</asp:Content>
