using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Threading.Tasks;
using Web.Models;

namespace Web.Services
{
    public class ComputadorService
    {
        private readonly string _connectionString;
        private readonly UserService _userService;

        public ComputadorService(IConfiguration configuration, UserService userService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _userService = userService;
        }

        public async Task<(List<Computador> Computadores, int TotalCount)> GetComputadoresAsync(ClaimsPrincipal user, string sortOrder, string searchString,
            List<string> currentFabricantes, List<string> currentSOs, List<string> currentProcessadorFabricantes, List<string> currentRamTipos, List<string> currentProcessadores, List<string> currentRams,
            int pageNumber = 1, int pageSize = 25)
        {
            var computadores = new List<Computador>();
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
                    whereClauses.Add("c.UserId = @UserId");
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
                    whereClauses.Add($"c.UserId IN ({string.Join(",", userIdsParams)})");
                }

                if (!string.IsNullOrEmpty(searchString))
                {
                    whereClauses.Add("(c.IP LIKE @search OR c.MAC LIKE @search OR u.Nome LIKE @search OR c.Hostname LIKE @search)");
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

                addInClause("c.Fabricante", currentFabricantes);
                addInClause("c.SO", currentSOs);
                addInClause("c.ProcessadorFabricante", currentProcessadorFabricantes);
                addInClause("c.RamTipo", currentRamTipos);
                addInClause("c.Processador", currentProcessadores);
                addInClause("c.Ram", currentRams);

                string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                string countSql = $"SELECT COUNT(*) FROM Computadores c LEFT JOIN Users u ON c.UserId = u.Id {whereSql}";
                using (var countCommand = new SqlCommand(countSql, connection))
                {
                    foreach (var p in parameters) countCommand.Parameters.AddWithValue(p.Key, p.Value);
                    totalCount = (int)await countCommand.ExecuteScalarAsync();
                }

                string orderBySql;
                switch (sortOrder)
                {
                    case "ip_desc": orderBySql = "ORDER BY c.IP DESC"; break;
                    case "mac": orderBySql = "ORDER BY c.MAC"; break;
                    case "mac_desc": orderBySql = "ORDER BY c.MAC DESC"; break;
                    case "user": orderBySql = "ORDER BY u.Nome"; break;
                    case "user_desc": orderBySql = "ORDER BY u.Nome DESC"; break;
                    case "hostname": orderBySql = "ORDER BY c.Hostname"; break;
                    case "hostname_desc": orderBySql = "ORDER BY c.Hostname DESC"; break;
                    case "os": orderBySql = "ORDER BY c.SO"; break;
                    case "os_desc": orderBySql = "ORDER BY c.SO DESC"; break;
                    case "date": orderBySql = "ORDER BY c.DataColeta"; break;
                    case "date_desc": orderBySql = "ORDER BY c.DataColeta DESC"; break;
                    default: orderBySql = "ORDER BY c.IP"; break;
                }

                string sql = $"SELECT c.*, u.Nome as UserName FROM Computadores c LEFT JOIN Users u ON c.UserId = u.Id {whereSql} {orderBySql} OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    foreach (var p in parameters) cmd.Parameters.AddWithValue(p.Key, p.Value);
                    cmd.Parameters.AddWithValue("@offset", (pageNumber - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var computador = new Computador
                            {
                                MAC = reader["MAC"].ToString(),
                                IP = reader["IP"].ToString(),
                                Hostname = reader["Hostname"].ToString(),
                                Fabricante = reader["Fabricante"].ToString(),
                                Processador = reader["Processador"].ToString(),
                                ProcessadorFabricante = reader["ProcessadorFabricante"].ToString(),
                                ProcessadorCore = reader["ProcessadorCore"].ToString(),
                                ProcessadorThread = reader["ProcessadorThread"].ToString(),
                                ProcessadorClock = reader["ProcessadorClock"].ToString(),
                                Ram = reader["Ram"].ToString(),
                                RamTipo = reader["RamTipo"].ToString(),
                                RamVelocidade = reader["RamVelocidade"].ToString(),
                                RamVoltagem = reader["RamVoltagem"].ToString(),
                                RamPorModule = reader["RamPorModule"].ToString(),
                                ArmazenamentoC = reader["ArmazenamentoC"].ToString(),
                                ArmazenamentoCTotal = reader["ArmazenamentoCTotal"].ToString(),
                                ArmazenamentoCLivre = reader["ArmazenamentoCLivre"].ToString(),
                                ArmazenamentoD = reader["ArmazenamentoD"].ToString(),
                                ArmazenamentoDTotal = reader["ArmazenamentoDTotal"].ToString(),
                                ArmazenamentoDLivre = reader["ArmazenamentoDLivre"].ToString(),
                                ConsumoCPU = reader["ConsumoCPU"].ToString(),
                                SO = reader["SO"].ToString(),
                                DataColeta = reader["DataColeta"] != DBNull.Value ? Convert.ToDateTime(reader["DataColeta"]) : (DateTime?)null,
                                UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : (int?)null
                            };
                            if (reader["UserName"] != DBNull.Value)
                            {
                                computador.User = new User { Nome = reader["UserName"].ToString() };
                            }
                            computadores.Add(computador);
                        }
                    }
                }
            }
            return (computadores, totalCount);
        }

        public async Task<List<string>> GetDistinctComputerValuesAsync(string columnName)
        {
            var values = new List<string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand($"SELECT DISTINCT {columnName} FROM Computadores WHERE {columnName} IS NOT NULL ORDER BY {columnName}", connection))
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

        public async Task CreateComputadorAsync(Computador computador)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "INSERT INTO Computadores (MAC, IP, UserId, Hostname, Fabricante, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, ArmazenamentoC, ArmazenamentoCTotal, ArmazenamentoCLivre, ArmazenamentoD, ArmazenamentoDTotal, ArmazenamentoDLivre, ConsumoCPU, SO, DataColeta) VALUES (@MAC, @IP, @UserId, @Hostname, @Fabricante, @Processador, @ProcessadorFabricante, @ProcessadorCore, @ProcessadorThread, @ProcessadorClock, @Ram, @RamTipo, @RamVelocidade, @RamVoltagem, @RamPorModule, @ArmazenamentoC, @ArmazenamentoCTotal, @ArmazenamentoCLivre, @ArmazenamentoD, @ArmazenamentoDTotal, @ArmazenamentoDLivre, @ConsumoCPU, @SO, @DataColeta)";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@MAC", computador.MAC);
                    cmd.Parameters.AddWithValue("@IP", (object)computador.IP ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserId", (object)computador.UserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Hostname", (object)computador.Hostname ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Fabricante", (object)computador.Fabricante ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Processador", (object)computador.Processador ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProcessadorFabricante", (object)computador.ProcessadorFabricante ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProcessadorCore", (object)computador.ProcessadorCore ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProcessadorThread", (object)computador.ProcessadorThread ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProcessadorClock", (object)computador.ProcessadorClock ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Ram", (object)computador.Ram ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RamTipo", (object)computador.RamTipo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RamVelocidade", (object)computador.RamVelocidade ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RamVoltagem", (object)computador.RamVoltagem ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RamPorModule", (object)computador.RamPorModule ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ArmazenamentoC", (object)computador.ArmazenamentoC ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ArmazenamentoCTotal", (object)computador.ArmazenamentoCTotal ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ArmazenamentoCLivre", (object)computador.ArmazenamentoCLivre ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ArmazenamentoD", (object)computador.ArmazenamentoD ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ArmazenamentoDTotal", (object)computador.ArmazenamentoDTotal ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ArmazenamentoDLivre", (object)computador.ArmazenamentoDLivre ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ConsumoCPU", (object)computador.ConsumoCPU ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SO", (object)computador.SO ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DataColeta", (object)DateTime.Now ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<Computador> FindComputadorByIdAsync(string id)
        {
            Computador computador = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT c.*, u.Nome as UserName FROM Computadores c LEFT JOIN Users u ON c.UserId = u.Id WHERE c.MAC = @MAC";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@MAC", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            computador = new Computador
                            {
                                MAC = reader["MAC"].ToString(),
                                IP = reader["IP"].ToString(),
                                Hostname = reader["Hostname"].ToString(),
                                Fabricante = reader["Fabricante"].ToString(),
                                Processador = reader["Processador"].ToString(),
                                ProcessadorFabricante = reader["ProcessadorFabricante"].ToString(),
                                ProcessadorCore = reader["ProcessadorCore"].ToString(),
                                ProcessadorThread = reader["ProcessadorThread"].ToString(),
                                ProcessadorClock = reader["ProcessadorClock"].ToString(),
                                Ram = reader["Ram"].ToString(),
                                RamTipo = reader["RamTipo"].ToString(),
                                RamVelocidade = reader["RamVelocidade"].ToString(),
                                RamVoltagem = reader["RamVoltagem"].ToString(),
                                RamPorModule = reader["RamPorModule"].ToString(),
                                ArmazenamentoC = reader["ArmazenamentoC"].ToString(),
                                ArmazenamentoCTotal = reader["ArmazenamentoCTotal"].ToString(),
                                ArmazenamentoCLivre = reader["ArmazenamentoCLivre"].ToString(),
                                ArmazenamentoD = reader["ArmazenamentoD"].ToString(),
                                ArmazenamentoDTotal = reader["ArmazenamentoDTotal"].ToString(),
                                ArmazenamentoDLivre = reader["ArmazenamentoDLivre"].ToString(),
                                ConsumoCPU = reader["ConsumoCPU"].ToString(),
                                SO = reader["SO"].ToString(),
                                DataColeta = reader["DataColeta"] != DBNull.Value ? Convert.ToDateTime(reader["DataColeta"]) : (DateTime?)null,
                                UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : (int?)null
                            };
                            if (reader["UserName"] != DBNull.Value)
                            {
                                computador.User = new User { Nome = reader["UserName"].ToString() };
                            }
                        }
                    }
                }
            }
            return computador;
        }

        public async Task UpdateComputadorAsync(Computador computador)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE Computadores SET IP = @IP, UserId = @UserId, Hostname = @Hostname, Fabricante = @Fabricante, Processador = @Processador, ProcessadorFabricante = @ProcessadorFabricante, ProcessadorCore = @ProcessadorCore, ProcessadorThread = @ProcessadorThread, ProcessadorClock = @ProcessadorClock, Ram = @Ram, RamTipo = @RamTipo, RamVelocidade = @RamVelocidade, RamVoltagem = @RamVoltagem, RamPorModule = @RamPorModule, ArmazenamentoC = @ArmazenamentoC, ArmazenamentoCTotal = @ArmazenamentoCTotal, ArmazenamentoCLivre = @ArmazenamentoCLivre, ArmazenamentoD = @ArmazenamentoD, ArmazenamentoDTotal = @ArmazenamentoDTotal, ArmazenamentoDLivre = @ArmazenamentoDLivre, ConsumoCPU = @ConsumoCPU, SO = @SO WHERE MAC = @MAC";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@MAC", computador.MAC);
                    cmd.Parameters.AddWithValue("@IP", (object)computador.IP ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserId", (object)computador.UserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Hostname", (object)computador.Hostname ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Fabricante", (object)computador.Fabricante ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Processador", (object)computador.Processador ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProcessadorFabricante", (object)computador.ProcessadorFabricante ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProcessadorCore", (object)computador.ProcessadorCore ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProcessadorThread", (object)computador.ProcessadorThread ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProcessadorClock", (object)computador.ProcessadorClock ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Ram", (object)computador.Ram ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RamTipo", (object)computador.RamTipo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RamVelocidade", (object)computador.RamVelocidade ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RamVoltagem", (object)computador.RamVoltagem ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RamPorModule", (object)computador.RamPorModule ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ArmazenamentoC", (object)computador.ArmazenamentoC ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ArmazenamentoCTotal", (object)computador.ArmazenamentoCTotal ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ArmazenamentoCLivre", (object)computador.ArmazenamentoCLivre ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ArmazenamentoD", (object)computador.ArmazenamentoD ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ArmazenamentoDTotal", (object)computador.ArmazenamentoDTotal ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ArmazenamentoDLivre", (object)computador.ArmazenamentoDLivre ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ConsumoCPU", (object)computador.ConsumoCPU ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SO", (object)computador.SO ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task DeleteComputadorAsync(string id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM Computadores WHERE MAC = @MAC";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@MAC", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
