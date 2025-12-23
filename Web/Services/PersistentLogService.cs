using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Web.Models;
using System.Data;

namespace Web.Services
{
    public class PersistentLogService
    {
        private readonly IDatabaseService _databaseService;

        public PersistentLogService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public Tuple<List<PersistentLog>, int> GetLogs(string entityTypeFilter, string actionTypeFilter, int pageNumber, int pageSize)
        {
            var logs = new List<PersistentLog>();
            int totalRecords = 0;
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string whereClause = "WHERE 1=1";
                var parameters = new List<Action<IDbCommand>>();

                if (!string.IsNullOrEmpty(entityTypeFilter))
                {
                    whereClause += " AND EntityType = @EntityType";
                    parameters.Add(cmd => {
                        var p = cmd.CreateParameter(); p.ParameterName = "@EntityType"; p.Value = entityTypeFilter; cmd.Parameters.Add(p);
                    });
                }
                if (!string.IsNullOrEmpty(actionTypeFilter))
                {
                    whereClause += " AND ActionType = @ActionType";
                    parameters.Add(cmd => {
                        var p = cmd.CreateParameter(); p.ParameterName = "@ActionType"; p.Value = actionTypeFilter; cmd.Parameters.Add(p);
                    });
                }

                string countSql = $"SELECT COUNT(*) FROM PersistentLogs {whereClause}";
                using (var countCmd = connection.CreateCommand())
                {
                    countCmd.CommandText = countSql;
                    foreach (var paramAction in parameters) paramAction(countCmd);
                    var result = countCmd.ExecuteScalar();
                    totalRecords = result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }

                string sql = $"SELECT Id, Timestamp, EntityType, ActionType, PerformedBy, Details FROM PersistentLogs {whereClause} ORDER BY Timestamp DESC LIMIT @PageSize OFFSET @Offset";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    foreach (var paramAction in parameters) paramAction(cmd);

                    var pSize = cmd.CreateParameter(); pSize.ParameterName = "@PageSize"; pSize.Value = pageSize; cmd.Parameters.Add(pSize);
                    var pOffset = cmd.CreateParameter(); pOffset.ParameterName = "@Offset"; pOffset.Value = (pageNumber - 1) * pageSize; cmd.Parameters.Add(pOffset);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var log = new PersistentLog();
                            log.Id = Convert.ToInt32(reader["Id"]);

                            var timestampObj = reader["Timestamp"];
                            if (timestampObj != DBNull.Value && DateTime.TryParse(timestampObj.ToString(), out DateTime dt))
                            {
                                log.Timestamp = dt;
                            }
                            else
                            {
                                log.Timestamp = DateTime.MinValue;
                            }

                            log.EntityType = reader["EntityType"] != DBNull.Value ? reader["EntityType"].ToString() : string.Empty;
                            log.ActionType = reader["ActionType"] != DBNull.Value ? reader["ActionType"].ToString() : string.Empty;
                            log.PerformedBy = reader["PerformedBy"] != DBNull.Value ? reader["PerformedBy"].ToString() : string.Empty;
                            log.Details = reader["Details"] != DBNull.Value ? reader["Details"].ToString() : string.Empty;

                            logs.Add(log);
                        }
                    }
                }
            }
            return new Tuple<List<PersistentLog>, int>(logs, totalRecords);
        }
    }
}
