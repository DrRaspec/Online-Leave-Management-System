using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;

namespace OnlineLeaveManagementSystem.Security
{
    public static class SecurityAuditLogger
    {
        private const string ConnectionStringName = "LeaveManagementConnection";

        public static void Log(string eventType, string userName, string details)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString))
                using (SqlCommand command = new SqlCommand(@"
INSERT INTO dbo.SecurityAuditLog (EventType, UserName, IpAddress, UserAgent, Details, CreatedAtUtc)
VALUES (@EventType, @UserName, @IpAddress, @UserAgent, @Details, GETUTCDATE())", connection))
                {
                    HttpRequest request = HttpContext.Current == null ? null : HttpContext.Current.Request;

                    command.Parameters.AddWithValue("@EventType", eventType ?? string.Empty);
                    command.Parameters.AddWithValue("@UserName", string.IsNullOrWhiteSpace(userName) ? (object)DBNull.Value : userName.Trim());
                    command.Parameters.AddWithValue("@IpAddress", request == null ? (object)DBNull.Value : (object)(request.UserHostAddress ?? string.Empty));
                    command.Parameters.AddWithValue("@UserAgent", request == null ? (object)DBNull.Value : (object)(request.UserAgent ?? string.Empty));
                    command.Parameters.AddWithValue("@Details", string.IsNullOrWhiteSpace(details) ? (object)DBNull.Value : details.Trim());

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch
            {
            }
        }
    }
}
