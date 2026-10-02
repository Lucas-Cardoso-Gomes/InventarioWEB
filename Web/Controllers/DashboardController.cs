using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public IActionResult Index(string filial, string setor, string coordenador, string dispositivo, string statusGarantia)
        {
            var viewModel = new DashboardViewModel
            {
                SelectedFilial = filial,
                SelectedSetor = setor,
                SelectedCoordenador = coordenador,
                SelectedDispositivo = dispositivo,
                SelectedStatusGarantia = statusGarantia
            };

            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();

                // Get Filter Options
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT DISTINCT Filial FROM Colaboradores WHERE Filial IS NOT NULL AND TRIM(Filial) != '' ORDER BY Filial";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) viewModel.Filiais.Add(reader.GetString(0));
                    }
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT DISTINCT Setor FROM Colaboradores WHERE Setor IS NOT NULL AND TRIM(Setor) != '' ORDER BY Setor";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) viewModel.Setores.Add(reader.GetString(0));
                    }
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT c.CPF, c.Nome FROM Colaboradores c INNER JOIN Usuarios u ON c.CPF = u.ColaboradorCPF WHERE u.Role = 'Coordenador' OR u.IsCoordinator = 1 ORDER BY c.Nome";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) viewModel.Coordenadores.Add(reader["Nome"].ToString());
                    }
                }

                // Get Support Tickets Summary
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT Status, COUNT(*) as Qtd FROM Chamados GROUP BY Status";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string st = reader["Status"].ToString();
                            int qtd = Convert.ToInt32(reader["Qtd"]);
                            if (st == "Aberto") viewModel.TotalChamadosAbertos = qtd;
                            else if (st == "Em Andamento") viewModel.TotalChamadosEmAndamento = qtd;
                        }
                    }
                }

                // Computadores
                if (string.IsNullOrEmpty(dispositivo) || dispositivo == "Computador")
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT comp.MAC, comp.Hostname, comp.DataGarantia, comp.Backup, comp.DataColeta, comp.BateriaWearLevel, comp.ConsumoCPU, col.Nome, col.Filial, col.Setor
                                            FROM Computadores comp 
                                            LEFT JOIN Colaboradores col ON comp.ColaboradorCPF = col.CPF";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string colFilial = reader["Filial"] != DBNull.Value ? reader["Filial"].ToString() : "";
                                string colSetor = reader["Setor"] != DBNull.Value ? reader["Setor"].ToString() : "";
                                string colNome = reader["Nome"] != DBNull.Value ? reader["Nome"].ToString() : "";

                                if (!string.IsNullOrEmpty(filial) && colFilial != filial) continue;
                                if (!string.IsNullOrEmpty(setor) && colSetor != setor) continue;

                                viewModel.TotalComputadores++;

                                double? bateriaWearLevel = null;
                                if (reader["BateriaWearLevel"] != DBNull.Value)
                                {
                                    string wearLevelStr = reader["BateriaWearLevel"].ToString();
                                    if (wearLevelStr.Contains("% Desgaste"))
                                    {
                                        var parts = wearLevelStr.Split('-');
                                        if (parts.Length == 2)
                                        {
                                            string percStr = parts[1].Replace("% Desgaste", "").Trim();
                                            if (double.TryParse(percStr, out double perc)) bateriaWearLevel = perc;
                                        }
                                    }
                                }

                                double? cpuUsage = null;
                                if (reader["ConsumoCPU"] != DBNull.Value)
                                {
                                    string cpuStr = reader["ConsumoCPU"].ToString().Replace("%", "").Trim();
                                    if (double.TryParse(cpuStr, out double cVal)) cpuUsage = cVal;
                                }

                                DateTime? dataColeta = reader["DataColeta"] != DBNull.Value ? Convert.ToDateTime(reader["DataColeta"]) : (DateTime?)null;
                                DateTime? dataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null;
                                string backup = reader["Backup"] != DBNull.Value ? reader["Backup"].ToString() : "";

                                // Risk checks
                                if (!dataColeta.HasValue || (DateTime.Now - dataColeta.Value).TotalDays > 15) viewModel.TotalAgentesInativos++;
                                if (string.IsNullOrEmpty(backup) || backup.Equals("Não", StringComparison.OrdinalIgnoreCase)) viewModel.TotalSemBackup++;
                                if (dataGarantia.HasValue && dataGarantia.Value < DateTime.Now) viewModel.TotalGarantiasVencidas++;

                                if (!string.IsNullOrEmpty(statusGarantia))
                                {
                                    if (statusGarantia == "Vencida" && (!dataGarantia.HasValue || dataGarantia.Value >= DateTime.Now)) continue;
                                    if (statusGarantia == "Valida" && (!dataGarantia.HasValue || dataGarantia.Value < DateTime.Now)) continue;
                                }

                                viewModel.Equipamentos.Add(new EquipamentoDashboardItem
                                {
                                    TipoEquipamento = "Computador",
                                    Identificador = reader["MAC"].ToString(),
                                    ModeloOuNome = reader["Hostname"].ToString(),
                                    DataGarantia = dataGarantia,
                                    Backup = backup,
                                    DataColeta = dataColeta,
                                    ColaboradorNome = colNome,
                                    Filial = colFilial,
                                    Setor = colSetor,
                                    BateriaWearLevel = bateriaWearLevel,
                                    CpuUsage = cpuUsage
                                });
                            }
                        }
                    }
                }

                // Monitores
                if (string.IsNullOrEmpty(dispositivo) || dispositivo == "Monitor")
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT m.PartNumber, m.Modelo, m.DataGarantia, col.Nome, col.Filial, col.Setor FROM Monitores m LEFT JOIN Colaboradores col ON m.ColaboradorCPF = col.CPF";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string colFilial = reader["Filial"] != DBNull.Value ? reader["Filial"].ToString() : "";
                                string colSetor = reader["Setor"] != DBNull.Value ? reader["Setor"].ToString() : "";

                                if (!string.IsNullOrEmpty(filial) && colFilial != filial) continue;
                                if (!string.IsNullOrEmpty(setor) && colSetor != setor) continue;

                                viewModel.TotalMonitores++;
                                DateTime? dataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null;
                                if (dataGarantia.HasValue && dataGarantia.Value < DateTime.Now) viewModel.TotalGarantiasVencidas++;

                                viewModel.Equipamentos.Add(new EquipamentoDashboardItem
                                {
                                    TipoEquipamento = "Monitor",
                                    Identificador = reader["PartNumber"].ToString(),
                                    ModeloOuNome = reader["Modelo"].ToString(),
                                    DataGarantia = dataGarantia,
                                    ColaboradorNome = reader["Nome"].ToString(),
                                    Filial = colFilial,
                                    Setor = colSetor
                                });
                            }
                        }
                    }
                }

                // Perifericos
                if (string.IsNullOrEmpty(dispositivo) || dispositivo == "Periférico")
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT p.PartNumber, p.Tipo, p.DataGarantia, col.Nome, col.Filial, col.Setor FROM Perifericos p LEFT JOIN Colaboradores col ON p.ColaboradorCPF = col.CPF";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string colFilial = reader["Filial"] != DBNull.Value ? reader["Filial"].ToString() : "";
                                string colSetor = reader["Setor"] != DBNull.Value ? reader["Setor"].ToString() : "";

                                if (!string.IsNullOrEmpty(filial) && colFilial != filial) continue;
                                if (!string.IsNullOrEmpty(setor) && colSetor != setor) continue;

                                viewModel.TotalPerifericos++;
                                DateTime? dataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null;
                                if (dataGarantia.HasValue && dataGarantia.Value < DateTime.Now) viewModel.TotalGarantiasVencidas++;

                                viewModel.Equipamentos.Add(new EquipamentoDashboardItem
                                {
                                    TipoEquipamento = "Periférico",
                                    Identificador = reader["PartNumber"].ToString(),
                                    ModeloOuNome = reader["Tipo"].ToString(),
                                    DataGarantia = dataGarantia,
                                    ColaboradorNome = reader["Nome"].ToString(),
                                    Filial = colFilial,
                                    Setor = colSetor
                                });
                            }
                        }
                    }
                }

                // Redes
                if (string.IsNullOrEmpty(dispositivo) || dispositivo == "Ativo de Rede")
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Id, Nome, DataGarantia, Localizacao FROM Rede";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                viewModel.TotalRedes++;
                                DateTime? dataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null;
                                if (dataGarantia.HasValue && dataGarantia.Value < DateTime.Now) viewModel.TotalGarantiasVencidas++;

                                viewModel.Equipamentos.Add(new EquipamentoDashboardItem
                                {
                                    TipoEquipamento = "Ativo de Rede",
                                    Identificador = reader["Id"].ToString(),
                                    ModeloOuNome = reader["Nome"].ToString(),
                                    DataGarantia = dataGarantia,
                                    Filial = reader["Localizacao"] != DBNull.Value ? reader["Localizacao"].ToString() : ""
                                });
                            }
                        }
                    }
                }

                // Smartphones
                if (string.IsNullOrEmpty(dispositivo) || dispositivo == "Smartphone")
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Id, Modelo, Usuario, Filial, DataGarantia FROM Smartphones";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string colFilial = reader["Filial"] != DBNull.Value ? reader["Filial"].ToString() : "";
                                if (!string.IsNullOrEmpty(filial) && colFilial != filial) continue;

                                viewModel.TotalSmartphones++;
                                DateTime? dataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null;
                                if (dataGarantia.HasValue && dataGarantia.Value < DateTime.Now) viewModel.TotalGarantiasVencidas++;

                                viewModel.Equipamentos.Add(new EquipamentoDashboardItem
                                {
                                    TipoEquipamento = "Smartphone",
                                    Identificador = reader["Id"].ToString(),
                                    ModeloOuNome = reader["Modelo"].ToString(),
                                    DataGarantia = dataGarantia,
                                    ColaboradorNome = reader["Usuario"].ToString(),
                                    Filial = colFilial
                                });
                            }
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
                computersWithBattery.Sort((a, b) => b.BateriaWearLevel.Value.CompareTo(a.BateriaWearLevel.Value));

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
                computersWithCpu.Sort((a, b) => b.CpuUsage.Value.CompareTo(a.CpuUsage.Value));

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
                    if (coletaCounts.ContainsKey(key)) coletaCounts[key]++;
                    else coletaCounts[key] = 1;
                }
            }

            var sortedColetas = new List<string>(coletaCounts.Keys);
            sortedColetas.Sort((a, b) => DateTime.ParseExact(a, "dd/MM/yyyy", null).CompareTo(DateTime.ParseExact(b, "dd/MM/yyyy", null)));

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
