using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
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
                        return MapUserFromReader(reader);
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
                var command = new SqlCommand(
                    "INSERT INTO Usuarios (Nome, Login, PasswordHash, Role, CPF, Email, SenhaEmail, Teams, SenhaTeams, EDespacho, SenhaEDespacho, Genius, SenhaGenius, Ibrooker, SenhaIbrooker, Adicional, SenhaAdicional, Setor, Smartphone, TelefoneFixo, Ramal, Alarme, Videoporteiro, Obs, DataInclusao, SupervisorId) " +
                    "VALUES (@Nome, @Login, @PasswordHash, @Role, @CPF, @Email, @SenhaEmail, @Teams, @SenhaTeams, @EDespacho, @SenhaEDespacho, @Genius, @SenhaGenius, @Ibrooker, @SenhaIbrooker, @Adicional, @SenhaAdicional, @Setor, @Smartphone, @TelefoneFixo, @Ramal, @Alarme, @Videoporteiro, @Obs, @DataInclusao, @SupervisorId)", connection);

                AddUserParameters(command, user);

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM Usuarios", connection);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(MapUserFromReader(reader));
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
                        return MapUserFromReader(reader);
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
                var query = "UPDATE Usuarios SET Nome = @Nome, Login = @Login, Role = @Role, CPF = @CPF, Email = @Email, SenhaEmail = @SenhaEmail, Teams = @Teams, SenhaTeams = @SenhaTeams, EDespacho = @EDespacho, SenhaEDespacho = @SenhaEDespacho, Genius = @Genius, SenhaGenius = @SenhaGenius, Ibrooker = @Ibrooker, SenhaIbrooker = @SenhaIbrooker, Adicional = @Adicional, SenhaAdicional = @SenhaAdicional, Setor = @Setor, Smartphone = @Smartphone, TelefoneFixo = @TelefoneFixo, Ramal = @Ramal, Alarme = @Alarme, Videoporteiro = @Videoporteiro, Obs = @Obs, DataAlteracao = @DataAlteracao, SupervisorId = @SupervisorId";
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    query += ", PasswordHash = @PasswordHash";
                }
                query += " WHERE Id = @Id";

                var command = new SqlCommand(query, connection);
                AddUserParameters(command, user);
                command.Parameters.AddWithValue("@Id", user.Id);

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

        public async Task<IEnumerable<User>> GetAllSupervisoresAsync()
        {
            var users = new List<User>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM Usuarios WHERE Role = 'Coordenador' OR Role = 'Admin'", connection);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(MapUserFromReader(reader));
                    }
                }
            }
            return users;
        }

        public async Task<IEnumerable<User>> GetUsersBySupervisorAsync(int supervisorId)
        {
            var users = new List<User>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM Usuarios WHERE SupervisorId = @SupervisorId", connection);
                command.Parameters.AddWithValue("@SupervisorId", supervisorId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(MapUserFromReader(reader));
                    }
                }
            }
            return users;
        }

        private User MapUserFromReader(SqlDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Nome = reader.GetString(reader.GetOrdinal("Nome")),
                Login = reader.GetString(reader.GetOrdinal("Login")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                Role = reader.GetString(reader.GetOrdinal("Role")),
                CPF = reader.IsDBNull(reader.GetOrdinal("CPF")) ? null : reader.GetString(reader.GetOrdinal("CPF")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                SenhaEmail = reader.IsDBNull(reader.GetOrdinal("SenhaEmail")) ? null : reader.GetString(reader.GetOrdinal("SenhaEmail")),
                Teams = reader.IsDBNull(reader.GetOrdinal("Teams")) ? null : reader.GetString(reader.GetOrdinal("Teams")),
                SenhaTeams = reader.IsDBNull(reader.GetOrdinal("SenhaTeams")) ? null : reader.GetString(reader.GetOrdinal("SenhaTeams")),
                EDespacho = reader.IsDBNull(reader.GetOrdinal("EDespacho")) ? null : reader.GetString(reader.GetOrdinal("EDespacho")),
                SenhaEDespacho = reader.IsDBNull(reader.GetOrdinal("SenhaEDespacho")) ? null : reader.GetString(reader.GetOrdinal("SenhaEDespacho")),
                Genius = reader.IsDBNull(reader.GetOrdinal("Genius")) ? null : reader.GetString(reader.GetOrdinal("Genius")),
                SenhaGenius = reader.IsDBNull(reader.GetOrdinal("SenhaGenius")) ? null : reader.GetString(reader.GetOrdinal("SenhaGenius")),
                Ibrooker = reader.IsDBNull(reader.GetOrdinal("Ibrooker")) ? null : reader.GetString(reader.GetOrdinal("Ibrooker")),
                SenhaIbrooker = reader.IsDBNull(reader.GetOrdinal("SenhaIbrooker")) ? null : reader.GetString(reader.GetOrdinal("SenhaIbrooker")),
                Adicional = reader.IsDBNull(reader.GetOrdinal("Adicional")) ? null : reader.GetString(reader.GetOrdinal("Adicional")),
                SenhaAdicional = reader.IsDBNull(reader.GetOrdinal("SenhaAdicional")) ? null : reader.GetString(reader.GetOrdinal("SenhaAdicional")),
                Setor = reader.IsDBNull(reader.GetOrdinal("Setor")) ? null : reader.GetString(reader.GetOrdinal("Setor")),
                Smartphone = reader.IsDBNull(reader.GetOrdinal("Smartphone")) ? null : reader.GetString(reader.GetOrdinal("Smartphone")),
                TelefoneFixo = reader.IsDBNull(reader.GetOrdinal("TelefoneFixo")) ? null : reader.GetString(reader.GetOrdinal("TelefoneFixo")),
                Ramal = reader.IsDBNull(reader.GetOrdinal("Ramal")) ? null : reader.GetString(reader.GetOrdinal("Ramal")),
                Alarme = reader.IsDBNull(reader.GetOrdinal("Alarme")) ? null : reader.GetString(reader.GetOrdinal("Alarme")),
                Videoporteiro = reader.IsDBNull(reader.GetOrdinal("Videoporteiro")) ? null : reader.GetString(reader.GetOrdinal("Videoporteiro")),
                Obs = reader.IsDBNull(reader.GetOrdinal("Obs")) ? null : reader.GetString(reader.GetOrdinal("Obs")),
                DataInclusao = reader.IsDBNull(reader.GetOrdinal("DataInclusao")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DataInclusao")),
                DataAlteracao = reader.IsDBNull(reader.GetOrdinal("DataAlteracao")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DataAlteracao")),
                SupervisorId = reader.IsDBNull(reader.GetOrdinal("SupervisorId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("SupervisorId"))
            };
        }

        private void AddUserParameters(SqlCommand command, User user)
        {
            command.Parameters.AddWithValue("@Nome", user.Nome);
            command.Parameters.AddWithValue("@Login", user.Login);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            command.Parameters.AddWithValue("@Role", user.Role);
            command.Parameters.AddWithValue("@CPF", (object)user.CPF ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", (object)user.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@SenhaEmail", (object)user.SenhaEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@Teams", (object)user.Teams ?? DBNull.Value);
            command.Parameters.AddWithValue("@SenhaTeams", (object)user.SenhaTeams ?? DBNull.Value);
            command.Parameters.AddWithValue("@EDespacho", (object)user.EDespacho ?? DBNull.Value);
            command.Parameters.AddWithValue("@SenhaEDespacho", (object)user.SenhaEDespacho ?? DBNull.Value);
            command.Parameters.AddWithValue("@Genius", (object)user.Genius ?? DBNull.Value);
            command.Parameters.AddWithValue("@SenhaGenius", (object)user.SenhaGenius ?? DBNull.Value);
            command.Parameters.AddWithValue("@Ibrooker", (object)user.Ibrooker ?? DBNull.Value);
            command.Parameters.AddWithValue("@SenhaIbrooker", (object)user.SenhaIbrooker ?? DBNull.Value);
            command.Parameters.AddWithValue("@Adicional", (object)user.Adicional ?? DBNull.Value);
            command.Parameters.AddWithValue("@SenhaAdicional", (object)user.SenhaAdicional ?? DBNull.Value);
            command.Parameters.AddWithValue("@Setor", (object)user.Setor ?? DBNull.Value);
            command.Parameters.AddWithValue("@Smartphone", (object)user.Smartphone ?? DBNull.Value);
            command.Parameters.AddWithValue("@TelefoneFixo", (object)user.TelefoneFixo ?? DBNull.Value);
            command.Parameters.AddWithValue("@Ramal", (object)user.Ramal ?? DBNull.Value);
            command.Parameters.AddWithValue("@Alarme", (object)user.Alarme ?? DBNull.Value);
            command.Parameters.AddWithValue("@Videoporteiro", (object)user.Videoporteiro ?? DBNull.Value);
            command.Parameters.AddWithValue("@Obs", (object)user.Obs ?? DBNull.Value);
            command.Parameters.AddWithValue("@DataInclusao", (object)user.DataInclusao ?? DBNull.Value);
            command.Parameters.AddWithValue("@DataAlteracao", (object)user.DataAlteracao ?? DBNull.Value);
            command.Parameters.AddWithValue("@SupervisorId", (object)user.SupervisorId ?? DBNull.Value);
        }
    }
}
