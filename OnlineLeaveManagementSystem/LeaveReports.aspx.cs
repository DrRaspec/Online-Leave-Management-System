using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI.WebControls;
using OnlineLeaveManagementSystem.Data;
using OnlineLeaveManagementSystem.Infrastructure;
using OnlineLeaveManagementSystem.Security;

namespace OnlineLeaveManagementSystem
{
    public partial class LeaveReports : AuthenticatedPage
    {
        private const int PageSize = 20;

        protected override string[] AllowedRoles
        {
            get { return new[] { AuthorizationHelper.AdminRole, AuthorizationHelper.HrRole, AuthorizationHelper.DepartmentAdminRole }; }
        }

        private int CurrentPage
        {
            get { return ViewState["LeaveReportsCurrentPage"] == null ? 1 : Convert.ToInt32(ViewState["LeaveReportsCurrentPage"]); }
            set { ViewState["LeaveReportsCurrentPage"] = value < 1 ? 1 : value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindDepartments();
                BindLeaveTypes();
                BindReport();
            }
        }

        protected void btnApplyFilters_Click(object sender, EventArgs e)
        {
            CurrentPage = 1;
            BindReport();
        }

        protected void btnPreviousPage_Click(object sender, EventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }

            BindReport();
        }

        protected void btnNextPage_Click(object sender, EventArgs e)
        {
            CurrentPage++;
            BindReport();
        }

        protected void btnExportCsv_Click(object sender, EventArgs e)
        {
            ExportReport("csv");
        }

        protected void btnExportPdf_Click(object sender, EventArgs e)
        {
            ExportReport("pdf");
        }

