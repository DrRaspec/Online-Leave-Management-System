<%@ Page Title="Company Settings" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CompanySettings.aspx.cs" Inherits="OnlineLeaveManagementSystem.CompanySettings" %>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="stack-lg">
        <div>
            <div class="page-section-label">
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 12a7.5 7.5 0 1 1 15 0 7.5 7.5 0 0 1-15 0Zm7.5-4.5v4.5l3 3" />
                </svg>
                Administration
            </div>
            <h2 class="section-title">Company Settings</h2>
            <p class="page-copy">Configure leave policy text, weekend rules, and region-based public holidays used by working-day calculations.</p>
        </div>

        <asp:Label ID="lblSettingsMessage" runat="server" CssClass="error-label" Visible="false" />

        <div class="card">
            <div class="card-head">Leave Policy</div>
            <div class="card-body">
                <div class="form-grid">
                    <div class="form-group">
                        <label for="<%= txtPolicyTitle.ClientID %>" class="form-label">Policy Title</label>
                        <asp:TextBox ID="txtPolicyTitle" runat="server" />
                    </div>
                    <div class="form-group">
                        <label for="<%= ddlHolidayRegion.ClientID %>" class="form-label">Holiday Region</label>
                        <asp:DropDownList ID="ddlHolidayRegion" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlHolidayRegion_SelectedIndexChanged" />
                    </div>
                </div>
                <div class="form-group" style="margin-top:20px;">
                    <label for="<%= txtPolicyText.ClientID %>" class="form-label">Policy Text</label>
                    <asp:TextBox ID="txtPolicyText" runat="server" TextMode="MultiLine" Rows="5" />
                </div>
                <div class="toggle-row">
                    <label><asp:CheckBox ID="chkSaturdayOff" runat="server" /> Saturday counts as weekend</label>
                    <label><asp:CheckBox ID="chkSundayOff" runat="server" /> Sunday counts as weekend</label>
                </div>
                <div class="btn-row" style="margin-top:20px;">
                    <asp:Button ID="btnSaveSettings" runat="server" Text="Save Settings" CssClass="btn-primary" OnClick="btnSaveSettings_Click" />
                </div>
            </div>
        </div>

        <div class="card">
            <div class="card-head"><asp:Label ID="lblHolidayCardTitle" runat="server" Text="Public Holidays" /></div>
            <div class="card-body">
                <div class="toolbar-inline">
                    <div class="form-grid" style="width:100%;">
                        <div class="form-group">
                            <label for="<%= txtHolidayDate.ClientID %>" class="form-label">Holiday Date</label>
                            <asp:TextBox ID="txtHolidayDate" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-group">
                            <label for="<%= txtHolidayName.ClientID %>" class="form-label">Holiday Name</label>
                            <asp:TextBox ID="txtHolidayName" runat="server" />
                        </div>
                        <div class="form-group">
                            <label class="form-label">Status</label>
                            <label><asp:CheckBox ID="chkHolidayActive" runat="server" Checked="true" /> Active</label>
                        </div>
                        <div class="form-group" style="justify-content:flex-end;">
                            <label class="form-label" style="visibility:hidden;">Save</label>
                            <asp:Button ID="btnSaveHoliday" runat="server" Text="Add / Update Holiday" CssClass="btn-secondary" OnClick="btnSaveHoliday_Click" />
                        </div>
                    </div>
                </div>
                <div class="form-hint" style="margin-bottom:16px;">
                    Holidays shown here and used in leave calculations follow the selected holiday region.
                </div>

                <div class="table-wrap">
                    <table class="data-table">
                        <thead>
                            <tr>
                                <th>Date</th>
                                <th>Name</th>
                                <th>Region</th>
                                <th>Status</th>
                            </tr>
                        </thead>
                        <tbody>
                            <asp:Repeater ID="rptHolidays" runat="server">
                                <ItemTemplate>
                                    <tr>
                                        <td><%# Eval("HolidayDate", "{0:dd MMM yyyy}") %></td>
                                        <td><%# Eval("Name") %></td>
                                        <td><%# Eval("Region") %></td>
                                        <td><span class='<%# Convert.ToBoolean(Eval("IsActive")) ? "status-badge status-success" : "status-badge status-danger" %>'><%# Convert.ToBoolean(Eval("IsActive")) ? "Active" : "Inactive" %></span></td>
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
</asp:Content>
