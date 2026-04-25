using System;
using System.Web;
using OnlineLeaveManagementSystem.Data;
using OnlineLeaveManagementSystem.Security;

namespace OnlineLeaveManagementSystem
{
    public partial class SiteMaster : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            AuthenticatedUser user = AuthManager.GetCurrentUser();
            if (user == null)
            {
                Response.Redirect("~/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            lblCurrentUser.Text = user.FullName;
            lblCurrentRole.Text = string.Format("{0} • {1}", user.Role, user.Department);
            lblPageSubtitle.Text = string.Format("Signed in as {0}", user.Username);

            // Set avatar initial from the user's name
            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                lblAvatarInitial.Text = user.FullName.Substring(0, 1).ToUpper();
            }

            bool canManageRequests = AuthorizationHelper.CanManageRequests(user);
            bool canViewReports = AuthorizationHelper.CanViewReports(user);
            bool isAdmin = AuthorizationHelper.IsAdmin(user);
            bool canSubmitLeave = AuthorizationHelper.CanSubmitLeaveRequests(user);
            int unreadCount = NotificationRepository.GetUnreadCount(user.Id);
            lnkApplyLeave.Visible = canSubmitLeave;
            lnkApplyLeave.CssClass = canSubmitLeave ? GetNavCss("ApplyLeave.aspx") : "nav-item";
            lnkManageRequests.Visible = canManageRequests;
            lnkManageRequests.CssClass = canManageRequests ? GetNavCss("ManageRequests.aspx") : "nav-item";
            lnkLeaveReports.Visible = canViewReports;
            lnkLeaveReports.CssClass = canViewReports ? GetNavCss("LeaveReports.aspx") : "nav-item";
            lnkNotifications.Visible = true;
            lnkNotifications.CssClass = GetNavCss("Notifications.aspx");
            lblNotificationCount.Visible = unreadCount > 0;
            lblNotificationCount.Text = unreadCount.ToString();
            lnkManageUsers.Visible = isAdmin;
            lnkManageUsers.CssClass = isAdmin ? GetNavCss("ManageUsers.aspx") : "nav-item";
            lnkCompanySettings.Visible = isAdmin;
            lnkCompanySettings.CssClass = isAdmin ? GetNavCss("CompanySettings.aspx") : "nav-item";
            lnkChangePassword.Visible = true;
        }

        protected string GetNavCss(string pageName)
        {
            string currentPage = VirtualPathUtility.GetFileName(Request.AppRelativeCurrentExecutionFilePath);
            bool isActive = string.Equals(currentPage, pageName, StringComparison.OrdinalIgnoreCase);
            return isActive ? "nav-item is-active" : "nav-item";
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            AuthManager.SignOut();
            Response.Redirect("~/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
