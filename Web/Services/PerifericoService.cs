using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Threading.Tasks;
using Web.Models;

namespace Web.Services
{
    public class PerifericoService
    {
        private readonly string _connectionString;
        private readonly UserService _userService;

        public PerifericoService(IConfiguration configuration, UserService userService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _userService = userService;
        }

        public async Task<(List<Periferico> Perifericos, int TotalCount)> GetPerifericosAsync(ClaimsPrincipal user, string searchString, int pageNumber = 1, int pageSize = 25)
        {
            var perifericos = new List<Periferico>();
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
                    whereClauses.Add("p.UserId = @UserId");
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
                    whereClauses.Add($"p.UserId IN ({string.Join(",", userIdsParams)})");
                }

                if (!string.IsNullOrEmpty(searchString))
                {
                    whereClauses.Add("(p.Tipo LIKE @search OR u.Nome LIKE @search OR p.PartNumber LIKE @search)");
                    parameters.Add("@search", $"%{searchString}%");
                }

                string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                string countSql = $"SELECT COUNT(*) FROM Perifericos p LEFT JOIN Users u ON p.UserId = u.Id {whereSql}";
                using (var countCommand = new SqlCommand(countSql, connection))
                {
                    foreach (var p in parameters) countCommand.Parameters.AddWithValue(p.Key, p.Value);
                    totalCount = (int)await countCommand.ExecuteScalarAsync();
                }

                string sql = $"SELECT p.*, u.Nome as UserName FROM Perifericos p LEFT JOIN Users u ON p.UserId = u.Id {whereSql} ORDER BY p.ID OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    foreach (var p in parameters) cmd.Parameters.AddWithValue(p.Key, p.Value);
                    cmd.Parameters.AddWithValue("@offset", (pageNumber - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var periferico = new Periferico
                            {
                                ID = Convert.ToInt32(reader["ID"]),
                                Tipo = reader["Tipo"].ToString(),
                                PartNumber = reader["PartNumber"].ToString(),
                                DataEntrega = reader["DataEntrega"] != DBNull.Value ? Convert.ToDateTime(reader["DataEntrega"]) : (DateTime?)null,
                                UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : (int?)null
                            };
                            if (reader["UserName"] != DBNull.Value)
                            {
                                periferico.User = new User { Nome = reader["UserName"].ToString() };
                            }
                            perifericos.Add(periferico);
                        }
                    }
                }
            }
            return (perifericos, totalCount);
        }

        public async Task CreatePerifericoAsync(Periferico periferico)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "INSERT INTO Perifericos (UserId, Tipo, PartNumber, DataEntrega) VALUES (@UserId, @Tipo, @PartNumber, @DataEntrega)";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", (object)periferico.UserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Tipo", periferico.Tipo);
                    cmd.Parameters.AddWithValue("@PartNumber", (object)periferico.PartNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DataEntrega", (object)periferico.DataEntrega ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<Periferico> FindPerifericoByIdAsync(int id)
        {
            Periferico periferico = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT p.*, u.Nome as UserName FROM Perifericos p LEFT JOIN Users u ON p.UserId = u.Id WHERE p.ID = @ID";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            periferico = new Periferico
                            {
                                ID = Convert.ToInt32(reader["ID"]),
                                Tipo = reader["Tipo"].ToString(),
                                PartNumber = reader["PartNumber"].ToString(),
                                DataEntrega = reader["DataEntrega"] != DBNull.Value ? Convert.ToDateTime(reader["DataEntrega"]) : (DateTime?)null,
                                UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : (int?)null
                            };
                            if (reader["UserName"] != DBNull.Value)
                            {
                                periferico.User = new User { Nome = reader["UserName"].ToString() };
                            }
                        }
                    }
                }
            }
            return periferico;
        }

        public async Task UpdatePerifericoAsync(Periferico periferico)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE Perifericos SET UserId = @UserId, Tipo = @Tipo, PartNumber = @PartNumber, DataEntrega = @DataEntrega WHERE ID = @ID";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@ID", periferico.ID);
                    cmd.Parameters.AddWithValue("@UserId", (object)periferico.UserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Tipo", periferico.Tipo);
                    cmd.Parameters.AddWithValue("@PartNumber", (object)periferico.PartNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DataEntrega", (object)periferico.DataEntrega ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task DeletePerifericoAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM Perifericos WHERE ID = @ID";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
