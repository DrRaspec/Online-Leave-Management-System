<%@ Page Title="Manage Requests" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ManageRequests.aspx.cs" Inherits="OnlineLeaveManagementSystem.ManageRequests" %>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="stack-lg">
        <div>
            <div class="page-section-label">
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                </svg>
                Administration
            </div>
            <h2 class="section-title">Manage Leave Requests</h2>
            <p class="page-copy">Review incoming leave requests and update their status.</p>
        </div>

        <div class="card">
            <div class="card-head" style="justify-content: space-between;">
                <div style="display:flex;align-items:center;gap:8px;">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 0 0 2.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 0 0-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 0 0 .75-.75 2.25 2.25 0 0 0-.1-.664m-5.8 0A2.251 2.251 0 0 1 13.5 2.25H15a2.251 2.251 0 0 1 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25Z" />
                    </svg>
                    All Requests
                </div>
                <asp:Label ID="lblRequestCount" runat="server" CssClass="status-badge status-neutral" />
            </div>
            <div class="card-body">
                <asp:Label ID="lblRequestsMessage" runat="server" CssClass="empty-state" Visible="false" />
                <div id="manageRequestsLoading" class="page-loading-overlay" aria-hidden="true">
                    <div class="page-loading-card">
                        <span class="page-loading-spinner" aria-hidden="true"></span>
                        <span>Loading requests...</span>
                    </div>
                </div>

                <div class="toolbar-inline">
                    <div class="form-grid" style="width:100%;">
                        <div class="form-group">
                            <label for="<%= txtSearch.ClientID %>" class="form-label">Search</label>
                            <asp:TextBox ID="txtSearch" runat="server" placeholder="Employee, username, or leave type" />
                        </div>
                        <div class="form-group">
                            <label for="<%= ddlStatusFilter.ClientID %>" class="form-label">Status</label>
                            <asp:DropDownList ID="ddlStatusFilter" runat="server" OnSelectedIndexChanged="ddlStatusFilter_SelectedIndexChanged">
                                <asp:ListItem Text="All Statuses" Value="All" />
                                <asp:ListItem Text="Pending" Value="Pending" />
                                <asp:ListItem Text="Approved" Value="Approved" />
                                <asp:ListItem Text="Rejected" Value="Rejected" />
                            </asp:DropDownList>
                        </div>
                        <div class="form-group">
                            <label for="<%= ddlDepartmentFilter.ClientID %>" class="form-label">Department</label>
                            <asp:DropDownList ID="ddlDepartmentFilter" runat="server" OnSelectedIndexChanged="ddlDepartmentFilter_SelectedIndexChanged" />
                        </div>
                        <div class="form-group" style="justify-content:flex-end;">
                            <label class="form-label" style="visibility:hidden;">Apply</label>
                            <div class="btn-row" style="justify-content:flex-end;">
                                <asp:Button ID="btnApplyFilters" runat="server" Text="Apply Filters" CssClass="btn-secondary" OnClick="btnApplyFilters_Click" />
                                <asp:Button ID="btnResetFilters" runat="server" Text="Reset" CssClass="btn-secondary" OnClick="btnResetFilters_Click" CausesValidation="false" />
                            </div>
                        </div>
                    </div>
                </div>

                <div class="requests-stack">
                    <asp:Repeater ID="rptRequests" runat="server" OnItemCommand="rptRequests_ItemCommand">
                        <ItemTemplate>
                            <div class="request-card">
                                <div class="request-layout">
                                    <div class="flex-grow-1">
                                        <div class="request-student"><%# Eval("FullName") %></div>
                                        <div class="request-detail"><%# Eval("Department") %> | <%# Eval("Username") %></div>
                                        <div class="request-grid">
                                            <div>
                                                <div class="request-detail">Leave Type</div>
                                                <div class="request-value"><%# Eval("LeaveType") %></div>
                                            </div>
                                            <div>
                                                <div class="request-detail">Date Range</div>
                                                <div class="request-value"><%# FormatDateRange(Eval("StartDate"), Eval("EndDate")) %></div>
                                            </div>
                                            <div>
                                                <div class="request-detail">Days Requested</div>
                                                <div class="request-value"><%# Eval("RequestedDays") %> day(s)</div>
                                            </div>
                                            <div>
                                                <div class="request-detail">Status</div>
                                                <span class='<%# GetStatusBadgeCss(Eval("Status")) %>'><%# Eval("Status") %></span>
                                            </div>
                                            <div>
                                                <div class="request-detail">Reason</div>
                                                <div class="request-value"><%# Eval("Reason") %></div>
                                            </div>
                                            <div>
                                                <div class="request-detail">Latest Review</div>
                                                <div class="request-value"><%# FormatReviewMeta(Eval("ReviewedByName"), Eval("ReviewedAt")) %></div>
                                            </div>
                                            <div>
                                                <div class="request-detail">Review Comment</div>
                                                <div class="request-value"><%# string.IsNullOrWhiteSpace(Convert.ToString(Eval("ReviewComment"))) ? "-" : Eval("ReviewComment") %></div>
                                            </div>
                                            <asp:PlaceHolder ID="phAttachment" runat="server" Visible='<%# !string.IsNullOrWhiteSpace(Convert.ToString(Eval("AttachmentPath"))) %>'>
                                                <div>
                                                    <div class="request-detail">Attachment</div>
                                                    <div class="request-value">
                                                        <a href="<%# ResolveUrl(Convert.ToString(Eval("AttachmentPath"))) %>" target="_blank" rel="noopener">View attachment</a>
                                                    </div>
                                                </div>
                                            </asp:PlaceHolder>
                                        </div>
                                    </div>
                                    <div class="request-actions request-review-panel">
                                        <asp:Label ID="lblSelfReviewBlocked" runat="server" CssClass="form-hint" Visible='<%# ShowSelfReviewNotice(Eval("Status"), Eval("RequesterUserId")) %>' Text="You cannot approve or reject your own leave request." />
                                        <asp:PlaceHolder ID="phPendingActions" runat="server" Visible='<%# CanReview(Eval("Status"), Eval("RequesterUserId")) %>'>
                                            <asp:TextBox ID="txtReviewComment" runat="server" TextMode="MultiLine" Rows="4" placeholder="Add an approval note or rejection reason" />
                                            <asp:Button ID="btnApprove" runat="server" Text="Approve" CssClass="btn-primary btn-approve" CommandName="Approve" CommandArgument='<%# Eval("Id") %>' />
                                            <asp:Button ID="btnReject" runat="server" Text="Reject" CssClass="btn-secondary btn-reject" CommandName="Reject" CommandArgument='<%# Eval("Id") %>' />
                                        </asp:PlaceHolder>
                                    </div>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>

                <asp:Panel ID="pnlPager" runat="server" CssClass="pager-bar" Visible="false">
                    <asp:Button ID="btnPreviousPage" runat="server" Text="Previous" CssClass="btn-secondary" OnClick="btnPreviousPage_Click" CausesValidation="false" />
                    <asp:Label ID="lblPageSummary" runat="server" CssClass="pager-summary" Text="Page 1 of 1" />
                    <asp:Button ID="btnNextPage" runat="server" Text="Next" CssClass="btn-secondary" OnClick="btnNextPage_Click" CausesValidation="false" />
                </asp:Panel>
            </div>
        </div>
    </div>
