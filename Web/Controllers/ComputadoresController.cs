using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using web.Models;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Web.Controllers
{
    public class ComputadoresController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<ComputadoresController> _logger;

        public ComputadoresController(IConfiguration configuration, ILogger<ComputadoresController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public IActionResult Index()
        {
            List<Computador> computadores = new List<Computador>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "SELECT MAC, IP, Usuario, Hostname, Fabricante, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, ArmazenamentoC, ArmazenamentoCTotal, ArmazenamentoCLivre, ArmazenamentoD, ArmazenamentoDTotal, ArmazenamentoDLivre, ConsumoCPU, SO, DataColeta FROM Computadores";

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Computador computador = new Computador
                                {
                                    MAC = reader["MAC"].ToString(),
                                    IP = reader["IP"].ToString(),
                                    Usuario = reader["Usuario"].ToString(),
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
                                    DataColeta = reader["DataColeta"] != DBNull.Value ? Convert.ToDateTime(reader["DataColeta"]) : (DateTime?)null
                                };
                                computadores.Add(computador);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de computadores.");
                ViewBag.Message = "Ocorreu um erro ao obter a lista de computadores. Por favor, tente novamente mais tarde.";
            }

            return View(computadores);
        }

        // GET: Computadores/Create
        public IActionResult Create()
        {
            return View(new ComputadorViewModel());
        }

        // POST: Computadores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ComputadorViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        string sql = "INSERT INTO Computadores (MAC, IP, Usuario, Hostname, Fabricante, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, ArmazenamentoC, ArmazenamentoCTotal, ArmazenamentoCLivre, ArmazenamentoD, ArmazenamentoDTotal, ArmazenamentoDLivre, ConsumoCPU, SO, DataColeta) VALUES (@MAC, @IP, @Usuario, @Hostname, @Fabricante, @Processador, @ProcessadorFabricante, @ProcessadorCore, @ProcessadorThread, @ProcessadorClock, @Ram, @RamTipo, @RamVelocidade, @RamVoltagem, @RamPorModule, @ArmazenamentoC, @ArmazenamentoCTotal, @ArmazenamentoCLivre, @ArmazenamentoD, @ArmazenamentoDTotal, @ArmazenamentoDLivre, @ConsumoCPU, @SO, @DataColeta)";

                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@MAC", viewModel.MAC);
                            cmd.Parameters.AddWithValue("@IP", (object)viewModel.IP ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Usuario", (object)viewModel.Usuario ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Hostname", (object)viewModel.Hostname ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Fabricante", (object)viewModel.Fabricante ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Processador", (object)viewModel.Processador ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorFabricante", (object)viewModel.ProcessadorFabricante ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorCore", (object)viewModel.ProcessadorCore ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorThread", (object)viewModel.ProcessadorThread ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorClock", (object)viewModel.ProcessadorClock ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Ram", (object)viewModel.Ram ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamTipo", (object)viewModel.RamTipo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamVelocidade", (object)viewModel.RamVelocidade ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamVoltagem", (object)viewModel.RamVoltagem ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamPorModule", (object)viewModel.RamPorModule ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoC", (object)viewModel.ArmazenamentoC ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoCTotal", (object)viewModel.ArmazenamentoCTotal ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoCLivre", (object)viewModel.ArmazenamentoCLivre ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoD", (object)viewModel.ArmazenamentoD ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoDTotal", (object)viewModel.ArmazenamentoDTotal ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoDLivre", (object)viewModel.ArmazenamentoDLivre ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ConsumoCPU", (object)viewModel.ConsumoCPU ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SO", (object)viewModel.SO ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DataColeta", DateTime.Now);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar um novo computador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o computador. Verifique se o MAC já existe.");
                }
            }
            return View(viewModel);
        }

        // GET: Computadores/Edit/5
        public IActionResult Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Computador computador = FindComputadorById(id);

            if (computador == null)
            {
                return NotFound();
            }

            var viewModel = new ComputadorViewModel
            {
                MAC = computador.MAC,
                IP = computador.IP,
                Usuario = computador.Usuario,
                Hostname = computador.Hostname,
                Fabricante = computador.Fabricante,
                Processador = computador.Processador,
                ProcessadorFabricante = computador.ProcessadorFabricante,
                ProcessadorCore = computador.ProcessadorCore,
                ProcessadorThread = computador.ProcessadorThread,
                ProcessadorClock = computador.ProcessadorClock,
                Ram = computador.Ram,
                RamTipo = computador.RamTipo,
                RamVelocidade = computador.RamVelocidade,
                RamVoltagem = computador.RamVoltagem,
                RamPorModule = computador.RamPorModule,
                ArmazenamentoC = computador.ArmazenamentoC,
                ArmazenamentoCTotal = computador.ArmazenamentoCTotal,
                ArmazenamentoCLivre = computador.ArmazenamentoCLivre,
                ArmazenamentoD = computador.ArmazenamentoD,
                ArmazenamentoDTotal = computador.ArmazenamentoDTotal,
                ArmazenamentoDLivre = computador.ArmazenamentoDLivre,
                ConsumoCPU = computador.ConsumoCPU,
                SO = computador.SO
            };

            return View(viewModel);
        }

        // POST: Computadores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, ComputadorViewModel viewModel)
        {
            if (id != viewModel.MAC)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        string sql = "UPDATE Computadores SET IP = @IP, Usuario = @Usuario, Hostname = @Hostname, Fabricante = @Fabricante, Processador = @Processador, ProcessadorFabricante = @ProcessadorFabricante, ProcessadorCore = @ProcessadorCore, ProcessadorThread = @ProcessadorThread, ProcessadorClock = @ProcessadorClock, Ram = @Ram, RamTipo = @RamTipo, RamVelocidade = @RamVelocidade, RamVoltagem = @RamVoltagem, RamPorModule = @RamPorModule, ArmazenamentoC = @ArmazenamentoC, ArmazenamentoCTotal = @ArmazenamentoCTotal, ArmazenamentoCLivre = @ArmazenamentoCLivre, ArmazenamentoD = @ArmazenamentoD, ArmazenamentoDTotal = @ArmazenamentoDTotal, ArmazenamentoDLivre = @ArmazenamentoDLivre, ConsumoCPU = @ConsumoCPU, SO = @SO WHERE MAC = @MAC";

                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@MAC", viewModel.MAC);
                            cmd.Parameters.AddWithValue("@IP", (object)viewModel.IP ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Usuario", (object)viewModel.Usuario ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Hostname", (object)viewModel.Hostname ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Fabricante", (object)viewModel.Fabricante ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Processador", (object)viewModel.Processador ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorFabricante", (object)viewModel.ProcessadorFabricante ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorCore", (object)viewModel.ProcessadorCore ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorThread", (object)viewModel.ProcessadorThread ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorClock", (object)viewModel.ProcessadorClock ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Ram", (object)viewModel.Ram ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamTipo", (object)viewModel.RamTipo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamVelocidade", (object)viewModel.RamVelocidade ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamVoltagem", (object)viewModel.RamVoltagem ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamPorModule", (object)viewModel.RamPorModule ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoC", (object)viewModel.ArmazenamentoC ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoCTotal", (object)viewModel.ArmazenamentoCTotal ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoCLivre", (object)viewModel.ArmazenamentoCLivre ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoD", (object)viewModel.ArmazenamentoD ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoDTotal", (object)viewModel.ArmazenamentoDTotal ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoDLivre", (object)viewModel.ArmazenamentoDLivre ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ConsumoCPU", (object)viewModel.ConsumoCPU ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SO", (object)viewModel.SO ?? DBNull.Value);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar o computador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o computador.");
                }
            }
            return View(viewModel);
        }

        // GET: Computadores/Delete/5
        public IActionResult Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Computador computador = FindComputadorById(id);

            if (computador == null)
            {
                return NotFound();
            }

            return View(computador);
        }

        // POST: Computadores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Route("Computadores/DeleteConfirmed/{id}")]
        public IActionResult DeleteConfirmed(string id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "DELETE FROM Computadores WHERE MAC = @MAC";

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@MAC", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir o computador.");
                ViewBag.Message = "Ocorreu um erro ao excluir o computador. Por favor, tente novamente mais tarde.";
                return View();
            }
        }

        private Computador FindComputadorById(string id)
        {
            Computador computador = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "SELECT MAC, IP, Usuario, Hostname, Fabricante, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, ArmazenamentoC, ArmazenamentoCTotal, ArmazenamentoCLivre, ArmazenamentoD, ArmazenamentoDTotal, ArmazenamentoDLivre, ConsumoCPU, SO, DataColeta FROM Computadores WHERE MAC = @MAC";

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@MAC", id);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                computador = new Computador
                                {
                                    MAC = reader["MAC"].ToString(),
                                    IP = reader["IP"].ToString(),
                                    Usuario = reader["Usuario"].ToString(),
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
                                    DataColeta = reader["DataColeta"] != DBNull.Value ? Convert.ToDateTime(reader["DataColeta"]) : (DateTime?)null
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter os detalhes do computador.");
                ViewBag.Message = "Ocorreu um erro ao obter os detalhes do computador. Por favor, tente novamente mais tarde.";
            }
            return computador;
        }
    }
}