<%@ Page Title="Policy Catalog" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PolicyCatalog.aspx.cs" Inherits="OnlineLeaveManagementSystem.PolicyCatalog" %>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="stack-lg">
        <div>
            <div class="page-section-label">
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 6.75h15m-15 5.25h15m-15 5.25h15" />
                </svg>
                Administration
            </div>
            <h2 class="section-title">Policy Catalog</h2>
            <p class="page-copy">Create and manage departments and leave types used across the system.</p>
        </div>

        <asp:Label ID="lblCatalogMessage" runat="server" CssClass="error-label" Visible="false" />
        <asp:Label ID="lblCatalogSuccess" runat="server" CssClass="success-label" Visible="false" />

        <div class="card">
            <div class="card-head">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 21h16.5M4.5 3h15M5.25 3v18m13.5-18v18M9 6.75h1.5m-1.5 3h1.5m-1.5 3h1.5m3-6H15m-1.5 3H15m-1.5 3H15M9 21v-3.375c0-.621.504-1.125 1.125-1.125h3.75c.621 0 1.125.504 1.125 1.125V21" />
                </svg>
                Departments
            </div>
            <div class="card-body">
                <div class="form-group">
                    <label for="<%= txtDepartmentName.ClientID %>" class="form-label">New Department</label>
                    <div class="form-grid" style="grid-template-columns:1fr auto;">
                        <asp:TextBox ID="txtDepartmentName" runat="server" MaxLength="100" placeholder="e.g. Procurement" />
                        <asp:Button ID="btnCreateDepartment" runat="server" Text="Add Department" CssClass="btn-primary" OnClick="btnCreateDepartment_Click" />
                    </div>
                </div>

                <div class="table-wrap mt-md">
                    <table class="data-table">
                        <thead>
                            <tr>
                                <th>Departments</th>
                                <th>Status</th>
                                <th>Action</th>
                            </tr>
                        </thead>
                        <tbody>
                            <asp:Repeater ID="rptDepartments" runat="server" OnItemCommand="rptDepartments_ItemCommand">
                                <ItemTemplate>
                                    <tr>
                                        <td>
                                            <div class="catalog-edit-grid">
                                                <asp:TextBox ID="txtDepartmentEditName" runat="server" Text='<%# Eval("Name") %>' MaxLength="100" CssClass="catalog-name-input" />
                                                <div class="form-hint"><%# GetDepartmentUsageText(Eval("ActiveUserCount")) %></div>
                                            </div>
                                        </td>
                                        <td>
                                            <div class="catalog-status-stack">
                                                <span class='<%# Convert.ToBoolean(Eval("IsActive")) ? "status-badge status-success" : "status-badge status-danger" %>'><%# Convert.ToBoolean(Eval("IsActive")) ? "Active" : "Inactive" %></span>
                                                <label class="checkbox-inline">
                                                    <asp:CheckBox ID="chkDepartmentIsActive" runat="server" Checked='<%# Convert.ToBoolean(Eval("IsActive")) %>' CssClass="catalog-active-checkbox" />
                                                    <span>Active</span>
                                                </label>
                                            </div>
                                        </td>
                                        <td>
                                            <asp:Button
                                                ID="btnSaveDepartment"
                                                runat="server"
                                                Text="Save"
                                                CssClass="btn-secondary policy-update-trigger"
                                                CommandName="UpdateDepartment"
                                                CommandArgument='<%# Eval("Id") %>'
                                                OnClientClick="return confirmCatalogUpdate(this);"
                                                data-item-type="department"
                                                data-original-name='<%# Eval("Name") %>'
                                                data-original-active='<%# Convert.ToBoolean(Eval("IsActive")) ? "true" : "false" %>'
                                                data-active-user-count='<%# Eval("ActiveUserCount") %>' />
                                        </td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>

        <div class="card">
            <div class="card-head">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0v-7.5A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5" />
                </svg>
                Leave Types
            </div>
            <div class="card-body">
                <div class="form-group">
                    <label for="<%= txtLeaveTypeName.ClientID %>" class="form-label">New Leave Type</label>
                    <div class="form-grid" style="grid-template-columns:2fr 1fr auto;">
                        <asp:TextBox ID="txtLeaveTypeName" runat="server" MaxLength="50" placeholder="e.g. Compassionate Leave" />
                        <asp:TextBox ID="txtLeaveTypeDefaultDays" runat="server" TextMode="Number" placeholder="Days" />
                        <asp:Button ID="btnCreateLeaveType" runat="server" Text="Add Leave Type" CssClass="btn-primary" OnClick="btnCreateLeaveType_Click" />
                    </div>
                    <label class="checkbox-inline mt-sm">
                        <asp:CheckBox ID="chkLeaveTypeRequiresAttachment" runat="server" />
                        <span>Requires attachment</span>
                    </label>
                </div>

                <div class="table-wrap mt-md">
                    <table class="data-table">
                        <thead>
                            <tr>
                                <th>Leave Types</th>
                                <th>Default</th>
                                <th>Rules</th>
                                <th>Action</th>
                            </tr>
                        </thead>
                        <tbody>
                            <asp:Repeater ID="rptLeaveTypes" runat="server" OnItemCommand="rptLeaveTypes_ItemCommand">
                                <ItemTemplate>
                                    <tr>
                                        <td>
                                            <div class="catalog-edit-grid">
                                                <asp:TextBox ID="txtLeaveTypeEditName" runat="server" Text='<%# Eval("Name") %>' MaxLength="50" CssClass="catalog-name-input" Enabled='<%# !IsProtectedLeaveType(Eval("Name")) %>' />
                                                <div class="form-hint"><%# GetLeaveTypeAdminHint(Eval("Name")) %></div>
                                                <div class="form-hint"><%# GetLeaveTypeUsageText(Eval("RequestCount"), Eval("BalanceCount")) %></div>
                                            </div>
                                        </td>
                                        <td>
                                            <div class="catalog-edit-grid catalog-edit-grid-compact">
                                                <asp:TextBox ID="txtLeaveTypeEditDefaultDays" runat="server" Text='<%# string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.#}", Eval("DefaultDays")) %>' TextMode="Number" Enabled='<%# !IsProtectedLeaveType(Eval("Name")) %>' />
                                                <asp:TextBox ID="txtLeaveTypeEditSortOrder" runat="server" Text='<%# Eval("SortOrder") %>' TextMode="Number" />
                                            </div>
                                        </td>
                                        <td>
                                            <div class="catalog-status-stack">
                                                <span class='<%# Convert.ToBoolean(Eval("IsActive")) ? "status-badge status-success" : "status-badge status-danger" %>'><%# Convert.ToBoolean(Eval("IsActive")) ? "Active" : "Inactive" %></span>
                                                <label class="checkbox-inline">
                                                    <asp:CheckBox ID="chkLeaveTypeEditRequiresAttachment" runat="server" Checked='<%# Convert.ToBoolean(Eval("RequiresAttachment")) %>' Enabled='<%# !IsProtectedLeaveType(Eval("Name")) %>' />
                                                    <span>Requires attachment</span>
                                                </label>
                                                <label class="checkbox-inline">
                                                    <asp:CheckBox ID="chkLeaveTypeEditIsActive" runat="server" Checked='<%# Convert.ToBoolean(Eval("IsActive")) %>' CssClass="catalog-active-checkbox" Enabled='<%# !IsProtectedLeaveType(Eval("Name")) %>' />
                                                    <span>Active</span>
                                                </label>
                                            </div>
                                        </td>
                                        <td>
                                            <asp:Button
                                                ID="btnSaveLeaveType"
                                                runat="server"
                                                Text="Save"
                                                CssClass="btn-secondary policy-update-trigger"
                                                CommandName="UpdateLeaveType"
                                                CommandArgument='<%# Eval("Id") %>'
                                                OnClientClick="return confirmCatalogUpdate(this);"
                                                data-item-type="leave-type"
                                                data-original-name='<%# Eval("Name") %>'
                                                data-original-active='<%# Convert.ToBoolean(Eval("IsActive")) ? "true" : "false" %>'
                                                data-request-count='<%# Eval("RequestCount") %>'
                                                data-balance-count='<%# Eval("BalanceCount") %>' />
                                        </td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
