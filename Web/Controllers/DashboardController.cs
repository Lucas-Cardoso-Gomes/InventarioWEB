using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Diretoria/RH")]
    public class DashboardController : Controller
    {
        private readonly IDatabaseService _databaseService;

        public DashboardController(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public IActionResult Index()
        {
            var viewModel = new DashboardViewModel();

            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();

                // Computadores
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT comp.MAC, comp.Hostname, comp.DataGarantia, comp.Backup, comp.DataColeta, col.Nome FROM Computadores comp LEFT JOIN Colaboradores col ON comp.ColaboradorCPF = col.CPF";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            viewModel.TotalComputadores++;
                            viewModel.Equipamentos.Add(new EquipamentoDashboardItem
                            {
                                TipoEquipamento = "Computador",
                                Identificador = reader["MAC"].ToString(),
                                ModeloOuNome = reader["Hostname"].ToString(),
                                DataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null,
                                Backup = reader["Backup"].ToString(),
                                DataColeta = reader["DataColeta"] != DBNull.Value ? Convert.ToDateTime(reader["DataColeta"]) : (DateTime?)null,
                                ColaboradorNome = reader["Nome"].ToString()
                            });
                        }
                    }
                }

                // Monitores
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT m.PartNumber, m.Modelo, m.DataGarantia, col.Nome FROM Monitores m LEFT JOIN Colaboradores col ON m.ColaboradorCPF = col.CPF";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            viewModel.TotalMonitores++;
                            viewModel.Equipamentos.Add(new EquipamentoDashboardItem
                            {
                                TipoEquipamento = "Monitor",
                                Identificador = reader["PartNumber"].ToString(),
                                ModeloOuNome = reader["Modelo"].ToString(),
                                DataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null,
                                ColaboradorNome = reader["Nome"].ToString()
                            });
                        }
                    }
                }

                // Perifericos
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT p.PartNumber, p.Tipo, p.DataGarantia, col.Nome FROM Perifericos p LEFT JOIN Colaboradores col ON p.ColaboradorCPF = col.CPF";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            viewModel.TotalPerifericos++;
                            viewModel.Equipamentos.Add(new EquipamentoDashboardItem
                            {
                                TipoEquipamento = "Periférico",
                                Identificador = reader["PartNumber"].ToString(),
                                ModeloOuNome = reader["Tipo"].ToString(),
                                DataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null,
                                ColaboradorNome = reader["Nome"].ToString()
                            });
                        }
                    }
                }

                // Redes
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Nome, DataGarantia FROM Rede";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            viewModel.TotalRedes++;
                            viewModel.Equipamentos.Add(new EquipamentoDashboardItem
                            {
                                TipoEquipamento = "Ativo de Rede",
                                Identificador = reader["Id"].ToString(),
                                ModeloOuNome = reader["Nome"].ToString(),
                                DataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null
                            });
                        }
                    }
                }

                // Smartphones
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Modelo, Usuario, DataGarantia FROM Smartphones";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            viewModel.TotalSmartphones++;
                            viewModel.Equipamentos.Add(new EquipamentoDashboardItem
                            {
                                TipoEquipamento = "Smartphone",
                                Identificador = reader["Id"].ToString(),
                                ModeloOuNome = reader["Modelo"].ToString(),
                                DataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null,
                                ColaboradorNome = reader["Usuario"].ToString()
                            });
                        }
                    }
                }
            }

            return View(viewModel);
        }
    }
}
