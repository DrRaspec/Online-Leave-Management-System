using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Web.Hosting;
using OnlineLeaveManagementSystem.Security;

namespace OnlineLeaveManagementSystem.Infrastructure
{
    public static class DatabaseInitializer
    {
        private const string ConnectionStringName = "LeaveManagementConnection";
        private const string DatabaseName = "OnlineLeaveManagementSystemDb";
        private const string PasswordCharset = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";

        public static void EnsureDatabase()
        {
            string filesPath = HostingEnvironment.MapPath("~/App_Data/LeaveAttachments");
            if (!string.IsNullOrWhiteSpace(filesPath))
            {
                Directory.CreateDirectory(filesPath);
            }

            string appConnectionString = ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
            SqlConnectionStringBuilder appBuilder = new SqlConnectionStringBuilder(appConnectionString);
            string serverConnectionString = BuildServerConnectionString(appBuilder);

            if (!string.IsNullOrWhiteSpace(appBuilder.AttachDBFilename))
            {
                string databaseFile = ExpandDataDirectory(appBuilder.AttachDBFilename);
                string databaseFolder = Path.GetDirectoryName(databaseFile);

                if (!string.IsNullOrWhiteSpace(databaseFolder))
                {
                    Directory.CreateDirectory(databaseFolder);
                }

                string logFile = Path.Combine(databaseFolder ?? string.Empty, DatabaseName + "_log.ldf");
                EnsureDatabaseFile(serverConnectionString, databaseFile, logFile);
            }
            else
            {
                EnsureServerDatabase(serverConnectionString, appBuilder.InitialCatalog);
            }

            EnsureSchema();
            MigrateLegacyPasswords();
            EnsureBootstrapAdmin();
            EnsureBootstrapAdminCredentialLifecycle();
        }

        private static string ExpandDataDirectory(string path)
        {
            string appDataPath = HostingEnvironment.MapPath("~/App_Data");
            return path.Replace("|DataDirectory|", appDataPath ?? string.Empty);
        }

        private static string BuildServerConnectionString(SqlConnectionStringBuilder builder)
        {
            SqlConnectionStringBuilder serverBuilder = new SqlConnectionStringBuilder(builder.ConnectionString);
            serverBuilder.InitialCatalog = "master";
            serverBuilder.AttachDBFilename = string.Empty;
            return serverBuilder.ConnectionString;
        }

