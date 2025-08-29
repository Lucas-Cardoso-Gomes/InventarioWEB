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
                            Diretoria = reader.IsDBNull(reader.GetOrdinal("Diretoria")) ? null : reader.GetString(reader.GetOrdinal("Diretoria")),
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
                var command = new SqlCommand("INSERT INTO Usuarios (Nome, Login, PasswordHash, Role, Diretoria, ColaboradorCPF) VALUES (@Nome, @Login, @PasswordHash, @Role, @Diretoria, @ColaboradorCPF)", connection);
                command.Parameters.AddWithValue("@Nome", user.Nome);
                command.Parameters.AddWithValue("@Login", user.Login);
                command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                command.Parameters.AddWithValue("@Role", user.Role);
                command.Parameters.AddWithValue("@Diretoria", (object)user.Diretoria ?? System.DBNull.Value);
                command.Parameters.AddWithValue("@ColaboradorCPF", (object)user.ColaboradorCPF ?? System.DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersWithColaboradoresAsync()
        {
            var users = new List<User>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT u.*, c.Nome as ColaboradorNome FROM Usuarios u LEFT JOIN Colaboradores c ON u.ColaboradorCPF = c.CPF", connection);
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
                            Diretoria = reader.IsDBNull(reader.GetOrdinal("Diretoria")) ? null : reader.GetString(reader.GetOrdinal("Diretoria")),
                            ColaboradorCPF = reader.IsDBNull(reader.GetOrdinal("ColaboradorCPF")) ? null : reader.GetString(reader.GetOrdinal("ColaboradorCPF")),
                        };
                        if (!reader.IsDBNull(reader.GetOrdinal("ColaboradorNome")))
                        {
                            user.Colaborador = new Colaborador { Nome = reader.GetString(reader.GetOrdinal("ColaboradorNome")) };
                        }
                        users.Add(user);
                    }
                }
            }
            return users;
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
                            Diretoria = reader.IsDBNull(reader.GetOrdinal("Diretoria")) ? null : reader.GetString(reader.GetOrdinal("Diretoria")),
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
                var query = new System.Text.StringBuilder("UPDATE Usuarios SET Nome = @Nome, Login = @Login, Role = @Role, Diretoria = @Diretoria, ColaboradorCPF = @ColaboradorCPF");
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    query.Append(", PasswordHash = @PasswordHash");
                }
                query.Append(" WHERE Id = @Id");

                var command = new SqlCommand(query.ToString(), connection);

                command.Parameters.AddWithValue("@Nome", user.Nome);
                command.Parameters.AddWithValue("@Login", user.Login);
                command.Parameters.AddWithValue("@Role", user.Role);
                command.Parameters.AddWithValue("@Diretoria", (object)user.Diretoria ?? System.DBNull.Value);
                command.Parameters.AddWithValue("@ColaboradorCPF", (object)user.ColaboradorCPF ?? System.DBNull.Value);
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
    }
}
