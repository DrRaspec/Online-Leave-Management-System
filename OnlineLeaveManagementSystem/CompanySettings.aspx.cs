using System;
using System.Data;
using System.Web.UI.WebControls;
using OnlineLeaveManagementSystem.Data;
using OnlineLeaveManagementSystem.Security;

namespace OnlineLeaveManagementSystem
{
    public partial class CompanySettings : AuthenticatedPage
    {
        protected override string[] AllowedRoles
        {
            get { return new[] { AuthorizationHelper.AdminRole }; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindHolidayRegions();
                BindPage();
            }
        }

        protected void btnSaveSettings_Click(object sender, EventArgs e)
        {
            try
            {
                SystemSettingsRepository.SaveSetting("LeavePolicyTitle", txtPolicyTitle.Text.Trim());
                SystemSettingsRepository.SaveSetting("LeavePolicyText", txtPolicyText.Text.Trim());
                SystemSettingsRepository.SaveSetting("HolidayCalendarRegion", ddlHolidayRegion.SelectedValue);
                SystemSettingsRepository.SaveSetting("WeekendSaturdayOff", chkSaturdayOff.Checked ? "1" : "0");
                SystemSettingsRepository.SaveSetting("WeekendSundayOff", chkSundayOff.Checked ? "1" : "0");
                lblSettingsMessage.Text = "Settings updated.";
                lblSettingsMessage.CssClass = "error-label status-success";
                lblSettingsMessage.Visible = true;
                BindPage();
            }
            catch (Exception ex)
            {
                lblSettingsMessage.Text = ex.Message;
                lblSettingsMessage.CssClass = "error-label";
                lblSettingsMessage.Visible = true;
            }
        }

        protected void btnSaveHoliday_Click(object sender, EventArgs e)
        {
            try
            {
                DateTime holidayDate;
                if (!DateTime.TryParse(txtHolidayDate.Text, out holidayDate))
                {
                    throw new InvalidOperationException("Enter a valid holiday date.");
                }

                SystemSettingsRepository.SavePublicHoliday(holidayDate, txtHolidayName.Text, ddlHolidayRegion.SelectedValue, chkHolidayActive.Checked);
                txtHolidayDate.Text = string.Empty;
                txtHolidayName.Text = string.Empty;
                chkHolidayActive.Checked = true;
                lblSettingsMessage.Text = "Holiday saved.";
                lblSettingsMessage.CssClass = "error-label status-success";
                lblSettingsMessage.Visible = true;
                BindHolidays();
            }
            catch (Exception ex)
            {
                lblSettingsMessage.Text = ex.Message;
                lblSettingsMessage.CssClass = "error-label";
                lblSettingsMessage.Visible = true;
            }
        }

        protected void ddlHolidayRegion_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindHolidays();
        }

        private void BindPage()
        {
            txtPolicyTitle.Text = SystemSettingsRepository.GetSetting("LeavePolicyTitle", "Company Leave Policy");
            txtPolicyText.Text = SystemSettingsRepository.GetSetting("LeavePolicyText", string.Empty);
            SelectHolidayRegion(SystemSettingsRepository.GetHolidayCalendarRegion());
            chkSaturdayOff.Checked = SystemSettingsRepository.GetBoolSetting("WeekendSaturdayOff", true);
            chkSundayOff.Checked = SystemSettingsRepository.GetBoolSetting("WeekendSundayOff", true);
            BindHolidays();
        }

        private void BindHolidays()
        {
            string region = ddlHolidayRegion.SelectedValue;
            lblHolidayCardTitle.Text = region + " Public Holidays";
            DataTable holidays = SystemSettingsRepository.GetPublicHolidays(DateTime.Today.Year, region);
            rptHolidays.DataSource = holidays;
            rptHolidays.DataBind();
        }

        private void BindHolidayRegions()
        {
            ddlHolidayRegion.Items.Clear();
            foreach (string region in SystemSettingsRepository.GetSupportedHolidayRegions())
            {
                ddlHolidayRegion.Items.Add(new ListItem(region, region));
            }
        }

        private void SelectHolidayRegion(string region)
        {
            ListItem item = ddlHolidayRegion.Items.FindByValue(region);
            if (item == null && ddlHolidayRegion.Items.Count > 0)
            {
                ddlHolidayRegion.SelectedIndex = 0;
                return;
            }

            if (item != null)
            {
                ddlHolidayRegion.ClearSelection();
                item.Selected = true;
            }
        }
    }
}
