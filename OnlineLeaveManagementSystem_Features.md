# Online Leave Management System - Detailed Feature Documentation

This document explains the full feature set of the Online Leave Management System in a clear and presentation-friendly way. It is written to help explain the project to a teacher, examiner, or stakeholder, including both the visible features and the important validation and security logic behind them.

## 1. Project Purpose

The Online Leave Management System is a web-based application for managing employee leave requests inside an organization. It supports:

- employee self-service leave submission
- manager, HR, and admin review workflows
- leave balance tracking
- policy configuration
- reporting and export
- secure authentication and role-based access

The goal of the system is not only to store leave records, but also to enforce real business rules such as leave eligibility, working-day calculation, department-level authority, and secure document handling.

## 2. Technical Architecture Summary

### Framework and Stack

- Built with ASP.NET Web Forms
- Backend written in C#
- Uses .NET Framework 4.7.2
- Uses SQL Server for data storage
- Uses ADO.NET with direct SQL commands

### Data Access Design

The project separates page UI logic from database logic.

- UI pages are implemented with `.aspx`, `.aspx.cs`, and `.designer.cs`
- Repository-style classes in the `Data` folder handle database access
- Security logic is grouped under the `Security` folder
- Infrastructure tasks such as database initialization and report generation are grouped under `Infrastructure`

This design makes the project easier to maintain and explain because each layer has a clear responsibility.

## 3. Database Initialization and First-Time System Setup

### Files Involved

- `Web.config`
- `Data/DbHelper.cs`
- `Infrastructure/DatabaseInitializer.cs`

### How It Works

Whenever the application opens a database connection, `DbHelper` first ensures that the database exists and is ready. The `DatabaseInitializer` automatically:

- creates the database if it does not exist
- creates required tables
- creates indexes and foreign keys
- seeds default departments
- seeds default leave types
- seeds default system settings
- seeds public holidays
- migrates certain legacy data scenarios

### First-Time Bootstrap Admin

When the system has zero users, it automatically creates a bootstrap `admin` account.

- username: `admin`
- password: generated automatically
- temporary password stored in `App_Data/bootstrap-admin.txt`

This solves the first-run setup problem where no one exists yet to log in and create users manually.

### Security Rule for Bootstrap Admin

The bootstrap admin account is treated as:

- a setup account
- an emergency recovery account
- not a normal day-to-day employee account

The bootstrap admin is forced to change password on first login, and the temporary password file is removed after the password is changed.

## 4. Authentication, Login, Password Change, and Lockout

### Files Involved

- `Login.aspx` / `Login.aspx.cs`
- `ChangePassword.aspx` / `ChangePassword.aspx.cs`
- `Security/AuthManager.cs`
- `Security/PasswordHasher.cs`
- `Security/AuthenticatedPage.cs`

### Login Process

When a user signs in:

1. the system checks whether username and password are provided
2. it looks up the account in the database
3. it checks whether the account is active
4. it checks whether the account is temporarily locked
5. it verifies the password using hashed password comparison
6. it resets the failed login counter if login succeeds
7. it creates the authenticated session

### Password Storage

Passwords are not stored as plain text.

- each password is hashed
- a salt is generated and stored
- login checks use password verification instead of direct text comparison

### Failed Login Protection

The system includes account lockout logic:

- after 5 failed login attempts, the account is locked
- lockout duration is 15 minutes
- successful login resets the failed login count

### Must-Change-Password Flow

Some accounts, especially newly created accounts or reset accounts, are forced to change password before continuing.

This applies to:

- bootstrap admin on first login
- newly created users with temporary passwords
- users whose password was reset by an admin

## 5. Security Configuration Summary

This section summarizes the main security rules configured in the current system. It focuses on policy and expected behavior rather than low-level implementation details.

### Bootstrap Admin Protection

- the bootstrap `admin` account is reserved for first-run setup and emergency recovery
- it is not intended to be used as a normal employee or day-to-day admin identity
- it must change its password on first login
- it cannot submit leave requests
- it cannot be disabled
- its role cannot be changed
- its profile information and department cannot be changed

### Authentication and Session Security

