using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Web.Models;

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
                            Role = reader.GetString(reader.GetOrdinal("Role"))
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
                var command = new SqlCommand("INSERT INTO Usuarios (Nome, Login, PasswordHash, Role) VALUES (@Nome, @Login, @PasswordHash, @Role)", connection);
                command.Parameters.AddWithValue("@Nome", user.Nome);
                command.Parameters.AddWithValue("@Login", user.Login);
                command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                command.Parameters.AddWithValue("@Role", user.Role);

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