        private static void EnsureServerDatabase(string serverConnectionString, string databaseName)
        {
            using (SqlConnection connection = new SqlConnection(serverConnectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                connection.Open();
                string safeDatabaseName = (databaseName ?? string.Empty).Replace("]", "]]");
                command.CommandText = @"
IF DB_ID(N'" + safeDatabaseName + @"') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [" + safeDatabaseName + @"]');
END";
                command.CommandTimeout = 120;
                command.ExecuteNonQuery();
            }
        }

        private static void EnsureDatabaseFile(string serverConnectionString, string databaseFile, string logFile)
        {
            using (SqlConnection connection = new SqlConnection(serverConnectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                connection.Open();

                string escapedDbFile = databaseFile.Replace("'", "''");
                string escapedLogFile = logFile.Replace("'", "''");
                bool databaseFileExists = File.Exists(databaseFile);
                bool logFileExists = File.Exists(logFile);

                command.CommandText = @"
IF DB_ID(N'" + DatabaseName + @"') IS NULL
BEGIN
    " + BuildCreateOrAttachDatabaseSql(escapedDbFile, escapedLogFile, databaseFileExists, logFileExists) + @"
END";
                command.CommandTimeout = 120;
                command.ExecuteNonQuery();
            }
        }

        private static string BuildCreateOrAttachDatabaseSql(string escapedDbFile, string escapedLogFile, bool databaseFileExists, bool logFileExists)
        {
            if (databaseFileExists)
            {
                if (logFileExists)
                {
                    return @"
    CREATE DATABASE [" + DatabaseName + @"]
    ON
    (
        FILENAME = N'" + escapedDbFile + @"'
    ),
    (
        FILENAME = N'" + escapedLogFile + @"'
    )
    FOR ATTACH;";
                }

                return @"
    CREATE DATABASE [" + DatabaseName + @"]
    ON
    (
        FILENAME = N'" + escapedDbFile + @"'
    )
    FOR ATTACH_REBUILD_LOG;";
            }

            return @"
    CREATE DATABASE [" + DatabaseName + @"]
    ON PRIMARY
    (
        NAME = N'" + DatabaseName + @"',
        FILENAME = N'" + escapedDbFile + @"'
    )
    LOG ON
    (
        NAME = N'" + DatabaseName + @"_log',
        FILENAME = N'" + escapedLogFile + @"'
    );";
        }

        private static void EnsureSchema()
        {
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = @"
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL,
        PasswordHash NVARCHAR(256) NULL,
        PasswordSalt NVARCHAR(128) NULL,
        FullName NVARCHAR(200) NOT NULL CONSTRAINT DF_Users_FullName DEFAULT (N'Employee'),
        DepartmentId INT NULL,
        Department NVARCHAR(100) NOT NULL CONSTRAINT DF_Users_Department DEFAULT (N'General'),
        [Role] NVARCHAR(20) NOT NULL CONSTRAINT DF_Users_Role DEFAULT (N'User'),
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        FailedLoginCount INT NOT NULL CONSTRAINT DF_Users_FailedLoginCount DEFAULT (0),
        LockoutEndUtc DATETIME NULL,
        LastLoginUtc DATETIME NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (GETDATE()),
        MustChangePassword BIT NOT NULL CONSTRAINT DF_Users_MustChangePassword DEFAULT (0),
        PasswordChangedAtUtc DATETIME NULL
    );
END;

IF OBJECT_ID(N'dbo.Departments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Departments
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Departments_IsActive DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Departments_CreatedAt DEFAULT (GETDATE())
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Departments_Name' AND object_id = OBJECT_ID(N'dbo.Departments'))
    CREATE UNIQUE INDEX IX_Departments_Name ON dbo.Departments(Name);

IF OBJECT_ID(N'dbo.LeaveTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeaveTypes
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL,
        DefaultDays DECIMAL(6,1) NOT NULL CONSTRAINT DF_LeaveTypes_DefaultDays DEFAULT (0),
        RequiresAttachment BIT NOT NULL CONSTRAINT DF_LeaveTypes_RequiresAttachment DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_LeaveTypes_IsActive DEFAULT (1),
        SortOrder INT NOT NULL CONSTRAINT DF_LeaveTypes_SortOrder DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_LeaveTypes_CreatedAt DEFAULT (GETDATE())
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LeaveTypes_Name' AND object_id = OBJECT_ID(N'dbo.LeaveTypes'))
    CREATE UNIQUE INDEX IX_LeaveTypes_Name ON dbo.LeaveTypes(Name);

IF OBJECT_ID(N'dbo.LeaveBalances', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeaveBalances
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId INT NOT NULL,
        LeaveTypeId INT NOT NULL,
        CalendarYear INT NOT NULL,
        BalanceDays DECIMAL(6,1) NOT NULL CONSTRAINT DF_LeaveBalances_BalanceDays DEFAULT (0),
        UsedDays DECIMAL(6,1) NOT NULL CONSTRAINT DF_LeaveBalances_UsedDays DEFAULT (0),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_LeaveBalances_UpdatedAt DEFAULT (GETDATE())
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LeaveBalances_UserTypeYear' AND object_id = OBJECT_ID(N'dbo.LeaveBalances'))
    CREATE UNIQUE INDEX IX_LeaveBalances_UserTypeYear ON dbo.LeaveBalances(UserId, LeaveTypeId, CalendarYear);

IF OBJECT_ID(N'dbo.LeaveRequestHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeaveRequestHistory
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        LeaveRequestId INT NOT NULL,
        ActorUserId INT NULL,
        ActionName NVARCHAR(50) NOT NULL,
        PreviousStatus NVARCHAR(20) NULL,
        NewStatus NVARCHAR(20) NULL,
        Comment NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_LeaveRequestHistory_CreatedAt DEFAULT (GETDATE())
    );
END;

IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemSettings
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SettingKey NVARCHAR(100) NOT NULL,
        SettingValue NVARCHAR(MAX) NULL,
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_SystemSettings_UpdatedAt DEFAULT (GETDATE())
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SystemSettings_Key' AND object_id = OBJECT_ID(N'dbo.SystemSettings'))
    CREATE UNIQUE INDEX IX_SystemSettings_Key ON dbo.SystemSettings(SettingKey);

IF OBJECT_ID(N'dbo.PublicHolidays', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PublicHolidays
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        HolidayDate DATE NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Region NVARCHAR(100) NOT NULL CONSTRAINT DF_PublicHolidays_Region DEFAULT (N'Cambodia'),
        IsActive BIT NOT NULL CONSTRAINT DF_PublicHolidays_IsActive DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PublicHolidays_CreatedAt DEFAULT (GETDATE())
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PublicHolidays_DateNameRegion' AND object_id = OBJECT_ID(N'dbo.PublicHolidays'))
    CREATE UNIQUE INDEX IX_PublicHolidays_DateNameRegion ON dbo.PublicHolidays(HolidayDate, Name, Region);

IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId INT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        LinkUrl NVARCHAR(255) NULL,
        IsRead BIT NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT (0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT (GETDATE())
    );
END;

IF COL_LENGTH(N'dbo.Users', N'PasswordHash') IS NULL
    ALTER TABLE dbo.Users ADD PasswordHash NVARCHAR(256) NULL;
IF COL_LENGTH(N'dbo.Users', N'PasswordSalt') IS NULL
    ALTER TABLE dbo.Users ADD PasswordSalt NVARCHAR(128) NULL;
IF COL_LENGTH(N'dbo.Users', N'FullName') IS NULL
    ALTER TABLE dbo.Users ADD FullName NVARCHAR(200) NOT NULL CONSTRAINT DF_Users_FullName_Migration DEFAULT (N'Employee');
IF COL_LENGTH(N'dbo.Users', N'DepartmentId') IS NULL
    ALTER TABLE dbo.Users ADD DepartmentId INT NULL;
IF COL_LENGTH(N'dbo.Users', N'Department') IS NULL
    ALTER TABLE dbo.Users ADD Department NVARCHAR(100) NOT NULL CONSTRAINT DF_Users_Department_Migration DEFAULT (N'General');
IF COL_LENGTH(N'dbo.Users', N'Role') IS NULL
    ALTER TABLE dbo.Users ADD [Role] NVARCHAR(20) NOT NULL CONSTRAINT DF_Users_Role_Migration DEFAULT (N'User');
IF COL_LENGTH(N'dbo.Users', N'IsActive') IS NULL
    ALTER TABLE dbo.Users ADD IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive_Migration DEFAULT (1);
IF COL_LENGTH(N'dbo.Users', N'FailedLoginCount') IS NULL
    ALTER TABLE dbo.Users ADD FailedLoginCount INT NOT NULL CONSTRAINT DF_Users_FailedLoginCount_Migration DEFAULT (0);
IF COL_LENGTH(N'dbo.Users', N'LockoutEndUtc') IS NULL
    ALTER TABLE dbo.Users ADD LockoutEndUtc DATETIME NULL;
IF COL_LENGTH(N'dbo.Users', N'LastLoginUtc') IS NULL
    ALTER TABLE dbo.Users ADD LastLoginUtc DATETIME NULL;
IF COL_LENGTH(N'dbo.Users', N'CreatedAt') IS NULL
    ALTER TABLE dbo.Users ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_Users_CreatedAt_Migration DEFAULT (GETDATE());
IF COL_LENGTH(N'dbo.Users', N'MustChangePassword') IS NULL
    ALTER TABLE dbo.Users ADD MustChangePassword BIT NOT NULL CONSTRAINT DF_Users_MustChangePassword_Migration DEFAULT (0);
IF COL_LENGTH(N'dbo.Users', N'PasswordChangedAtUtc') IS NULL
    ALTER TABLE dbo.Users ADD PasswordChangedAtUtc DATETIME NULL;

IF COL_LENGTH(N'dbo.Users', N'Password') IS NOT NULL
BEGIN
    DECLARE @passwordNullable BIT = 0;
    SELECT @passwordNullable = c.is_nullable
    FROM sys.columns c
    WHERE c.object_id = OBJECT_ID(N'dbo.Users') AND c.name = N'Password';

    IF @passwordNullable = 0
        ALTER TABLE dbo.Users ALTER COLUMN [Password] NVARCHAR(255) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_Username' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE UNIQUE INDEX IX_Users_Username ON dbo.Users(Username);

IF OBJECT_ID(N'dbo.LeaveRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeaveRequests
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId INT NOT NULL,
        LeaveTypeId INT NULL,
        LeaveType NVARCHAR(50) NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NOT NULL,
        Reason NVARCHAR(500) NULL,
        AttachmentFileName NVARCHAR(255) NULL,
        AttachmentPath NVARCHAR(255) NULL,
        [Status] NVARCHAR(20) NOT NULL CONSTRAINT DF_LeaveRequests_Status DEFAULT (N'Pending'),
        ReviewedByUserId INT NULL,
        ReviewedAt DATETIME NULL,
        ReviewComment NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_LeaveRequests_CreatedAt DEFAULT (GETDATE())
    );
END;

IF COL_LENGTH(N'dbo.LeaveRequests', N'LeaveTypeId') IS NULL
    ALTER TABLE dbo.LeaveRequests ADD LeaveTypeId INT NULL;
IF COL_LENGTH(N'dbo.LeaveRequests', N'Reason') IS NULL
    ALTER TABLE dbo.LeaveRequests ADD Reason NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.LeaveRequests', N'AttachmentFileName') IS NULL
    ALTER TABLE dbo.LeaveRequests ADD AttachmentFileName NVARCHAR(255) NULL;
IF COL_LENGTH(N'dbo.LeaveRequests', N'AttachmentPath') IS NULL
    ALTER TABLE dbo.LeaveRequests ADD AttachmentPath NVARCHAR(255) NULL;
IF COL_LENGTH(N'dbo.LeaveRequests', N'ReviewedByUserId') IS NULL
    ALTER TABLE dbo.LeaveRequests ADD ReviewedByUserId INT NULL;
IF COL_LENGTH(N'dbo.LeaveRequests', N'ReviewedAt') IS NULL
    ALTER TABLE dbo.LeaveRequests ADD ReviewedAt DATETIME NULL;
IF COL_LENGTH(N'dbo.LeaveRequests', N'ReviewComment') IS NULL
    ALTER TABLE dbo.LeaveRequests ADD ReviewComment NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.LeaveRequests', N'CreatedAt') IS NULL
    ALTER TABLE dbo.LeaveRequests ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_LeaveRequests_CreatedAt_Migration DEFAULT (GETDATE());
IF COL_LENGTH(N'dbo.LeaveRequests', N'RequestedDays') IS NULL
    ALTER TABLE dbo.LeaveRequests ADD RequestedDays INT NULL;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.LeaveRequests') AND name = N'FK_LeaveRequests_Users'
)
BEGIN
    ALTER TABLE dbo.LeaveRequests WITH CHECK
    ADD CONSTRAINT FK_LeaveRequests_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.LeaveRequests') AND name = N'FK_LeaveRequests_ReviewedBy'
)
BEGIN
    ALTER TABLE dbo.LeaveRequests WITH CHECK
    ADD CONSTRAINT FK_LeaveRequests_ReviewedBy FOREIGN KEY (ReviewedByUserId) REFERENCES dbo.Users(Id);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.Users') AND name = N'FK_Users_Departments'
)
BEGIN
    ALTER TABLE dbo.Users WITH CHECK
    ADD CONSTRAINT FK_Users_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(Id);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.LeaveRequests') AND name = N'FK_LeaveRequests_LeaveTypes'
)
BEGIN
    ALTER TABLE dbo.LeaveRequests WITH CHECK
    ADD CONSTRAINT FK_LeaveRequests_LeaveTypes FOREIGN KEY (LeaveTypeId) REFERENCES dbo.LeaveTypes(Id);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.LeaveBalances') AND name = N'FK_LeaveBalances_Users'
)
BEGIN
    ALTER TABLE dbo.LeaveBalances WITH CHECK
    ADD CONSTRAINT FK_LeaveBalances_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.LeaveBalances') AND name = N'FK_LeaveBalances_LeaveTypes'
)
BEGIN
    ALTER TABLE dbo.LeaveBalances WITH CHECK
    ADD CONSTRAINT FK_LeaveBalances_LeaveTypes FOREIGN KEY (LeaveTypeId) REFERENCES dbo.LeaveTypes(Id);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.LeaveRequestHistory') AND name = N'FK_LeaveRequestHistory_LeaveRequests'
)
BEGIN
    ALTER TABLE dbo.LeaveRequestHistory WITH CHECK
    ADD CONSTRAINT FK_LeaveRequestHistory_LeaveRequests FOREIGN KEY (LeaveRequestId) REFERENCES dbo.LeaveRequests(Id);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.LeaveRequestHistory') AND name = N'FK_LeaveRequestHistory_Users'
)
BEGIN
    ALTER TABLE dbo.LeaveRequestHistory WITH CHECK
    ADD CONSTRAINT FK_LeaveRequestHistory_Users FOREIGN KEY (ActorUserId) REFERENCES dbo.Users(Id);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.Notifications') AND name = N'FK_Notifications_Users'
)
BEGIN
    ALTER TABLE dbo.Notifications WITH CHECK
    ADD CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = N'HolidayCalendarRegion')
    INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, UpdatedAt) VALUES (N'HolidayCalendarRegion', N'Cambodia', GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = N'WeekendSaturdayOff')
    INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, UpdatedAt) VALUES (N'WeekendSaturdayOff', N'1', GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = N'WeekendSundayOff')
    INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, UpdatedAt) VALUES (N'WeekendSundayOff', N'1', GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = N'LeavePolicyTitle')
    INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, UpdatedAt) VALUES (N'LeavePolicyTitle', N'Company Leave Policy', GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = N'LeavePolicyText')
    INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, UpdatedAt) VALUES (N'LeavePolicyText', N'Leave duration is counted using working days only. Cambodia public holidays and configured weekend days are excluded from the calculation.', GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE Name = N'Administration')
    INSERT INTO dbo.Departments (Name, IsActive, CreatedAt) VALUES (N'Administration', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE Name = N'General')
    INSERT INTO dbo.Departments (Name, IsActive, CreatedAt) VALUES (N'General', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE Name = N'Human Resources')
    INSERT INTO dbo.Departments (Name, IsActive, CreatedAt) VALUES (N'Human Resources', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE Name = N'Finance')
    INSERT INTO dbo.Departments (Name, IsActive, CreatedAt) VALUES (N'Finance', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE Name = N'Operations')
    INSERT INTO dbo.Departments (Name, IsActive, CreatedAt) VALUES (N'Operations', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE Name = N'IT')
    INSERT INTO dbo.Departments (Name, IsActive, CreatedAt) VALUES (N'IT', 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.LeaveTypes WHERE Name = N'Annual Leave')
    INSERT INTO dbo.LeaveTypes (Name, DefaultDays, RequiresAttachment, IsActive, SortOrder, CreatedAt) VALUES (N'Annual Leave', 18, 0, 1, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.LeaveTypes WHERE Name = N'Sick Leave')
    INSERT INTO dbo.LeaveTypes (Name, DefaultDays, RequiresAttachment, IsActive, SortOrder, CreatedAt) VALUES (N'Sick Leave', 10, 1, 1, 2, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.LeaveTypes WHERE Name = N'Personal Leave')
    INSERT INTO dbo.LeaveTypes (Name, DefaultDays, RequiresAttachment, IsActive, SortOrder, CreatedAt) VALUES (N'Personal Leave', 5, 0, 1, 3, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.LeaveTypes WHERE Name = N'Emergency Leave')
    INSERT INTO dbo.LeaveTypes (Name, DefaultDays, RequiresAttachment, IsActive, SortOrder, CreatedAt) VALUES (N'Emergency Leave', 3, 0, 1, 4, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.LeaveTypes WHERE Name = N'Unpaid Leave')
    INSERT INTO dbo.LeaveTypes (Name, DefaultDays, RequiresAttachment, IsActive, SortOrder, CreatedAt) VALUES (N'Unpaid Leave', 0, 0, 1, 5, GETDATE());

