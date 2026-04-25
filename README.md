# Online Leave Management System

An ASP.NET Web Forms leave management application for handling employee leave requests, approvals, balances, reporting, notifications, and company leave settings.

## Overview

This project is built with:

- ASP.NET Web Forms
- C# on .NET Framework 4.7.2
- ADO.NET with SQL Server
- Forms Authentication with role-based access control

The application supports both employee self-service workflows and administrator or HR operations in a single system.

## Main Features

- Secure login and password change flow
- Bootstrap admin creation for first-time setup
- Role-based access for Employee, HR, Department Admin, and Admin
- Leave request submission with reason and attachment support
- Working-day calculation that respects holidays and weekend settings
- Leave balance tracking
- Request approval and rejection with review comments
- Employee leave history view
- Reporting with filterable exports to CSV, PDF, and DOCX
- Company settings for departments, leave types, and holidays
- User management and password reset flows
- Notification and audit logging features
- Secure attachment download authorization

## Project Structure

- [OnlineLeaveManagementSystem](/D:/NUM/ASP/OnlineLeaveManagementSystem/OnlineLeaveManagementSystem)
  Main Web Forms application
- [OnlineLeaveManagementSystem/Data](/D:/NUM/ASP/OnlineLeaveManagementSystem/OnlineLeaveManagementSystem/Data)
  Repository and database access classes
- [OnlineLeaveManagementSystem/Infrastructure](/D:/NUM/ASP/OnlineLeaveManagementSystem/OnlineLeaveManagementSystem/Infrastructure)
  Database initialization and export helpers
- [OnlineLeaveManagementSystem/Security](/D:/NUM/ASP/OnlineLeaveManagementSystem/OnlineLeaveManagementSystem/Security)
  Authentication, authorization, password hashing, and audit logic
- [OnlineLeaveManagementSystem/Content](/D:/NUM/ASP/OnlineLeaveManagementSystem/OnlineLeaveManagementSystem/Content)
  Shared CSS styles
- [OnlineLeaveManagementSystem/Scripts](/D:/NUM/ASP/OnlineLeaveManagementSystem/OnlineLeaveManagementSystem/Scripts)
  Shared client-side JavaScript
- [OnlineLeaveManagementSystem_Features.md](/D:/NUM/ASP/OnlineLeaveManagementSystem/OnlineLeaveManagementSystem_Features.md)
  Extended feature-by-feature documentation

## Requirements

- Windows
- Visual Studio with ASP.NET and web development support
- .NET Framework 4.7.2 targeting pack
- SQL Server or SQL Server Express
- NuGet package restore enabled

## Important Build Note

This is a classic ASP.NET Web Application project, not an ASP.NET Core project.

It depends on `Microsoft.WebApplication.targets`, so it should be built and run with Visual Studio or a machine that has the full Web Application build targets installed. A plain `dotnet build` environment is usually not enough on its own.

## Setup

1. Clone the repository.
2. Open [OnlineLeaveManagementSystem.slnx](/D:/NUM/ASP/OnlineLeaveManagementSystem/OnlineLeaveManagementSystem.slnx) in Visual Studio.
3. Restore NuGet packages.
4. Copy [Web.config.example](/D:/NUM/ASP/OnlineLeaveManagementSystem/OnlineLeaveManagementSystem/Web.config.example) to `OnlineLeaveManagementSystem/Web.config`.
5. Update the `LeaveManagementConnection` connection string.
6. Run the web project with IIS Express or your local IIS profile.

## Database Behavior

The application initializes its database automatically on first use through [DbHelper.cs](/D:/NUM/ASP/OnlineLeaveManagementSystem/OnlineLeaveManagementSystem/Data/DbHelper.cs:8) and [DatabaseInitializer.cs](/D:/NUM/ASP/OnlineLeaveManagementSystem/OnlineLeaveManagementSystem/Infrastructure/DatabaseInitializer.cs:10).

On startup it can:

- create the database if it does not already exist
- create tables and supporting schema
- migrate some legacy data scenarios
- create a bootstrap `admin` account when the system has no users

## First Login

On a brand-new system, a bootstrap admin account is created automatically if no users exist.

- Username: `admin`
- Password: generated automatically and stored temporarily in `App_Data/bootstrap-admin.txt`

After the first successful password change, that temporary password file is removed.

## Authentication and Security

- Passwords are stored using hashing logic, not plain text
- Forms authentication protects the application
- Anonymous users are denied by default except where explicitly allowed
- Secure attachment downloads are permission-checked before serving files
- Reviewers cannot approve or reject their own leave requests
- The bootstrap `admin` account is reserved for setup and recovery, not normal employee leave submission

## Reports and Exports

The reporting area supports:

- filtering by status, department, leave type, date range, and search text
- on-screen pagination for large result sets
- export of the full filtered result set to CSV, PDF, and DOCX

## Configuration Notes

- Sample configuration is provided in [Web.config.example](/D:/NUM/ASP/OnlineLeaveManagementSystem/OnlineLeaveManagementSystem/Web.config.example)
- The project currently references `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` version `2.0.1`
- Default document routing points to `Login.aspx`
- Maximum upload size is configured in `Web.config`

## Useful Pages

- `Login.aspx`
- `Dashboard.aspx`
- `ApplyLeave.aspx`
- `MyLeaves.aspx`
- `ManageRequests.aspx`
- `LeaveReports.aspx`
- `ManageUsers.aspx`
- `CompanySettings.aspx`
- `Notifications.aspx`

## Notes for Developers

- Data access is centralized in repository-style classes under `Data`
- UI pages are implemented with `.aspx`, `.aspx.cs`, and `.designer.cs` files
- Shared page layout is defined in [Site.Master](/D:/NUM/ASP/OnlineLeaveManagementSystem/OnlineLeaveManagementSystem/Site.Master)
- Styling is primarily in [site.css](/D:/NUM/ASP/OnlineLeaveManagementSystem/OnlineLeaveManagementSystem/Content/site.css)

## Related Documentation

For a fuller explanation of the business flows and feature behavior, see [OnlineLeaveManagementSystem_Features.md](/D:/NUM/ASP/OnlineLeaveManagementSystem/OnlineLeaveManagementSystem_Features.md).
