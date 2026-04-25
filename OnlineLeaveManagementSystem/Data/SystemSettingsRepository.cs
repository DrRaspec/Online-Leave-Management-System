using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace OnlineLeaveManagementSystem.Data
{
    public static class SystemSettingsRepository
    {
        private static readonly string[] SupportedHolidayRegions =
        {
            "Cambodia",
            "Laos",
            "Myanmar"
        };

        public static string[] GetSupportedHolidayRegions()
        {
            return (string[])SupportedHolidayRegions.Clone();
        }

        public static DataTable GetAllSettings()
        {
            return DbHelper.ExecuteDataTable(@"
SELECT SettingKey, SettingValue, UpdatedAt
FROM dbo.SystemSettings
ORDER BY SettingKey ASC;");
        }

        public static string GetSetting(string key, string defaultValue)
        {
            object result = DbHelper.ExecuteScalar(
                "SELECT TOP 1 SettingValue FROM dbo.SystemSettings WHERE SettingKey = @SettingKey;",
                new SqlParameter("@SettingKey", key));

            return result == null || result == DBNull.Value ? defaultValue : Convert.ToString(result);
        }

        public static bool GetBoolSetting(string key, bool defaultValue)
        {
            string value = GetSetting(key, defaultValue ? "1" : "0");
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        public static void SaveSetting(string key, string value)
        {
            DbHelper.ExecuteNonQuery(@"
IF EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = @SettingKey)
BEGIN
    UPDATE dbo.SystemSettings
    SET SettingValue = @SettingValue,
        UpdatedAt = GETDATE()
    WHERE SettingKey = @SettingKey;
END
ELSE
BEGIN
    INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, UpdatedAt)
    VALUES (@SettingKey, @SettingValue, GETDATE());
END;",
                new SqlParameter("@SettingKey", key),
                new SqlParameter("@SettingValue", (object)value ?? DBNull.Value));
        }

        public static DataTable GetPublicHolidays(int year)
        {
            return GetPublicHolidays(year, GetHolidayCalendarRegion());
        }

        public static DataTable GetPublicHolidays(int year, string region)
        {
            string normalizedRegion = NormalizeHolidayRegion(region);
            return DbHelper.ExecuteDataTable(@"
SELECT Id, HolidayDate, Name, Region, IsActive
FROM dbo.PublicHolidays
WHERE YEAR(HolidayDate) = @Year
  AND Region = @Region
ORDER BY HolidayDate ASC, Name ASC;",
                new SqlParameter("@Year", year),
                new SqlParameter("@Region", normalizedRegion));
        }

        public static HashSet<DateTime> GetHolidayDates(DateTime startDate, DateTime endDate)
        {
            string region = GetHolidayCalendarRegion();
            DataTable table = DbHelper.ExecuteDataTable(@"
SELECT HolidayDate
FROM dbo.PublicHolidays
WHERE IsActive = 1
  AND Region = @Region
  AND HolidayDate BETWEEN @StartDate AND @EndDate;",
                new SqlParameter("@Region", region),
                new SqlParameter("@StartDate", startDate.Date),
                new SqlParameter("@EndDate", endDate.Date));

            HashSet<DateTime> dates = new HashSet<DateTime>();
            foreach (DataRow row in table.Rows)
            {
                dates.Add(Convert.ToDateTime(row["HolidayDate"]).Date);
            }

            return dates;
        }

        public static string GetHolidayCalendarRegion()
        {
            return NormalizeHolidayRegion(GetSetting("HolidayCalendarRegion", "Cambodia"));
        }

        public static void SavePublicHoliday(DateTime holidayDate, string name, string region, bool isActive)
        {
            string normalizedName = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                throw new InvalidOperationException("Holiday name is required.");
            }

            string normalizedRegion = NormalizeHolidayRegion(region);

            DbHelper.ExecuteNonQuery(@"
IF EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = @HolidayDate AND Name = @Name AND Region = @Region)
BEGIN
    UPDATE dbo.PublicHolidays
    SET IsActive = @IsActive
    WHERE HolidayDate = @HolidayDate
      AND Name = @Name
      AND Region = @Region;
END
ELSE
BEGIN
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt)
    VALUES (@HolidayDate, @Name, @Region, @IsActive, GETDATE());
END;",
                new SqlParameter("@HolidayDate", holidayDate.Date),
                new SqlParameter("@Name", normalizedName),
                new SqlParameter("@Region", normalizedRegion),
                new SqlParameter("@IsActive", isActive));
        }

        private static string NormalizeHolidayRegion(string region)
        {
            string normalized = (region ?? string.Empty).Trim();
            foreach (string supportedRegion in SupportedHolidayRegions)
            {
                if (string.Equals(supportedRegion, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return supportedRegion;
                }
            }

            return "Cambodia";
        }
    }
}