INSERT INTO dbo.Departments (Name, IsActive, CreatedAt)
SELECT DISTINCT LTRIM(RTRIM(u.Department)), 1, GETDATE()
FROM dbo.Users u
WHERE LTRIM(RTRIM(ISNULL(u.Department, N''))) <> N''
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.Departments d
      WHERE d.Name = LTRIM(RTRIM(u.Department))
  );

UPDATE u
SET u.DepartmentId = d.Id,
    u.Department = d.Name
FROM dbo.Users u
INNER JOIN dbo.Departments d ON d.Name = LTRIM(RTRIM(u.Department))
WHERE u.DepartmentId IS NULL OR u.Department <> d.Name;

INSERT INTO dbo.LeaveTypes (Name, DefaultDays, RequiresAttachment, IsActive, SortOrder, CreatedAt)
SELECT DISTINCT LTRIM(RTRIM(lr.LeaveType)), 0, 0, 1, 100, GETDATE()
FROM dbo.LeaveRequests lr
WHERE LTRIM(RTRIM(ISNULL(lr.LeaveType, N''))) <> N''
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.LeaveTypes lt
      WHERE lt.Name = LTRIM(RTRIM(lr.LeaveType))
  );

UPDATE lr
SET lr.LeaveTypeId = lt.Id,
    lr.LeaveType = lt.Name
