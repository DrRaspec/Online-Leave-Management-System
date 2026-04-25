using System;
using System.Linq;
using System.Web;
using System.Web.UI;

namespace OnlineLeaveManagementSystem.Security
{
    public abstract class AuthenticatedPage : Page
    {
        protected virtual string[] AllowedRoles
        {
            get { return new string[0]; }
        }

        protected AuthenticatedUser CurrentUser
        {
            get { return AuthManager.GetCurrentUser(); }
        }

        protected override void OnLoad(EventArgs e)
        {
            AuthenticatedUser currentUser = AuthManager.GetCurrentUser();
            if (currentUser == null)
            {
                string returnUrl = HttpUtility.UrlEncode(Request.RawUrl);
                Response.Redirect("~/Login.aspx?ReturnUrl=" + returnUrl, false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            string currentPage = VirtualPathUtility.GetFileName(Request.AppRelativeCurrentExecutionFilePath);
            bool isChangePasswordPage = string.Equals(currentPage, "ChangePassword.aspx", StringComparison.OrdinalIgnoreCase);
            if (currentUser.MustChangePassword && !isChangePasswordPage)
            {
                Response.Redirect("~/ChangePassword.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (AllowedRoles.Length > 0 &&
                !AllowedRoles.Any(role => string.Equals(role, currentUser.Role, StringComparison.OrdinalIgnoreCase)))
            {
                Response.Redirect("~/Dashboard.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            base.OnLoad(e);
        }
    }
}
