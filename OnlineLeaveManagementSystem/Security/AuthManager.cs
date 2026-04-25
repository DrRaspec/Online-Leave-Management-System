using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;
using System.Web.Security;

namespace OnlineLeaveManagementSystem.Security
{
    public enum SignInStatus
    {
        Success,
        InvalidCredentials,
        LockedOut,
        Inactive
    }

    public sealed class AuthenticatedUser
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public int? DepartmentId { get; set; }
        public string Department { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }
    }

    public sealed class SignInResult
    {
        public SignInStatus Status { get; set; }
        public AuthenticatedUser User { get; set; }
        public string Message { get; set; }
    }

    public static class AuthManager
    {
        private const string ConnectionStringName = "LeaveManagementConnection";
        private const string RequestUserKey = "__CurrentUser";
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public static SignInResult ValidateUser(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return Invalid("Username and password are required.");
            }

            string normalizedUserName = username.Trim();

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString))
            using (SqlCommand command = new SqlCommand(@"
SELECT TOP 1 Id, Username, PasswordHash, PasswordSalt, FullName, DepartmentId, Department, [Role], IsActive, FailedLoginCount, LockoutEndUtc, MustChangePassword
FROM dbo.Users
WHERE Username = @Username", connection))
            {
                command.Parameters.AddWithValue("@Username", normalizedUserName);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        SecurityAuditLogger.Log("LoginFailed", normalizedUserName, "Unknown username.");
                        return Invalid("Invalid username or password.");
                    }

                    int userId = Convert.ToInt32(reader["Id"]);
                    bool isActive = Convert.ToBoolean(reader["IsActive"]);
                    DateTime? lockoutEndUtc = reader["LockoutEndUtc"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LockoutEndUtc"]);

                    if (!isActive)
                    {
                        SecurityAuditLogger.Log("LoginBlocked", normalizedUserName, "Inactive account.");
                        return new SignInResult
                        {
                            Status = SignInStatus.Inactive,
                            Message = "This account is inactive. Please contact an administrator for access."
                        };
                    }

                    if (lockoutEndUtc.HasValue && lockoutEndUtc.Value > DateTime.UtcNow)
                    {
                        SecurityAuditLogger.Log("LoginLockedOut", normalizedUserName, "Account is temporarily locked.");
                        return new SignInResult
                        {
                            Status = SignInStatus.LockedOut,
                            Message = "Too many failed login attempts. Please wait 15 minutes and try again, or contact an administrator if you need help."
                        };
                    }

                    string salt = Convert.ToString(reader["PasswordSalt"]);
                    string hash = Convert.ToString(reader["PasswordHash"]);
                    if (!PasswordHasher.VerifyPassword(password, salt, hash))
                    {
                        reader.Close();
                        RegisterFailedLogin(connection, userId, normalizedUserName);
                        return Invalid("Invalid username or password.");
                    }

                    reader.Close();
                    ResetFailedLogin(connection, userId);

                    AuthenticatedUser user = GetUserById(connection, userId);
                    SecurityAuditLogger.Log("LoginSucceeded", normalizedUserName, "Successful sign-in.");

                    return new SignInResult
                    {
                        Status = SignInStatus.Success,
                        User = user,
                        Message = "Sign-in successful."
                    };
                }
            }
        }

        public static AuthenticatedUser GetCurrentUser()
        {
            HttpContext context = HttpContext.Current;
            if (context == null)
            {
                return null;
            }

            object cached = context.Items[RequestUserKey];
            if (cached is AuthenticatedUser)
            {
                return (AuthenticatedUser)cached;
            }

            if (context.User == null || context.User.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                return null;
            }

            string username = context.User.Identity.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString))
            using (SqlCommand command = new SqlCommand(@"
SELECT TOP 1 Id, Username, FullName, DepartmentId, Department, [Role], IsActive, MustChangePassword
FROM dbo.Users
WHERE Username = @Username", connection))
            {
                command.Parameters.AddWithValue("@Username", username.Trim());
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read() || !Convert.ToBoolean(reader["IsActive"]))
                    {
                        return null;
                    }

                    AuthenticatedUser user = new AuthenticatedUser
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Username = Convert.ToString(reader["Username"]),
                        FullName = Convert.ToString(reader["FullName"]),
                        DepartmentId = reader["DepartmentId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["DepartmentId"]),
                        Department = Convert.ToString(reader["Department"]),
                        Role = Convert.ToString(reader["Role"]),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        MustChangePassword = reader["MustChangePassword"] != DBNull.Value && Convert.ToBoolean(reader["MustChangePassword"])
                    };

                    context.Items[RequestUserKey] = user;
                    return user;
                }
            }
        }

        public static void SignIn(string username, bool isPersistent)
        {
            FormsAuthentication.SetAuthCookie(username, isPersistent);
        }

        public static void SignOut()
        {
            string userName = HttpContext.Current != null && HttpContext.Current.User != null ? HttpContext.Current.User.Identity.Name : string.Empty;
            SecurityAuditLogger.Log("Logout", userName, "User signed out.");
            FormsAuthentication.SignOut();
        }

        private static SignInResult Invalid(string message)
        {
            return new SignInResult
            {
                Status = SignInStatus.InvalidCredentials,
                Message = message
            };
        }

        private static void RegisterFailedLogin(SqlConnection connection, int userId, string userName)
        {
            using (SqlCommand command = new SqlCommand(@"
UPDATE dbo.Users
SET FailedLoginCount = FailedLoginCount + 1,
    LockoutEndUtc = CASE WHEN FailedLoginCount + 1 >= @MaxFailedAttempts THEN DATEADD(MINUTE, @LockoutMinutes, GETUTCDATE()) ELSE NULL END
WHERE Id = @Id", connection))
            {
                command.Parameters.AddWithValue("@Id", userId);
                command.Parameters.AddWithValue("@MaxFailedAttempts", MaxFailedAttempts);
                command.Parameters.AddWithValue("@LockoutMinutes", (int)LockoutDuration.TotalMinutes);
                command.ExecuteNonQuery();
            }

            SecurityAuditLogger.Log("LoginFailed", userName, "Invalid password.");
        }

        private static void ResetFailedLogin(SqlConnection connection, int userId)
        {
            using (SqlCommand command = new SqlCommand(@"
UPDATE dbo.Users
SET FailedLoginCount = 0,
    LockoutEndUtc = NULL,
    LastLoginUtc = GETUTCDATE()
WHERE Id = @Id", connection))
            {
                command.Parameters.AddWithValue("@Id", userId);
                command.ExecuteNonQuery();
            }
        }

        private static AuthenticatedUser GetUserById(SqlConnection connection, int userId)
        {
            using (SqlCommand command = new SqlCommand(@"
SELECT TOP 1 Id, Username, FullName, DepartmentId, Department, [Role], IsActive, MustChangePassword
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

                    return new AuthenticatedUser
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Username = Convert.ToString(reader["Username"]),
                        FullName = Convert.ToString(reader["FullName"]),
                        DepartmentId = reader["DepartmentId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["DepartmentId"]),
                        Department = Convert.ToString(reader["Department"]),
                        Role = Convert.ToString(reader["Role"]),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        MustChangePassword = reader["MustChangePassword"] != DBNull.Value && Convert.ToBoolean(reader["MustChangePassword"])
                    };
                }
            }
        }
    }
}
