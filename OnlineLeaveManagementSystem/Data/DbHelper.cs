using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using OnlineLeaveManagementSystem.Infrastructure;

namespace OnlineLeaveManagementSystem.Data
{
    public static class DbHelper
    {
        private const string ConnectionStringName = "LeaveManagementConnection";
        private static bool isInitialized;

        public static SqlConnection GetOpenConnection()
        {
            EnsureInitialized();
            SqlConnection connection = new SqlConnection(GetConnectionString());
            connection.Open();
            return connection;
        }

        public static string GetConnectionString()
        {
            EnsureInitialized();
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[ConnectionStringName];

            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    "Connection string 'LeaveManagementConnection' was not found in Web.config.");
            }

            return settings.ConnectionString;
        }

        private static void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            DatabaseInitializer.EnsureDatabase();
            isInitialized = true;
        }

        public static DataTable ExecuteDataTable(string query, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = GetOpenConnection())
            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);
                return dataTable;
            }
        }

        public static int ExecuteNonQuery(string query, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = GetOpenConnection())
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                return command.ExecuteNonQuery();
            }
        }

        public static object ExecuteScalar(string query, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = GetOpenConnection())
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                return command.ExecuteScalar();
            }
        }
    }
}
