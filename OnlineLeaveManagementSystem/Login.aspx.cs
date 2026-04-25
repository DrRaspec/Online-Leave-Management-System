using System;
using System.Web;
using OnlineLeaveManagementSystem.Security;

namespace OnlineLeaveManagementSystem
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && Request.IsAuthenticated && AuthManager.GetCurrentUser() != null)
            {
                Response.Redirect("~/Dashboard.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            SignInResult result = AuthManager.ValidateUser(username, password);
            if (result.Status == SignInStatus.Success && result.User != null)
            {
                Session["Username"] = result.User.Username;
                Session["Role"] = result.User.Role;
                Session["UserId"] = result.User.Id;
                Session["FullName"] = result.User.FullName;
                Session["MustChangePassword"] = result.User.MustChangePassword;
                AuthManager.SignIn(result.User.Username, false);

                string returnUrl = Request.QueryString["ReturnUrl"];
                string redirectUrl = result.User.MustChangePassword
                    ? "~/ChangePassword.aspx"
                    : (string.IsNullOrWhiteSpace(returnUrl) ? "~/Dashboard.aspx" : returnUrl);

                Response.Redirect(redirectUrl, false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            lblMessage.Text = HttpUtility.HtmlEncode(GetLoginMessage(result));
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtUsername.Text = string.Empty;
            txtPassword.Text = string.Empty;
            lblMessage.Text = string.Empty;
            txtUsername.Focus();
        }

        private static string GetLoginMessage(SignInResult result)
        {
            if (result == null)
            {
                return "Unable to sign in right now. Please try again.";
            }

            if (result.Status == SignInStatus.Inactive)
            {
                return "This account is inactive. Please contact an administrator for access.";
            }

            if (result.Status == SignInStatus.LockedOut)
            {
                return "Too many failed login attempts. Please wait 15 minutes and try again, or contact an administrator if you need help.";
            }

            return string.IsNullOrWhiteSpace(result.Message)
                ? "Invalid username or password."
                : result.Message;
        }
    }
}
