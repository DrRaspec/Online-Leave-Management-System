using System;
using System.Data;
using System.Globalization;
using System.Web.UI.WebControls;
using OnlineLeaveManagementSystem.Security;
using OnlineLeaveManagementSystem.Data;

namespace OnlineLeaveManagementSystem
{
    public partial class ManageUsers : AuthenticatedPage
    {
        protected override string[] AllowedRoles
        {
            get { return new[] { "Admin" }; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindRoleList(ddlNewRole);
                BindDepartmentList(ddlNewDepartment, false);
                BindDepartmentList(ddlDepartmentFilter, true);
                BindPage();
            }
        }

        protected void btnCreateUser_Click(object sender, EventArgs e)
        {
            string username = txtNewUsername.Text.Trim();

            try
            {
                string temporaryPassword = UserAccountManager.CreateUser(
                    CurrentUser,
                    username,
                    txtNewFullName.Text,
                    ddlNewDepartment.SelectedValue,
                    ddlNewRole.SelectedValue,
                    chkNewIsActive.Checked);

                txtNewUsername.Text = string.Empty;
                txtNewFullName.Text = string.Empty;
                ddlNewDepartment.SelectedIndex = 0;
                ddlNewRole.SelectedIndex = 0;
                chkNewIsActive.Checked = true;

                ShowSuccess(string.Format("User '{0}' created successfully. Temporary password: {1}", Server.HtmlEncode(username), Server.HtmlEncode(temporaryPassword)));
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }

            BindPage();
        }

        protected void rptUsers_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            DataRowView row = e.Item.DataItem as DataRowView;
            if (row == null)
            {
                return;
            }

            DropDownList ddlRole = e.Item.FindControl("ddlRole") as DropDownList;
            DropDownList ddlDepartment = e.Item.FindControl("ddlDepartment") as DropDownList;
            CheckBox chkIsActive = e.Item.FindControl("chkIsActive") as CheckBox;

            if (ddlRole != null)
            {
                BindRoleList(ddlRole);
                ListItem item = ddlRole.Items.FindByValue(Convert.ToString(row["Role"]));
                if (item != null)
                {
                    ddlRole.ClearSelection();
                    item.Selected = true;
                }

                bool isBootstrapAccount = string.Equals(Convert.ToString(row["Username"]), AuthorizationHelper.BootstrapAdminUsername, StringComparison.OrdinalIgnoreCase);
                bool isAnotherAdmin = string.Equals(Convert.ToString(row["Role"]), AuthorizationHelper.AdminRole, StringComparison.OrdinalIgnoreCase) &&
                                      Convert.ToInt32(row["Id"]) != CurrentUser.Id;
                ddlRole.Enabled = !isBootstrapAccount && (!isAnotherAdmin || AuthorizationHelper.IsBootstrapAdmin(CurrentUser));
            }

            if (ddlDepartment != null)
            {
                BindDepartmentList(ddlDepartment, false);
                ListItem departmentItem = ddlDepartment.Items.FindByValue(Convert.ToString(row["Department"]));
                if (departmentItem != null)
                {
                    ddlDepartment.ClearSelection();
                    departmentItem.Selected = true;
                }
            }

            if (chkIsActive != null)
            {
                chkIsActive.Checked = Convert.ToBoolean(row["IsActive"]);
                bool isAnotherAdmin = string.Equals(Convert.ToString(row["Role"]), AuthorizationHelper.AdminRole, StringComparison.OrdinalIgnoreCase) &&
                                      Convert.ToInt32(row["Id"]) != CurrentUser.Id;
                chkIsActive.Enabled = !isAnotherAdmin || AuthorizationHelper.IsBootstrapAdmin(CurrentUser);
            }
        }

