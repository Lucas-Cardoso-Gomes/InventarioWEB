using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Web.Models;

namespace Web.Services
{
    public class PersistentLogService
    {
        private readonly string _connectionString;

        public PersistentLogService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
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
