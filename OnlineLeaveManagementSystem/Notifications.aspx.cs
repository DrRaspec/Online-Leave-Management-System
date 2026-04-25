using System;
using System.Data;
using OnlineLeaveManagementSystem.Data;
using OnlineLeaveManagementSystem.Security;

namespace OnlineLeaveManagementSystem
{
    public partial class Notifications : AuthenticatedPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindNotifications();
            }
        }

        protected void btnMarkAllRead_Click(object sender, EventArgs e)
        {
            NotificationRepository.MarkAllAsRead(CurrentUser.Id);
            BindNotifications();
        }

        private void BindNotifications()
        {
            try
            {
                DataTable notifications = NotificationRepository.GetNotificationsForUser(CurrentUser.Id, false);
                rptNotifications.DataSource = notifications;
                rptNotifications.DataBind();
                lblNotificationsEmpty.Visible = notifications.Rows.Count == 0;
                lblNotificationMessage.Visible = false;
            }
            catch (Exception ex)
            {
                lblNotificationMessage.Text = ex.Message;
                lblNotificationMessage.CssClass = "error-label";
                lblNotificationMessage.Visible = true;
                lblNotificationsEmpty.Visible = false;
            }
        }
    }
}
