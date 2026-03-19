using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;
using Web.Models;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System;

namespace Web.Services
{
    public class UserService
    {
        private readonly IDatabaseService _databaseService;

        public UserService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<User> FindByLoginAsync(string login)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                // Note: IDbConnection doesn't have OpenAsync, use Open
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Usuarios WHERE Login = @Login";
                    var p = command.CreateParameter(); p.ParameterName = "@Login"; p.Value = login; command.Parameters.Add(p);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nome = reader["Nome"].ToString(),
                                Login = reader["Login"].ToString(),
                                PasswordHash = reader["PasswordHash"].ToString(),
                                Role = reader["Role"].ToString(),
                                IsCoordinator = Convert.ToBoolean(reader["IsCoordinator"]),
                                ColaboradorCPF = reader["ColaboradorCPF"] != DBNull.Value ? reader["ColaboradorCPF"].ToString() : null
                            };
                        }
                    }
                }
            }
            return await Task.FromResult<User>(null);
        }

        public async Task CreateAsync(User user)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Usuarios (Nome, Login, PasswordHash, Role, ColaboradorCPF, IsCoordinator) VALUES (@Nome, @Login, @PasswordHash, @Role, @ColaboradorCPF, @IsCoordinator)";

                    var p1 = command.CreateParameter(); p1.ParameterName = "@Nome"; p1.Value = user.Nome; command.Parameters.Add(p1);
                    var p2 = command.CreateParameter(); p2.ParameterName = "@Login"; p2.Value = user.Login; command.Parameters.Add(p2);
                    var p3 = command.CreateParameter(); p3.ParameterName = "@PasswordHash"; p3.Value = user.PasswordHash; command.Parameters.Add(p3);
                    var p4 = command.CreateParameter(); p4.ParameterName = "@Role"; p4.Value = user.Role; command.Parameters.Add(p4);
                    var p5 = command.CreateParameter(); p5.ParameterName = "@ColaboradorCPF"; p5.Value = (object)user.ColaboradorCPF ?? DBNull.Value; command.Parameters.Add(p5);
                    var p6 = command.CreateParameter(); p6.ParameterName = "@IsCoordinator"; p6.Value = user.IsCoordinator ? 1 : 0; command.Parameters.Add(p6);

                    command.ExecuteNonQuery();
                }
            }
            await Task.CompletedTask;
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

            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();

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

                using (var countCommand = connection.CreateCommand())
                {
                    countCommand.CommandText = countSql;
                    foreach (var p in parameters)
                    {
                        var param = countCommand.CreateParameter(); param.ParameterName = p.Key; param.Value = p.Value; countCommand.Parameters.Add(param);
                    }
                    var result = countCommand.ExecuteScalar();
                    viewModel.TotalCount = result != DBNull.Value ? Convert.ToInt32(result) : 0;
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

                string sql = $"SELECT u.*, c.Nome as ColaboradorNome {baseSql} {whereSql} {orderBySql} LIMIT @pageSize OFFSET @offset";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    foreach (var p in parameters)
                    {
                        var param = command.CreateParameter(); param.ParameterName = p.Key; param.Value = p.Value; command.Parameters.Add(param);
                    }
                    var pOffset = command.CreateParameter(); pOffset.ParameterName = "@offset"; pOffset.Value = (pageNumber - 1) * pageSize; command.Parameters.Add(pOffset);
                    var pPageSize = command.CreateParameter(); pPageSize.ParameterName = "@pageSize"; pPageSize.Value = pageSize; command.Parameters.Add(pPageSize);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var user = new User
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nome = reader["Nome"].ToString(),
                                Login = reader["Login"].ToString(),
                                Role = reader["Role"].ToString(),
                                IsCoordinator = Convert.ToBoolean(reader["IsCoordinator"]),
                                ColaboradorCPF = reader["ColaboradorCPF"] != DBNull.Value ? reader["ColaboradorCPF"].ToString() : null,
                                Colaborador = new Colaborador()
                            };
                            if (reader["ColaboradorNome"] != DBNull.Value)
                            {
                                user.Colaborador.Nome = reader["ColaboradorNome"].ToString();
                            }
                            viewModel.Users.Add(user);
                        }
                    }
                }
            }
            return viewModel;
        }

        private async Task<List<string>> GetDistinctUserValuesAsync(IDbConnection connection, string columnName)
        {
            var values = new List<string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT DISTINCT {columnName} FROM Usuarios WHERE {columnName} IS NOT NULL ORDER BY {columnName}";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        values.Add(reader[0].ToString());
                    }
                }
            }
            return await Task.FromResult(values);
        }

        public async Task<User> FindByIdAsync(int id)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Usuarios WHERE Id = @Id";
                    var p = command.CreateParameter(); p.ParameterName = "@Id"; p.Value = id; command.Parameters.Add(p);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nome = reader["Nome"].ToString(),
                                Login = reader["Login"].ToString(),
                                PasswordHash = reader["PasswordHash"].ToString(),
                                Role = reader["Role"].ToString(),
                                IsCoordinator = Convert.ToBoolean(reader["IsCoordinator"]),
                                ColaboradorCPF = reader["ColaboradorCPF"] != DBNull.Value ? reader["ColaboradorCPF"].ToString() : null
                            };
                        }
                    }
                }
            }
            return await Task.FromResult<User>(null);
        }

        public async Task UpdateAsync(User user)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                var query = new System.Text.StringBuilder("UPDATE Usuarios SET Nome = @Nome, Login = @Login, Role = @Role, ColaboradorCPF = @ColaboradorCPF, IsCoordinator = @IsCoordinator");
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    query.Append(", PasswordHash = @PasswordHash");
                }
                query.Append(" WHERE Id = @Id");

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query.ToString();

                    var p1 = command.CreateParameter(); p1.ParameterName = "@Nome"; p1.Value = user.Nome; command.Parameters.Add(p1);
                    var p2 = command.CreateParameter(); p2.ParameterName = "@Login"; p2.Value = user.Login; command.Parameters.Add(p2);
                    var p3 = command.CreateParameter(); p3.ParameterName = "@Role"; p3.Value = user.Role; command.Parameters.Add(p3);
                    var p4 = command.CreateParameter(); p4.ParameterName = "@ColaboradorCPF"; p4.Value = (object)user.ColaboradorCPF ?? DBNull.Value; command.Parameters.Add(p4);
                    var p5 = command.CreateParameter(); p5.ParameterName = "@IsCoordinator"; p5.Value = user.IsCoordinator ? 1 : 0; command.Parameters.Add(p5);
                    var p6 = command.CreateParameter(); p6.ParameterName = "@Id"; p6.Value = user.Id; command.Parameters.Add(p6);

                    if (!string.IsNullOrEmpty(user.PasswordHash))
                    {
                        var p7 = command.CreateParameter(); p7.ParameterName = "@PasswordHash"; p7.Value = user.PasswordHash; command.Parameters.Add(p7);
                    }

                    command.ExecuteNonQuery();
                }
            }
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Usuarios WHERE Id = @Id";
                    var p = command.CreateParameter(); p.ParameterName = "@Id"; p.Value = id; command.Parameters.Add(p);
                    command.ExecuteNonQuery();
                }
            }
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<Colaborador>> GetAllColaboradoresAsync()
        {
            var colaboradores = new List<Colaborador>();
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Colaboradores";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var colaborador = new Colaborador
                            {
                                CPF = reader["CPF"].ToString(),
                                Nome = reader["Nome"].ToString(),
                                Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null,
                                SenhaEmail = reader["SenhaEmail"] != DBNull.Value ? reader["SenhaEmail"].ToString() : null
                            };
                            colaboradores.Add(colaborador);
                        }
                    }
                }
            }
            return await Task.FromResult(colaboradores);
        }
    }
}
