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
                                    DataColeta = Convert.ToDateTime(reader["DataColeta"])
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
            return View();
        }

        // POST: Computadores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("MAC,IP,Usuario,Hostname,Fabricante,Processador,ProcessadorFabricante,ProcessadorCore,ProcessadorThread,ProcessadorClock,Ram,RamTipo,RamVelocidade,RamVoltagem,ArmazenamentoC,ArmazenamentoCTotal,ArmazenamentoCLivre,ArmazenamentoD,ArmazenamentoDTotal,ArmazenamentoDLivre,ConsumoCPU,SO,DataColeta")] Computador computador)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        string sql = "INSERT INTO Computadores (MAC, IP, Usuario, Hostname, Fabricante, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, ArmazenamentoC, ArmazenamentoCTotal, ArmazenamentoCLivre, ArmazenamentoD, ArmazenamentoDTotal, ArmazenamentoDLivre, ConsumoCPU, SO, DataColeta) VALUES (@MAC, @IP, @Usuario, @Hostname, @Fabricante, @Processador, @ProcessadorFabricante, @ProcessadorCore, @ProcessadorThread, @ProcessadorClock, @Ram, @RamTipo, @RamVelocidade, @RamVoltagem, @ArmazenamentoC, @ArmazenamentoCTotal, @ArmazenamentoCLivre, @ArmazenamentoD, @ArmazenamentoDTotal, @ArmazenamentoDLivre, @ConsumoCPU, @SO, @DataColeta)";

                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@MAC", computador.MAC);
                            cmd.Parameters.AddWithValue("@IP", computador.IP);
                            cmd.Parameters.AddWithValue("@Usuario", computador.Usuario);
                            cmd.Parameters.AddWithValue("@Hostname", computador.Hostname);
                            cmd.Parameters.AddWithValue("@Fabricante", computador.Fabricante);
                            cmd.Parameters.AddWithValue("@Processador", computador.Processador);
                            cmd.Parameters.AddWithValue("@ProcessadorFabricante", computador.ProcessadorFabricante);
                            cmd.Parameters.AddWithValue("@ProcessadorCore", computador.ProcessadorCore);
                            cmd.Parameters.AddWithValue("@ProcessadorThread", computador.ProcessadorThread);
                            cmd.Parameters.AddWithValue("@ProcessadorClock", computador.ProcessadorClock);
                            cmd.Parameters.AddWithValue("@Ram", computador.Ram);
                            cmd.Parameters.AddWithValue("@RamTipo", computador.RamTipo);
                            cmd.Parameters.AddWithValue("@RamVelocidade", computador.RamVelocidade);
                            cmd.Parameters.AddWithValue("@RamVoltagem", computador.RamVoltagem);
                            cmd.Parameters.AddWithValue("@RamPorModule", computador.RamPorModule);
                            cmd.Parameters.AddWithValue("@ArmazenamentoC", computador.ArmazenamentoC);
                            cmd.Parameters.AddWithValue("@ArmazenamentoCTotal", computador.ArmazenamentoCTotal);
                            cmd.Parameters.AddWithValue("@ArmazenamentoCLivre", computador.ArmazenamentoCLivre);
                            cmd.Parameters.AddWithValue("@ArmazenamentoD", computador.ArmazenamentoD);
                            cmd.Parameters.AddWithValue("@ArmazenamentoDTotal", computador.ArmazenamentoDTotal);
                            cmd.Parameters.AddWithValue("@ArmazenamentoDLivre", computador.ArmazenamentoDLivre);
                            cmd.Parameters.AddWithValue("@ConsumoCPU", computador.ConsumoCPU);
                            cmd.Parameters.AddWithValue("@SO", computador.SO);
                            cmd.Parameters.AddWithValue("@DataColeta", computador.DataColeta);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar um novo computador.");
                    ViewBag.Message = "Ocorreu um erro ao criar um novo computador. Por favor, tente novamente mais tarde.";
                }
            }
            return View(computador);
        }

        // GET: Computadores/Edit/5
        public IActionResult Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

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
                                    DataColeta = Convert.ToDateTime(reader["DataColeta"])
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

            if (computador == null)
            {
                return NotFound();
            }

            return View(computador);
        }

        // POST: Computadores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, [Bind("MAC,IP,Usuario,Hostname,Fabricante,Processador,ProcessadorFabricante,ProcessadorCore,ProcessadorThread,ProcessadorClock,Ram,RamTipo,RamVelocidade,RamVoltagem,ArmazenamentoC,ArmazenamentoCTotal,ArmazenamentoCLivre,ArmazenamentoD,ArmazenamentoDTotal,ArmazenamentoDLivre,ConsumoCPU,SO,DataColeta")] Computador computador)
        {
            if (id != computador.MAC)
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

                        string sql = "UPDATE Computadores SET IP = @IP, Usuario = @Usuario, Hostname = @Hostname, Fabricante = @Fabricante, Processador = @Processador, ProcessadorFabricante = @ProcessadorFabricante, ProcessadorCore = @ProcessadorCore, ProcessadorThread = @ProcessadorThread, ProcessadorClock = @ProcessadorClock, Ram = @Ram, RamTipo = @RamTipo, RamVelocidade = @RamVelocidade, RamVoltagem = @RamVoltagem, RamPorModule = @RamPorModule, ArmazenamentoC = @ArmazenamentoC, ArmazenamentoCTotal = @ArmazenamentoCTotal, ArmazenamentoCLivre = @ArmazenamentoCLivre, ArmazenamentoD = @ArmazenamentoD, ArmazenamentoDTotal = @ArmazenamentoDTotal, ArmazenamentoDLivre = @ArmazenamentoDLivre, ConsumoCPU = @ConsumoCPU, SO = @SO, DataColeta = @DataColeta WHERE MAC = @MAC";

                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@MAC", computador.MAC);
                            cmd.Parameters.AddWithValue("@IP", computador.IP);
                            cmd.Parameters.AddWithValue("@Usuario", computador.Usuario);
                            cmd.Parameters.AddWithValue("@Hostname", computador.Hostname);
                            cmd.Parameters.AddWithValue("@Fabricante", computador.Fabricante);
                            cmd.Parameters.AddWithValue("@Processador", computador.Processador);
                            cmd.Parameters.AddWithValue("@ProcessadorFabricante", computador.ProcessadorFabricante);
                            cmd.Parameters.AddWithValue("@ProcessadorCore", computador.ProcessadorCore);
                            cmd.Parameters.AddWithValue("@ProcessadorThread", computador.ProcessadorThread);
                            cmd.Parameters.AddWithValue("@ProcessadorClock", computador.ProcessadorClock);
                            cmd.Parameters.AddWithValue("@Ram", computador.Ram);
                            cmd.Parameters.AddWithValue("@RamTipo", computador.RamTipo);
                            cmd.Parameters.AddWithValue("@RamVelocidade", computador.RamVelocidade);
                            cmd.Parameters.AddWithValue("@RamVoltagem", computador.RamVoltagem);
                            cmd.Parameters.AddWithValue("@RamPorModule", computador.RamPorModule);
                            cmd.Parameters.AddWithValue("@ArmazenamentoC", computador.ArmazenamentoC);
                            cmd.Parameters.AddWithValue("@ArmazenamentoCTotal", computador.ArmazenamentoCTotal);
                            cmd.Parameters.AddWithValue("@ArmazenamentoCLivre", computador.ArmazenamentoCLivre);
                            cmd.Parameters.AddWithValue("@ArmazenamentoD", computador.ArmazenamentoD);
                            cmd.Parameters.AddWithValue("@ArmazenamentoDTotal", computador.ArmazenamentoDTotal);
                            cmd.Parameters.AddWithValue("@ArmazenamentoDLivre", computador.ArmazenamentoDLivre);
                            cmd.Parameters.AddWithValue("@ConsumoCPU", computador.ConsumoCPU);
                            cmd.Parameters.AddWithValue("@SO", computador.SO);
                            cmd.Parameters.AddWithValue("@DataColeta", computador.DataColeta);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar o computador.");
                    ViewBag.Message = "Ocorreu um erro ao editar o computador. Por favor, tente novamente mais tarde.";
                }
            }
            return View(computador);
        }

        // GET: Computadores/Delete/5
        public IActionResult Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

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
                                    DataColeta = Convert.ToDateTime(reader["DataColeta"])
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
    }
}