</asp:Content>
<asp:Content ID="ScriptsContent" ContentPlaceHolderID="ScriptsContent" runat="server">
    <script>
        (function () {
            function showLoading() {
                var overlay = document.getElementById("manageRequestsLoading");
                if (overlay) {
                    overlay.classList.add("is-visible");
                }
                document.body.classList.add("is-page-loading");
            }

            function bindLoadingTrigger(element, eventName) {
                if (!element) {
                    return;
                }

                element.addEventListener(eventName, function () {
                    showLoading();
                });
            }

            function bindFilterPostBack(element, eventName, postBackTarget) {
                if (!element) {
                    return;
                }

                element.addEventListener(eventName, function () {
                    showLoading();
                    if (typeof __doPostBack === "function") {
                        __doPostBack(postBackTarget, "");
                    }
                });
            }

            document.addEventListener("DOMContentLoaded", function () {
                bindLoadingTrigger(document.getElementById("<%= btnApplyFilters.ClientID %>"), "click");
                bindLoadingTrigger(document.getElementById("<%= btnResetFilters.ClientID %>"), "click");
                bindFilterPostBack(document.getElementById("<%= ddlStatusFilter.ClientID %>"), "change", "<%= ddlStatusFilter.UniqueID %>");
                bindFilterPostBack(document.getElementById("<%= ddlDepartmentFilter.ClientID %>"), "change", "<%= ddlDepartmentFilter.UniqueID %>");
                bindLoadingTrigger(document.getElementById("<%= btnPreviousPage.ClientID %>"), "click");
                bindLoadingTrigger(document.getElementById("<%= btnNextPage.ClientID %>"), "click");

                var actionButtons = document.querySelectorAll(".btn-approve, .btn-reject");
                for (var i = 0; i < actionButtons.length; i++) {
                    bindLoadingTrigger(actionButtons[i], "click");
                }
            });
        })();
    </script>
</asp:Content>
