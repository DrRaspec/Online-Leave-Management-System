<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="OnlineLeaveManagementSystem.Dashboard" %>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <asp:Label ID="lblDashboardMessage" runat="server" CssClass="error-label" Visible="false" />

    <div class="stack-lg">
        <div>
            <div class="page-section-label">
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6A2.25 2.25 0 0 1 6 3.75h2.25A2.25 2.25 0 0 1 10.5 6v2.25a2.25 2.25 0 0 1-2.25 2.25H6a2.25 2.25 0 0 1-2.25-2.25V6Z" />
                </svg>
                Overview
            </div>
            <h2 class="section-title">Leave Dashboard</h2>
            <p class="page-copy">A quick snapshot of leave activity and the latest requests.</p>
        </div>

        <div class="dash-grid">
            <!-- Total Requests -->
            <div class="dash-card">
                <div class="dash-card-top">
                    <div>
                        <div class="dashboard-summary-label">Total Requests</div>
                        <div class="dashboard-summary-value"><asp:Label ID="lblTotalRequests" runat="server" /></div>
                    </div>
                    <div class="dash-card-icon">
                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M8.25 7.5V6.108c0-1.135.845-2.098 1.976-2.192.373-.03.748-.057 1.123-.08M15.75 18H18a2.25 2.25 0 0 0 2.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 0 0-1.123-.08M15.75 18.75v-1.875a3.375 3.375 0 0 0-3.375-3.375h-1.5a1.125 1.125 0 0 1-1.125-1.125v-1.5A3.375 3.375 0 0 0 6.375 7.5H5.25m11.9-3.664A2.251 2.251 0 0 0 15 2.25h-1.5a2.251 2.251 0 0 0-2.15 1.586m5.8 0c.065.21.1.433.1.664v.75h-6V4.5c0-.231.035-.454.1-.664M6.75 7.5H4.875c-.621 0-1.125.504-1.125 1.125v12c0 .621.504 1.125 1.125 1.125h14.25c.621 0 1.125-.504 1.125-1.125V16.5a9 9 0 0 0-9-9Z" />
                        </svg>
                    </div>
                </div>
                <div class="page-copy">All leave applications stored in the system.</div>
            </div>

            <!-- Pending Requests -->
            <div class="dash-card">
                <div class="dash-card-top">
                    <div>
                        <div class="dashboard-summary-label">Pending</div>
                        <div class="dashboard-summary-value"><asp:Label ID="lblPendingRequests" runat="server" /></div>
                    </div>
                    <div class="dash-card-icon">
                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                        </svg>
                    </div>
                </div>
                <div class="page-copy">Requests waiting for admin review.</div>
            </div>

            <!-- Approved -->
            <div class="dash-card">
                <div class="dash-card-top">
                    <div>
                        <div class="dashboard-summary-label">Approved</div>
                        <div class="dashboard-summary-value"><asp:Label ID="lblApprovedRequests" runat="server" /></div>
                    </div>
                    <div class="dash-card-icon">
                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                        </svg>
                    </div>
                </div>
                <div class="page-copy">Approved leave records across the system.</div>
            </div>

            <!-- Active Users -->
            <div class="dash-card">
                <div class="dash-card-top">
                    <div>
                        <div class="dashboard-summary-label">Active Users</div>
                        <div class="dashboard-summary-value"><asp:Label ID="lblActiveUsers" runat="server" /></div>
                    </div>
                    <div class="dash-card-icon">
                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M15 19.128a9.38 9.38 0 0 0 2.625.372 9.337 9.337 0 0 0 4.121-.952 4.125 4.125 0 0 0-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 0 1 8.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0 1 11.964-3.07M12 6.375a3.375 3.375 0 1 1-6.75 0 3.375 3.375 0 0 1 6.75 0Zm8.25 2.25a2.625 2.625 0 1 1-5.25 0 2.625 2.625 0 0 1 5.25 0Z" />
                        </svg>
                    </div>
                </div>
                <div class="page-copy">Employees and admins with active access.</div>
            </div>
        </div>

        <div class="card">
            <div class="card-head">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 12h16.5m-16.5 3.75h16.5M3.75 19.5h16.5M5.625 4.5h12.75a1.875 1.875 0 0 1 0 3.75H5.625a1.875 1.875 0 0 1 0-3.75Z" />
                </svg>
                Recent Leave Requests
            </div>
            <div class="table-wrap">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Employee</th>
                            <th>Department</th>
                            <th>Leave Type</th>
                            <th>Date Range</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        <asp:Repeater ID="rptRecentRequests" runat="server">
                            <ItemTemplate>
                                <tr>
                                    <td><%# Eval("FullName") %></td>
                                    <td><%# Eval("Department") %></td>
                                    <td><%# Eval("LeaveType") %></td>
                                    <td><%# FormatDateRange(Eval("StartDate"), Eval("EndDate")) %></td>
                                    <td><span class='<%# GetStatusBadgeCss(Eval("Status")) %>'><%# Eval("Status") %></span></td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
                <asp:Label ID="lblRecentRequestsEmpty" runat="server" CssClass="empty-state" Visible="false" Text="No leave requests found yet." />
            </div>
        </div>
    </div>
</asp:Content>
<asp:Content ID="ScriptsContent" ContentPlaceHolderID="ScriptsContent" runat="server">
</asp:Content>