- all authenticated pages require login unless explicitly allowed
- passwords are stored as salted hashes, not plain text
- failed login attempts trigger temporary account lockout
- users with temporary or reset passwords must change password before normal access continues

### Access Control Rules

- permissions are enforced by role, not just by page visibility
- `Admin`, `HR`, and `DepartmentAdmin` have different access scopes
- department-based restrictions mainly apply to `DepartmentAdmin`
- only the bootstrap admin can change another admin's role
- only the bootstrap admin can deactivate another admin account
- an admin cannot disable their own account
- an admin cannot remove their own admin role

### Administrative Safety Rules

- at least one active admin account must always remain in the system
- the system recommends keeping at least two active named admin accounts
- password reset is used for admin recovery instead of changing protected admin ownership rules

### Data and File Protection

- attachment downloads are authorized before a file is returned
- uploaded leave evidence is stored outside casual public access patterns
- department and role rules are checked again in backend logic, not only in the UI

### Monitoring and Accountability

- important security actions are written to the audit log
- examples include login events, password resets, account changes, and leave review actions

## 6. Role-Based Access Control

### Files Involved

- `Security/AuthorizationHelper.cs`
- `Site.Master.cs`
- page-level `AllowedRoles` definitions

### Supported Roles

- `Admin`
- `HR`
- `DepartmentAdmin`
- `User`

### Permission Model

The system clearly separates:

- `Department`: the employee's organizational team
- `Role`: the employee's system permission level

This is an important business design decision.

### Role Responsibilities

#### Admin

- full system access
- manage users
- access policy catalog
- access company settings
- review leave requests for all departments
- access reports for all departments

#### HR

- review leave requests across all departments
- view reports across all departments
- cannot manage users
- cannot access admin-only company settings or policy catalog

#### DepartmentAdmin

- review leave requests only for their own department
- view reports only for their own department
- cannot manage users
- cannot access admin-only company settings or policy catalog

#### User

- apply for leave
- view own leave history
- view own leave balances
- receive notifications
- cannot review other employees' requests

### Important Scope Rule

Departments do not automatically give permissions.

For example:

- a user in `Administration` with role `User` is still just a normal employee
- a user in `IT` with role `Admin` is a full system admin
- a user in `Human Resources` with role `HR` can work across departments because the role grants that authority

## 7. Dashboard

### Files Involved

- `Dashboard.aspx`
- `Dashboard.aspx.cs`

### Current Behavior

The dashboard is the landing page after login. It currently shows:

- total leave requests
- pending requests
- approved requests
- active users
- recent leave requests

### Presentation Note

The dashboard currently displays organization-level summary data. This behavior is useful for admins and reviewers, but for a future refinement, the dashboard can be made role-aware so normal users only see personal data.

## 8. User and Account Management

### Files Involved

- `ManageUsers.aspx`
- `ManageUsers.aspx.cs`
- `Security/UserAccountManager.cs`

### Main Functions

Admins can:

- create users
- assign department
- assign role
- activate or deactivate users
- update full name
- reset passwords
- view user summary metrics
- filter users by department or search text

### Important Validation Rules

- username must be unique
- username cannot contain spaces
- full name is required
- department is required
- role must be valid
- an admin cannot disable their own account
- an admin cannot remove their own admin role
- the bootstrap admin account cannot be disabled
- the bootstrap admin user profile cannot be changed
- the bootstrap admin department cannot be changed
- the bootstrap admin role cannot be changed
- only the bootstrap admin can change another admin's role
- only the bootstrap admin can deactivate another admin account

### Admin Continuity Protection

The system protects admin continuity by enforcing:

- at least one active admin must remain
- recommended minimum of two active named admins

This supports recovery if one admin forgets a password or leaves the organization.

### Password Reset Behavior

When an admin resets a user password:

- the new temporary password is stored as a new secure hash
- the user is forced to change password on next login
- lockout state is cleared

## 9. Policy Catalog

### Files Involved

- `PolicyCatalog.aspx`
- `PolicyCatalog.aspx.cs`
- `Data/LeaveManagementRepository.cs`

### Why It Exists

