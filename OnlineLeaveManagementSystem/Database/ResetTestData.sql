SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DELETE FROM dbo.LeaveRequestHistory;
DELETE FROM dbo.Notifications;
DELETE FROM dbo.LeaveBalances;
DELETE FROM dbo.LeaveRequests;
DELETE FROM dbo.Users;
DELETE FROM dbo.LeaveTypes;
DELETE FROM dbo.Departments;
DELETE FROM dbo.PublicHolidays;
DELETE FROM dbo.SystemSettings;

IF OBJECT_ID(N'dbo.SecurityAuditLog', N'U') IS NOT NULL
BEGIN
    DELETE FROM dbo.SecurityAuditLog;
END;

DBCC CHECKIDENT ('dbo.LeaveRequestHistory', RESEED, 0);
DBCC CHECKIDENT ('dbo.Notifications', RESEED, 0);
DBCC CHECKIDENT ('dbo.LeaveBalances', RESEED, 0);
DBCC CHECKIDENT ('dbo.LeaveRequests', RESEED, 0);
DBCC CHECKIDENT ('dbo.Users', RESEED, 0);
DBCC CHECKIDENT ('dbo.LeaveTypes', RESEED, 0);
DBCC CHECKIDENT ('dbo.Departments', RESEED, 0);
DBCC CHECKIDENT ('dbo.PublicHolidays', RESEED, 0);
DBCC CHECKIDENT ('dbo.SystemSettings', RESEED, 0);

IF OBJECT_ID(N'dbo.SecurityAuditLog', N'U') IS NOT NULL
BEGIN
    DBCC CHECKIDENT ('dbo.SecurityAuditLog', RESEED, 0);
END;

COMMIT TRANSACTION;

PRINT 'All application data has been cleared.';
PRINT 'Next application startup will re-seed defaults and recreate the bootstrap admin account.';
