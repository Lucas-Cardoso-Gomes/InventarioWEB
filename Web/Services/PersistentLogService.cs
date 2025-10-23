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

        public Tuple<List<PersistentLog>, int> GetLogs(string entityTypeFilter, string actionTypeFilter, int pageNumber, int pageSize)
        {
            var logs = new List<PersistentLog>();
            int totalRecords = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string whereClause = "WHERE 1=1";
                if (!string.IsNullOrEmpty(entityTypeFilter))
                {
                    whereClause += " AND EntityType = @EntityType";
                }
                if (!string.IsNullOrEmpty(actionTypeFilter))
                {
                    whereClause += " AND ActionType = @ActionType";
                }

                string countSql = $"SELECT COUNT(*) FROM PersistentLogs {whereClause}";
                using (SqlCommand countCmd = new SqlCommand(countSql, connection))
                {
                    if (!string.IsNullOrEmpty(entityTypeFilter))
                    {
                        countCmd.Parameters.AddWithValue("@EntityType", entityTypeFilter);
                    }
                    if (!string.IsNullOrEmpty(actionTypeFilter))
                    {
                        countCmd.Parameters.AddWithValue("@ActionType", actionTypeFilter);
                    }
                    totalRecords = (int)countCmd.ExecuteScalar();
                }

                string sql = $"SELECT Id, Timestamp, EntityType, ActionType, PerformedBy, Details FROM PersistentLogs {whereClause} ORDER BY Timestamp DESC OFFSET {(pageNumber - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
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
            return new Tuple<List<PersistentLog>, int>(logs, totalRecords);
        }
    }
}