This page was split out from `ManageUsers` to reduce page complexity and separate policy setup from user account administration.

### Functions

Admins can use the Policy Catalog to manage:

- departments
- leave types

### Department Management

Admins can:

- create departments
- rename departments
- activate or deactivate departments

When a department name changes:

- linked user department names are also updated

### Leave Type Management

Admins can:

- create leave types
- configure default days
- configure attachment requirement
- activate or deactivate leave types
- change sort order

### Protected System Leave Type: Unpaid Leave

`Unpaid Leave` is treated as a special system leave type.

It is now protected so that admins cannot:

- rename it
- deactivate it
- change its default balance
- change its attachment rule

Only its `SortOrder` may be changed.

This prevents accidental damage to the system's fallback leave policy.

## 10. Company Settings

### Files Involved

- `CompanySettings.aspx`
- `CompanySettings.aspx.cs`
- `Data/SystemSettingsRepository.cs`

### Functions

Admins can configure:

- leave policy title
- leave policy text
- holiday calendar region
- whether Saturday counts as weekend
- whether Sunday counts as weekend
- public holidays and their active state

These settings are important because they directly affect leave-day calculation logic.

## 11. Apply for Leave

### Files Involved

- `ApplyLeave.aspx`
- `ApplyLeave.aspx.cs`
- `Data/LeaveManagementRepository.cs`
- `Data/SystemSettingsRepository.cs`

### Basic Workflow

An employee:

1. chooses a leave type
2. enters start date and end date
3. writes a reason if needed
4. uploads an attachment if required
5. submits the request

The system then:

- validates the input
- calculates the number of working leave days
- checks leave balance
- saves the request
- saves attachment metadata if applicable
- inserts request history
- notifies reviewers

### Validation and Business Rules

This is one of the most important parts of the system.

The application enforces all of the following:

- the user must be signed in
- the bootstrap admin account cannot apply for leave
- end date cannot be earlier than start date
- date range must contain at least one working leave day
- weekends do not count as leave days if configured as weekends
- public holidays do not count as leave days
- some leave types require an attachment
- inactive leave types cannot be used
- paid leave requests cannot exceed remaining balance
- if balance is insufficient, the system instructs the user to use `Unpaid Leave`

### Working-Day Calculation

The system does not simply count calendar days.

It loops through the date range and excludes:

- configured weekend days
- public holidays from the selected holiday region

This means leave is calculated using real working days.

### Non-Working-Day Protection

If the selected range contains no valid working leave days after weekend and holiday exclusion, the request is rejected. This prevents users from submitting leave entirely on non-working days.

### Date Window and Backdate Rules

The `ApplyLeave` page also includes date validation rules to prevent unrealistic or policy-breaking submissions.

The system supports:

- leave requests inside the current leave year
- only limited backdated requests

This helps protect balance accuracy and prevents abuse of very old leave submissions.

### Attachment Upload

Attachments may be uploaded for leave types that require proof, such as medical leave. The application saves:

- the original attachment file name
- a secure internal file reference

The file is not exposed through a public folder path.

## 12. Leave Balance Management

### Files Involved

- `Data/LeaveManagementRepository.cs`

### How It Works

The system maintains a `LeaveBalances` table for each active user and leave type per calendar year.

Balances are created automatically when needed.

For standard leave types:

- `BalanceDays` stores the granted amount
- `UsedDays` stores the approved usage

When a paid request is approved:

- `UsedDays` is increased

When the request is `Unpaid Leave`:

- no paid leave balance is consumed

This allows the system to distinguish clearly between paid and unpaid leave.

## 13. Leave Review, Approval, and Rejection

### Files Involved

- `ManageRequests.aspx`
- `ManageRequests.aspx.cs`
- `Data/LeaveManagementRepository.cs`

### Who Can Review

- `Admin`
- `HR`
- `DepartmentAdmin`

### Review Scope

- `Admin` and `HR` can review across all departments
- `DepartmentAdmin` can review only their own department

### Review Rules

The system enforces the following:

- reviewer must have request-management permission
- department admins cannot review requests outside their department
- reviewers cannot approve or reject their own leave request
- only pending requests can be reviewed
- if approving a paid request, remaining balance is checked again at approval time