FROM dbo.LeaveRequests lr
INNER JOIN dbo.LeaveTypes lt ON lt.Name = LTRIM(RTRIM(lr.LeaveType))
WHERE lr.LeaveTypeId IS NULL OR lr.LeaveType <> lt.Name;

IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-01-01' AND Name = N'International New Year''s Day' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-01-01', N'International New Year''s Day', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-01-07' AND Name = N'Victory Day over Genocide' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-01-07', N'Victory Day over Genocide', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-03-08' AND Name = N'International Women''s Day' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-03-08', N'International Women''s Day', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-04-14' AND Name = N'Khmer New Year' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-04-14', N'Khmer New Year', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-04-15' AND Name = N'Khmer New Year' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-04-15', N'Khmer New Year', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-04-16' AND Name = N'Khmer New Year' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-04-16', N'Khmer New Year', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-05-01' AND Name = N'International Labour Day' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-05-01', N'International Labour Day', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-05-01' AND Name = N'Visak Bochea Day' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-05-01', N'Visak Bochea Day', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-05-05' AND Name = N'Royal Ploughing Ceremony' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-05-05', N'Royal Ploughing Ceremony', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-05-14' AND Name = N'King Norodom Sihamoni''s Birthday' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-05-14', N'King Norodom Sihamoni''s Birthday', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-06-18' AND Name = N'Queen Mother Norodom Monineath Sihanouk''s Birthday' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-06-18', N'Queen Mother Norodom Monineath Sihanouk''s Birthday', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-09-24' AND Name = N'Constitutional Day' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-09-24', N'Constitutional Day', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-10-10' AND Name = N'Pchum Ben Festival' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-10-10', N'Pchum Ben Festival', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-10-11' AND Name = N'Pchum Ben Festival' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-10-11', N'Pchum Ben Festival', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-10-12' AND Name = N'Pchum Ben Festival' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-10-12', N'Pchum Ben Festival', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-10-15' AND Name = N'Commemoration Day of the King Father (Norodom Sihanouk)' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-10-15', N'Commemoration Day of the King Father (Norodom Sihanouk)', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-10-29' AND Name = N'King Norodom Sihamoni''s Coronation Day' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-10-29', N'King Norodom Sihamoni''s Coronation Day', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-11-09' AND Name = N'National Independence Day' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-11-09', N'National Independence Day', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-11-23' AND Name = N'Water Festival (Bon Om Touk)' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-11-23', N'Water Festival (Bon Om Touk)', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-11-24' AND Name = N'Water Festival (Bon Om Touk)' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-11-24', N'Water Festival (Bon Om Touk)', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-11-25' AND Name = N'Water Festival (Bon Om Touk)' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-11-25', N'Water Festival (Bon Om Touk)', N'Cambodia', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.PublicHolidays WHERE HolidayDate = '2026-12-29' AND Name = N'Peace Day in Cambodia' AND Region = N'Cambodia')
    INSERT INTO dbo.PublicHolidays (HolidayDate, Name, Region, IsActive, CreatedAt) VALUES ('2026-12-29', N'Peace Day in Cambodia', N'Cambodia', 1, GETDATE());

