# Online Leave Management System - Feature Documentation

This document outlines the core features of the Online Leave Management System, detailing the files responsible for each feature and explaining how the data processing works. This overview is designed to be easily readable and is perfect for presenting to your teacher or instructor.

## 🛠️ Technical Architecture Summary (For Your Presentation)
If your teacher asks how the system is built or processes data overall, you can explain the following:
* **Framework:** Built using **ASP.NET Web Forms** (C# backend with `.aspx` frontend UI).
* **Database Access:** Uses **ADO.NET** (Direct SQL commands) to talk to the SQL database. All database code is cleanly separated into "Repository" classes (like `LeaveManagementRepository.cs`) rather than being mixed into the UI pages.
* **Security & Auth:** Passwords are cryptographically hashed (`PasswordHasher.cs`). A strict Role-Based Access Control (RBAC) system ensures Employees cannot access Admin pages.
* **Smart Processing:** The system doesn't just count days; it intelligently loops through requested dates and checks a database table of company holidays and weekend settings to calculate true "Working Days".

---

## 1. Database Connection, Initialization & Admin Bootstrapping
* **Files Involved:**
  * `Web.config` (Stores the Connection String)
  * `Data/DbHelper.cs` (Connection Management)
  * `Infrastructure/DatabaseInitializer.cs` (Database Creation & Admin Bootstrapping)
* **How it works:** 
  This is the foundation of the system. The connection string (`LeaveManagementConnection`) is securely stored in `Web.config`. Whenever the system needs data, it uses `DbHelper.cs` to open an ADO.NET `SqlConnection`. 
  Before opening the very first connection, `DbHelper` triggers `DatabaseInitializer.cs`. This script automatically checks if the SQL Server database and tables exist—if they don't, it creates them from scratch.
* **Why do we Bootstrap an Admin?** 
  When a system is deployed for the very first time, the database is completely empty. If there are no users, no one can log in, which means no one can access the "Manage Users" page to create employee accounts (a "chicken-and-egg" problem). To solve this, the system uses a technique called "bootstrapping". `DatabaseInitializer` checks if the database has zero users. If it is empty, it automatically creates a default `admin` account with a securely generated 18-character random password. It then saves this password to a hidden text file (`App_Data/bootstrap-admin.txt`) on the server so the system administrator can log in for the very first time and configure the system.
* **Important security rule for the bootstrap admin:**  
  The default `admin` account is treated as a setup and emergency-recovery account, not as a normal employee profile. It should not be shared by many people. Instead, the real people who manage the system should each receive their own named Admin, HR, or Department Admin account. The application now blocks the bootstrap `admin` account from submitting leave requests so the system owner identity stays separate from day-to-day employee activity.
* **Password lifecycle best practice:**  
  The bootstrap `admin` account is created with a temporary password and is forced to change that password on first login. After the bootstrap password is successfully changed, the temporary password file is deleted so that the initial credential does not remain on the server longer than necessary.

## 2. User Authentication & Authorization (Login System)
* **Files Involved:**
  * `Login.aspx` / `Login.aspx.cs` (User Interface & Login Logic)
  * `ChangePassword.aspx` / `ChangePassword.aspx.cs` (Password Management)
  * `Security/AuthManager.cs` (Authentication Logic)
  * `Security/AuthorizationHelper.cs` (Role-based Access)
  * `Security/PasswordHasher.cs` (Security/Encryption)
* **How it works:** 
  When a user logs in, the `AuthManager` verifies their username and password by hashing the input with `PasswordHasher` and comparing it to the database. Upon success, a secure session/token is created. The `AuthorizationHelper` checks the user's role (Employee, Manager, or Admin) to ensure they can only access pages and actions they are permitted to see.
* **Password recovery model:**  
  Password recovery is handled through controlled administrator actions, not through a hardcoded fallback password. The recommended operating model is to keep at least two active named admin accounts so one administrator can reset another admin account if someone forgets a password.

## 3. Apply for Leave (Leave Submission)
* **Files Involved:**
  * `ApplyLeave.aspx` / `ApplyLeave.aspx.cs` (Form Interface)
  * `Data/LeaveManagementRepository.cs` (Database Operations)
* **How it works:**
  An employee selects a date range, leave type (e.g., Annual, Sick), and writes a reason. They can also upload an attachment (like a medical certificate). The system calculates the actual "working days" requested by automatically ignoring weekends and company holidays (fetched via `SystemSettingsRepository`). It checks if the employee has enough leave balance, securely saves the uploaded file to the server, and inserts the leave request into the database with a "Pending" status.
* **Paid vs unpaid leave policy:**  
  Standard leave types still require enough remaining balance. If an employee has no paid balance left, the system guides them to submit `Unpaid Leave` instead of silently overusing paid leave. `Unpaid Leave` is stored as its own request type so that HR and payroll can handle salary deduction or leave-without-pay rules clearly and consistently.
* **Date validation rule:**  
  To keep yearly leave balances accurate, requests must stay inside the current leave year. The system also allows a small backdated window (up to 30 days) so employees can still submit emergency or delayed leave shortly after it happens, but it blocks unrealistic old dates and cross-year ranges that would make the balance display inconsistent.
* **Access control rule:**  
  The bootstrap `admin` account cannot submit leave requests. This keeps the default superuser identity reserved for system setup and recovery, while real staff members use their own named accounts for leave activity.

## 4. Manage Leave Requests (Approval / Rejection)
* **Files Involved:**
  * `ManageRequests.aspx` / `ManageRequests.aspx.cs` (Manager Dashboard Interface)
  * `Data/LeaveManagementRepository.cs` (Database Operations)
* **How it works:**
  Managers and Admins can view a list of all "Pending" requests. They can filter this list by department or search for specific employees. When a manager approves a request, the system calls `UpdateRequestStatus` which permanently deducts the requested days from the employee's `LeaveBalances` table for paid leave types. Unpaid leave requests are approved without consuming paid leave balances. The system also logs the reviewer's comment and creates a history record of the status change.
* **Conflict-of-interest protection:**  
  Reviewers cannot approve or reject their own leave requests. The UI hides the action buttons for self-owned pending requests, and the repository layer also blocks self-approval on the server for safety.

## 5. Employee Leave Tracking (My Leaves)
* **Files Involved:**
  * `MyLeaves.aspx` / `MyLeaves.aspx.cs` (Employee View Interface)
  * `Data/LeaveManagementRepository.cs` (Database Operations)
* **How it works:**
  Employees can view their entire history of leave requests. The page retrieves data specifically filtered for the logged-in user's ID via `GetUserRequests`. It displays the status (Pending, Approved, Rejected) and allows the employee to see any comments left by their manager.

## 6. System Dashboard
* **Files Involved:**
  * `Dashboard.aspx` / `Dashboard.aspx.cs` (Dashboard Interface)
* **How it works:**
  The dashboard serves as the landing page. It aggregates data to display quick summary widgets. For employees, it shows remaining leave balances and recent request statuses. For admins/managers, it displays the total number of pending requests needing attention. 

## 7. Reporting & Data Export
* **Files Involved:**
  * `LeaveReports.aspx` / `LeaveReports.aspx.cs` (Reporting Interface)
  * `Infrastructure/ReportExportBuilder.cs` (File Generation Logic)
  * `Data/LeaveManagementRepository.cs` (Data Retrieval)
* **How it works:**
  Admins and managers can filter leave data by date range, department, leave type, and status. Once filtered, they can export the data. The `ReportExportBuilder` dynamically generates a file in memory—formatting it as a `.csv`, `.pdf`, or `.docx` document—and streams the bytes directly to the user's browser for download.

## 8. Company & System Settings
* **Files Involved:**
  * `CompanySettings.aspx` / `CompanySettings.aspx.cs` (Settings Interface)
  * `Data/SystemSettingsRepository.cs` (Settings Database Logic)
* **How it works:**
  Admins can configure the system's global rules. This includes adding/removing Departments, defining Leave Types (along with their default yearly days), and setting up the company Holiday Calendar. These settings dictate how the system calculates working days when an employee applies for leave.

## 9. User & Account Management
* **Files Involved:**
  * `ManageUsers.aspx` / `ManageUsers.aspx.cs` (Admin Interface)
  * `Security/UserAccountManager.cs` (User Database Logic)
* **How it works:**
  Admins have full control over the employee database. Using the `UserAccountManager`, they can create new employee accounts, assign them to departments, set their roles (Employee, Manager, HR, Department Admin, Admin), and reset passwords if an employee is locked out.
* **Best-practice account model:**  
  The system is designed so multiple people can have elevated access, but each person should use their own named account. The bootstrap `admin` account should remain active for emergency access, but it should not become the shared everyday account for managers or HR staff.
* **Admin continuity protection:**  
  The user-management workflow warns administrators when the system has fewer than two active admin accounts. It also blocks role or activation changes that would reduce the system below the recommended two-admin safety threshold once that level of coverage exists. This supports safer recovery when one administrator forgets a password or leaves the organization.
* **Admin-on-admin control rules:**  
  Named admins can still recover another admin account by resetting its password, but they cannot freely take away another admin's authority. Only the bootstrap `admin` account can change another admin's role or deactivate another admin account. The bootstrap `admin` itself cannot be disabled, which preserves one high-trust emergency owner account for the system.

## 10. Secure File Attachments
* **Files Involved:**
  * `SecureAttachmentDownload.aspx` / `SecureAttachmentDownload.aspx.cs` (Download Handler)
* **How it works:**
  Because leave attachments can contain sensitive medical information, they are not stored in public folders. When a user tries to download an attachment, this page intercepts the request, verifies that the user is either the original requester or an authorized manager/admin, and only then serves the file.

## 11. Notifications & Auditing
* **Files Involved:**
  * `Notifications.aspx` / `Notifications.aspx.cs` (Notifications Interface)
  * `Data/NotificationRepository.cs` (Notification Logic)
  * `Security/SecurityAuditLogger.cs` (Auditing Logic)
* **How it works:**
  The system automatically generates internal alerts. For example, submitting a leave request notifies the managers, and approving a request notifies the employee. Furthermore, `SecurityAuditLogger` silently records critical security events (like logins or status modifications) into an audit table for tracking and compliance.
