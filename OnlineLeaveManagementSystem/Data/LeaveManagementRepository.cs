using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using OnlineLeaveManagementSystem.Security;

namespace OnlineLeaveManagementSystem.Data
{
    public static class LeaveManagementRepository
    {
        private const string UnpaidLeaveName = "Unpaid Leave";

        public sealed class PagedDataTableResult
        {
            public DataTable Data { get; set; }
            public int TotalCount { get; set; }
        }

        public sealed class LeaveReportSummaryResult
        {
            public int TotalRecords { get; set; }
            public decimal TotalRequestedDays { get; set; }
            public int PendingCount { get; set; }
            public int ApprovedCount { get; set; }
        }

        public static DataTable GetDepartments(bool includeInactive)
        {
            string query = @"
SELECT
    d.Id,
    d.Name,
    d.IsActive,
    d.CreatedAt,
    (SELECT COUNT(1) FROM dbo.Users u WHERE u.DepartmentId = d.Id AND u.IsActive = 1) AS ActiveUserCount
FROM dbo.Departments";

            if (!includeInactive)
            {
                query += " WHERE IsActive = 1";
            }

            query += " ORDER BY Name ASC;";
            return DbHelper.ExecuteDataTable(query);
        }

        public static void CreateDepartment(string name)
        {
            string normalizedName = NormalizeRequired(name, "Department name");

            try
            {
                DbHelper.ExecuteNonQuery(@"
INSERT INTO dbo.Departments (Name, IsActive, CreatedAt)
VALUES (@Name, 1, GETDATE());",
                    new SqlParameter("@Name", normalizedName));
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                throw new InvalidOperationException("That department already exists.");
            }
        }