<asp:Content ID="ScriptsContent" ContentPlaceHolderID="ScriptsContent" runat="server">
    <script>
        function confirmCatalogUpdate(button) {
            if (!button) {
                return true;
            }

            var row = button.closest("tr");
            if (!row) {
                return true;
            }

            var itemType = button.getAttribute("data-item-type") || "item";
            var originalName = button.getAttribute("data-original-name") || "";
            var originalActive = button.getAttribute("data-original-active") === "true";
            var nameInput = row.querySelector(".catalog-name-input");
            var activeCheckbox = row.querySelector(".catalog-active-checkbox input, input.catalog-active-checkbox");
            var currentName = nameInput ? nameInput.value.trim() : originalName;
            var currentActive = activeCheckbox ? activeCheckbox.checked : originalActive;
            var messages = [];

            if (currentName !== originalName) {
                messages.push("You are renaming this " + itemType + " from \"" + originalName + "\" to \"" + currentName + "\".");
            }

            if (originalActive && !currentActive) {
                if (itemType === "department") {
                    var activeUserCount = parseInt(button.getAttribute("data-active-user-count") || "0", 10);
                    messages.push(activeUserCount > 0
                        ? "This department is still used by " + activeUserCount + " active user(s). Deactivation will stop new assignments but keep existing users and history."
                        : "This department will no longer be available for new user assignments.");
                } else {
                    var requestCount = parseInt(button.getAttribute("data-request-count") || "0", 10);
                    var balanceCount = parseInt(button.getAttribute("data-balance-count") || "0", 10);
                    messages.push((requestCount > 0 || balanceCount > 0)
                        ? "This leave type is already used in existing requests or balances. Deactivation will only block new use and keep history intact."
                        : "This leave type will no longer be available for new leave requests.");
                }
            }

            if (messages.length === 0) {
                return true;
            }

            return window.confirm(messages.join("\n\n") + "\n\nContinue?");
        }
    </script>
</asp:Content>
