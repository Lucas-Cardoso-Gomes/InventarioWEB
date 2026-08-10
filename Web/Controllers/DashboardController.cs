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
                    cmd.CommandText = "SELECT comp.MAC, comp.Hostname, comp.DataGarantia, comp.Backup, comp.DataColeta, comp.BateriaWearLevel, comp.ConsumoCPU, col.Nome FROM Computadores comp LEFT JOIN Colaboradores col ON comp.ColaboradorCPF = col.CPF";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            viewModel.TotalComputadores++;

                            double? bateriaWearLevel = null;
                            if (reader["BateriaWearLevel"] != DBNull.Value)
                            {
                                string wearLevelStr = reader["BateriaWearLevel"].ToString();
                                // Parse format "Ciclo: X - Y% Desgaste"
                                if (wearLevelStr.Contains("% Desgaste"))
                                {
                                    var parts = wearLevelStr.Split('-');
                                    if (parts.Length == 2)
                                    {
                                        string percStr = parts[1].Replace("% Desgaste", "").Trim();
                                        if (double.TryParse(percStr, out double perc))
                                        {
                                            bateriaWearLevel = perc;
                                        }
                                    }
                                }
                            }

                            double? cpuUsage = null;
                            if (reader["ConsumoCPU"] != DBNull.Value)
                            {
                                string cpuStr = reader["ConsumoCPU"].ToString().Replace("%", "").Trim();
                                if (double.TryParse(cpuStr, out double cVal))
                                {
                                    cpuUsage = cVal;
                                }
                            }

                            viewModel.Equipamentos.Add(new EquipamentoDashboardItem
                            {
                                TipoEquipamento = "Computador",
                                Identificador = reader["MAC"].ToString(),
                                ModeloOuNome = reader["Hostname"].ToString(),
                                DataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null,
                                Backup = reader["Backup"].ToString(),
                                DataColeta = reader["DataColeta"] != DBNull.Value ? Convert.ToDateTime(reader["DataColeta"]) : (DateTime?)null,
                                ColaboradorNome = reader["Nome"].ToString(),
                                BateriaWearLevel = bateriaWearLevel,
                                CpuUsage = cpuUsage
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

            // Calculate Pie Chart Data
            int temGarantia = 0;
            int semGarantia = 0;
            int semDados = 0;
            var expirations = new Dictionary<string, int>();

            foreach (var item in viewModel.Equipamentos)
            {
                if (item.DataGarantia.HasValue)
                {
                    if (item.DataGarantia.Value >= DateTime.Now)
                    {
                        temGarantia++;
                        var key = item.DataGarantia.Value.ToString("MM/yyyy");
                        if (expirations.ContainsKey(key)) expirations[key]++;
                        else expirations[key] = 1;
                    }
                    else
                    {
                        semGarantia++;
                    }
                }
                else
                {
                    semDados++;
                }
            }

            viewModel.GarantiaPieData = new List<int> { temGarantia, semGarantia, semDados };
            
            var sortedExpirations = new List<string>(expirations.Keys);
            sortedExpirations.Sort((a, b) => DateTime.ParseExact(a, "MM/yyyy", null).CompareTo(DateTime.ParseExact(b, "MM/yyyy", null)));
            
            foreach (var key in sortedExpirations)
            {
                viewModel.GarantiaBarData.Add(new ChartData { Label = key, Value = expirations[key] });
            }

            // Calculate Battery Wear Data
            double totalWear = 0;
            int wearCount = 0;
            var computersWithBattery = new List<EquipamentoDashboardItem>();

            foreach (var item in viewModel.Equipamentos)
            {
                if (item.TipoEquipamento == "Computador" && item.BateriaWearLevel.HasValue)
                {
                    double wear = item.BateriaWearLevel.Value;
                    totalWear += wear;
                    wearCount++;
                    computersWithBattery.Add(item);

                    if (wear >= 50) viewModel.BatteryCriticalCount++;
                    else if (wear >= 25) viewModel.BatteryWarningCount++;
                    else viewModel.BatteryGoodCount++;
                }
            }

            if (wearCount > 0)
            {
                viewModel.AverageBatteryWear = totalWear / wearCount;
                computersWithBattery.Sort((a, b) => b.BateriaWearLevel.Value.CompareTo(a.BateriaWearLevel.Value)); // Sort descending

                viewModel.WorstBatteryComputer = computersWithBattery[0];

                int topCount = Math.Min(5, computersWithBattery.Count);
                for (int i = 0; i < topCount; i++)
                {
                    viewModel.TopWorstBatteries.Add(computersWithBattery[i]);
                }
            }

            // Calculate CPU Usage Data
            double totalCpu = 0;
            int cpuCount = 0;
            var computersWithCpu = new List<EquipamentoDashboardItem>();

            foreach (var item in viewModel.Equipamentos)
            {
                if (item.TipoEquipamento == "Computador" && item.CpuUsage.HasValue)
                {
                    double cpu = item.CpuUsage.Value;
                    totalCpu += cpu;
                    cpuCount++;
                    computersWithCpu.Add(item);

                    if (cpu >= 50) viewModel.CpuCriticalCount++;
                    else if (cpu >= 25) viewModel.CpuWarningCount++;
                    else viewModel.CpuGoodCount++;
                }
            }

            if (cpuCount > 0)
            {
                viewModel.AverageCpuUsage = totalCpu / cpuCount;
                computersWithCpu.Sort((a, b) => b.CpuUsage.Value.CompareTo(a.CpuUsage.Value)); // Sort descending

                viewModel.WorstCpuComputer = computersWithCpu[0];

                int topCpuCount = Math.Min(5, computersWithCpu.Count);
                for (int i = 0; i < topCpuCount; i++)
                {
                    viewModel.TopWorstCpus.Add(computersWithCpu[i]);
                }
            }

            // Calculate Coleta Data
            var coletaCounts = new Dictionary<string, int>();
            foreach (var item in viewModel.Equipamentos)
            {
                if (item.TipoEquipamento == "Computador" && item.DataColeta.HasValue)
                {
                    string key = item.DataColeta.Value.ToString("dd/MM/yyyy");
                    if (coletaCounts.ContainsKey(key))
                    {
                        coletaCounts[key]++;
                    }
                    else
                    {
                        coletaCounts[key] = 1;
                    }
                }
            }

            var sortedColetas = new List<string>(coletaCounts.Keys);
            sortedColetas.Sort((a, b) => DateTime.ParseExact(a, "dd/MM/yyyy", null).CompareTo(DateTime.ParseExact(b, "dd/MM/yyyy", null)));

            // Show last 7 days of data at most for the chart
            int startIndex = Math.Max(0, sortedColetas.Count - 7);
            for (int i = startIndex; i < sortedColetas.Count; i++)
            {
                var key = sortedColetas[i];
                viewModel.ColetaBarData.Add(new ChartData { Label = key, Value = coletaCounts[key] });
            }

            return View(viewModel);
        }
    }
}