        public static void UpdateDepartment(int departmentId, string name, bool isActive)
        {
            string normalizedName = NormalizeRequired(name, "Department name");

            using (SqlConnection connection = DbHelper.GetOpenConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    string previousName;
                    using (SqlCommand selectCommand = new SqlCommand("SELECT TOP 1 Name FROM dbo.Departments WHERE Id = @Id;", connection, transaction))
                    {
                        selectCommand.Parameters.AddWithValue("@Id", departmentId);
                        object result = selectCommand.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            throw new InvalidOperationException("That department could not be found.");
                        }

                        previousName = Convert.ToString(result);
                    }

                    using (SqlCommand updateCommand = new SqlCommand(@"
UPDATE dbo.Departments
SET Name = @Name,
    IsActive = @IsActive
WHERE Id = @Id;", connection, transaction))
                    {
                        updateCommand.Parameters.AddWithValue("@Id", departmentId);
                        updateCommand.Parameters.AddWithValue("@Name", normalizedName);
                        updateCommand.Parameters.AddWithValue("@IsActive", isActive);
                        updateCommand.ExecuteNonQuery();
                    }

                    if (!string.Equals(previousName, normalizedName, StringComparison.OrdinalIgnoreCase))
                    {
                        using (SqlCommand userCommand = new SqlCommand(@"
UPDATE dbo.Users
SET Department = @NewDepartment
WHERE DepartmentId = @DepartmentId;", connection, transaction))
                        {
                            userCommand.Parameters.AddWithValue("@DepartmentId", departmentId);
                            userCommand.Parameters.AddWithValue("@NewDepartment", normalizedName);
                            userCommand.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
                catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
                {
                    transaction.Rollback();
                    throw new InvalidOperationException("That department already exists.");
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public static DataTable GetLeaveTypes(bool includeInactive)
        {
            string query = @"
SELECT
    lt.Id,
    lt.Name,
    lt.DefaultDays,
    lt.RequiresAttachment,
    lt.IsActive,
    lt.SortOrder,
    lt.CreatedAt,
    (SELECT COUNT(1) FROM dbo.LeaveBalances lb WHERE lb.LeaveTypeId = lt.Id) AS BalanceCount,
    (SELECT COUNT(1) FROM dbo.LeaveRequests lr WHERE lr.LeaveTypeId = lt.Id) AS RequestCount
FROM dbo.LeaveTypes lt";

            if (!includeInactive)
            {
                query += " WHERE IsActive = 1";
            }

            query += " ORDER BY SortOrder ASC, Name ASC;";
            return DbHelper.ExecuteDataTable(query);
        }

        public static void CreateLeaveType(string name, decimal defaultDays, bool requiresAttachment)
        {
            string normalizedName = NormalizeRequired(name, "Leave type name");
            if (defaultDays < 0)
            {
                throw new InvalidOperationException("Default balance must be zero or greater.");
            }

            try
            {
                DbHelper.ExecuteNonQuery(@"
INSERT INTO dbo.LeaveTypes (Name, DefaultDays, RequiresAttachment, IsActive, SortOrder, CreatedAt)
VALUES
(
    @Name,
    @DefaultDays,
    @RequiresAttachment,
    1,
    ISNULL((SELECT MAX(SortOrder) + 1 FROM dbo.LeaveTypes), 1),
    GETDATE()
);",
                    new SqlParameter("@Name", normalizedName),
                    new SqlParameter("@DefaultDays", defaultDays),
                    new SqlParameter("@RequiresAttachment", requiresAttachment));
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                throw new InvalidOperationException("That leave type already exists.");
            }

            EnsureBalancesForAllUsers(DateTime.Today.Year);
        }

        public static void UpdateLeaveType(int leaveTypeId, string name, decimal defaultDays, bool requiresAttachment, bool isActive, int sortOrder)
        {
            string normalizedName = NormalizeRequired(name, "Leave type name");
            if (defaultDays < 0)
            {
                throw new InvalidOperationException("Default balance must be zero or greater.");
            }

            if (sortOrder < 1)
            {
                throw new InvalidOperationException("Sort order must be 1 or greater.");
            }

            using (SqlConnection connection = DbHelper.GetOpenConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    string previousName;
                    using (SqlCommand selectCommand = new SqlCommand("SELECT TOP 1 Name FROM dbo.LeaveTypes WHERE Id = @Id;", connection, transaction))
                    {
                        selectCommand.Parameters.AddWithValue("@Id", leaveTypeId);
                        object result = selectCommand.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            throw new InvalidOperationException("That leave type could not be found.");
                        }

                        previousName = Convert.ToString(result);
                    }

                    bool isProtectedUnpaidLeave = string.Equals(previousName, UnpaidLeaveName, StringComparison.OrdinalIgnoreCase);
                    if (isProtectedUnpaidLeave && !string.Equals(normalizedName, UnpaidLeaveName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("The Unpaid Leave type name cannot be changed.");
                    }

                    if (isProtectedUnpaidLeave && !isActive)
                    {
                        throw new InvalidOperationException("The Unpaid Leave type cannot be deactivated.");
                    }

                    using (SqlCommand updateCommand = new SqlCommand(@"
UPDATE dbo.LeaveTypes
SET Name = @Name,
    DefaultDays = @DefaultDays,
    RequiresAttachment = @RequiresAttachment,
    IsActive = @IsActive,
    SortOrder = @SortOrder
WHERE Id = @Id;", connection, transaction))
                    {
                        updateCommand.Parameters.AddWithValue("@Id", leaveTypeId);
                        updateCommand.Parameters.AddWithValue("@Name", normalizedName);
                        updateCommand.Parameters.AddWithValue("@DefaultDays", defaultDays);
                        updateCommand.Parameters.AddWithValue("@RequiresAttachment", requiresAttachment);
                        updateCommand.Parameters.AddWithValue("@IsActive", isActive);
                        updateCommand.Parameters.AddWithValue("@SortOrder", sortOrder);
                        updateCommand.ExecuteNonQuery();
                    }

                    if (!string.Equals(previousName, normalizedName, StringComparison.OrdinalIgnoreCase))
                    {
                        using (SqlCommand requestCommand = new SqlCommand(@"
UPDATE dbo.LeaveRequests
SET LeaveType = @LeaveType
WHERE LeaveTypeId = @LeaveTypeId;", connection, transaction))
                        {
                            requestCommand.Parameters.AddWithValue("@LeaveTypeId", leaveTypeId);
                            requestCommand.Parameters.AddWithValue("@LeaveType", normalizedName);
                            requestCommand.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
                catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
                {
                    transaction.Rollback();
                    throw new InvalidOperationException("That leave type already exists.");
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public static DataTable GetUserBalances(int userId, int calendarYear)
        {
            return DbHelper.ExecuteDataTable(@"
SELECT
    lt.Id AS LeaveTypeId,
    lt.Name,
    lt.RequiresAttachment,
    CAST(lb.BalanceDays AS DECIMAL(6,1)) AS BalanceDays,
    CAST(lb.UsedDays AS DECIMAL(6,1)) AS UsedDays,
    CAST(lb.BalanceDays - lb.UsedDays AS DECIMAL(6,1)) AS RemainingDays,
    lb.CalendarYear
FROM dbo.LeaveBalances lb
INNER JOIN dbo.LeaveTypes lt ON lt.Id = lb.LeaveTypeId
WHERE lb.UserId = @UserId
  AND lb.CalendarYear = @CalendarYear
  AND lt.IsActive = 1
  AND lt.Name <> @UnpaidLeaveName
ORDER BY lt.SortOrder ASC, lt.Name ASC;",
                new SqlParameter("@UserId", userId),
                new SqlParameter("@CalendarYear", calendarYear),
                new SqlParameter("@UnpaidLeaveName", UnpaidLeaveName));
        }

        public static DataTable GetUserRequests(int userId, string selectedStatus)
        {
            string query = @"
SELECT
    lr.Id,
    lr.LeaveType,
    lr.StartDate,
    lr.EndDate,
    lr.Status,
    lr.CreatedAt,
    lr.AttachmentPath,
    lr.ReviewComment,
    reviewer.FullName AS ReviewedByName,
    lr.ReviewedAt,
    ISNULL(lr.RequestedDays, DATEDIFF(DAY, lr.StartDate, lr.EndDate) + 1) AS RequestedDays
FROM dbo.LeaveRequests lr
LEFT JOIN dbo.Users reviewer ON reviewer.Id = lr.ReviewedByUserId
WHERE lr.UserId = @UserId";

            if (!string.Equals(selectedStatus, "All", StringComparison.OrdinalIgnoreCase))
            {
                query += " AND lr.Status = @Status";
            }

            query += " ORDER BY lr.CreatedAt DESC, lr.StartDate DESC;";

            using (SqlConnection connection = DbHelper.GetOpenConnection())
            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                if (!string.Equals(selectedStatus, "All", StringComparison.OrdinalIgnoreCase))
                {
                    command.Parameters.AddWithValue("@Status", selectedStatus);
                }

                DataTable data = new DataTable();
                adapter.Fill(data);
                return data;
            }
        }

        public static PagedDataTableResult GetManageableRequestsPage(
            AuthenticatedUser currentUser,
            string selectedStatus,
            string selectedDepartment,
            string searchText,
            int pageNumber,
            int pageSize)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 10;
            }

            string fromClause = @"
FROM dbo.LeaveRequests lr
INNER JOIN dbo.Users u ON u.Id = lr.UserId
LEFT JOIN dbo.Users reviewer ON reviewer.Id = lr.ReviewedByUserId
WHERE 1 = 1";
            List<SqlParameter> parameters = BuildManageableRequestsParameters(currentUser, selectedStatus, selectedDepartment, searchText, ref fromClause);

            using (SqlConnection connection = DbHelper.GetOpenConnection())
            using (SqlCommand countCommand = connection.CreateCommand())
            using (SqlCommand dataCommand = connection.CreateCommand())
            using (SqlDataAdapter adapter = new SqlDataAdapter(dataCommand))
            {
                countCommand.CommandText = "SELECT COUNT(1) " + fromClause + ";";
                AddParameters(countCommand, parameters);

                int totalCount = Convert.ToInt32(countCommand.ExecuteScalar());
                int offset = (pageNumber - 1) * pageSize;

                dataCommand.CommandText = @"
SELECT
    lr.Id,
    u.Id AS RequesterUserId,
    u.Username,
    u.FullName,
    u.Department,
    lr.LeaveType,
    lr.StartDate,
    lr.EndDate,
    lr.Status,
    lr.Reason,
    lr.ReviewComment,
    lr.CreatedAt,
    reviewer.FullName AS ReviewedByName,
    lr.ReviewedAt,
    ISNULL(lr.RequestedDays, DATEDIFF(DAY, lr.StartDate, lr.EndDate) + 1) AS RequestedDays
" + fromClause + @"
ORDER BY CASE WHEN lr.Status = N'Pending' THEN 0 ELSE 1 END, lr.CreatedAt DESC, lr.StartDate DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
                AddParameters(dataCommand, parameters);
                dataCommand.Parameters.AddWithValue("@Offset", offset);
                dataCommand.Parameters.AddWithValue("@PageSize", pageSize);

                DataTable data = new DataTable();
                adapter.Fill(data);

                return new PagedDataTableResult
                {
                    Data = data,
                    TotalCount = totalCount
                };
            }
        }

        public static DataTable GetManageableRequests(AuthenticatedUser currentUser, string selectedStatus, string selectedDepartment, string searchText)
        {
            using (SqlConnection connection = DbHelper.GetOpenConnection())
            using (SqlCommand command = connection.CreateCommand())
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                command.CommandText = @"
SELECT
    lr.Id,
    u.Id AS RequesterUserId,
    u.Username,
    u.FullName,
    u.Department,
    lr.LeaveType,
    lr.StartDate,
    lr.EndDate,
    lr.Status,
    lr.Reason,
    lr.ReviewComment,
    lr.CreatedAt,
    reviewer.FullName AS ReviewedByName,
    lr.ReviewedAt,
    ISNULL(lr.RequestedDays, DATEDIFF(DAY, lr.StartDate, lr.EndDate) + 1) AS RequestedDays
FROM dbo.LeaveRequests lr
INNER JOIN dbo.Users u ON u.Id = lr.UserId
LEFT JOIN dbo.Users reviewer ON reviewer.Id = lr.ReviewedByUserId
WHERE 1 = 1";

                if (AuthorizationHelper.IsDepartmentAdmin(currentUser))
                {
                    command.CommandText += " AND u.Department = @CurrentDepartment";
                    command.Parameters.AddWithValue("@CurrentDepartment", currentUser.Department);
                }

                if (!string.IsNullOrWhiteSpace(selectedStatus) && !string.Equals(selectedStatus, "All", StringComparison.OrdinalIgnoreCase))
                {
                    command.CommandText += " AND lr.Status = @Status";
                    command.Parameters.AddWithValue("@Status", selectedStatus);
                }

                if (!string.IsNullOrWhiteSpace(selectedDepartment) && !string.Equals(selectedDepartment, "All", StringComparison.OrdinalIgnoreCase) && AuthorizationHelper.CanSelectAnyDepartment(currentUser))
                {
                    command.CommandText += " AND u.Department = @Department";
                    command.Parameters.AddWithValue("@Department", selectedDepartment.Trim());
                }

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    command.CommandText += " AND (u.FullName LIKE @Search OR u.Username LIKE @Search OR lr.LeaveType LIKE @Search)";
                    command.Parameters.AddWithValue("@Search", "%" + searchText.Trim() + "%");
                }

                command.CommandText += " ORDER BY CASE WHEN lr.Status = N'Pending' THEN 0 ELSE 1 END, lr.CreatedAt DESC, lr.StartDate DESC;";

                DataTable data = new DataTable();
                adapter.Fill(data);
                return data;
            }
        }

        public static PagedDataTableResult GetLeaveReportPage(
            AuthenticatedUser currentUser,
            string selectedStatus,
            string selectedDepartment,
            string selectedLeaveType,
            DateTime? startDate,
            DateTime? endDate,
            string searchText,
            int pageNumber,
            int pageSize)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 20;
            }

            string fromClause = @"
FROM dbo.LeaveRequests lr
INNER JOIN dbo.Users u ON u.Id = lr.UserId
LEFT JOIN dbo.Users reviewer ON reviewer.Id = lr.ReviewedByUserId
WHERE 1 = 1";
            List<SqlParameter> parameters = BuildLeaveReportParameters(currentUser, selectedStatus, selectedDepartment, selectedLeaveType, startDate, endDate, searchText, ref fromClause);

            using (SqlConnection connection = DbHelper.GetOpenConnection())
            using (SqlCommand countCommand = connection.CreateCommand())
            using (SqlCommand dataCommand = connection.CreateCommand())
            using (SqlDataAdapter adapter = new SqlDataAdapter(dataCommand))
            {
                countCommand.CommandText = "SELECT COUNT(1) " + fromClause + ";";
                AddParameters(countCommand, parameters);

                int totalCount = Convert.ToInt32(countCommand.ExecuteScalar());
                int offset = (pageNumber - 1) * pageSize;

                dataCommand.CommandText = @"
SELECT
    lr.Id,
    u.Username,
    u.FullName,
    u.Department,
    lr.LeaveType,
    lr.StartDate,
    lr.EndDate,
    lr.Status,
    lr.Reason,
    lr.CreatedAt,
    reviewer.FullName AS ReviewedByName,
    lr.ReviewedAt,
    lr.ReviewComment,
    ISNULL(lr.RequestedDays, DATEDIFF(DAY, lr.StartDate, lr.EndDate) + 1) AS RequestedDays
" + fromClause + @"
ORDER BY lr.StartDate DESC, lr.CreatedAt DESC, u.FullName ASC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
                AddParameters(dataCommand, parameters);
                dataCommand.Parameters.AddWithValue("@Offset", offset);
                dataCommand.Parameters.AddWithValue("@PageSize", pageSize);

                DataTable data = new DataTable();
                adapter.Fill(data);

                return new PagedDataTableResult
                {
                    Data = data,
                    TotalCount = totalCount
                };
            }
        }

        public static LeaveReportSummaryResult GetLeaveReportSummary(
            AuthenticatedUser currentUser,
            string selectedStatus,
            string selectedDepartment,
            string selectedLeaveType,
            DateTime? startDate,
            DateTime? endDate,
            string searchText)
        {
            string fromClause = @"
FROM dbo.LeaveRequests lr
INNER JOIN dbo.Users u ON u.Id = lr.UserId
LEFT JOIN dbo.Users reviewer ON reviewer.Id = lr.ReviewedByUserId
WHERE 1 = 1";
            List<SqlParameter> parameters = BuildLeaveReportParameters(currentUser, selectedStatus, selectedDepartment, selectedLeaveType, startDate, endDate, searchText, ref fromClause);

            using (SqlConnection connection = DbHelper.GetOpenConnection())
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT
    COUNT(1) AS TotalRecords,
    ISNULL(SUM(ISNULL(lr.RequestedDays, DATEDIFF(DAY, lr.StartDate, lr.EndDate) + 1)), 0) AS TotalRequestedDays,
    SUM(CASE WHEN lr.Status = N'Pending' THEN 1 ELSE 0 END) AS PendingCount,
    SUM(CASE WHEN lr.Status = N'Approved' THEN 1 ELSE 0 END) AS ApprovedCount
" + fromClause + ";";
                AddParameters(command, parameters);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return new LeaveReportSummaryResult();
                    }

                    return new LeaveReportSummaryResult
                    {
                        TotalRecords = reader["TotalRecords"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalRecords"]),
                        TotalRequestedDays = reader["TotalRequestedDays"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalRequestedDays"]),
                        PendingCount = reader["PendingCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PendingCount"]),
                        ApprovedCount = reader["ApprovedCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ApprovedCount"])
                    };
                }
            }
        }

        public static DataTable GetLeaveReport(
            AuthenticatedUser currentUser,
            string selectedStatus,
            string selectedDepartment,
            string selectedLeaveType,
            DateTime? startDate,
            DateTime? endDate,
            string searchText)
        {
            using (SqlConnection connection = DbHelper.GetOpenConnection())
            using (SqlCommand command = connection.CreateCommand())
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                command.CommandText = @"
SELECT
    lr.Id,
    u.Username,
    u.FullName,
    u.Department,
    lr.LeaveType,
    lr.StartDate,
    lr.EndDate,
    lr.Status,
    lr.Reason,
    lr.CreatedAt,
    reviewer.FullName AS ReviewedByName,
    lr.ReviewedAt,
    lr.ReviewComment,
    ISNULL(lr.RequestedDays, DATEDIFF(DAY, lr.StartDate, lr.EndDate) + 1) AS RequestedDays
FROM dbo.LeaveRequests lr
INNER JOIN dbo.Users u ON u.Id = lr.UserId
LEFT JOIN dbo.Users reviewer ON reviewer.Id = lr.ReviewedByUserId
WHERE 1 = 1";

                if (AuthorizationHelper.IsDepartmentAdmin(currentUser))
                {
                    command.CommandText += " AND u.Department = @CurrentDepartment";
                    command.Parameters.AddWithValue("@CurrentDepartment", currentUser.Department);
                }

                if (!string.IsNullOrWhiteSpace(selectedStatus) && !string.Equals(selectedStatus, "All", StringComparison.OrdinalIgnoreCase))
                {
                    command.CommandText += " AND lr.Status = @Status";
                    command.Parameters.AddWithValue("@Status", selectedStatus);
                }

                if (!string.IsNullOrWhiteSpace(selectedDepartment) &&
                    !string.Equals(selectedDepartment, "All", StringComparison.OrdinalIgnoreCase) &&
                    AuthorizationHelper.CanSelectAnyDepartment(currentUser))
                {
                    command.CommandText += " AND u.Department = @Department";
                    command.Parameters.AddWithValue("@Department", selectedDepartment.Trim());
                }

                if (!string.IsNullOrWhiteSpace(selectedLeaveType) && !string.Equals(selectedLeaveType, "All", StringComparison.OrdinalIgnoreCase))
                {
                    command.CommandText += " AND lr.LeaveType = @LeaveType";
                    command.Parameters.AddWithValue("@LeaveType", selectedLeaveType.Trim());
                }

                if (startDate.HasValue)
                {
                    command.CommandText += " AND lr.StartDate >= @StartDate";
                    command.Parameters.AddWithValue("@StartDate", startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    command.CommandText += " AND lr.EndDate <= @EndDate";
                    command.Parameters.AddWithValue("@EndDate", endDate.Value.Date);
                }

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    command.CommandText += " AND (u.FullName LIKE @Search OR u.Username LIKE @Search OR lr.LeaveType LIKE @Search OR ISNULL(lr.Reason, N'') LIKE @Search)";
                    command.Parameters.AddWithValue("@Search", "%" + searchText.Trim() + "%");
                }

                command.CommandText += " ORDER BY lr.StartDate DESC, lr.CreatedAt DESC, u.FullName ASC;";

                DataTable data = new DataTable();
                adapter.Fill(data);
                return data;
            }
        }

        public static int SubmitLeaveRequest(
            AuthenticatedUser currentUser,
            int leaveTypeId,
            DateTime startDate,
            DateTime endDate,
            string reason,
            string attachmentFileName,
            string attachmentPath)
        {
            if (currentUser == null)
            {
                throw new InvalidOperationException("You must be signed in to submit leave.");
            }

            if (!AuthorizationHelper.CanSubmitLeaveRequests(currentUser))
            {
                throw new InvalidOperationException("The bootstrap admin account cannot submit leave requests. Please use a named employee, manager, or HR account instead.");
            }

            if (endDate < startDate)
            {
                throw new InvalidOperationException("End date cannot be earlier than start date.");
            }

            int requestedDays = CalculateRequestedDays(startDate, endDate);
            int requestId;

            using (SqlConnection connection = DbHelper.GetOpenConnection())
            {
                EnsureBalancesForUser(connection, currentUser.Id, DateTime.Today.Year);

                LeaveTypeRecord leaveType = GetLeaveType(connection, leaveTypeId);
                if (leaveType == null || !leaveType.IsActive)
                {
                    throw new InvalidOperationException("The selected leave type is no longer available.");
                }

                if (leaveType.RequiresAttachment && string.IsNullOrWhiteSpace(attachmentPath))
                {
                    throw new InvalidOperationException("This leave type requires an attachment.");
                }

                bool isUnpaidLeave = IsUnpaidLeave(leaveType.Name);
                decimal remainingDays = isUnpaidLeave ? 0 : GetRemainingBalance(connection, currentUser.Id, leaveTypeId, startDate.Year);
                if (!isUnpaidLeave && remainingDays < requestedDays)
                {
                    throw new InvalidOperationException(string.Format("Only {0:0.#} day(s) remaining for {1}. Reduce the request or submit Unpaid Leave instead.", remainingDays, leaveType.Name));
                }

                using (SqlCommand command = new SqlCommand(@"
INSERT INTO dbo.LeaveRequests
(
    UserId,
    LeaveTypeId,
    LeaveType,
    StartDate,
    EndDate,
    RequestedDays,
    Reason,
    AttachmentFileName,
    AttachmentPath,
    ReviewComment
)
OUTPUT INSERTED.Id
VALUES
(
    @UserId,
    @LeaveTypeId,
    @LeaveType,
    @StartDate,
    @EndDate,
    @RequestedDays,
    @Reason,
    @AttachmentFileName,
    @AttachmentPath,
    NULL
);", connection))
                {
                    command.Parameters.AddWithValue("@UserId", currentUser.Id);
                    command.Parameters.AddWithValue("@LeaveTypeId", leaveTypeId);
                    command.Parameters.AddWithValue("@LeaveType", leaveType.Name);
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);
                    command.Parameters.AddWithValue("@RequestedDays", requestedDays);
                    command.Parameters.AddWithValue("@Reason", string.IsNullOrWhiteSpace(reason) ? (object)DBNull.Value : reason.Trim());
                    command.Parameters.AddWithValue("@AttachmentFileName", (object)attachmentFileName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@AttachmentPath", (object)attachmentPath ?? DBNull.Value);
                    requestId = Convert.ToInt32(command.ExecuteScalar());
                }

                InsertHistory(connection, requestId, currentUser.Id, "Submitted", "Pending", "Pending", null);
            }

            SecurityAuditLogger.Log("LeaveRequestSubmitted", currentUser.Username, string.Format("Submitted leave request #{0}.", requestId));
            NotificationRepository.NotifyReviewersForSubmission(currentUser, requestId);
            return requestId;
        }

        public static void UpdateRequestStatus(AuthenticatedUser currentUser, int requestId, string status, string reviewComment)
        {
            if (!AuthorizationHelper.CanManageRequests(currentUser))
            {
                throw new InvalidOperationException("You do not have permission to review leave requests.");
            }

            int requesterUserId = 0;
            using (SqlConnection connection = DbHelper.GetOpenConnection())
            {
                RequestRecord request = GetRequestRecord(connection, requestId);
                if (request == null)
                {
                    throw new InvalidOperationException("The selected request could not be found.");
                }

                if (!AuthorizationHelper.CanManageDepartment(currentUser, request.Department))
                {
                    throw new InvalidOperationException("You can only review requests for your own department.");
                }

                if (!AuthorizationHelper.CanReviewOwnRequest(currentUser, request.UserId))
                {
                    throw new InvalidOperationException("You cannot approve or reject your own leave request.");
                }

                if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Only pending requests can be reviewed.");
                }

                if (string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    bool isUnpaidLeave = IsUnpaidLeave(request.LeaveTypeName);
                    if (!isUnpaidLeave)
                    {
                        EnsureBalancesForUser(connection, request.UserId, request.StartDate.Year);
                        decimal remainingDays = GetRemainingBalance(connection, request.UserId, request.LeaveTypeId, request.StartDate.Year);
                        if (remainingDays < request.RequestedDays)
                        {
                            throw new InvalidOperationException(string.Format("This request now exceeds the remaining {0:0.#} day(s) available.", remainingDays));
                        }

                        using (SqlCommand balanceCommand = new SqlCommand(@"
UPDATE dbo.LeaveBalances
SET UsedDays = UsedDays + @RequestedDays
WHERE UserId = @UserId
  AND LeaveTypeId = @LeaveTypeId
  AND CalendarYear = @CalendarYear;", connection))
                        {
                            balanceCommand.Parameters.AddWithValue("@RequestedDays", request.RequestedDays);
                            balanceCommand.Parameters.AddWithValue("@UserId", request.UserId);
                            balanceCommand.Parameters.AddWithValue("@LeaveTypeId", request.LeaveTypeId);
                            balanceCommand.Parameters.AddWithValue("@CalendarYear", request.StartDate.Year);
                            balanceCommand.ExecuteNonQuery();
                        }
                    }
                }

                using (SqlCommand command = new SqlCommand(@"
UPDATE dbo.LeaveRequests
SET Status = @Status,
    ReviewedByUserId = @ReviewedByUserId,
    ReviewedAt = GETDATE(),
    ReviewComment = @ReviewComment
WHERE Id = @Id;", connection))
                {
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@Id", requestId);
                    command.Parameters.AddWithValue("@ReviewedByUserId", currentUser.Id);
                    command.Parameters.AddWithValue("@ReviewComment", string.IsNullOrWhiteSpace(reviewComment) ? (object)DBNull.Value : reviewComment.Trim());
                    command.ExecuteNonQuery();
                }

                InsertHistory(connection, requestId, currentUser.Id, status, request.Status, status, reviewComment);
                SecurityAuditLogger.Log("LeaveRequestStatusUpdated", currentUser.Username, string.Format("Set leave request #{0} for '{1}' to '{2}'.", requestId, request.Username, status));
                requesterUserId = request.UserId;
            }

            if (requesterUserId > 0)
            {
                NotificationRepository.NotifyRequesterStatusChanged(requesterUserId, status);
            }
        }

        public static void EnsureBalancesForAllUsers(int calendarYear)
        {
            using (SqlConnection connection = DbHelper.GetOpenConnection())
            {
                DataTable users = DbHelper.ExecuteDataTable("SELECT Id FROM dbo.Users WHERE IsActive = 1;");
                foreach (DataRow row in users.Rows)
                {
                    EnsureBalancesForUser(connection, Convert.ToInt32(row["Id"]), calendarYear);
                }
            }
        }

        public static void EnsureBalancesForUser(int userId, int calendarYear)
        {
            using (SqlConnection connection = DbHelper.GetOpenConnection())
            {
                EnsureBalancesForUser(connection, userId, calendarYear);
            }
        }

        public static int ResolveDepartmentId(SqlConnection connection, string departmentName)
        {
            string normalizedName = NormalizeRequired(departmentName, "Department");

            using (SqlCommand command = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE Name = @Name)
BEGIN
    INSERT INTO dbo.Departments (Name, IsActive, CreatedAt)
    VALUES (@Name, 1, GETDATE());
END;

SELECT TOP 1 Id
FROM dbo.Departments
WHERE Name = @Name;", connection))
            {
                command.Parameters.AddWithValue("@Name", normalizedName);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static void EnsureBalancesForUser(SqlConnection connection, int userId, int calendarYear)
        {
            using (SqlCommand command = new SqlCommand(@"
INSERT INTO dbo.LeaveBalances (UserId, LeaveTypeId, CalendarYear, BalanceDays, UsedDays, UpdatedAt)
SELECT
    @UserId,
    lt.Id,
    @CalendarYear,
    lt.DefaultDays,
    0,
    GETDATE()
FROM dbo.LeaveTypes lt
WHERE lt.IsActive = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.LeaveBalances lb
      WHERE lb.UserId = @UserId
        AND lb.LeaveTypeId = lt.Id
        AND lb.CalendarYear = @CalendarYear
  );", connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@CalendarYear", calendarYear);
                command.ExecuteNonQuery();
            }
        }

        private static decimal GetRemainingBalance(SqlConnection connection, int userId, int leaveTypeId, int calendarYear)
        {
            using (SqlCommand command = new SqlCommand(@"
SELECT CAST(ISNULL(BalanceDays - UsedDays, 0) AS DECIMAL(6,1))
FROM dbo.LeaveBalances
WHERE UserId = @UserId
  AND LeaveTypeId = @LeaveTypeId
  AND CalendarYear = @CalendarYear;", connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@LeaveTypeId", leaveTypeId);
                command.Parameters.AddWithValue("@CalendarYear", calendarYear);
                object result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToDecimal(result);
            }
        }

        private static LeaveTypeRecord GetLeaveType(SqlConnection connection, int leaveTypeId)
        {
            using (SqlCommand command = new SqlCommand(@"
SELECT TOP 1 Id, Name, RequiresAttachment, IsActive
FROM dbo.LeaveTypes
WHERE Id = @Id;", connection))
            {
                command.Parameters.AddWithValue("@Id", leaveTypeId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new LeaveTypeRecord
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Name = Convert.ToString(reader["Name"]),
                        RequiresAttachment = Convert.ToBoolean(reader["RequiresAttachment"]),
                        IsActive = Convert.ToBoolean(reader["IsActive"])
                    };
                }
            }
        }

        private static RequestRecord GetRequestRecord(SqlConnection connection, int requestId)
        {
            using (SqlCommand command = new SqlCommand(@"
SELECT TOP 1
    lr.Id,
    lr.UserId,
    lr.LeaveTypeId,
    lr.LeaveType,
    lr.StartDate,
    lr.EndDate,
    lr.Status,
    lr.RequestedDays,
    u.Username,
    u.Department
FROM dbo.LeaveRequests lr
INNER JOIN dbo.Users u ON u.Id = lr.UserId
WHERE lr.Id = @Id;", connection))
            {
                command.Parameters.AddWithValue("@Id", requestId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    DateTime startDate = Convert.ToDateTime(reader["StartDate"]);
                    DateTime endDate = Convert.ToDateTime(reader["EndDate"]);

                    return new RequestRecord
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        UserId = Convert.ToInt32(reader["UserId"]),
                        LeaveTypeId = reader["LeaveTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["LeaveTypeId"]),
                        LeaveTypeName = Convert.ToString(reader["LeaveType"]),
                        StartDate = startDate,
                        EndDate = endDate,
                        Status = Convert.ToString(reader["Status"]),
                        Username = Convert.ToString(reader["Username"]),
                        Department = Convert.ToString(reader["Department"]),
                        RequestedDays = reader["RequestedDays"] == DBNull.Value ? CalculateRequestedDays(startDate, endDate) : Convert.ToInt32(reader["RequestedDays"])
                    };
                }
            }
        }

        private static void InsertHistory(SqlConnection connection, int requestId, int actorUserId, string action, string previousStatus, string newStatus, string comment)
        {
            using (SqlCommand command = new SqlCommand(@"
INSERT INTO dbo.LeaveRequestHistory
(
    LeaveRequestId,
    ActorUserId,
    ActionName,
    PreviousStatus,
    NewStatus,
    Comment,
    CreatedAt
)
VALUES
(
    @LeaveRequestId,
    @ActorUserId,
    @ActionName,
    @PreviousStatus,
    @NewStatus,
    @Comment,
    GETDATE()
);", connection))
            {
                command.Parameters.AddWithValue("@LeaveRequestId", requestId);
                command.Parameters.AddWithValue("@ActorUserId", actorUserId);
                command.Parameters.AddWithValue("@ActionName", action);
                command.Parameters.AddWithValue("@PreviousStatus", string.IsNullOrWhiteSpace(previousStatus) ? (object)DBNull.Value : previousStatus);
                command.Parameters.AddWithValue("@NewStatus", string.IsNullOrWhiteSpace(newStatus) ? (object)DBNull.Value : newStatus);
                command.Parameters.AddWithValue("@Comment", string.IsNullOrWhiteSpace(comment) ? (object)DBNull.Value : comment.Trim());
                command.ExecuteNonQuery();
            }
        }

        private static string NormalizeRequired(string value, string label)
        {
            string normalized = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new InvalidOperationException(label + " is required.");
            }

            return normalized;
        }

        public static int CalculateRequestedDays(DateTime startDate, DateTime endDate)
        {
            if (endDate.Date < startDate.Date)
            {
                return 0;
            }

            bool saturdayOff = SystemSettingsRepository.GetBoolSetting("WeekendSaturdayOff", true);
            bool sundayOff = SystemSettingsRepository.GetBoolSetting("WeekendSundayOff", true);
            var holidays = SystemSettingsRepository.GetHolidayDates(startDate, endDate);
            int count = 0;

            for (DateTime date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (saturdayOff && date.DayOfWeek == DayOfWeek.Saturday)
                {
                    continue;
                }

                if (sundayOff && date.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                if (holidays.Contains(date))
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private static bool IsUnpaidLeave(string leaveTypeName)
        {
            return string.Equals(leaveTypeName, UnpaidLeaveName, StringComparison.OrdinalIgnoreCase);
        }

        private static List<SqlParameter> BuildManageableRequestsParameters(
            AuthenticatedUser currentUser,
            string selectedStatus,
            string selectedDepartment,
            string searchText,
            ref string fromClause)
        {
            List<SqlParameter> parameters = new List<SqlParameter>();

            if (AuthorizationHelper.IsDepartmentAdmin(currentUser))
            {
                fromClause += " AND u.Department = @CurrentDepartment";
                parameters.Add(new SqlParameter("@CurrentDepartment", currentUser.Department));
            }

            if (!string.IsNullOrWhiteSpace(selectedStatus) && !string.Equals(selectedStatus, "All", StringComparison.OrdinalIgnoreCase))
            {
                fromClause += " AND lr.Status = @Status";
                parameters.Add(new SqlParameter("@Status", selectedStatus));
            }

            if (!string.IsNullOrWhiteSpace(selectedDepartment) &&
                !string.Equals(selectedDepartment, "All", StringComparison.OrdinalIgnoreCase) &&
                AuthorizationHelper.CanSelectAnyDepartment(currentUser))
            {
                fromClause += " AND u.Department = @Department";
                parameters.Add(new SqlParameter("@Department", selectedDepartment.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                fromClause += " AND (u.FullName LIKE @Search OR u.Username LIKE @Search OR lr.LeaveType LIKE @Search)";
                parameters.Add(new SqlParameter("@Search", "%" + searchText.Trim() + "%"));
            }

            return parameters;
        }

        private static List<SqlParameter> BuildLeaveReportParameters(
            AuthenticatedUser currentUser,
            string selectedStatus,
            string selectedDepartment,
            string selectedLeaveType,
            DateTime? startDate,
            DateTime? endDate,
            string searchText,
            ref string fromClause)
        {
            List<SqlParameter> parameters = new List<SqlParameter>();

            if (AuthorizationHelper.IsDepartmentAdmin(currentUser))
            {
                fromClause += " AND u.Department = @CurrentDepartment";
                parameters.Add(new SqlParameter("@CurrentDepartment", currentUser.Department));
            }

            if (!string.IsNullOrWhiteSpace(selectedStatus) && !string.Equals(selectedStatus, "All", StringComparison.OrdinalIgnoreCase))
            {
                fromClause += " AND lr.Status = @Status";
                parameters.Add(new SqlParameter("@Status", selectedStatus));
            }

            if (!string.IsNullOrWhiteSpace(selectedDepartment) &&
                !string.Equals(selectedDepartment, "All", StringComparison.OrdinalIgnoreCase) &&
                AuthorizationHelper.CanSelectAnyDepartment(currentUser))
            {
                fromClause += " AND u.Department = @Department";
                parameters.Add(new SqlParameter("@Department", selectedDepartment.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(selectedLeaveType) && !string.Equals(selectedLeaveType, "All", StringComparison.OrdinalIgnoreCase))
            {
                fromClause += " AND lr.LeaveType = @LeaveType";
                parameters.Add(new SqlParameter("@LeaveType", selectedLeaveType.Trim()));
            }

            if (startDate.HasValue)
            {
                fromClause += " AND lr.StartDate >= @StartDate";
                parameters.Add(new SqlParameter("@StartDate", startDate.Value.Date));
            }

            if (endDate.HasValue)
            {
                fromClause += " AND lr.EndDate <= @EndDate";
                parameters.Add(new SqlParameter("@EndDate", endDate.Value.Date));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                fromClause += " AND (u.FullName LIKE @Search OR u.Username LIKE @Search OR lr.LeaveType LIKE @Search OR ISNULL(lr.Reason, N'') LIKE @Search)";
                parameters.Add(new SqlParameter("@Search", "%" + searchText.Trim() + "%"));
            }

            return parameters;
        }

        private static void AddParameters(SqlCommand command, IEnumerable<SqlParameter> parameters)
        {
            foreach (SqlParameter parameter in parameters)
            {
                command.Parameters.AddWithValue(parameter.ParameterName, parameter.Value);
            }
        }

        private sealed class LeaveTypeRecord
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool RequiresAttachment { get; set; }
            public bool IsActive { get; set; }
        }

        private sealed class RequestRecord
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public int LeaveTypeId { get; set; }
            public string LeaveTypeName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Status { get; set; }
            public string Username { get; set; }
            public string Department { get; set; }
            public int RequestedDays { get; set; }
        }
    }
}
