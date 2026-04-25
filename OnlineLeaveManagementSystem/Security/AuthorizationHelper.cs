using System;

namespace OnlineLeaveManagementSystem.Security
{
    public static class AuthorizationHelper
    {
        public const string BootstrapAdminUsername = "admin";
        public const string AdminRole = "Admin";
        public const string HrRole = "HR";
        public const string DepartmentAdminRole = "DepartmentAdmin";
        public const string UserRole = "User";

        public static bool IsAdmin(AuthenticatedUser user)
        {
            return user != null && string.Equals(user.Role, AdminRole, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsHr(AuthenticatedUser user)
        {
            return user != null && string.Equals(user.Role, HrRole, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDepartmentAdmin(AuthenticatedUser user)
        {
            return user != null && string.Equals(user.Role, DepartmentAdminRole, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsBootstrapAdmin(AuthenticatedUser user)
        {
            return user != null && string.Equals(user.Username, BootstrapAdminUsername, StringComparison.OrdinalIgnoreCase);
        }

        public static bool CanManageRequests(AuthenticatedUser user)
        {
            return IsAdmin(user) || IsHr(user) || IsDepartmentAdmin(user);
        }

        public static bool CanSubmitLeaveRequests(AuthenticatedUser user)
        {
            return user != null && !IsBootstrapAdmin(user);
        }

        public static bool CanViewReports(AuthenticatedUser user)
        {
            return CanManageRequests(user);
        }

        public static bool CanSelectAnyDepartment(AuthenticatedUser user)
        {
            return IsAdmin(user) || IsHr(user);
        }

        public static bool CanManageDepartment(AuthenticatedUser user, string departmentName)
        {
            if (CanSelectAnyDepartment(user))
            {
                return true;
            }

            return IsDepartmentAdmin(user) &&
                   string.Equals((user.Department ?? string.Empty).Trim(), (departmentName ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public static bool CanReviewOwnRequest(AuthenticatedUser user, int requesterUserId)
        {
            return user != null && user.Id != requesterUserId;
        }
    }
}