UPDATE dbo.LeaveRequests
SET RequestedDays = DATEDIFF(DAY, StartDate, EndDate) + 1
WHERE RequestedDays IS NULL OR RequestedDays <= 0;

INSERT INTO dbo.LeaveBalances (UserId, LeaveTypeId, CalendarYear, BalanceDays, UsedDays, UpdatedAt)
SELECT
    u.Id,
    lt.Id,
    YEAR(GETDATE()),
    lt.DefaultDays,
    CASE
        WHEN lt.Name = N'Annual Leave' THEN
            CAST
            (
                ISNULL
                (
                    (
                        SELECT SUM(ISNULL(lr.RequestedDays, DATEDIFF(DAY, lr.StartDate, lr.EndDate) + 1))
                        FROM dbo.LeaveRequests lr
                        WHERE lr.UserId = u.Id
                          AND lr.LeaveTypeId = lt.Id
                          AND lr.Status = N'Approved'
                          AND YEAR(lr.StartDate) = YEAR(GETDATE())
                    ),
                    0
                ) AS DECIMAL(6,1)
            )
        ELSE
            CAST
            (
                ISNULL
                (
                    (
                        SELECT SUM(ISNULL(lr.RequestedDays, DATEDIFF(DAY, lr.StartDate, lr.EndDate) + 1))
                        FROM dbo.LeaveRequests lr
                        WHERE lr.UserId = u.Id
                          AND lr.LeaveTypeId = lt.Id
                          AND lr.Status = N'Approved'
                          AND YEAR(lr.StartDate) = YEAR(GETDATE())
                    ),
                    0
                ) AS DECIMAL(6,1)
            )
    END,
    GETDATE()
