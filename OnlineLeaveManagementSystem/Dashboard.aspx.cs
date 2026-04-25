using System;
using System.Data;
using OnlineLeaveManagementSystem.Data;
using OnlineLeaveManagementSystem.Security;

namespace OnlineLeaveManagementSystem
{
    public partial class Dashboard : AuthenticatedPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindDashboard();
            }
        }

        private void BindDashboard()
        {
            try
            {
                DataTable summaryTable = DbHelper.ExecuteDataTable(@"
SELECT
    (SELECT COUNT(1) FROM dbo.LeaveRequests) AS TotalRequests,
    (SELECT COUNT(1) FROM dbo.LeaveRequests WHERE [Status] = N'Pending') AS PendingRequests,
    (SELECT COUNT(1) FROM dbo.LeaveRequests WHERE [Status] = N'Approved') AS ApprovedRequests,
    (SELECT COUNT(1) FROM dbo.Users WHERE IsActive = 1) AS ActiveUsers;");

                if (summaryTable.Rows.Count > 0)
                {
                    DataRow row = summaryTable.Rows[0];
                    lblTotalRequests.Text = Convert.ToString(row["TotalRequests"]);
                    lblPendingRequests.Text = Convert.ToString(row["PendingRequests"]);
                    lblApprovedRequests.Text = Convert.ToString(row["ApprovedRequests"]);
                    lblActiveUsers.Text = Convert.ToString(row["ActiveUsers"]);
                }

                DataTable recentRequests = DbHelper.ExecuteDataTable(@"
SELECT TOP 6
    lr.StartDate,
    lr.EndDate,
    lr.LeaveType,
    lr.Status,
    u.FullName,
    u.Department
FROM dbo.LeaveRequests lr
INNER JOIN dbo.Users u ON u.Id = lr.UserId
ORDER BY lr.CreatedAt DESC, lr.StartDate DESC;");

                rptRecentRequests.DataSource = recentRequests;
                rptRecentRequests.DataBind();
                lblRecentRequestsEmpty.Visible = recentRequests.Rows.Count == 0;
            }
            catch (Exception)
            {
                lblDashboardMessage.Text = "Unable to load dashboard data. Please verify the database connection.";
                lblDashboardMessage.Visible = true;
            }
        }

        protected string FormatDateRange(object startDateValue, object endDateValue)
        {
            DateTime startDate = Convert.ToDateTime(startDateValue);
            DateTime endDate = Convert.ToDateTime(endDateValue);
            return string.Format("{0:dd MMM yyyy} - {1:dd MMM yyyy}", startDate, endDate);
        }

        protected string GetStatusBadgeCss(object statusValue)
        {
            string status = Convert.ToString(statusValue);

            if (string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                return "status-badge status-success";
            }

            if (string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                return "status-badge status-danger";
            }

            return "status-badge status-warning";
        }
    }
}
