using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.UI.WebControls;
using OnlineLeaveManagementSystem.Data;
using OnlineLeaveManagementSystem.Security;

namespace OnlineLeaveManagementSystem
{
    public partial class ApplyLeave : AuthenticatedPage
    {
        private const int MaxUploadBytes = 5 * 1024 * 1024;
        private const int MaxBackdatedRequestDays = 30;
        private static readonly Dictionary<string, string[]> AllowedContentTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { ".pdf", new[] { "application/pdf" } },
            { ".png", new[] { "image/png" } },
            { ".jpg", new[] { "image/jpeg" } },
            { ".jpeg", new[] { "image/jpeg" } },
            { ".docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/zip" } }
        };

        protected void Page_Load(object sender, EventArgs e)
        {
            ConfigureSubmissionAccess();
            ConfigureDateInputs();

            if (!IsPostBack)
            {
                BindLeaveTypes();
                BindBalances();
                BindPolicy();
            }
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            if (!AuthorizationHelper.CanSubmitLeaveRequests(CurrentUser))
            {
                ShowMessage("The bootstrap admin account is reserved for setup and emergency administration. Please sign in with a named employee, manager, or HR account to request leave.", false);
                return;
            }

            DateTime startDate;
            DateTime endDate;

            if (!DateTime.TryParse(txtStartDate.Text, out startDate) || !DateTime.TryParse(txtEndDate.Text, out endDate))
            {
                ShowMessage("Please select valid start and end dates.", false);
                return;
            }

            if (string.IsNullOrWhiteSpace(ddlLeaveType.SelectedValue))
            {
                ShowMessage("Please select a leave type.", false);
                return;
            }

            if (endDate < startDate)
            {
                ShowMessage("End date cannot be earlier than start date.", false);
                return;
            }

            string dateValidationMessage;
            if (!ValidateRequestDates(startDate, endDate, out dateValidationMessage))
            {
                ShowMessage(dateValidationMessage, false);
                return;
            }

            try
            {
                string attachmentFileName = null;
                string attachmentPath = null;
                int leaveTypeId;

                if (fileAttachment.HasFile)
                {
                    string validationError;
                    if (!TrySaveAttachment(out attachmentFileName, out attachmentPath, out validationError))
                    {
                        ShowMessage(validationError, false);
                        return;
                    }
                }

                if (!int.TryParse(ddlLeaveType.SelectedValue, out leaveTypeId))
                {
                    ShowMessage("Please choose a valid leave type.", false);
                    return;
                }

                int requestedDays = LeaveManagementRepository.CalculateRequestedDays(startDate, endDate);
                if (requestedDays <= 0)
                {
                    ShowMessage("The selected date range does not contain any working leave days after weekends and the selected holiday region's public holidays are excluded.", false);
                    return;
                }

                LeaveManagementRepository.SubmitLeaveRequest(
                    CurrentUser,
                    leaveTypeId,
                    startDate,
                    endDate,
                    txtReason.Text,
                    attachmentFileName,
                    attachmentPath);

                ShowMessage(string.Format("Leave application submitted successfully for {0} working day(s).", requestedDays), true);
                ddlLeaveType.SelectedIndex = 0;
                txtStartDate.Text = string.Empty;
                txtEndDate.Text = string.Empty;
                txtReason.Text = string.Empty;
                BindBalances();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, false);
            }
        }

        private void BindLeaveTypes()
        {
            ddlLeaveType.Items.Clear();
            ddlLeaveType.Items.Add(new ListItem("Select leave type", string.Empty));

            foreach (System.Data.DataRow row in LeaveManagementRepository.GetLeaveTypes(false).Rows)
            {
                ddlLeaveType.Items.Add(new ListItem(Convert.ToString(row["Name"]), Convert.ToString(row["Id"])));
            }
        }

        private void BindBalances()
        {
            var balances = LeaveManagementRepository.GetUserBalances(CurrentUser.Id, DateTime.Today.Year);
            rptBalances.DataSource = balances;
            rptBalances.DataBind();
            lblBalancesEmpty.Visible = balances.Rows.Count == 0;
        }

        private void BindPolicy()
        {
            lblPolicyTitle.Text = Server.HtmlEncode(SystemSettingsRepository.GetSetting("LeavePolicyTitle", "Company Leave Policy"));
            lblPolicyText.Text = Server.HtmlEncode(SystemSettingsRepository.GetSetting("LeavePolicyText", string.Empty));
            lblHolidayCalculationHint.Text = Server.HtmlEncode(string.Format("{0} public holidays and configured weekend days are excluded from the leave-day calculation.", SystemSettingsRepository.GetHolidayCalendarRegion()));
        }

        private void ConfigureSubmissionAccess()
        {
            bool canSubmit = AuthorizationHelper.CanSubmitLeaveRequests(CurrentUser);
            ddlLeaveType.Enabled = canSubmit;
            txtStartDate.Enabled = canSubmit;
            txtEndDate.Enabled = canSubmit;
            fileAttachment.Enabled = canSubmit;
            txtReason.Enabled = canSubmit;
            btnSubmit.Enabled = canSubmit;

            if (!canSubmit)
            {
                ShowMessage("The bootstrap admin account is for initial setup and recovery only. Create a named admin or employee account for day-to-day leave activity.", false);
            }
        }

        private void ConfigureDateInputs()
        {
            DateTime today = DateTime.Today;
            DateTime earliestAllowedDate = GetEarliestAllowedDate(today);
            DateTime latestAllowedDate = new DateTime(today.Year, 12, 31);
            string minValue = earliestAllowedDate.ToString("yyyy-MM-dd");
            string maxValue = latestAllowedDate.ToString("yyyy-MM-dd");

            txtStartDate.Attributes["min"] = minValue;
            txtStartDate.Attributes["max"] = maxValue;
            txtEndDate.Attributes["min"] = minValue;
            txtEndDate.Attributes["max"] = maxValue;
        }

        private static DateTime GetEarliestAllowedDate(DateTime today)
        {
            DateTime backdatedLimit = today.AddDays(-MaxBackdatedRequestDays);
            DateTime startOfYear = new DateTime(today.Year, 1, 1);
            return backdatedLimit > startOfYear ? backdatedLimit : startOfYear;
        }

        private static bool ValidateRequestDates(DateTime startDate, DateTime endDate, out string message)
        {
            DateTime today = DateTime.Today;
            DateTime earliestAllowedDate = GetEarliestAllowedDate(today);
            DateTime latestAllowedDate = new DateTime(today.Year, 12, 31);

            if (startDate.Year != today.Year || endDate.Year != today.Year)
            {
                message = string.Format("Leave requests must stay within the current leave year ({0}).", today.Year);
                return false;
            }

            if (startDate.Date < earliestAllowedDate)
            {
                message = string.Format("You can only submit backdated leave up to {0} day(s) ago.", MaxBackdatedRequestDays);
                return false;
            }

            if (endDate.Date > latestAllowedDate)
            {
                message = string.Format("Leave requests can only be submitted up to {0:dd MMM yyyy}.", latestAllowedDate);
                return false;
            }

            message = null;
            return true;
        }

        protected string FormatBalance(object value)
        {
            return string.Format("{0:0.#} days", Convert.ToDecimal(value));
        }

        private bool TrySaveAttachment(out string attachmentFileName, out string attachmentPath, out string validationError)
        {
            attachmentFileName = null;
            attachmentPath = null;
            validationError = null;

            HttpPostedFile postedFile = fileAttachment.PostedFile;
            if (postedFile == null)
            {
                return true;
            }

            if (postedFile.ContentLength <= 0 || postedFile.ContentLength > MaxUploadBytes)
            {
                validationError = "Attachment size must be between 1 byte and 5 MB.";
                return false;
            }

            string extension = Path.GetExtension(fileAttachment.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedContentTypes.ContainsKey(extension))
            {
                validationError = "Only PDF, DOCX, PNG, and JPG files are allowed.";
                return false;
            }

            string contentType = postedFile.ContentType ?? string.Empty;
            bool contentTypeAllowed = false;
            foreach (string allowedContentType in AllowedContentTypes[extension])
            {
                if (string.Equals(contentType, allowedContentType, StringComparison.OrdinalIgnoreCase))
                {
                    contentTypeAllowed = true;
                    break;
                }
            }

            if (!contentTypeAllowed)
            {
                validationError = "The uploaded file type is not allowed.";
                return false;
            }

            if (!HasExpectedFileSignature(postedFile.InputStream, extension))
            {
                validationError = "The uploaded file content does not match the selected file type.";
                return false;
            }

            string uploadsPath = Server.MapPath("~/App_Data/LeaveAttachments");
            Directory.CreateDirectory(uploadsPath);

            attachmentFileName = Path.GetFileName(fileAttachment.FileName);
            string savedFileName = string.Format("{0}_{1}{2}", DateTime.UtcNow.Ticks, Guid.NewGuid().ToString("N"), extension);
            string savePath = Path.Combine(uploadsPath, savedFileName);
            postedFile.InputStream.Position = 0;
            fileAttachment.SaveAs(savePath);
            attachmentPath = "~/SecureAttachmentDownload.aspx?file=" + HttpUtility.UrlEncode(savedFileName);
            return true;
        }

        private static bool HasExpectedFileSignature(Stream stream, string extension)
        {
            if (!stream.CanRead)
            {
                return false;
            }

            byte[] header = new byte[8];
            long originalPosition = stream.CanSeek ? stream.Position : 0;
            int bytesRead = stream.Read(header, 0, header.Length);
            if (stream.CanSeek)
            {
                stream.Position = originalPosition;
            }

            if (bytesRead < 4)
            {
                return false;
            }

            if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46;
            }

            if (string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
            {
                return bytesRead >= 8 &&
                       header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                       header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;
            }

            if (string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                return header[0] == 0xFF && header[1] == 0xD8;
            }

            if (string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase))
            {
                return header[0] == 0x50 && header[1] == 0x4B;
            }

            return false;
        }

        private void ShowMessage(string message, bool isSuccess)
        {
            lblSuccessMessage.Text = message;
            lblSuccessMessage.CssClass = isSuccess ? "error-label status-success" : "error-label";
            lblSuccessMessage.Visible = true;
        }
    }
}
