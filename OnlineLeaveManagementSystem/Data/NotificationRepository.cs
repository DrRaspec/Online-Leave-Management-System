using System;
using System.Data;
using System.Data.SqlClient;
using OnlineLeaveManagementSystem.Security;

namespace OnlineLeaveManagementSystem.Data
{
    public static class NotificationRepository
    {
        public static DataTable GetNotificationsForUser(int userId, bool unreadOnly)
        {
            string query = @"
SELECT Id, Title, Message, LinkUrl, IsRead, CreatedAt
FROM dbo.Notifications
WHERE UserId = @UserId";

            if (unreadOnly)
            {
                query += " AND IsRead = 0";
            }

            query += " ORDER BY CreatedAt DESC, Id DESC;";

            return DbHelper.ExecuteDataTable(query, new SqlParameter("@UserId", userId));
        }

        public static int GetUnreadCount(int userId)
        {
            object result = DbHelper.ExecuteScalar(
                "SELECT COUNT(1) FROM dbo.Notifications WHERE UserId = @UserId AND IsRead = 0;",
                new SqlParameter("@UserId", userId));

            return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }

        public static void MarkAllAsRead(int userId)
        {
            DbHelper.ExecuteNonQuery(
                "UPDATE dbo.Notifications SET IsRead = 1 WHERE UserId = @UserId AND IsRead = 0;",
                new SqlParameter("@UserId", userId));
        }

        public static void CreateNotification(int userId, string title, string message, string linkUrl)
        {
            string normalizedTitle = NormalizeRequired(title, "Notification title");
            string normalizedMessage = NormalizeRequired(message, "Notification message");

            DbHelper.ExecuteNonQuery(@"
INSERT INTO dbo.Notifications (UserId, Title, Message, LinkUrl, IsRead, CreatedAt)
VALUES (@UserId, @Title, @Message, @LinkUrl, 0, GETDATE());",
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Title", normalizedTitle),
                new SqlParameter("@Message", normalizedMessage),
                new SqlParameter("@LinkUrl", string.IsNullOrWhiteSpace(linkUrl) ? (object)DBNull.Value : linkUrl));
        }

        public static void NotifyReviewersForSubmission(AuthenticatedUser actor, int requestId)
        {
            DataTable recipients = DbHelper.ExecuteDataTable(@"
SELECT Id
FROM dbo.Users
WHERE IsActive = 1
  AND
  (
      [Role] = N'Admin'
      OR [Role] = N'HR'
      OR ([Role] = N'DepartmentAdmin' AND Department = @Department)
  );",
                new SqlParameter("@Department", actor.Department));

            foreach (DataRow row in recipients.Rows)
            {
                int userId = Convert.ToInt32(row["Id"]);
                if (userId == actor.Id)
                {
                    continue;
                }

                CreateNotification(
                    userId,
                    "New leave request",
                    string.Format("{0} submitted a new leave request for review.", actor.FullName),
                    "~/ManageRequests.aspx");
            }
        }

        public static void NotifyRequesterStatusChanged(int userId, string status)
        {
            CreateNotification(
                userId,
                "Leave request updated",
                string.Format("Your leave request has been marked as {0}.", status),
                "~/MyLeaves.aspx");
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
    }
}
