using System;
using System.Data;
using System.Web.UI.WebControls;
using OnlineLeaveManagementSystem.Data;
using OnlineLeaveManagementSystem.Security;

namespace OnlineLeaveManagementSystem
{
    public partial class ManageRequests : AuthenticatedPage
    {
        private const int PageSize = 10;

        protected override string[] AllowedRoles
        {
            get { return new[] { AuthorizationHelper.AdminRole, AuthorizationHelper.HrRole, AuthorizationHelper.DepartmentAdminRole }; }
        }

        private int CurrentPage
        {
            get { return ViewState["ManageRequestsCurrentPage"] == null ? 1 : Convert.ToInt32(ViewState["ManageRequestsCurrentPage"]); }
            set { ViewState["ManageRequestsCurrentPage"] = value < 1 ? 1 : value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindDepartments();
                BindRequests();
            }
        }

        protected void btnApplyFilters_Click(object sender, EventArgs e)
        {
            CurrentPage = 1;
            BindRequests();
        }

        protected void ddlStatusFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentPage = 1;
            BindRequests();
        }

        protected void ddlDepartmentFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentPage = 1;
            BindRequests();
        }

        protected void btnPreviousPage_Click(object sender, EventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }

            BindRequests();
        }

        protected void btnNextPage_Click(object sender, EventArgs e)
        {
            CurrentPage++;
            BindRequests();
        }

        protected void rptRequests_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int requestId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out requestId))
            {
                return;
            }

            TextBox txtReviewComment = e.Item.FindControl("txtReviewComment") as TextBox;
            string reviewComment = txtReviewComment == null ? string.Empty : txtReviewComment.Text;

            if (e.CommandName == "Approve")
            {
                UpdateRequestStatus(requestId, "Approved", reviewComment);
            }
            else if (e.CommandName == "Reject")
            {
                UpdateRequestStatus(requestId, "Rejected", reviewComment);
            }

            BindRequests();
        }

        private void BindDepartments()
        {
            ddlDepartmentFilter.Items.Clear();
            ddlDepartmentFilter.Items.Add(new ListItem("All Departments", "All"));

            DataTable departments = LeaveManagementRepository.GetDepartments(false);
            foreach (DataRow row in departments.Rows)
            {
                string departmentName = Convert.ToString(row["Name"]);
                ddlDepartmentFilter.Items.Add(new ListItem(departmentName, departmentName));
            }

            bool canSelectAnyDepartment = AuthorizationHelper.CanSelectAnyDepartment(CurrentUser);
            ddlDepartmentFilter.Enabled = canSelectAnyDepartment;
            if (!canSelectAnyDepartment)
            {
                ListItem currentDepartment = ddlDepartmentFilter.Items.FindByValue(CurrentUser.Department);
                if (currentDepartment != null)
                {
                    ddlDepartmentFilter.ClearSelection();
                    currentDepartment.Selected = true;
                }
            }
        }

        private void BindRequests()
        {
            try
            {
                LeaveManagementRepository.PagedDataTableResult pagedResult = LeaveManagementRepository.GetManageableRequestsPage(
                    CurrentUser,
                    ddlStatusFilter.SelectedValue,
                    ddlDepartmentFilter.SelectedValue,
                    txtSearch.Text,
                    CurrentPage,
                    PageSize);
                int totalPages = GetTotalPages(pagedResult.TotalCount, PageSize);
                if (CurrentPage > totalPages)
                {
                    CurrentPage = totalPages;
                    pagedResult = LeaveManagementRepository.GetManageableRequestsPage(
                        CurrentUser,
                        ddlStatusFilter.SelectedValue,
                        ddlDepartmentFilter.SelectedValue,
                        txtSearch.Text,
                        CurrentPage,
                        PageSize);
                }

                DataTable requestsTable = pagedResult.Data;

                rptRequests.DataSource = requestsTable;
                rptRequests.DataBind();

                lblRequestCount.Text = pagedResult.TotalCount + " requests";
                lblPageSummary.Text = pagedResult.TotalCount == 0
                    ? "No requests"
                    : string.Format("Page {0} of {1}", CurrentPage, totalPages);
                btnPreviousPage.Enabled = CurrentPage > 1;
                btnNextPage.Enabled = CurrentPage < totalPages;
                pnlPager.Visible = pagedResult.TotalCount > PageSize;
                lblRequestsMessage.Visible = requestsTable.Rows.Count == 0;
                lblRequestsMessage.CssClass = "empty-state";
                lblRequestsMessage.Text = requestsTable.Rows.Count == 0 ? "No leave requests found for the selected filters." : string.Empty;
            }
            catch (Exception ex)
            {
                rptRequests.DataSource = null;
                rptRequests.DataBind();
                lblRequestCount.Text = "0 requests";
                lblPageSummary.Text = "Page 1 of 1";
                btnPreviousPage.Enabled = false;
                btnNextPage.Enabled = false;
                pnlPager.Visible = false;
                lblRequestsMessage.Text = ex.Message;
                lblRequestsMessage.CssClass = "error-label";
                lblRequestsMessage.Visible = true;
            }
        }

        private void UpdateRequestStatus(int requestId, string status, string reviewComment)
        {
            LeaveManagementRepository.UpdateRequestStatus(CurrentUser, requestId, status, reviewComment);
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

        protected bool CanReview(object statusValue, object requesterUserIdValue)
        {
            if (!string.Equals(Convert.ToString(statusValue), "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int requesterUserId;
            return int.TryParse(Convert.ToString(requesterUserIdValue), out requesterUserId) &&
                   AuthorizationHelper.CanReviewOwnRequest(CurrentUser, requesterUserId);
        }

        protected bool ShowSelfReviewNotice(object statusValue, object requesterUserIdValue)
        {
            if (!string.Equals(Convert.ToString(statusValue), "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int requesterUserId;
            return int.TryParse(Convert.ToString(requesterUserIdValue), out requesterUserId) &&
                   !AuthorizationHelper.CanReviewOwnRequest(CurrentUser, requesterUserId);
        }

        protected string FormatReviewMeta(object reviewedByValue, object reviewedAtValue)
        {
            if (reviewedAtValue == null || reviewedAtValue == DBNull.Value)
            {
                return "Awaiting review";
            }

            string reviewer = Convert.ToString(reviewedByValue);
            if (string.IsNullOrWhiteSpace(reviewer))
            {
                reviewer = "Reviewer";
            }

            return string.Format("{0} on {1:dd MMM yyyy HH:mm}", reviewer, Convert.ToDateTime(reviewedAtValue));
        }

        private static int GetTotalPages(int totalCount, int pageSize)
        {
            return Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        }
    }
}
