<%@ Page Title="My Leaves" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MyLeaves.aspx.cs" Inherits="OnlineLeaveManagementSystem.MyLeaves" %>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="stack-lg">
        <div>
            <div class="page-section-label">
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z" />
                </svg>
                Leave History
            </div>
            <h2 class="section-title">My Leave Requests</h2>
            <p class="page-copy">Review all your submitted leave requests and filter by status.</p>
        </div>

        <div class="card">
            <div class="card-head">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M12 6v12m6-6H6" />
                </svg>
                My Balances
            </div>
            <div class="card-body">
                <div class="dash-grid">
                    <asp:Repeater ID="rptBalances" runat="server">
                        <ItemTemplate>
                            <div class="dash-card leave-balance-card">
                                <div class="dash-card-top">
                                    <div>
                                        <div class="dashboard-summary-label"><%# Eval("Name") %></div>
                                        <div class="dashboard-summary-value" style="font-size:24px;"><%# Eval("RemainingDays", "{0:0.#}") %></div>
                                    </div>
                                    <span class="status-badge status-neutral">Remaining</span>
                                </div>
                                <div class="page-copy">Used <%# FormatBalance(Eval("UsedDays")) %> of <%# FormatBalance(Eval("BalanceDays")) %></div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
                <asp:Label ID="lblBalancesEmpty" runat="server" CssClass="empty-state" Visible="false" Text="No balances available yet." />
            </div>
        </div>

        <div class="card">
            <div class="card-head" style="justify-content: space-between;">
                <div style="display:flex;align-items:center;gap:8px;">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M8.25 6.75h12M8.25 12h12m-12 5.25h12M3.75 6.75h.007v.008H3.75V6.75Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0ZM3.75 12h.007v.008H3.75V12Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Zm-.375 5.25h.007v.008H3.75v-.008Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Z" />
                    </svg>
                    Request List
                </div>
                <div class="leaves-filter" style="margin:0;">
                    <div class="btn-row" style="margin:0;align-items:center;">
                        <asp:DropDownList ID="ddlStatusFilter" runat="server" AutoPostBack="true" CssClass="w-full" OnSelectedIndexChanged="ddlStatusFilter_SelectedIndexChanged" style="min-height:34px;font-size:12.5px;">
                            <asp:ListItem Text="All Status" Value="All" />
                            <asp:ListItem Text="Pending" Value="Pending" />
                            <asp:ListItem Text="Approved" Value="Approved" />
                            <asp:ListItem Text="Rejected" Value="Rejected" />
                        </asp:DropDownList>
                        <asp:Button ID="btnResetFilters" runat="server" Text="Reset" CssClass="btn-secondary" OnClick="btnResetFilters_Click" CausesValidation="false" />
                    </div>
                </div>
            </div>
            <div class="card-body">
                <asp:Label ID="lblLeavesMessage" runat="server" CssClass="empty-state" Visible="false" />

                <asp:Repeater ID="rptLeaves" runat="server">
                    <ItemTemplate>
                        <div class="leave-card">
                            <div class="leave-card-main">
                                <div>
                                    <div class="leave-type"><%# Eval("LeaveType") %></div>
                                    <div class="leave-meta">
                                        <svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" style="display:inline;vertical-align:-2px;margin-right:4px;">
                                            <path stroke-linecap="round" stroke-linejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0v-7.5A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5" />
                                        </svg>
                                        <%# FormatDateRange(Eval("StartDate"), Eval("EndDate")) %>
                                    </div>
                                    <div class="leave-meta">Submitted: <%# Eval("CreatedAt", "{0:dd MMM yyyy}") %></div>
                                    <div class="leave-meta">Duration: <%# Eval("RequestedDays") %> day(s)</div>
                                    <div class="leave-meta">Review: <%# FormatReview(Eval("ReviewedByName"), Eval("ReviewedAt"), Eval("ReviewComment")) %></div>
                                    <asp:PlaceHolder ID="phAttachment" runat="server" Visible='<%# !string.IsNullOrWhiteSpace(Convert.ToString(Eval("AttachmentPath"))) %>'>
                                        <div class="leave-meta">
                                            <svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" style="display:inline;vertical-align:-2px;margin-right:4px;">
                                                <path stroke-linecap="round" stroke-linejoin="round" d="m18.375 12.739-7.693 7.693a4.5 4.5 0 0 1-6.364-6.364l10.94-10.94A3 3 0 1 1 19.5 7.372L8.552 18.32m.009-.01-.01.01m5.699-9.941-7.81 7.81a1.5 1.5 0 0 0 2.112 2.13" />
                                            </svg>
                                            <a href="<%# ResolveUrl(Convert.ToString(Eval("AttachmentPath"))) %>" target="_blank" rel="noopener">View attachment</a>
                                        </div>
                                    </asp:PlaceHolder>
                                </div>
                                <span class='<%# GetStatusBadgeCss(Eval("Status")) %>'><%# Eval("Status") %></span>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </div>
</asp:Content>
<asp:Content ID="ScriptsContent" ContentPlaceHolderID="ScriptsContent" runat="server">
</asp:Content>
