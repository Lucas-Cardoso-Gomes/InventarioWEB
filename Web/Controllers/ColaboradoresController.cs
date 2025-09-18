using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador")]
    public class ColaboradoresController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<ColaboradoresController> _logger;
        private readonly PersistentLogService _persistentLogService;

        public ColaboradoresController(IConfiguration configuration, ILogger<ColaboradoresController> logger, PersistentLogService persistentLogService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
            _persistentLogService = persistentLogService;
        }

        // GET: Colaboradores
        public IActionResult Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var colaboradores = new List<Colaborador>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "SELECT c.*, co.Nome as CoordenadorNome FROM Colaboradores c LEFT JOIN Colaboradores co ON c.CoordenadorCPF = co.CPF";
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        sql += " WHERE c.Nome LIKE @search OR c.CPF LIKE @search OR c.Email LIKE @search";
                    }
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        if (!string.IsNullOrEmpty(searchString))
                        {
                            cmd.Parameters.AddWithValue("@search", $"%{searchString}%");
                        }

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                colaboradores.Add(new Colaborador
                                {
                                    CPF = reader["CPF"].ToString(),
                                    Nome = reader["Nome"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    SenhaEmail = reader["SenhaEmail"].ToString(),
                                    Teams = reader["Teams"].ToString(),
                                    SenhaTeams = reader["SenhaTeams"].ToString(),
                                    EDespacho = reader["EDespacho"].ToString(),
                                    SenhaEDespacho = reader["SenhaEDespacho"].ToString(),
                                    Genius = reader["Genius"].ToString(),
                                    SenhaGenius = reader["SenhaGenius"].ToString(),
                                    Ibrooker = reader["Ibrooker"].ToString(),
                                    SenhaIbrooker = reader["SenhaIbrooker"].ToString(),
                                    Adicional = reader["Adicional"].ToString(),
                                    SenhaAdicional = reader["SenhaAdicional"].ToString(),
                                    Filial = reader["Filial"].ToString(),
                                    Setor = reader["Setor"].ToString(),
                                    Smartphone = reader["Smartphone"].ToString(),
                                    TelefoneFixo = reader["TelefoneFixo"].ToString(),
                                    Ramal = reader["Ramal"].ToString(),
                                    Alarme = reader["Alarme"].ToString(),
                                    Videoporteiro = reader["Videoporteiro"].ToString(),
                                    Obs = reader["Obs"].ToString(),
                                    DataInclusao = reader["DataInclusao"] != DBNull.Value ? Convert.ToDateTime(reader["DataInclusao"]) : (DateTime?)null,
                                    DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                                    CoordenadorCPF = reader["CoordenadorCPF"] != DBNull.Value ? reader["CoordenadorCPF"].ToString() : null,
                                    CoordenadorNome = reader["CoordenadorNome"] != DBNull.Value ? reader["CoordenadorNome"].ToString() : null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de colaboradores.");
                // Handle error, maybe return an error view
            }
            return View(colaboradores);
        }

        // GET: Colaboradores/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.Coordenadores = new SelectList(GetCoordenadores(), "CPF", "Nome");
            return View();
        }

        // POST: Colaboradores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(Colaborador colaborador)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = @"INSERT INTO Colaboradores (CPF, Nome, Email, SenhaEmail, Teams, SenhaTeams, EDespacho, SenhaEDespacho, Genius, SenhaGenius, Ibrooker, SenhaIbrooker, Adicional, SenhaAdicional, Filial, Setor, Smartphone, TelefoneFixo, Ramal, Alarme, Videoporteiro, Obs, DataInclusao, CoordenadorCPF) 
                                       VALUES (@CPF, @Nome, @Email, @SenhaEmail, @Teams, @SenhaTeams, @EDespacho, @SenhaEDespacho, @Genius, @SenhaGenius, @Ibrooker, @SenhaIbrooker, @Adicional, @SenhaAdicional, @Filial, @Setor, @Smartphone, @TelefoneFixo, @Ramal, @Alarme, @Videoporteiro, @Obs, @DataInclusao, @CoordenadorCPF)";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@CPF", colaborador.CPF);
                            cmd.Parameters.AddWithValue("@Nome", colaborador.Nome);
                            cmd.Parameters.AddWithValue("@Email", (object)colaborador.Email ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaEmail", (object)colaborador.SenhaEmail ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Teams", (object)colaborador.Teams ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaTeams", (object)colaborador.SenhaTeams ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@EDespacho", (object)colaborador.EDespacho ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaEDespacho", (object)colaborador.SenhaEDespacho ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Genius", (object)colaborador.Genius ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaGenius", (object)colaborador.SenhaGenius ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Ibrooker", (object)colaborador.Ibrooker ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaIbrooker", (object)colaborador.SenhaIbrooker ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Adicional", (object)colaborador.Adicional ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaAdicional", (object)colaborador.SenhaAdicional ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Filial", (object)colaborador.Filial ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Setor", (object)colaborador.Setor ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Smartphone", (object)colaborador.Smartphone ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TelefoneFixo", (object)colaborador.TelefoneFixo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Ramal", (object)colaborador.Ramal ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Alarme", (object)colaborador.Alarme ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Videoporteiro", (object)colaborador.Videoporteiro ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Obs", (object)colaborador.Obs ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DataInclusao", DateTime.Now);
                            cmd.Parameters.AddWithValue("@CoordenadorCPF", (object)colaborador.CoordenadorCPF ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    _persistentLogService.AddLog("Colaborador", "Create", User.Identity.Name, $"Collaborator '{colaborador.Nome}' created.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar colaborador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o colaborador. Verifique se o CPF já existe.");
                }
            }
            ViewBag.Coordenadores = new SelectList(GetCoordenadores(), "CPF", "Nome", colaborador.CoordenadorCPF);
            return View(colaborador);
        }

        // GET: Colaboradores/Edit/5
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id)
        {
            if (id == null) return NotFound();
            Colaborador colaborador = FindColaboradorById(id);
            if (colaborador == null) return NotFound();
            ViewBag.Coordenadores = new SelectList(GetCoordenadores(), "CPF", "Nome", colaborador.CoordenadorCPF);
            return View(colaborador);
        }

        // POST: Colaboradores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id, Colaborador colaborador)
        {
            if (id != colaborador.CPF) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = @"UPDATE Colaboradores SET 
                                       Nome = @Nome, Email = @Email, SenhaEmail = @SenhaEmail, Teams = @Teams, SenhaTeams = @SenhaTeams, 
                                       EDespacho = @EDespacho, SenhaEDespacho = @SenhaEDespacho, Genius = @Genius, SenhaGenius = @SenhaGenius, 
                                       Ibrooker = @Ibrooker, SenhaIbrooker = @SenhaIbrooker, Adicional = @Adicional, SenhaAdicional = @SenhaAdicional, 
                                       Filial = @Filial, Setor = @Setor, Smartphone = @Smartphone, TelefoneFixo = @TelefoneFixo, Ramal = @Ramal, Alarme = @Alarme, Videoporteiro = @Videoporteiro,
                                       Obs = @Obs, DataAlteracao = @DataAlteracao, CoordenadorCPF = @CoordenadorCPF
                                       WHERE CPF = @CPF";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@CPF", colaborador.CPF);
                            cmd.Parameters.AddWithValue("@Nome", colaborador.Nome);
                            cmd.Parameters.AddWithValue("@Email", (object)colaborador.Email ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaEmail", (object)colaborador.SenhaEmail ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Teams", (object)colaborador.Teams ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaTeams", (object)colaborador.SenhaTeams ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@EDespacho", (object)colaborador.EDespacho ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaEDespacho", (object)colaborador.SenhaEDespacho ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Genius", (object)colaborador.Genius ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaGenius", (object)colaborador.SenhaGenius ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Ibrooker", (object)colaborador.Ibrooker ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaIbrooker", (object)colaborador.SenhaIbrooker ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Adicional", (object)colaborador.Adicional ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaAdicional", (object)colaborador.SenhaAdicional ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Filial", (object)colaborador.Filial ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Setor", (object)colaborador.Setor ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Smartphone", (object)colaborador.Smartphone ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TelefoneFixo", (object)colaborador.TelefoneFixo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Ramal", (object)colaborador.Ramal ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Alarme", (object)colaborador.Alarme ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Videoporteiro", (object)colaborador.Videoporteiro ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Obs", (object)colaborador.Obs ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DataAlteracao", DateTime.Now);
                            cmd.Parameters.AddWithValue("@CoordenadorCPF", (object)colaborador.CoordenadorCPF ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    _persistentLogService.AddLog("Colaborador", "Update", User.Identity.Name, $"Collaborator '{colaborador.Nome}' updated.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar colaborador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o colaborador.");
                }
            }
            ViewBag.Coordenadores = new SelectList(GetCoordenadores(), "CPF", "Nome", colaborador.CoordenadorCPF);
            return View(colaborador);
        }

        // GET: Colaboradores/Delete/5
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            if (id == null) return NotFound();
            Colaborador colaborador = FindColaboradorById(id);
            if (colaborador == null) return NotFound();
            return View(colaborador);
        }

        // POST: Colaboradores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteConfirmed(string id)
        {
            try
            {
                var colaborador = FindColaboradorById(id);
                if (colaborador != null)
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        // You might want to check for foreign key dependencies before deleting
                        string sql = "DELETE FROM Colaboradores WHERE CPF = @CPF";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@CPF", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    _persistentLogService.AddLog("Colaborador", "Delete", User.Identity.Name, $"Collaborator '{colaborador.Nome}' deleted.");
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir colaborador.");
                // Redirect to a view with the error message
                Colaborador colaborador = FindColaboradorById(id);
                ViewBag.ErrorMessage = "Erro ao excluir. Verifique se o colaborador está associado a computadores, monitores ou periféricos.";
                return View(colaborador);
            }
        }

        // POST: Colaboradores/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Por favor, selecione um arquivo para importar.";
                return RedirectToAction(nameof(Index));
            }

            var colaboradores = new List<Colaborador>();
            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        // Pula o cabeçalho
                        reader.Read();

                        while (reader.Read())
                        {
                            try
                            {
                                colaboradores.Add(new Colaborador
                                {
                                    CPF = reader.GetValue(0)?.ToString(),
                                    Nome = reader.GetValue(1)?.ToString(),
                                    Email = reader.GetValue(2)?.ToString(),
                                    SenhaEmail = reader.GetValue(3)?.ToString(),
                                    Teams = reader.GetValue(4)?.ToString(),
                                    SenhaTeams = reader.GetValue(5)?.ToString(),
                                    EDespacho = reader.GetValue(6)?.ToString(),
                                    SenhaEDespacho = reader.GetValue(7)?.ToString(),
                                    Genius = reader.GetValue(8)?.ToString(),
                                    SenhaGenius = reader.GetValue(9)?.ToString(),
                                    Ibrooker = reader.GetValue(10)?.ToString(),
                                    SenhaIbrooker = reader.GetValue(11)?.ToString(),
                                    Adicional = reader.GetValue(12)?.ToString(),
                                    SenhaAdicional = reader.GetValue(13)?.ToString(),
                                    Filial = reader.GetValue(14)?.ToString(),
                                    Setor = reader.GetValue(15)?.ToString(),
                                    Smartphone = reader.GetValue(16)?.ToString(),
                                    TelefoneFixo = reader.GetValue(17)?.ToString(),
                                    Ramal = reader.GetValue(18)?.ToString(),
                                    Alarme = reader.GetValue(19)?.ToString(),
                                    Videoporteiro = reader.GetValue(20)?.ToString(),
                                    Obs = reader.GetValue(21)?.ToString(),
                                    CoordenadorCPF = reader.GetValue(22)?.ToString()
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Erro ao ler uma linha do arquivo Excel. Linha ignorada.");
                                // Continue para a próxima linha
                            }
                        }
                    }
                }

                int registrosInseridos = 0;
                int registrosAtualizados = 0;

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    foreach (var colaborador in colaboradores)
                    {
                        if (colaborador == null || string.IsNullOrWhiteSpace(colaborador.CPF) || string.IsNullOrWhiteSpace(colaborador.Nome))
                        {
                            _logger.LogWarning("Registro de colaborador inválido ou com CPF/Nome nulos foi ignorado.");
                            continue;
                        }

                        var colaboradorExistente = FindColaboradorById(colaborador.CPF, connection);
                        if (colaboradorExistente != null)
                        {
                            // Atualizar colaborador existente
                            string updateSql = @"UPDATE Colaboradores SET
                                               Nome = @Nome, Email = @Email, SenhaEmail = @SenhaEmail, Teams = @Teams, SenhaTeams = @SenhaTeams,
                                               EDespacho = @EDespacho, SenhaEDespacho = @SenhaEDespacho, Genius = @Genius, SenhaGenius = @SenhaGenius,
                                               Ibrooker = @Ibrooker, SenhaIbrooker = @SenhaIbrooker, Adicional = @Adicional, SenhaAdicional = @SenhaAdicional,
                                               Filial = @Filial, Setor = @Setor, Smartphone = @Smartphone, TelefoneFixo = @TelefoneFixo, Ramal = @Ramal, Alarme = @Alarme, Videoporteiro = @Videoporteiro,
                                               Obs = @Obs, DataAlteracao = @DataAlteracao, CoordenadorCPF = @CoordenadorCPF
                                               WHERE CPF = @CPF";
                            using (SqlCommand cmd = new SqlCommand(updateSql, connection))
                            {
                                // Adicionar parâmetros para atualização
                                cmd.Parameters.AddWithValue("@CPF", colaborador.CPF);
                                cmd.Parameters.AddWithValue("@Nome", colaborador.Nome);
                                cmd.Parameters.AddWithValue("@Email", (object)colaborador.Email ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SenhaEmail", (object)colaborador.SenhaEmail ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Teams", (object)colaborador.Teams ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SenhaTeams", (object)colaborador.SenhaTeams ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDespacho", (object)colaborador.EDespacho ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SenhaEDespacho", (object)colaborador.SenhaEDespacho ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Genius", (object)colaborador.Genius ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SenhaGenius", (object)colaborador.SenhaGenius ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Ibrooker", (object)colaborador.Ibrooker ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SenhaIbrooker", (object)colaborador.SenhaIbrooker ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Adicional", (object)colaborador.Adicional ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SenhaAdicional", (object)colaborador.SenhaAdicional ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Filial", (object)colaborador.Filial ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Setor", (object)colaborador.Setor ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Smartphone", (object)colaborador.Smartphone ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@TelefoneFixo", (object)colaborador.TelefoneFixo ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Ramal", (object)colaborador.Ramal ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Alarme", (object)colaborador.Alarme ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Videoporteiro", (object)colaborador.Videoporteiro ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Obs", (object)colaborador.Obs ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@DataAlteracao", DateTime.Now);
                                cmd.Parameters.AddWithValue("@CoordenadorCPF", (object)colaborador.CoordenadorCPF ?? DBNull.Value);
                                await cmd.ExecuteNonQueryAsync();
                                registrosAtualizados++;
                            }
                        }
                        else
                        {
                            // Inserir novo colaborador
                            string insertSql = @"INSERT INTO Colaboradores (CPF, Nome, Email, SenhaEmail, Teams, SenhaTeams, EDespacho, SenhaEDespacho, Genius, SenhaGenius, Ibrooker, SenhaIbrooker, Adicional, SenhaAdicional, Filial, Setor, Smartphone, TelefoneFixo, Ramal, Alarme, Videoporteiro, Obs, DataInclusao, CoordenadorCPF)
                                                   VALUES (@CPF, @Nome, @Email, @SenhaEmail, @Teams, @SenhaTeams, @EDespacho, @SenhaEDespacho, @Genius, @SenhaGenius, @Ibrooker, @SenhaIbrooker, @Adicional, @SenhaAdicional, @Filial, @Setor, @Smartphone, @TelefoneFixo, @Ramal, @Alarme, @Videoporteiro, @Obs, @DataInclusao, @CoordenadorCPF)";
                            using (SqlCommand cmd = new SqlCommand(insertSql, connection))
                            {
                                // Adicionar parâmetros para inserção
                                cmd.Parameters.AddWithValue("@CPF", colaborador.CPF);
                                cmd.Parameters.AddWithValue("@Nome", colaborador.Nome);
                                cmd.Parameters.AddWithValue("@Email", (object)colaborador.Email ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SenhaEmail", (object)colaborador.SenhaEmail ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Teams", (object)colaborador.Teams ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SenhaTeams", (object)colaborador.SenhaTeams ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDespacho", (object)colaborador.EDespacho ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SenhaEDespacho", (object)colaborador.SenhaEDespacho ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Genius", (object)colaborador.Genius ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SenhaGenius", (object)colaborador.SenhaGenius ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Ibrooker", (object)colaborador.Ibrooker ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SenhaIbrooker", (object)colaborador.SenhaIbrooker ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Adicional", (object)colaborador.Adicional ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SenhaAdicional", (object)colaborador.SenhaAdicional ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Filial", (object)colaborador.Filial ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Setor", (object)colaborador.Setor ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Smartphone", (object)colaborador.Smartphone ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@TelefoneFixo", (object)colaborador.TelefoneFixo ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Ramal", (object)colaborador.Ramal ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Alarme", (object)colaborador.Alarme ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Videoporteiro", (object)colaborador.Videoporteiro ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Obs", (object)colaborador.Obs ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@DataInclusao", DateTime.Now);
                                cmd.Parameters.AddWithValue("@CoordenadorCPF", (object)colaborador.CoordenadorCPF ?? DBNull.Value);
                                await cmd.ExecuteNonQueryAsync();
                                registrosInseridos++;
                            }
                        }
                    }
                }
                TempData["SuccessMessage"] = $"Importação concluída com sucesso! {registrosInseridos} colaboradores inseridos e {registrosAtualizados} atualizados.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao importar arquivo Excel.");
                TempData["ErrorMessage"] = "Ocorreu um erro ao importar o arquivo. Verifique o formato e o conteúdo.";
            }

            return RedirectToAction(nameof(Index));
        }

        private Colaborador FindColaboradorById(string id)
        {
            Colaborador colaborador = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    colaborador = FindColaboradorById(id, connection);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao encontrar colaborador por ID.");
            }
            return colaborador;
        }

        private Colaborador FindColaboradorById(string id, SqlConnection connection)
        {
            Colaborador colaborador = null;
            string sql = "SELECT * FROM Colaboradores WHERE CPF = @CPF";
            using (SqlCommand cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@CPF", id);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        colaborador = new Colaborador
                        {
                            CPF = reader["CPF"].ToString(),
                            Nome = reader["Nome"].ToString(),
                            Email = reader["Email"].ToString(),
                            SenhaEmail = reader["SenhaEmail"].ToString(),
                            Teams = reader["Teams"].ToString(),
                            SenhaTeams = reader["SenhaTeams"].ToString(),
                            EDespacho = reader["EDespacho"].ToString(),
                            SenhaEDespacho = reader["SenhaEDespacho"].ToString(),
                            Genius = reader["Genius"].ToString(),
                            SenhaGenius = reader["SenhaGenius"].ToString(),
                            Ibrooker = reader["Ibrooker"].ToString(),
                            SenhaIbrooker = reader["SenhaIbrooker"].ToString(),
                            Adicional = reader["Adicional"].ToString(),
                            SenhaAdicional = reader["SenhaAdicional"].ToString(),
                            Filial = reader["Filial"].ToString(),
                            Setor = reader["Setor"].ToString(),
                            Smartphone = reader["Smartphone"].ToString(),
                            TelefoneFixo = reader["TelefoneFixo"].ToString(),
                            Ramal = reader["Ramal"].ToString(),
                            Alarme = reader["Alarme"].ToString(),
                            Videoporteiro = reader["Videoporteiro"].ToString(),
                            Obs = reader["Obs"].ToString(),
                            DataInclusao = reader["DataInclusao"] != DBNull.Value ? Convert.ToDateTime(reader["DataInclusao"]) : (DateTime?)null,
                            DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                            CoordenadorCPF = reader["CoordenadorCPF"] != DBNull.Value ? reader["CoordenadorCPF"].ToString() : null
                        };
                    }
                }
            }
            return colaborador;
        }

        private List<Colaborador> GetCoordenadores()
        {
            var coordenadores = new List<Colaborador>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "SELECT c.CPF, c.Nome FROM Colaboradores c INNER JOIN Usuarios u ON c.CPF = u.ColaboradorCPF WHERE u.Role = 'Coordenador' OR u.IsCoordinator = 1 ORDER BY c.Nome";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                coordenadores.Add(new Colaborador
                                {
                                    CPF = reader["CPF"].ToString(),
                                    Nome = reader["Nome"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de coordenadores.");
            }
            return coordenadores;
        }
    }
}