        protected void btnExportDocx_Click(object sender, EventArgs e)
        {
            ExportReport("docx");
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

        protected string FormatReview(object reviewedByValue, object reviewedAtValue, object reviewCommentValue)
        {
            string reviewer = Convert.ToString(reviewedByValue);
            string reviewComment = Convert.ToString(reviewCommentValue);

            if (reviewedAtValue == null || reviewedAtValue == DBNull.Value)
            {
                return "Awaiting review";
            }

            string stamp = string.Format("{0} on {1:dd MMM yyyy}", string.IsNullOrWhiteSpace(reviewer) ? "Reviewer" : reviewer, Convert.ToDateTime(reviewedAtValue));
            if (string.IsNullOrWhiteSpace(reviewComment))
            {
                return stamp;
            }

            return stamp + " | " + reviewComment;
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

        private void BindLeaveTypes()
        {
            ddlLeaveTypeFilter.Items.Clear();
            ddlLeaveTypeFilter.Items.Add(new ListItem("All Leave Types", "All"));

            DataTable leaveTypes = LeaveManagementRepository.GetLeaveTypes(false);
            foreach (DataRow row in leaveTypes.Rows)
            {
                string leaveTypeName = Convert.ToString(row["Name"]);
                ddlLeaveTypeFilter.Items.Add(new ListItem(leaveTypeName, leaveTypeName));
            }
        }

        private void BindReport()
        {
            try
            {
                LeaveManagementRepository.PagedDataTableResult pagedResult = LeaveManagementRepository.GetLeaveReportPage(
                    CurrentUser,
                    ddlStatusFilter.SelectedValue,
                    ddlDepartmentFilter.SelectedValue,
                    ddlLeaveTypeFilter.SelectedValue,
                    ParseDate(txtStartDate.Text),
                    ParseDate(txtEndDate.Text),
                    txtSearch.Text,
                    CurrentPage,
                    PageSize);
                int totalPages = GetTotalPages(pagedResult.TotalCount, PageSize);
                if (CurrentPage > totalPages)
                {
                    CurrentPage = totalPages;
                    pagedResult = LeaveManagementRepository.GetLeaveReportPage(
                        CurrentUser,
                        ddlStatusFilter.SelectedValue,
                        ddlDepartmentFilter.SelectedValue,
                        ddlLeaveTypeFilter.SelectedValue,
                        ParseDate(txtStartDate.Text),
                        ParseDate(txtEndDate.Text),
                        txtSearch.Text,
                        CurrentPage,
                        PageSize);
                }

                LeaveManagementRepository.LeaveReportSummaryResult summary = GetReportSummary();
                DataTable reportTable = pagedResult.Data;
                rptReportRows.DataSource = reportTable;
                rptReportRows.DataBind();

                lblResultCount.Text = summary.TotalRecords + " records";
                lblTotalRecords.Text = summary.TotalRecords.ToString();
                lblRequestedDays.Text = summary.TotalRequestedDays.ToString("0.#");
                lblPendingCount.Text = summary.PendingCount.ToString();
                lblApprovedCount.Text = summary.ApprovedCount.ToString();
                lblPageSummary.Text = summary.TotalRecords == 0
                    ? "No records"
                    : string.Format("Page {0} of {1}", CurrentPage, totalPages);
                btnPreviousPage.Enabled = CurrentPage > 1;
                btnNextPage.Enabled = CurrentPage < totalPages;
                pnlPager.Visible = summary.TotalRecords > PageSize;
                lblEmptyReport.Visible = reportTable.Rows.Count == 0;
                lblReportMessage.Visible = false;
            }
            catch (Exception ex)
            {
                rptReportRows.DataSource = null;
                rptReportRows.DataBind();
                lblResultCount.Text = "0 records";
                lblTotalRecords.Text = "0";
                lblRequestedDays.Text = "0";
                lblPendingCount.Text = "0";
                lblApprovedCount.Text = "0";
                lblPageSummary.Text = "Page 1 of 1";
                btnPreviousPage.Enabled = false;
                btnNextPage.Enabled = false;
                pnlPager.Visible = false;
                lblEmptyReport.Visible = false;
                lblReportMessage.Text = ex.Message;
                lblReportMessage.CssClass = "error-label";
                lblReportMessage.Visible = true;
            }
        }

        private void ExportReport(string format)
        {
            DataTable reportTable = GetReportData();
            string fileBaseName = string.Format("leave-report-{0:yyyyMMdd-HHmm}", DateTime.Now);
            string generatedBy = string.Format("{0} ({1})", CurrentUser.FullName, CurrentUser.Role);
            List<string> filterSummary = BuildFilterSummary();
            byte[] payload;
            string contentType;
            string fileName;

            switch (format)
            {
                case "pdf":
                    payload = ReportExportBuilder.BuildPdf(reportTable, "Leave Report", filterSummary, generatedBy);
                    contentType = "application/pdf";
                    fileName = fileBaseName + ".pdf";
                    break;
                case "docx":
                    payload = ReportExportBuilder.BuildDocx(reportTable, "Leave Report", filterSummary, generatedBy);
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    fileName = fileBaseName + ".docx";
                    break;
                default:
                    payload = ReportExportBuilder.BuildCsv(reportTable);
                    contentType = "text/csv";
                    fileName = fileBaseName + ".csv";
                    break;
            }

            Response.Clear();
            Response.Buffer = true;
            Response.ContentType = contentType;
            Response.AddHeader("content-disposition", "attachment; filename=" + fileName);
            Response.BinaryWrite(payload);
            Response.Flush();
            Response.End();
        }

        private DataTable GetReportData()
        {
            return LeaveManagementRepository.GetLeaveReport(
                CurrentUser,
                ddlStatusFilter.SelectedValue,
                ddlDepartmentFilter.SelectedValue,
                ddlLeaveTypeFilter.SelectedValue,
                ParseDate(txtStartDate.Text),
                ParseDate(txtEndDate.Text),
                txtSearch.Text);
        }

        private LeaveManagementRepository.LeaveReportSummaryResult GetReportSummary()
        {
            return LeaveManagementRepository.GetLeaveReportSummary(
                CurrentUser,
                ddlStatusFilter.SelectedValue,
                ddlDepartmentFilter.SelectedValue,
                ddlLeaveTypeFilter.SelectedValue,
                ParseDate(txtStartDate.Text),
                ParseDate(txtEndDate.Text),
                txtSearch.Text);
        }

        private List<string> BuildFilterSummary()
        {
            return new List<string>
            {
                "Status: " + ddlStatusFilter.SelectedValue,
                "Department: " + ddlDepartmentFilter.SelectedValue,
                "Leave type: " + ddlLeaveTypeFilter.SelectedValue,
                "Start date: " + (string.IsNullOrWhiteSpace(txtStartDate.Text) ? "Any" : txtStartDate.Text),
                "End date: " + (string.IsNullOrWhiteSpace(txtEndDate.Text) ? "Any" : txtEndDate.Text),
                "Search: " + (string.IsNullOrWhiteSpace(txtSearch.Text) ? "None" : txtSearch.Text.Trim())
            };
        }

        private static DateTime? ParseDate(string value)
        {
            DateTime parsed;
            return DateTime.TryParse(value, out parsed) ? parsed.Date : (DateTime?)null;
        }

        private static int GetTotalPages(int totalCount, int pageSize)
        {
            return Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        }
    }
}
