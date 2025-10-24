using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Web.Models;
using System.Collections.Generic;

namespace Web.Services
{
    public class UserService
    {
        private readonly string _connectionString;

        public UserService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<User> FindByLoginAsync(string login)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM Usuarios WHERE Login = @Login", connection);
                command.Parameters.AddWithValue("@Login", login);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new User
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Nome = reader.GetString(reader.GetOrdinal("Nome")),
                            Login = reader.GetString(reader.GetOrdinal("Login")),
                            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                            Role = reader.GetString(reader.GetOrdinal("Role")),
                            IsCoordinator = reader.GetBoolean(reader.GetOrdinal("IsCoordinator")),
                            ColaboradorCPF = reader.IsDBNull(reader.GetOrdinal("ColaboradorCPF")) ? null : reader.GetString(reader.GetOrdinal("ColaboradorCPF"))
                        };
                    }
                }
            }
            return null;
        }

        public async Task CreateAsync(User user)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("INSERT INTO Usuarios (Nome, Login, PasswordHash, Role, ColaboradorCPF, IsCoordinator) VALUES (@Nome, @Login, @PasswordHash, @Role, @ColaboradorCPF, @IsCoordinator)", connection);
                command.Parameters.AddWithValue("@Nome", user.Nome);
                command.Parameters.AddWithValue("@Login", user.Login);
                command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                command.Parameters.AddWithValue("@Role", user.Role);
                command.Parameters.AddWithValue("@ColaboradorCPF", (object)user.ColaboradorCPF ?? System.DBNull.Value);
                command.Parameters.AddWithValue("@IsCoordinator", user.IsCoordinator);

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<UserIndexViewModel> GetAllUsersWithColaboradoresAsync(string sortOrder, string searchString, List<string> currentRoles, int pageNumber, int pageSize)
        {
            var viewModel = new UserIndexViewModel
            {
                Users = new List<User>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchString = searchString,
                CurrentSort = sortOrder,
                CurrentRoles = currentRoles ?? new List<string>()
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                viewModel.Roles = await GetDistinctUserValuesAsync(connection, "Role");

                var whereClauses = new List<string>();
                var parameters = new Dictionary<string, object>();
                string baseSql = "FROM Usuarios u LEFT JOIN Colaboradores c ON u.ColaboradorCPF = c.CPF";

                if (!string.IsNullOrEmpty(searchString))
                {
                    whereClauses.Add("(u.Nome LIKE @search OR u.Login LIKE @search OR c.Nome LIKE @search)");
                    parameters.Add("@search", $"%{searchString}%");
                }

                if (currentRoles != null && currentRoles.Any())
                {
                    var roleParams = new List<string>();
                    for (int i = 0; i < currentRoles.Count; i++)
                    {
                        var paramName = $"@role{i}";
                        roleParams.Add(paramName);
                        parameters.Add(paramName, currentRoles[i]);
                    }
                    whereClauses.Add($"u.Role IN ({string.Join(", ", roleParams)})");
                }

                string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";
                string countSql = $"SELECT COUNT(u.Id) {baseSql} {whereSql}";

                using (var countCommand = new SqlCommand(countSql, connection))
                {
                    foreach (var p in parameters)
                    {
                        countCommand.Parameters.AddWithValue(p.Key, p.Value);
                    }
                    viewModel.TotalCount = (int)await countCommand.ExecuteScalarAsync();
                }

                string orderBySql;
                switch (sortOrder)
                {
                    case "nome_desc":
                        orderBySql = "ORDER BY u.Nome DESC";
                        break;
                    case "login":
                        orderBySql = "ORDER BY u.Login";
                        break;
                    case "login_desc":
                        orderBySql = "ORDER BY u.Login DESC";
                        break;
                    case "role":
                        orderBySql = "ORDER BY u.Role";
                        break;
                    case "role_desc":
                        orderBySql = "ORDER BY u.Role DESC";
                        break;
                    default:
                        orderBySql = "ORDER BY u.Nome";
                        break;
                }

                string sql = $"SELECT u.*, c.Nome as ColaboradorNome {baseSql} {whereSql} {orderBySql} OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
                using (var command = new SqlCommand(sql, connection))
                {
                    foreach (var p in parameters)
                    {
                        command.Parameters.AddWithValue(p.Key, p.Value);
                    }
                    command.Parameters.AddWithValue("@offset", (pageNumber - 1) * pageSize);
                    command.Parameters.AddWithValue("@pageSize", pageSize);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var user = new User
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Nome = reader.GetString(reader.GetOrdinal("Nome")),
                                Login = reader.GetString(reader.GetOrdinal("Login")),
                                Role = reader.GetString(reader.GetOrdinal("Role")),
                                IsCoordinator = reader.GetBoolean(reader.GetOrdinal("IsCoordinator")),
                                ColaboradorCPF = reader.IsDBNull(reader.GetOrdinal("ColaboradorCPF")) ? null : reader.GetString(reader.GetOrdinal("ColaboradorCPF")),
                                Colaborador = new Colaborador()
                            };
                            if (!reader.IsDBNull(reader.GetOrdinal("ColaboradorNome")))
                            {
                                user.Colaborador.Nome = reader.GetString(reader.GetOrdinal("ColaboradorNome"));
                            }
                            viewModel.Users.Add(user);
                        }
                    }
                }
            }
            return viewModel;
        }

        private async Task<List<string>> GetDistinctUserValuesAsync(SqlConnection connection, string columnName)
        {
            var values = new List<string>();
            using (var command = new SqlCommand($"SELECT DISTINCT {columnName} FROM Usuarios WHERE {columnName} IS NOT NULL ORDER BY {columnName}", connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        values.Add(reader.GetString(0));
                    }
                }
            }
            return values;
        }

        public async Task<User> FindByIdAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM Usuarios WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new User
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Nome = reader.GetString(reader.GetOrdinal("Nome")),
                            Login = reader.GetString(reader.GetOrdinal("Login")),
                            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                            Role = reader.GetString(reader.GetOrdinal("Role")),
                            IsCoordinator = reader.GetBoolean(reader.GetOrdinal("IsCoordinator")),
                            ColaboradorCPF = reader.IsDBNull(reader.GetOrdinal("ColaboradorCPF")) ? null : reader.GetString(reader.GetOrdinal("ColaboradorCPF"))
                        };
                    }
                }
            }
            return null;
        }

        public async Task UpdateAsync(User user)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = new System.Text.StringBuilder("UPDATE Usuarios SET Nome = @Nome, Login = @Login, Role = @Role, ColaboradorCPF = @ColaboradorCPF, IsCoordinator = @IsCoordinator");
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    query.Append(", PasswordHash = @PasswordHash");
                }
                query.Append(" WHERE Id = @Id");

                var command = new SqlCommand(query.ToString(), connection);

                command.Parameters.AddWithValue("@Nome", user.Nome);
                command.Parameters.AddWithValue("@Login", user.Login);
                command.Parameters.AddWithValue("@Role", user.Role);
                command.Parameters.AddWithValue("@ColaboradorCPF", (object)user.ColaboradorCPF ?? System.DBNull.Value);
                command.Parameters.AddWithValue("@IsCoordinator", user.IsCoordinator);
                command.Parameters.AddWithValue("@Id", user.Id);
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                }

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("DELETE FROM Usuarios WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<IEnumerable<Colaborador>> GetAllColaboradoresAsync()
        {
            var colaboradores = new List<Colaborador>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM Colaboradores", connection);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var colaborador = new Colaborador
                        {
                            CPF = reader.GetString(reader.GetOrdinal("CPF")),
                            Nome = reader.GetString(reader.GetOrdinal("Nome")),
                            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                            SenhaEmail = reader.IsDBNull(reader.GetOrdinal("SenhaEmail")) ? null : reader.GetString(reader.GetOrdinal("SenhaEmail"))
                        };
                        colaboradores.Add(colaborador);
                    }
                }
            }
            return colaboradores;
        }
    }
}
