using System;
using OnlineLeaveManagementSystem.Security;

namespace OnlineLeaveManagementSystem
{
    public partial class ChangePassword : AuthenticatedPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && CurrentUser != null)
            {
                bool mustChangePassword = CurrentUser.MustChangePassword;
                lnkCancelPasswordChange.Visible = !mustChangePassword;

                if (mustChangePassword)
                {
                    lblIntroMessage.Text = "Password update required before you can continue.";
                    lblIntroMessage.Visible = true;
                }
            }
        }

        protected void btnUpdatePassword_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.Equals(txtNewPassword.Text, txtConfirmPassword.Text, StringComparison.Ordinal))
                {
                    ShowError("The new password and confirmation do not match.");
                    return;
                }

                string message;
                if (!UserAccountManager.ChangePassword(CurrentUser.Id, txtCurrentPassword.Text, txtNewPassword.Text, out message))
                {
                    ShowError(message);
                    return;
                }

                txtCurrentPassword.Text = string.Empty;
                txtNewPassword.Text = string.Empty;
                txtConfirmPassword.Text = string.Empty;
                Session["MustChangePassword"] = false;

                Response.Redirect("~/Dashboard.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void ShowError(string message)
        {
            lblPasswordMessage.Text = message;
            lblPasswordMessage.Visible = true;
            lblPasswordSuccess.Text = string.Empty;
            lblPasswordSuccess.Visible = false;
        }
    }
}