FROM dbo.Users u
CROSS JOIN dbo.LeaveTypes lt
WHERE u.IsActive = 1
  AND lt.IsActive = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.LeaveBalances lb
      WHERE lb.UserId = u.Id
        AND lb.LeaveTypeId = lt.Id
        AND lb.CalendarYear = YEAR(GETDATE())
  );

IF OBJECT_ID(N'dbo.SecurityAuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SecurityAuditLog
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EventType NVARCHAR(100) NOT NULL,
        UserName NVARCHAR(50) NULL,
        IpAddress NVARCHAR(64) NULL,
        UserAgent NVARCHAR(512) NULL,
        Details NVARCHAR(1000) NULL,
        CreatedAtUtc DATETIME NOT NULL CONSTRAINT DF_SecurityAuditLog_CreatedAtUtc DEFAULT (GETUTCDATE())
    );
END;";
                command.CommandTimeout = 120;
                command.ExecuteNonQuery();
            }
        }

        private static void MigrateLegacyPasswords()
        {
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString))
            {
                connection.Open();

                bool hasLegacyPasswordColumn;
                using (SqlCommand columnCheck = new SqlCommand("SELECT CASE WHEN COL_LENGTH(N'dbo.Users', N'Password') IS NULL THEN 0 ELSE 1 END", connection))
                {
                    hasLegacyPasswordColumn = Convert.ToInt32(columnCheck.ExecuteScalar()) == 1;
                }

                if (!hasLegacyPasswordColumn)
                {
                    return;
                }

                DataTable users = new DataTable();
                using (SqlCommand command = new SqlCommand("SELECT Id, [Password] FROM dbo.Users WHERE [Password] IS NOT NULL AND (PasswordHash IS NULL OR PasswordSalt IS NULL)", connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    users.Load(reader);
                }

                foreach (DataRow row in users.Rows)
                {
                    string legacyPassword = Convert.ToString(row["Password"]);
                    string salt = PasswordHasher.GenerateSalt();
                    string hash = PasswordHasher.HashPassword(legacyPassword, salt);

                    using (SqlCommand updateCommand = new SqlCommand("UPDATE dbo.Users SET PasswordHash = @PasswordHash, PasswordSalt = @PasswordSalt, [Password] = NULL WHERE Id = @Id", connection))
                    {
                        updateCommand.Parameters.AddWithValue("@PasswordHash", hash);
                        updateCommand.Parameters.AddWithValue("@PasswordSalt", salt);
                        updateCommand.Parameters.AddWithValue("@Id", Convert.ToInt32(row["Id"]));
                        updateCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void EnsureBootstrapAdmin()
        {
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString))
            using (SqlCommand countCommand = new SqlCommand("SELECT COUNT(1) FROM dbo.Users", connection))
            {
                connection.Open();
                if (Convert.ToInt32(countCommand.ExecuteScalar()) > 0)
                {
                    return;
                }
            }

            string password = GenerateBootstrapPassword();
            EnsureUser("admin", password, "System Administrator", "Administration", "Admin", true);
            WriteBootstrapCredentialFile(password);
        }

        private static void EnsureUser(string username, string password, string fullName, string department, string role, bool mustChangePassword)
        {
            string salt = PasswordHasher.GenerateSalt();
            string hash = PasswordHasher.HashPassword(password, salt);

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = @"
IF EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username)
BEGIN
    DECLARE @DepartmentId INT;
    SELECT TOP 1 @DepartmentId = Id FROM dbo.Departments WHERE Name = @Department;

    UPDATE dbo.Users
    SET PasswordHash = CASE WHEN PasswordHash IS NULL OR PasswordSalt IS NULL THEN @PasswordHash ELSE PasswordHash END,
        PasswordSalt = CASE WHEN PasswordHash IS NULL OR PasswordSalt IS NULL THEN @PasswordSalt ELSE PasswordSalt END,
        FullName = @FullName,
        DepartmentId = @DepartmentId,
        Department = @Department,
        [Role] = @Role,
        IsActive = 1,
        MustChangePassword = CASE WHEN PasswordHash IS NULL OR PasswordSalt IS NULL THEN @MustChangePassword ELSE MustChangePassword END
    WHERE Username = @Username;
END
ELSE
BEGIN
    INSERT INTO dbo.Users (Username, PasswordHash, PasswordSalt, FullName, DepartmentId, Department, [Role], IsActive, CreatedAt, MustChangePassword)
    VALUES (@Username, @PasswordHash, @PasswordSalt, @FullName, (SELECT TOP 1 Id FROM dbo.Departments WHERE Name = @Department), @Department, @Role, 1, GETDATE(), @MustChangePassword);
END";
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@PasswordHash", hash);
                command.Parameters.AddWithValue("@PasswordSalt", salt);
                command.Parameters.AddWithValue("@FullName", fullName);
                command.Parameters.AddWithValue("@Department", department);
                command.Parameters.AddWithValue("@Role", role);
                command.Parameters.AddWithValue("@MustChangePassword", mustChangePassword);
                command.ExecuteNonQuery();
            }
        }

        private static void EnsureBootstrapAdminCredentialLifecycle()
        {
            string appDataPath = HostingEnvironment.MapPath("~/App_Data");
            string filePath = string.IsNullOrWhiteSpace(appDataPath) ? null : Path.Combine(appDataPath, "bootstrap-admin.txt");
            bool bootstrapCredentialFileExists = !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath);

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = @"
UPDATE dbo.Users
SET MustChangePassword = CASE WHEN PasswordChangedAtUtc IS NULL THEN 1 ELSE MustChangePassword END
WHERE Username = @Username
  AND IsActive = 1;";
                command.Parameters.AddWithValue("@Username", AuthorizationHelper.BootstrapAdminUsername);
                command.ExecuteNonQuery();

                command.Parameters.Clear();
                command.CommandText = @"
