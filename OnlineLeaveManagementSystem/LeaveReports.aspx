<%@ Page Title="Leave Reports" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="LeaveReports.aspx.cs" Inherits="OnlineLeaveManagementSystem.LeaveReports" %>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="stack-lg">
        <div>
            <div class="page-section-label">
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 3v18m0-6h16.5m-16.5-6h10.5m-10.5-6h16.5" />
                </svg>
                Reporting
            </div>
            <h2 class="section-title">Leave Reports</h2>
            <p class="page-copy">Filter leave activity, review trends, and export the current result set for HR and department operations.</p>
        </div>

        <div class="card">
            <div class="card-head" style="justify-content: space-between;">
                <div style="display:flex;align-items:center;gap:8px;">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 3v18m0-6h16.5m-16.5-6h10.5m-10.5-6h16.5" />
                    </svg>
                    Report Filters
                </div>
                <asp:Label ID="lblResultCount" runat="server" CssClass="status-badge status-neutral" />
            </div>
            <div class="card-body">
                <asp:Label ID="lblReportMessage" runat="server" CssClass="empty-state" Visible="false" />

                <div class="toolbar-inline">
                    <div class="form-grid" style="width:100%;">
                        <div class="form-group">
                            <label for="<%= txtSearch.ClientID %>" class="form-label">Search</label>
                            <asp:TextBox ID="txtSearch" runat="server" placeholder="Employee, username, leave type, or reason" />
                        </div>
                        <div class="form-group">
                            <label for="<%= ddlStatusFilter.ClientID %>" class="form-label">Status</label>
                            <asp:DropDownList ID="ddlStatusFilter" runat="server">
                                <asp:ListItem Text="All Statuses" Value="All" />
                                <asp:ListItem Text="Pending" Value="Pending" />
                                <asp:ListItem Text="Approved" Value="Approved" />
                                <asp:ListItem Text="Rejected" Value="Rejected" />
                            </asp:DropDownList>
                        </div>
                        <div class="form-group">
                            <label for="<%= ddlDepartmentFilter.ClientID %>" class="form-label">Department</label>
                            <asp:DropDownList ID="ddlDepartmentFilter" runat="server" />
                        </div>
                        <div class="form-group">
                            <label for="<%= ddlLeaveTypeFilter.ClientID %>" class="form-label">Leave Type</label>
                            <asp:DropDownList ID="ddlLeaveTypeFilter" runat="server" />
                        </div>
                        <div class="form-group">
                            <label for="<%= txtStartDate.ClientID %>" class="form-label">Start Date</label>
                            <asp:TextBox ID="txtStartDate" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-group">
                            <label for="<%= txtEndDate.ClientID %>" class="form-label">End Date</label>
                            <asp:TextBox ID="txtEndDate" runat="server" TextMode="Date" />
                        </div>
                    </div>
                </div>

                <div class="report-actions">
                    <asp:Button ID="btnApplyFilters" runat="server" Text="Apply Filters" CssClass="btn-secondary" OnClick="btnApplyFilters_Click" />
                    <asp:Button ID="btnExportCsv" runat="server" Text="Export CSV" CssClass="btn-secondary" OnClick="btnExportCsv_Click" CausesValidation="false" />
                    <asp:Button ID="btnExportPdf" runat="server" Text="Export PDF" CssClass="btn-secondary" OnClick="btnExportPdf_Click" CausesValidation="false" />
                    <asp:Button ID="btnExportDocx" runat="server" Text="Export DOCX" CssClass="btn-secondary" OnClick="btnExportDocx_Click" CausesValidation="false" />
                </div>
            </div>
        </div>

        <div class="dash-grid">
            <div class="dash-card">
                <div class="dash-card-top">
                    <div>
                        <div class="dashboard-summary-label">Records</div>
                        <div class="dashboard-summary-value"><asp:Label ID="lblTotalRecords" runat="server" /></div>
                    </div>
                    <span class="status-badge status-neutral">Filtered</span>
                </div>
                <div class="page-copy">Rows in the current report view.</div>
            </div>
            <div class="dash-card">
                <div class="dash-card-top">
                    <div>
                        <div class="dashboard-summary-label">Requested Days</div>
                        <div class="dashboard-summary-value"><asp:Label ID="lblRequestedDays" runat="server" /></div>
                    </div>
                    <span class="status-badge status-warning">Total</span>
                </div>
                <div class="page-copy">Summed from the current filtered result set.</div>
            </div>
            <div class="dash-card">
                <div class="dash-card-top">
                    <div>
                        <div class="dashboard-summary-label">Pending</div>
                        <div class="dashboard-summary-value"><asp:Label ID="lblPendingCount" runat="server" /></div>
                    </div>
                    <span class="status-badge status-warning">Awaiting</span>
                </div>
                <div class="page-copy">Requests still waiting for action.</div>
            </div>
            <div class="dash-card">
                <div class="dash-card-top">
                    <div>
                        <div class="dashboard-summary-label">Approved</div>
                        <div class="dashboard-summary-value"><asp:Label ID="lblApprovedCount" runat="server" /></div>
                    </div>
                    <span class="status-badge status-success">Approved</span>
                </div>
                <div class="page-copy">Approved requests in the current view.</div>
            </div>
        </div>

        <div class="card">
            <div class="card-head">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M8.25 6.75h12M8.25 12h12m-12 5.25h12M3.75 6.75h.007v.008H3.75V6.75Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0ZM3.75 12h.007v.008H3.75V12Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Zm-.375 5.25h.007v.008H3.75v-.008Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Z" />
                </svg>
                Report Data
            </div>
            <div class="card-body">
                <div class="table-wrap">
                    <table class="data-table">
                        <thead>
                            <tr>
                                <th>Employee</th>
                                <th>Department</th>
                                <th>Leave Type</th>
                                <th>Date Range</th>
                                <th>Days</th>
                                <th>Status</th>
                                <th>Submitted</th>
                                <th>Review</th>
                            </tr>
                        </thead>
                        <tbody>
                            <asp:Repeater ID="rptReportRows" runat="server">
                                <ItemTemplate>
                                    <tr>
                                        <td>
                                            <div class="report-person"><%# Eval("FullName") %></div>
                                            <div class="report-subtle"><%# Eval("Username") %></div>
                                        </td>
                                        <td><%# Eval("Department") %></td>
                                        <td><%# Eval("LeaveType") %></td>
                                        <td><%# FormatDateRange(Eval("StartDate"), Eval("EndDate")) %></td>
                                        <td><%# Eval("RequestedDays") %></td>
                                        <td><span class='<%# GetStatusBadgeCss(Eval("Status")) %>'><%# Eval("Status") %></span></td>
                                        <td><%# Eval("CreatedAt", "{0:dd MMM yyyy}") %></td>
                                        <td><%# FormatReview(Eval("ReviewedByName"), Eval("ReviewedAt"), Eval("ReviewComment")) %></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
                <asp:Label ID="lblEmptyReport" runat="server" CssClass="empty-state" Visible="false" Text="No leave records match the selected filters." />
            </div>
        </div>
    </div>
</asp:Content>
<asp:Content ID="ScriptsContent" ContentPlaceHolderID="ScriptsContent" runat="server">
</asp:Content>
