using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.IO;
using System.Web.Hosting;
using OnlineLeaveManagementSystem.Data;

namespace OnlineLeaveManagementSystem.Security
{
    public static class UserAccountManager
    {
        private const int RecommendedMinimumActiveAdmins = 2;
        private static readonly string[] SupportedRoles =
        {
            AuthorizationHelper.AdminRole,
            AuthorizationHelper.HrRole,
            AuthorizationHelper.DepartmentAdminRole,
            AuthorizationHelper.UserRole
        };

        public static string[] GetSupportedRoles()
        {
            return SupportedRoles.ToArray();
        }

        public static DataTable GetUsers()
        {
            return DbHelper.ExecuteDataTable(@"
SELECT
    Id,
    Username,
    FullName,
    DepartmentId,
    Department,
    [Role],
    IsActive,
    FailedLoginCount,
    LockoutEndUtc,
    LastLoginUtc,
    CreatedAt,
    MustChangePassword
FROM dbo.Users
ORDER BY
    CASE WHEN [Role] = N'Admin' THEN 0 ELSE 1 END,
    CASE WHEN [Role] = N'HR' THEN 1 ELSE 2 END,
    IsActive DESC,
    FullName ASC,
    Username ASC;");
        }

        public static DataTable GetSummary()
        {
            return DbHelper.ExecuteDataTable(@"
SELECT
    COUNT(1) AS TotalUsers,
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveUsers,
    SUM(CASE WHEN [Role] = N'Admin' AND IsActive = 1 THEN 1 ELSE 0 END) AS ActiveAdmins,
    SUM(CASE WHEN MustChangePassword = 1 THEN 1 ELSE 0 END) AS PasswordResetsPending,
    SUM(CASE WHEN LockoutEndUtc IS NOT NULL AND LockoutEndUtc > GETUTCDATE() THEN 1 ELSE 0 END) AS LockedUsers,
    CASE WHEN SUM(CASE WHEN [Role] = N'Admin' AND IsActive = 1 THEN 1 ELSE 0 END) >= " + RecommendedMinimumActiveAdmins + @" THEN 1 ELSE 0 END AS MeetsRecommendedAdminCoverage
FROM dbo.Users;");
        }

        public static string CreateUser(AuthenticatedUser actor, string username, string fullName, string department, string role, bool isActive)
        {
            string normalizedUserName = NormalizeUserName(username);
            string normalizedFullName = NormalizeRequired(fullName, "Full name");
            string normalizedDepartment = NormalizeRequired(department, "Department");
            string normalizedRole = NormalizeRole(role);
            string temporaryPassword = GenerateTemporaryPassword();
            string salt = PasswordHasher.GenerateSalt();
            string hash = PasswordHasher.HashPassword(temporaryPassword, salt);

            try
            {
                using (SqlConnection connection = DbHelper.GetOpenConnection())
                {
                    int departmentId = LeaveManagementRepository.ResolveDepartmentId(connection, normalizedDepartment);

                    using (SqlCommand command = new SqlCommand(@"
INSERT INTO dbo.Users
(
    Username,
    PasswordHash,
    PasswordSalt,
    FullName,
    DepartmentId,
    Department,
    [Role],
    IsActive,
    FailedLoginCount,
    LockoutEndUtc,
    LastLoginUtc,
    CreatedAt,
    MustChangePassword,
    PasswordChangedAtUtc
)
VALUES
(
    @Username,
    @PasswordHash,
    @PasswordSalt,
    @FullName,
    @DepartmentId,
    @Department,
    @Role,
    @IsActive,
    0,
    NULL,
    NULL,
    GETDATE(),
    1,
    NULL
)",
                        connection))
                    {
                        command.Parameters.AddWithValue("@Username", normalizedUserName);
                        command.Parameters.AddWithValue("@PasswordHash", hash);
                        command.Parameters.AddWithValue("@PasswordSalt", salt);
                        command.Parameters.AddWithValue("@FullName", normalizedFullName);
                        command.Parameters.AddWithValue("@DepartmentId", departmentId);
                        command.Parameters.AddWithValue("@Department", normalizedDepartment);
                        command.Parameters.AddWithValue("@Role", normalizedRole);
                        command.Parameters.AddWithValue("@IsActive", isActive);
                        command.ExecuteNonQuery();
                    }

                    object createdUserId;
                    using (SqlCommand lookupCommand = new SqlCommand("SELECT TOP 1 Id FROM dbo.Users WHERE Username = @Username;", connection))
                    {
                        lookupCommand.Parameters.AddWithValue("@Username", normalizedUserName);
                        createdUserId = lookupCommand.ExecuteScalar();
                    }
                    if (createdUserId != null && createdUserId != DBNull.Value)
                    {
                        LeaveManagementRepository.EnsureBalancesForUser(Convert.ToInt32(createdUserId), DateTime.Today.Year);
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                throw new InvalidOperationException("That username is already in use.");
            }

            SecurityAuditLogger.Log("UserCreated", GetActorName(actor), string.Format("Created user '{0}' with role '{1}' in '{2}'.", normalizedUserName, normalizedRole, normalizedDepartment));
            return temporaryPassword;
        }

        public static void UpdateUser(AuthenticatedUser actor, int userId, string fullName, string department, string role, bool isActive)
        {
            if (actor == null)
            {
                throw new InvalidOperationException("You must be signed in to manage users.");
            }

            string normalizedFullName = NormalizeRequired(fullName, "Full name");
            string normalizedDepartment = NormalizeRequired(department, "Department");
            string normalizedRole = NormalizeRole(role);

            using (SqlConnection connection = DbHelper.GetOpenConnection())
            {
                UserRecord target = GetUserRecord(connection, userId);
                if (target == null)
                {
                    throw new InvalidOperationException("The selected user account could not be found.");
                }

                bool removingAdminAccess = string.Equals(target.Role, "Admin", StringComparison.OrdinalIgnoreCase) &&
                                           (!string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase) || !isActive);
                bool changingAnotherAdminRole = actor.Id != target.Id &&
                                                string.Equals(target.Role, AuthorizationHelper.AdminRole, StringComparison.OrdinalIgnoreCase) &&
                                                !string.Equals(normalizedRole, target.Role, StringComparison.OrdinalIgnoreCase);
                bool disablingAnotherAdmin = actor.Id != target.Id &&
                                             string.Equals(target.Role, AuthorizationHelper.AdminRole, StringComparison.OrdinalIgnoreCase) &&
                                             !isActive;

                if (actor.Id == target.Id && !isActive)
                {
                    throw new InvalidOperationException("You cannot disable your own account.");
                }

                if (actor.Id == target.Id && !string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("You cannot remove your own admin role.");
                }

                if (string.Equals(target.Username, AuthorizationHelper.BootstrapAdminUsername, StringComparison.OrdinalIgnoreCase) && !isActive)
                {
                    throw new InvalidOperationException("The default admin account cannot be disabled.");
                }

                if (string.Equals(target.Username, AuthorizationHelper.BootstrapAdminUsername, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(normalizedRole, AuthorizationHelper.AdminRole, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("The default admin account role cannot be changed.");
                }

                if (string.Equals(target.Username, AuthorizationHelper.BootstrapAdminUsername, StringComparison.OrdinalIgnoreCase) &&
                    (!string.Equals(normalizedFullName, target.FullName, StringComparison.OrdinalIgnoreCase) ||
                     !string.Equals(normalizedDepartment, target.Department, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("The bootstrap admin account profile cannot be changed. Create and use named admin accounts for day-to-day administration.");
                }

                if (changingAnotherAdminRole && !AuthorizationHelper.IsBootstrapAdmin(actor))
                {
                    throw new InvalidOperationException("Only the bootstrap admin account can change another admin's role. Use password reset for admin recovery instead.");
                }

                if (disablingAnotherAdmin && !AuthorizationHelper.IsBootstrapAdmin(actor))
                {
                    throw new InvalidOperationException("Only the bootstrap admin account can deactivate another admin. Use password reset for admin recovery instead.");
                }

                if (removingAdminAccess && !HasAnotherActiveAdmin(connection, target.Id))
                {
                    throw new InvalidOperationException("At least one active admin account must remain in the system.");
                }

                if (removingAdminAccess && !MeetsRecommendedAdminCoverageAfterChange(connection, target.Id))
                {
                    throw new InvalidOperationException("Keep at least two active admin accounts. Create or activate another named admin before removing this admin access.");
                }

                using (SqlCommand command = new SqlCommand(@"
UPDATE dbo.Users
SET FullName = @FullName,
    DepartmentId = @DepartmentId,
    Department = @Department,
    [Role] = @Role,
    IsActive = @IsActive
WHERE Id = @Id", connection))
                {
                    int departmentId = LeaveManagementRepository.ResolveDepartmentId(connection, normalizedDepartment);
                    command.Parameters.AddWithValue("@Id", userId);
                    command.Parameters.AddWithValue("@FullName", normalizedFullName);
                    command.Parameters.AddWithValue("@DepartmentId", departmentId);
                    command.Parameters.AddWithValue("@Department", normalizedDepartment);
                    command.Parameters.AddWithValue("@Role", normalizedRole);
                    command.Parameters.AddWithValue("@IsActive", isActive);
                    command.ExecuteNonQuery();
                }

                LeaveManagementRepository.EnsureBalancesForUser(userId, DateTime.Today.Year);

                SecurityAuditLogger.Log(
                    "UserUpdated",
                    GetActorName(actor),
                    string.Format("Updated user '{0}' to role '{1}', active={2}, department='{3}'.", target.Username, normalizedRole, isActive ? "true" : "false", normalizedDepartment));
            }
        }

        public static void ResetPassword(AuthenticatedUser actor, int userId, string temporaryPassword)
        {
            if (actor == null)
            {
                throw new InvalidOperationException("You must be signed in to manage users.");
            }

            ValidatePasswordForUse(temporaryPassword);
            string salt = PasswordHasher.GenerateSalt();
            string hash = PasswordHasher.HashPassword(temporaryPassword, salt);

            using (SqlConnection connection = DbHelper.GetOpenConnection())
            {
                UserRecord target = GetUserRecord(connection, userId);
                if (target == null)
                {
                    throw new InvalidOperationException("The selected user account could not be found.");
                }

                if (string.Equals(target.Role, AuthorizationHelper.AdminRole, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(actor.Role, AuthorizationHelper.AdminRole, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Only an admin can reset another admin account.");
                }

                using (SqlCommand command = new SqlCommand(@"
UPDATE dbo.Users
SET PasswordHash = @PasswordHash,
    PasswordSalt = @PasswordSalt,
    FailedLoginCount = 0,
    LockoutEndUtc = NULL,
    MustChangePassword = 1,
    PasswordChangedAtUtc = NULL
WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", userId);
                    command.Parameters.AddWithValue("@PasswordHash", hash);
                    command.Parameters.AddWithValue("@PasswordSalt", salt);
                    command.ExecuteNonQuery();
                }

                SecurityAuditLogger.Log("PasswordResetByAdmin", GetActorName(actor), string.Format("Reset password for '{0}'.", target.Username));
            }
        }

        public static bool ChangePassword(int userId, string currentPassword, string newPassword, out string message)
        {
            message = null;
            ValidatePasswordForUse(newPassword);

            using (SqlConnection connection = DbHelper.GetOpenConnection())
            {
                UserRecord target = GetUserRecord(connection, userId);
                if (target == null)
                {
                    message = "Your account could not be found.";
                    return false;
                }

                if (!PasswordHasher.VerifyPassword(currentPassword, target.PasswordSalt, target.PasswordHash))
                {
                    message = "Your current password is incorrect.";
                    return false;
                }

                if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
                {
                    message = "Please choose a new password that is different from the current one.";
                    return false;
                }

                string salt = PasswordHasher.GenerateSalt();
                string hash = PasswordHasher.HashPassword(newPassword, salt);

                using (SqlCommand command = new SqlCommand(@"
UPDATE dbo.Users
SET PasswordHash = @PasswordHash,
    PasswordSalt = @PasswordSalt,
    MustChangePassword = 0,
    PasswordChangedAtUtc = GETUTCDATE(),
    FailedLoginCount = 0,
    LockoutEndUtc = NULL
WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", userId);
                    command.Parameters.AddWithValue("@PasswordHash", hash);
                    command.Parameters.AddWithValue("@PasswordSalt", salt);
                    command.ExecuteNonQuery();
                }

                if (string.Equals(target.Username, AuthorizationHelper.BootstrapAdminUsername, StringComparison.OrdinalIgnoreCase))
                {
                    RemoveBootstrapCredentialFile();
                }

                SecurityAuditLogger.Log("PasswordChanged", target.Username, "Password changed successfully.");
                message = "Your password has been updated.";
                return true;
            }
        }

        public static void ValidatePasswordForUse(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
            {
                throw new InvalidOperationException("Password must be at least 12 characters long.");
            }

            if (!password.Any(char.IsUpper))
            {
                throw new InvalidOperationException("Password must include at least one uppercase letter.");
            }

            if (!password.Any(char.IsLower))
            {
                throw new InvalidOperationException("Password must include at least one lowercase letter.");
            }

            if (!password.Any(char.IsDigit))
            {
                throw new InvalidOperationException("Password must include at least one number.");
            }

            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                throw new InvalidOperationException("Password must include at least one symbol.");
            }
        }

        private static UserRecord GetUserRecord(SqlConnection connection, int userId)
        {
            using (SqlCommand command = new SqlCommand(@"
SELECT TOP 1 Id, Username, FullName, Department, [Role], IsActive, PasswordHash, PasswordSalt
FROM dbo.Users
WHERE Id = @Id", connection))
            {
                command.Parameters.AddWithValue("@Id", userId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new UserRecord
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Username = Convert.ToString(reader["Username"]),
                        FullName = Convert.ToString(reader["FullName"]),
                        Department = Convert.ToString(reader["Department"]),
                        Role = Convert.ToString(reader["Role"]),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        PasswordHash = Convert.ToString(reader["PasswordHash"]),
                        PasswordSalt = Convert.ToString(reader["PasswordSalt"])
                    };
                }
            }
        }

        private static bool HasAnotherActiveAdmin(SqlConnection connection, int excludedUserId)
        {
            using (SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.Users
WHERE [Role] = N'Admin'
  AND IsActive = 1
  AND Id <> @Id", connection))
            {
                command.Parameters.AddWithValue("@Id", excludedUserId);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        public static bool MeetsRecommendedAdminCoverage()
        {
            object result = DbHelper.ExecuteScalar(@"
SELECT COUNT(1)
FROM dbo.Users
WHERE [Role] = N'Admin'
  AND IsActive = 1;");

            return Convert.ToInt32(result) >= RecommendedMinimumActiveAdmins;
        }

        private static bool MeetsRecommendedAdminCoverageAfterChange(SqlConnection connection, int excludedUserId)
        {
            using (SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.Users
WHERE [Role] = N'Admin'
  AND IsActive = 1
  AND Id <> @Id;", connection))
            {
                command.Parameters.AddWithValue("@Id", excludedUserId);
                return Convert.ToInt32(command.ExecuteScalar()) >= RecommendedMinimumActiveAdmins;
            }
        }

        private static string NormalizeUserName(string username)
        {
            string normalized = NormalizeRequired(username, "Username");
            if (normalized.Contains(" "))
            {
                throw new InvalidOperationException("Username cannot contain spaces.");
            }

            return normalized;
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

        private static string NormalizeRole(string role)
        {
            string normalized = NormalizeRequired(role, "Role");
            string matchedRole = SupportedRoles.FirstOrDefault(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase));
            if (matchedRole == null)
            {
                throw new InvalidOperationException("The selected role is not supported.");
            }

            return matchedRole;
        }

        private static string GenerateTemporaryPassword()
        {
            const string allowed = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
            char[] chars = new char[18];
            byte[] bytes = new byte[chars.Length];

            using (var random = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                random.GetBytes(bytes);
            }

            for (int index = 0; index < chars.Length; index++)
            {
                chars[index] = allowed[bytes[index] % allowed.Length];
            }

            return new string(chars);
        }

        private static string GetActorName(AuthenticatedUser actor)
        {
            return actor == null ? string.Empty : actor.Username;
        }

        private static void RemoveBootstrapCredentialFile()
        {
            string appDataPath = HostingEnvironment.MapPath("~/App_Data");
            if (string.IsNullOrWhiteSpace(appDataPath))
            {
                return;
            }

            string filePath = Path.Combine(appDataPath, "bootstrap-admin.txt");
            if (!File.Exists(filePath))
            {
                return;
            }

            File.Delete(filePath);
        }

        private sealed class UserRecord
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string FullName { get; set; }
            public string Department { get; set; }
            public string Role { get; set; }
            public bool IsActive { get; set; }
            public string PasswordHash { get; set; }
            public string PasswordSalt { get; set; }
        }
    }
}