SELECT TOP 1 PasswordChangedAtUtc
FROM dbo.Users
WHERE Username = @Username;";
                command.Parameters.AddWithValue("@Username", AuthorizationHelper.BootstrapAdminUsername);
                object passwordChangedAtUtc = command.ExecuteScalar();

                if (bootstrapCredentialFileExists && passwordChangedAtUtc != null && passwordChangedAtUtc != DBNull.Value)
                {
                    File.Delete(filePath);
                }
            }
        }

        private static string GenerateBootstrapPassword()
        {
            char[] chars = new char[18];
            byte[] bytes = new byte[chars.Length];

            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(bytes);
            }

            for (int index = 0; index < chars.Length; index++)
            {
                chars[index] = PasswordCharset[bytes[index] % PasswordCharset.Length];
            }

            return new string(chars);
        }

        private static void WriteBootstrapCredentialFile(string password)
        {
            string appDataPath = HostingEnvironment.MapPath("~/App_Data");
            if (string.IsNullOrWhiteSpace(appDataPath))
            {
                return;
            }

            Directory.CreateDirectory(appDataPath);
            string filePath = Path.Combine(appDataPath, "bootstrap-admin.txt");
            if (File.Exists(filePath))
            {
                return;
            }

            File.WriteAllText(
                filePath,
                "Bootstrap administrator account created." + Environment.NewLine +
                "Username: admin" + Environment.NewLine +
                "Temporary password: " + password + Environment.NewLine +
                "This file is temporary and should be removed automatically after the bootstrap admin changes password.");
        }
    }
}
