<%@ Page Title="Apply Leave" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ApplyLeave.aspx.cs" Inherits="OnlineLeaveManagementSystem.ApplyLeave" %>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="stack-lg">
        <div>
            <div class="page-section-label">
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v6m3-3H9m12 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                </svg>
                New Request
            </div>
            <h2 class="section-title">Apply for Leave</h2>
            <p class="page-copy">Complete the form below to submit a new leave request for review.</p>
        </div>

        <asp:Label ID="lblSuccessMessage" runat="server" CssClass="error-label" Visible="false" />

        <div class="card">
            <div class="card-head">Leave Policy</div>
            <div class="card-body">
                <div class="page-copy"><asp:Label ID="lblPolicyTitle" runat="server" CssClass="report-person" /></div>
                <div class="page-copy" style="margin-top:8px;"><asp:Label ID="lblPolicyText" runat="server" /></div>
            </div>
        </div>

        <div class="card">
            <div class="card-head">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M12 6v12m6-6H6" />
                </svg>
                Current Leave Balances
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
                                    <span class='<%# Convert.ToBoolean(Eval("RequiresAttachment")) ? "status-badge status-warning" : "status-badge status-neutral" %>'>
                                        <%# Convert.ToBoolean(Eval("RequiresAttachment")) ? "Attachment" : "Standard" %>
                                    </span>
                                </div>
                                <div class="page-copy">
                                    Used <%# FormatBalance(Eval("UsedDays")) %> of <%# FormatBalance(Eval("BalanceDays")) %>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
                <asp:Label ID="lblBalancesEmpty" runat="server" CssClass="empty-state" Visible="false" Text="No leave balances are configured yet." />
            </div>
        </div>

        <div class="card">
            <div class="card-head">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m3.75 9v6m3-3H9m1.5-12H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z" />
                </svg>
                Leave Details
            </div>
            <div class="card-body">
                <div class="form-grid">
                    <div class="form-group">
                        <label for="<%= ddlLeaveType.ClientID %>" class="form-label">Leave Type</label>
                        <asp:DropDownList ID="ddlLeaveType" runat="server" CssClass="w-full" />
                    </div>

                    <div class="form-group">
                        <label for="<%= fileAttachment.ClientID %>" class="form-label">Attachment (Optional)</label>
                        <asp:FileUpload ID="fileAttachment" runat="server" CssClass="w-full" />
                        <div class="form-hint">Accepted: PDF, DOC, DOCX, PNG, JPG. Max 5 MB.</div>
                    </div>

                    <div class="form-group">
                        <label for="<%= txtStartDate.ClientID %>" class="form-label">Start Date</label>
                        <asp:TextBox ID="txtStartDate" runat="server" TextMode="Date" CssClass="w-full" />
                    </div>

                    <div class="form-group">
                        <label for="<%= txtEndDate.ClientID %>" class="form-label">End Date</label>
                        <asp:TextBox ID="txtEndDate" runat="server" TextMode="Date" CssClass="w-full" />
                    </div>
                </div>

                <div class="form-group" style="margin-top: 20px;">
                    <label for="<%= txtReason.ClientID %>" class="form-label">Reason</label>
                    <asp:TextBox ID="txtReason" runat="server" TextMode="MultiLine" Rows="5" CssClass="w-full" placeholder="Provide the reason for your leave request" />
                </div>

                <div class="form-hint" style="margin-top:16px;">
                    <asp:Label ID="lblHolidayCalculationHint" runat="server" />
                </div>
                <div class="form-hint" style="margin-top:6px;">
                    Backdated requests are allowed for up to 30 days and must stay within the current leave year.
                </div>

                <div class="btn-row" style="margin-top: 20px;">
                    <asp:Button ID="btnSubmit" runat="server" Text="Submit Request" CssClass="btn-primary" OnClick="btnSubmit_Click" />
                </div>
            </div>
        </div>
    </div>
</asp:Content>
<asp:Content ID="ScriptsContent" ContentPlaceHolderID="ScriptsContent" runat="server">
</asp:Content>