        protected void rptUsers_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int userId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out userId))
            {
                return;
            }

            try
            {
                if (string.Equals(e.CommandName, "UpdateUser", StringComparison.OrdinalIgnoreCase))
                {
                    TextBox txtFullName = e.Item.FindControl("txtFullName") as TextBox;
                    DropDownList ddlDepartment = e.Item.FindControl("ddlDepartment") as DropDownList;
                    DropDownList ddlRole = e.Item.FindControl("ddlRole") as DropDownList;
                    CheckBox chkIsActive = e.Item.FindControl("chkIsActive") as CheckBox;

                    UserAccountManager.UpdateUser(
                        CurrentUser,
                        userId,
                        txtFullName == null ? string.Empty : txtFullName.Text,
                        ddlDepartment == null ? string.Empty : ddlDepartment.SelectedValue,
                        ddlRole == null ? string.Empty : ddlRole.SelectedValue,
                        chkIsActive != null && chkIsActive.Checked);

                    ShowSuccess("User account updated.");
                }
                else if (string.Equals(e.CommandName, "ResetPassword", StringComparison.OrdinalIgnoreCase))
                {
                    TextBox txtResetPassword = e.Item.FindControl("txtResetPassword") as TextBox;
                    string temporaryPassword = txtResetPassword == null ? string.Empty : txtResetPassword.Text;

                    UserAccountManager.ResetPassword(CurrentUser, userId, temporaryPassword);

                    if (txtResetPassword != null)
                    {
                        txtResetPassword.Text = string.Empty;
                    }

                    ShowSuccess("Password reset complete. The user must sign in with the temporary password you provided and change it immediately.");
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }

            BindPage();
        }

        protected void btnApplyFilters_Click(object sender, EventArgs e)
        {
            BindUsers();
        }

        protected void ddlDepartmentFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindUsers();
        }

        protected void btnCreateDepartment_Click(object sender, EventArgs e)
        {
            try
            {
                LeaveManagementRepository.CreateDepartment(txtDepartmentName.Text);
                txtDepartmentName.Text = string.Empty;
                ShowSuccess("Department created.");
                BindDepartmentList(ddlNewDepartment, false);
                BindDepartmentList(ddlDepartmentFilter, true);
                BindPage();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        protected void btnCreateLeaveType_Click(object sender, EventArgs e)
        {
            try
            {
                decimal defaultDays;
                if (!decimal.TryParse(txtLeaveTypeDefaultDays.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out defaultDays))
                {
                    ShowError("Enter a valid default balance.");
                    return;
                }

                LeaveManagementRepository.CreateLeaveType(txtLeaveTypeName.Text, defaultDays, chkLeaveTypeRequiresAttachment.Checked);
                txtLeaveTypeName.Text = string.Empty;
                txtLeaveTypeDefaultDays.Text = string.Empty;
                chkLeaveTypeRequiresAttachment.Checked = false;
                ShowSuccess("Leave type created.");
                BindPage();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        protected string GetAccountStatusCss(object isActiveValue)
        {
            return Convert.ToBoolean(isActiveValue) ? "status-badge status-success" : "status-badge status-danger";
        }

        protected string GetAccountStatusText(object isActiveValue)
        {
            return Convert.ToBoolean(isActiveValue) ? "Active" : "Inactive";
        }

        protected string GetPasswordStatusCss(object mustChangePasswordValue)
        {
            return Convert.ToBoolean(mustChangePasswordValue) ? "status-badge status-warning" : "status-badge status-neutral";
        }

        protected string GetPasswordStatusText(object mustChangePasswordValue)
        {
            return Convert.ToBoolean(mustChangePasswordValue) ? "Reset Pending" : "Password Set";
        }

        protected bool IsLockedOut(object lockoutEndUtcValue)
        {
            if (lockoutEndUtcValue == null || lockoutEndUtcValue == DBNull.Value)
            {
                return false;
            }

            return Convert.ToDateTime(lockoutEndUtcValue) > DateTime.UtcNow;
        }

        protected string FormatDate(object value)
        {
            return value == null || value == DBNull.Value ? "recently" : Convert.ToDateTime(value).ToString("dd MMM yyyy");
        }

        protected string FormatLastLogin(object value)
        {
            return value == null || value == DBNull.Value ? "Never signed in" : Convert.ToDateTime(value).ToString("dd MMM yyyy HH:mm");
        }

        private void BindPage()
        {
            BindSummary();
            BindPolicyData();
            BindUsers();
        }

        private void BindSummary()
        {
            DataTable summary = UserAccountManager.GetSummary();
            if (summary.Rows.Count == 0)
            {
                lblTotalUsers.Text = "0";
                lblActiveUsers.Text = "0";
                lblActiveAdmins.Text = "0";
                lblPasswordResetsPending.Text = "0";
                lblLockedUsers.Text = "0 locked";
                lblAdminCoverageWarning.Visible = false;
                return;
            }

            DataRow row = summary.Rows[0];
            lblTotalUsers.Text = Convert.ToString(row["TotalUsers"]);
            lblActiveUsers.Text = Convert.ToString(row["ActiveUsers"]);
            lblActiveAdmins.Text = Convert.ToString(row["ActiveAdmins"]);
            lblPasswordResetsPending.Text = Convert.ToString(row["PasswordResetsPending"]);
            lblLockedUsers.Text = string.Format("{0} locked", Convert.ToString(row["LockedUsers"]));

            bool meetsRecommendedCoverage = row["MeetsRecommendedAdminCoverage"] != DBNull.Value && Convert.ToInt32(row["MeetsRecommendedAdminCoverage"]) == 1;
            lblAdminCoverageWarning.Text = "Best practice is to keep at least two active named admin accounts so one admin can recover the other if needed.";
            lblAdminCoverageWarning.Visible = !meetsRecommendedCoverage;
        }

        private void BindUsers()
        {
            DataTable users = UserAccountManager.GetUsers();
            DataView view = users.DefaultView;
            string filter = BuildUserFilter();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                view.RowFilter = filter;
            }

            rptUsers.DataSource = view;
            rptUsers.DataBind();
            lblUsersEmpty.Visible = view.Count == 0;
        }

        private void BindPolicyData()
        {
            rptDepartments.DataSource = LeaveManagementRepository.GetDepartments(false);
            rptDepartments.DataBind();

            rptLeaveTypes.DataSource = LeaveManagementRepository.GetLeaveTypes(false);
            rptLeaveTypes.DataBind();
        }

        private void BindRoleList(ListControl control)
        {
            if (control == null)
            {
                return;
            }

            control.Items.Clear();
            foreach (string role in UserAccountManager.GetSupportedRoles())
            {
                control.Items.Add(new ListItem(role, role));
            }
        }

        private void BindDepartmentList(ListControl control, bool includeAllOption)
        {
            if (control == null)
            {
                return;
            }

            string selectedValue = control.SelectedValue;
            control.Items.Clear();
            if (includeAllOption)
            {
                control.Items.Add(new ListItem("All Departments", "All"));
            }

            DataTable departments = LeaveManagementRepository.GetDepartments(false);
            foreach (DataRow row in departments.Rows)
            {
                string name = Convert.ToString(row["Name"]);
                control.Items.Add(new ListItem(name, name));
            }

            if (!string.IsNullOrWhiteSpace(selectedValue))
            {
                ListItem selectedItem = control.Items.FindByValue(selectedValue);
                if (selectedItem != null)
                {
                    control.ClearSelection();
                    selectedItem.Selected = true;
                }
            }
        }

        private string BuildUserFilter()
        {
            string search = txtUserSearch.Text.Trim();
            string selectedDepartment = ddlDepartmentFilter.SelectedValue;
            System.Collections.Generic.List<string> filters = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string escaped = EscapeRowFilterValue(search);
                filters.Add(string.Format("(Username LIKE '%{0}%' OR FullName LIKE '%{0}%' OR Department LIKE '%{0}%')", escaped));
            }

            if (!string.IsNullOrWhiteSpace(selectedDepartment) && !string.Equals(selectedDepartment, "All", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add(string.Format("Department = '{0}'", EscapeRowFilterValue(selectedDepartment)));
            }

            return string.Join(" AND ", filters.ToArray());
        }

        private static string EscapeRowFilterValue(string value)
        {
            return (value ?? string.Empty).Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");
        }

        private void ShowError(string message)
        {
            lblUsersMessage.Text = message;
            lblUsersMessage.Visible = true;
            lblUsersSuccess.Text = string.Empty;
            lblUsersSuccess.Visible = false;
        }

        private void ShowSuccess(string message)
        {
            lblUsersSuccess.Text = message;
            lblUsersSuccess.Visible = true;
            lblUsersMessage.Text = string.Empty;
            lblUsersMessage.Visible = false;
        }
    }
}
