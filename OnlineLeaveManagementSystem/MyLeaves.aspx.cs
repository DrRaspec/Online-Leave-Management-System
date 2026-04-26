using System;
using System.Data;
using OnlineLeaveManagementSystem.Data;
using OnlineLeaveManagementSystem.Security;

namespace OnlineLeaveManagementSystem
{
    public partial class MyLeaves : AuthenticatedPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindBalances();
                BindLeaves();
            }
        }

        protected void ddlStatusFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindLeaves();
        }

        protected void btnResetFilters_Click(object sender, EventArgs e)
        {
            ddlStatusFilter.SelectedValue = "All";
            BindLeaves();
        }

        private void BindLeaves()
        {
            try
            {
                DataTable leavesTable = LeaveManagementRepository.GetUserRequests(CurrentUser.Id, ddlStatusFilter.SelectedValue);

                rptLeaves.DataSource = leavesTable;
                rptLeaves.DataBind();

                bool hasRows = leavesTable.Rows.Count > 0;
                lblLeavesMessage.Visible = !hasRows;
                lblLeavesMessage.Text = hasRows ? string.Empty : "No leave requests found for the selected filter.";
            }
            catch (Exception)
            {
                rptLeaves.DataSource = null;
                rptLeaves.DataBind();
                lblLeavesMessage.Text = "Unable to load leave requests. Please check the database connection.";
                lblLeavesMessage.CssClass = "error-label";
                lblLeavesMessage.Visible = true;
            }
        }

        private void BindBalances()
        {
            DataTable balances = LeaveManagementRepository.GetUserBalances(CurrentUser.Id, DateTime.Today.Year);
            rptBalances.DataSource = balances;
            rptBalances.DataBind();
            lblBalancesEmpty.Visible = balances.Rows.Count == 0;
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

        protected string FormatBalance(object value)
        {
            return string.Format("{0:0.#} days", Convert.ToDecimal(value));
        }

        protected string FormatReview(object reviewedByValue, object reviewedAtValue, object reviewCommentValue)
        {
            if (reviewedAtValue == null || reviewedAtValue == DBNull.Value)
            {
                return "Awaiting review";
            }

            string reviewer = Convert.ToString(reviewedByValue);
            string comment = Convert.ToString(reviewCommentValue);
            string summary = string.Format("{0} on {1:dd MMM yyyy HH:mm}", string.IsNullOrWhiteSpace(reviewer) ? "Reviewer" : reviewer, Convert.ToDateTime(reviewedAtValue));
            if (!string.IsNullOrWhiteSpace(comment))
            {
                summary += " - " + comment;
            }

            return summary;
        }
    }
}
