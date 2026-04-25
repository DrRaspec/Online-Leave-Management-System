using System;
using System.Data;
using System.Globalization;
using System.Web.UI.WebControls;
using OnlineLeaveManagementSystem.Security;
using OnlineLeaveManagementSystem.Data;

namespace OnlineLeaveManagementSystem
{
    public partial class PolicyCatalog : AuthenticatedPage
    {
        private const string UnpaidLeaveName = "Unpaid Leave";

        protected override string[] AllowedRoles
        {
            get { return new[] { "Admin" }; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindPolicyData();
            }
        }

        protected void btnCreateDepartment_Click(object sender, EventArgs e)
        {
            try
            {
                LeaveManagementRepository.CreateDepartment(txtDepartmentName.Text);
                txtDepartmentName.Text = string.Empty;
                ShowSuccess("Department created.");
                BindPolicyData();
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
                BindPolicyData();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        protected void rptDepartments_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!string.Equals(e.CommandName, "UpdateDepartment", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int departmentId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out departmentId))
            {
                return;
            }

            try
            {
                TextBox txtDepartmentEditName = e.Item.FindControl("txtDepartmentEditName") as TextBox;
                CheckBox chkDepartmentIsActive = e.Item.FindControl("chkDepartmentIsActive") as CheckBox;

                LeaveManagementRepository.UpdateDepartment(
                    departmentId,
                    txtDepartmentEditName == null ? string.Empty : txtDepartmentEditName.Text,
                    chkDepartmentIsActive != null && chkDepartmentIsActive.Checked);

                ShowSuccess("Department updated.");
                BindPolicyData();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        protected void rptLeaveTypes_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!string.Equals(e.CommandName, "UpdateLeaveType", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int leaveTypeId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out leaveTypeId))
            {
                return;
            }

            try
            {
                TextBox txtLeaveTypeEditName = e.Item.FindControl("txtLeaveTypeEditName") as TextBox;
                TextBox txtLeaveTypeEditDefaultDays = e.Item.FindControl("txtLeaveTypeEditDefaultDays") as TextBox;
                TextBox txtLeaveTypeEditSortOrder = e.Item.FindControl("txtLeaveTypeEditSortOrder") as TextBox;
                CheckBox chkLeaveTypeEditRequiresAttachment = e.Item.FindControl("chkLeaveTypeEditRequiresAttachment") as CheckBox;
                CheckBox chkLeaveTypeEditIsActive = e.Item.FindControl("chkLeaveTypeEditIsActive") as CheckBox;

                decimal defaultDays;
                if (!decimal.TryParse(txtLeaveTypeEditDefaultDays == null ? string.Empty : txtLeaveTypeEditDefaultDays.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out defaultDays))
                {
                    ShowError("Enter a valid default balance for the leave type.");
                    return;
                }

                int sortOrder;
                if (!int.TryParse(txtLeaveTypeEditSortOrder == null ? string.Empty : txtLeaveTypeEditSortOrder.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out sortOrder))
                {
                    ShowError("Enter a valid sort order for the leave type.");
                    return;
                }

                LeaveManagementRepository.UpdateLeaveType(
                    leaveTypeId,
                    txtLeaveTypeEditName == null ? string.Empty : txtLeaveTypeEditName.Text,
                    defaultDays,
                    chkLeaveTypeEditRequiresAttachment != null && chkLeaveTypeEditRequiresAttachment.Checked,
                    chkLeaveTypeEditIsActive != null && chkLeaveTypeEditIsActive.Checked,
                    sortOrder);

                ShowSuccess("Leave type updated.");
                BindPolicyData();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        protected string GetDepartmentUsageText(object activeUserCountValue)
        {
            int count = activeUserCountValue == DBNull.Value ? 0 : Convert.ToInt32(activeUserCountValue);
            return count == 0 ? "Not assigned to active users." : string.Format("Used by {0} active user(s).", count);
        }

        protected string GetLeaveTypeUsageText(object requestCountValue, object balanceCountValue)
        {
            int requestCount = requestCountValue == DBNull.Value ? 0 : Convert.ToInt32(requestCountValue);
            int balanceCount = balanceCountValue == DBNull.Value ? 0 : Convert.ToInt32(balanceCountValue);
            if (requestCount == 0 && balanceCount == 0)
            {
                return "No requests or balances yet.";
            }

            return string.Format("Used in {0} request(s) and {1} balance row(s).", requestCount, balanceCount);
        }

        protected bool IsProtectedLeaveType(object leaveTypeNameValue)
        {
            return string.Equals(Convert.ToString(leaveTypeNameValue), UnpaidLeaveName, StringComparison.OrdinalIgnoreCase);
        }

        protected string GetLeaveTypeAdminHint(object leaveTypeNameValue)
        {
            return IsProtectedLeaveType(leaveTypeNameValue)
                ? "System leave type: only sort order can be changed."
                : "Order: lower numbers appear first.";
        }

        private void BindPolicyData()
        {
            rptDepartments.DataSource = LeaveManagementRepository.GetDepartments(true);
            rptDepartments.DataBind();

            rptLeaveTypes.DataSource = LeaveManagementRepository.GetLeaveTypes(true);
            rptLeaveTypes.DataBind();
        }

        private void ShowError(string message)
        {
            lblCatalogMessage.Text = message;
            lblCatalogMessage.Visible = true;
            lblCatalogSuccess.Text = string.Empty;
            lblCatalogSuccess.Visible = false;
        }

        private void ShowSuccess(string message)
        {
            lblCatalogSuccess.Text = message;
            lblCatalogSuccess.Visible = true;
            lblCatalogMessage.Text = string.Empty;
            lblCatalogMessage.Visible = false;
        }
    }
}
