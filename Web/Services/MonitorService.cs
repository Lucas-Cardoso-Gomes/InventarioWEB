using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Threading.Tasks;
using Web.Models;

namespace Web.Services
{
    public class MonitorService
    {
        private readonly string _connectionString;
        private readonly UserService _userService;

        public MonitorService(IConfiguration configuration, UserService userService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _userService = userService;
        }

        public async Task<(List<Web.Models.Monitor> Monitores, int TotalCount)> GetMonitoresAsync(ClaimsPrincipal user, string searchString, List<string> currentMarcas, List<string> currentTamanhos, List<string> currentModelos, int pageNumber = 1, int pageSize = 25)
        {
            var monitores = new List<Web.Models.Monitor>();
            int totalCount = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var whereClauses = new List<string>();
                var parameters = new Dictionary<string, object>();

                // Role-based access control
                if (user.IsInRole(Role.Normal.ToString()))
                {
                    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier).Value);
                    whereClauses.Add("m.UserId = @UserId");
                    parameters.Add("@UserId", userId);
                }
                else if (user.IsInRole(Role.Coordenador.ToString()))
                {
                    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier).Value);
                    var subordinados = await _userService.GetSubordinadosAsync(userId);
                    var userIds = new List<int> { userId };
                    userIds.AddRange(subordinados.Select(s => s.Id));

                    var userIdsParams = new List<string>();
                    for(int i = 0; i < userIds.Count; i++)
                    {
                        var paramName = $"@userId{i}";
                        userIdsParams.Add(paramName);
                        parameters.Add(paramName, userIds[i]);
                    }
                    whereClauses.Add($"m.UserId IN ({string.Join(",", userIdsParams)})");
                }

                if (!string.IsNullOrEmpty(searchString))
                {
                    whereClauses.Add("(m.PartNumber LIKE @search OR u.Nome LIKE @search OR m.Marca LIKE @search OR m.Modelo LIKE @search)");
                    parameters.Add("@search", $"%{searchString}%");
                }

                Action<string, List<string>> addInClause = (columnName, values) =>
                {
                    if (values != null && values.Any())
                    {
                        var paramNames = new List<string>();
                        for (int i = 0; i < values.Count; i++)
                        {
                            var paramName = $"@{columnName.ToLower().Replace(".", "")}{i}";
                            paramNames.Add(paramName);
                            parameters.Add(paramName, values[i]);
                        }
                        whereClauses.Add($"{columnName} IN ({string.Join(", ", paramNames)})");
                    }
                };

                addInClause("m.Marca", currentMarcas);
                addInClause("m.Tamanho", currentTamanhos);
                addInClause("m.Modelo", currentModelos);

                string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                string countSql = $"SELECT COUNT(*) FROM Monitores m LEFT JOIN Users u ON m.UserId = u.Id {whereSql}";
                using (var countCommand = new SqlCommand(countSql, connection))
                {
                    foreach (var p in parameters) countCommand.Parameters.AddWithValue(p.Key, p.Value);
                    totalCount = (int)await countCommand.ExecuteScalarAsync();
                }

                string sql = $"SELECT m.*, u.Nome as UserName FROM Monitores m LEFT JOIN Users u ON m.UserId = u.Id {whereSql} ORDER BY m.PartNumber OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    foreach (var p in parameters) cmd.Parameters.AddWithValue(p.Key, p.Value);
                    cmd.Parameters.AddWithValue("@offset", (pageNumber - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var monitor = new Web.Models.Monitor
                            {
                                PartNumber = reader["PartNumber"].ToString(),
                                Marca = reader["Marca"].ToString(),
                                Modelo = reader["Modelo"].ToString(),
                                Tamanho = reader["Tamanho"].ToString(),
                                UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : (int?)null
                            };
                            if (reader["UserName"] != DBNull.Value)
                            {
                                monitor.User = new User { Nome = reader["UserName"].ToString() };
                            }
                            monitores.Add(monitor);
                        }
                    }
                }
            }
            return (monitores, totalCount);
        }

        public async Task CreateMonitorAsync(Web.Models.Monitor monitor)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "INSERT INTO Monitores (PartNumber, UserId, Marca, Modelo, Tamanho) VALUES (@PartNumber, @UserId, @Marca, @Modelo, @Tamanho)";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@PartNumber", monitor.PartNumber);
                    cmd.Parameters.AddWithValue("@UserId", (object)monitor.UserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Marca", (object)monitor.Marca ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Modelo", monitor.Modelo);
                    cmd.Parameters.AddWithValue("@Tamanho", (object)monitor.Tamanho ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<Web.Models.Monitor> FindMonitorByIdAsync(string id)
        {
            Web.Models.Monitor monitor = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT m.*, u.Nome as UserName FROM Monitores m LEFT JOIN Users u ON m.UserId = u.Id WHERE m.PartNumber = @PartNumber";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@PartNumber", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            monitor = new Web.Models.Monitor
                            {
                                PartNumber = reader["PartNumber"].ToString(),
                                Marca = reader["Marca"].ToString(),
                                Modelo = reader["Modelo"].ToString(),
                                Tamanho = reader["Tamanho"].ToString(),
                                UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : (int?)null
                            };
                            if (reader["UserName"] != DBNull.Value)
                            {
                                monitor.User = new User { Nome = reader["UserName"].ToString() };
                            }
                        }
                    }
                }
            }
            return monitor;
        }

        public async Task UpdateMonitorAsync(Web.Models.Monitor monitor)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE Monitores SET UserId = @UserId, Marca = @Marca, Modelo = @Modelo, Tamanho = @Tamanho WHERE PartNumber = @PartNumber";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@PartNumber", monitor.PartNumber);
                    cmd.Parameters.AddWithValue("@UserId", (object)monitor.UserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Marca", (object)monitor.Marca ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Modelo", monitor.Modelo);
                    cmd.Parameters.AddWithValue("@Tamanho", (object)monitor.Tamanho ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task DeleteMonitorAsync(string id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM Monitores WHERE PartNumber = @PartNumber";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@PartNumber", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<string>> GetDistinctMonitorValuesAsync(string columnName)
        {
            var values = new List<string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand($"SELECT DISTINCT {columnName} FROM Monitores WHERE {columnName} IS NOT NULL ORDER BY {columnName}", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            values.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return values;
        }
    }
}
