using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using web.Models;

namespace Web.Services
{
    public class PersistentLogService
    {
        private readonly string _connectionString;

        public PersistentLogService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public void AddLog(string entityType, string actionType, string performedBy, string details)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "INSERT INTO PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details) VALUES (@Timestamp, @EntityType, @ActionType, @PerformedBy, @Details)";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EntityType", entityType);
                    cmd.Parameters.AddWithValue("@ActionType", actionType);
                    cmd.Parameters.AddWithValue("@PerformedBy", performedBy);
                    cmd.Parameters.AddWithValue("@Details", (object)details ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<PersistentLog> GetLogs(string entityTypeFilter, string actionTypeFilter)
        {
            var logs = new List<PersistentLog>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT Id, Timestamp, EntityType, ActionType, PerformedBy, Details FROM PersistentLogs WHERE 1=1";
                if (!string.IsNullOrEmpty(entityTypeFilter))
                {
                    sql += " AND EntityType = @EntityType";
                }
                if (!string.IsNullOrEmpty(actionTypeFilter))
                {
                    sql += " AND ActionType = @ActionType";
                }
                sql += " ORDER BY Timestamp DESC";

                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    if (!string.IsNullOrEmpty(entityTypeFilter))
                    {
                        cmd.Parameters.AddWithValue("@EntityType", entityTypeFilter);
                    }
                    if (!string.IsNullOrEmpty(actionTypeFilter))
                    {
                        cmd.Parameters.AddWithValue("@ActionType", actionTypeFilter);
                    }

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new PersistentLog
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Timestamp = Convert.ToDateTime(reader["Timestamp"]),
                                EntityType = reader["EntityType"].ToString(),
                                ActionType = reader["ActionType"].ToString(),
                                PerformedBy = reader["PerformedBy"].ToString(),
                                Details = reader["Details"].ToString()
                            });
                        }
                    }
                }
            }
            return logs;
        }
    }
}