That final balance re-check is important because another request may already have consumed part of the balance after the original submission.

### Review Output

On approval or rejection, the system stores:

- new status
- reviewer ID
- review date/time
- review comment
- request history entry

The requester is then notified of the result.

## 14. My Leaves Page

### Files Involved

- `MyLeaves.aspx`
- `MyLeaves.aspx.cs`
- `Data/LeaveManagementRepository.cs`

### Purpose

This page gives each employee a self-service view of their own leave data.

It includes:

- leave balance display
- personal request history
- request status tracking
- review comments
- attachment links where applicable

Users do not use this page to see other employees' requests.

## 15. Notifications

### Files Involved

- `Notifications.aspx`
- `Notifications.aspx.cs`
- `Data/NotificationRepository.cs`

### Purpose

The system provides internal notifications so users do not need to constantly check every page manually.

### Notification Examples

- when a leave request is submitted, reviewers are notified
- when a request status changes, the requester is notified
- users can see their notifications on the Notifications page

DepartmentAdmin notifications are department-aware, so a department admin is notified only for requests from their own department.

## 16. Reports and Export

### Files Involved

- `LeaveReports.aspx`
- `LeaveReports.aspx.cs`
- `Infrastructure/ReportExportBuilder.cs`
- `Data/LeaveManagementRepository.cs`

### Main Functions

Authorized reviewers can:

- filter by request status
- filter by department
- filter by leave type
- filter by date range
- search by employee name, username, leave type, or reason

### Export Formats

The system can export filtered results to:

- CSV
- PDF
- DOCX

This is useful for management review, HR records, and formal submission.

## 17. Secure Attachment Download

### Files Involved

- `SecureAttachmentDownload.aspx`
- `SecureAttachmentDownload.aspx.cs`

### Why It Matters

Leave attachments may contain sensitive information such as medical documents. Because of that, the system does not expose them openly.

Before a file is served, the application verifies that the requesting user is authorized to access it.

This prevents unauthorized access to sensitive documents.

## 18. Security Audit Logging

### Files Involved

- `Security/SecurityAuditLogger.cs`

### Purpose

The system records important security-related events to support accountability and troubleshooting.

Examples include:

- successful login
- failed login
- login blocked due to inactivity
- login blocked due to lockout
- logout
- user creation
- user updates
- password resets
- leave request submission
- request status updates

This provides an audit trail of sensitive system actions.

## 19. Validation Summary for Presentation

If your teacher asks what "smart rules" or "business validations" exist in your project, you can summarize them like this:

- usernames must be unique
- passwords are hashed, not stored as plain text
- accounts lock after repeated failed login attempts
- inactive users cannot log in
- forced password change is enforced for temporary credentials
- bootstrap admin cannot be used as a normal employee leave account
- only valid roles can be assigned
- admins cannot accidentally remove all admin access from the system
- department admins are limited to their own department
- reviewers cannot review their own leave request
- end date cannot be before start date
- leave must contain at least one real working day
- weekends and public holidays are excluded from leave-day calculation
- leave types can require attachment
- insufficient paid balance blocks paid leave submission
- unpaid leave is protected as a special system leave type

## 20. Important Pages in the Current Project

The current project includes these important functional pages:

- `Login.aspx`
- `ChangePassword.aspx`
- `Dashboard.aspx`
- `ApplyLeave.aspx`
- `MyLeaves.aspx`
- `ManageRequests.aspx`
- `LeaveReports.aspx`
- `ManageUsers.aspx`
- `PolicyCatalog.aspx`
- `CompanySettings.aspx`
- `Notifications.aspx`

## 21. Final Project Strengths

The strongest parts of this project are:

- real role-based access control
- detailed business validation
- working-day calculation using holidays and weekends
- secure first-run setup with bootstrap admin
- separation between user management and policy management
- support for unpaid leave as a special controlled case
- reporting and export features
- notification and audit features

This means the system is not just a basic CRUD application. It includes business logic, access control, security design, and real workflow behavior that make it suitable for a realistic leave management scenario.
