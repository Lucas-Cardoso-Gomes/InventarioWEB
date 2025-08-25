using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using web.Models;
using Microsoft.AspNetCore.Authorization;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador")]
    public class ExportarController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<ExportarController> _logger;

        public ExportarController(IConfiguration configuration, ILogger<ExportarController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public IActionResult Index()
        {
            var viewModel = new ExportarViewModel();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                // Computer filters
                viewModel.Fabricantes = GetDistinctValues(connection, "Computadores", "Fabricante");
                viewModel.SOs = GetDistinctValues(connection, "Computadores", "SO");
                viewModel.ProcessadorFabricantes = GetDistinctValues(connection, "Computadores", "ProcessadorFabricante");
                viewModel.RamTipos = GetDistinctValues(connection, "Computadores", "RamTipo");
                viewModel.Processadores = GetDistinctValues(connection, "Computadores", "Processador");
                viewModel.Rams = GetDistinctValues(connection, "Computadores", "Ram");

                // Monitor filters
                viewModel.Marcas = GetDistinctValues(connection, "Monitores", "Marca");
                viewModel.Tamanhos = GetDistinctValues(connection, "Monitores", "Tamanho");
                viewModel.Modelos = GetDistinctValues(connection, "Monitores", "Modelo");

                // Periferico filters
                viewModel.TiposPeriferico = GetDistinctValues(connection, "Perifericos", "Tipo");
            }
            return View(viewModel);
        }

        private List<string> GetDistinctValues(SqlConnection connection, string tableName, string columnName)
        {
            var values = new List<string>();
            var sql = $"SELECT DISTINCT {columnName} FROM {tableName} WHERE {columnName} IS NOT NULL ORDER BY {columnName}";
            using (var command = new SqlCommand(sql, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        values.Add(reader[0].ToString());
                    }
                }
            }
            return values;
        }

        [HttpPost]
        public IActionResult Export(ExportarViewModel viewModel)
        {
            var csvBuilder = new StringBuilder();
            string fileName = $"export_{viewModel.DeviceType}_{DateTime.Now:yyyyMMddHHmmss}.csv";
            string sql = "";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var whereClauses = new List<string>();
                var parameters = new Dictionary<string, object>();

                Action<string, List<string>> addInClause = (columnName, values) =>
                {
                    if (values != null && values.Any())
                    {
                        var paramNames = new List<string>();
                        for (int i = 0; i < values.Count; i++)
                        {
                            var paramName = $"@{columnName.ToLower().Replace(" ", "")}{i}";
                            paramNames.Add(paramName);
                            parameters.Add(paramName, values[i]);
                        }
                        whereClauses.Add($"{columnName} IN ({string.Join(", ", paramNames)})");
                    }
                };

                switch (viewModel.DeviceType)
                {
                    case DeviceType.Computadores:
                        addInClause("Fabricante", viewModel.CurrentFabricantes);
                        addInClause("SO", viewModel.CurrentSOs);
                        addInClause("ProcessadorFabricante", viewModel.CurrentProcessadorFabricantes);
                        addInClause("RamTipo", viewModel.CurrentRamTipos);
                        addInClause("Processador", viewModel.CurrentProcessadores);
                        addInClause("Ram", viewModel.CurrentRams);

                        csvBuilder.AppendLine("MAC,IP,ColaboradorNome,Hostname,Fabricante,Processador,ProcessadorFabricante,ProcessadorCore,ProcessadorThread,ProcessadorClock,Ram,RamTipo,RamVelocidade,RamVoltagem,RamPorModule,ArmazenamentoC,ArmazenamentoCTotal,ArmazenamentoCLivre,ArmazenamentoD,ArmazenamentoDTotal,ArmazenamentoDLivre,ConsumoCPU,SO,DataColeta");
                        sql = "SELECT * FROM Computadores";
                        break;

                    case DeviceType.Monitores:
                        addInClause("Marca", viewModel.CurrentMarcas);
                        addInClause("Tamanho", viewModel.CurrentTamanhos);
                        addInClause("Modelo", viewModel.CurrentModelos);

                        csvBuilder.AppendLine("PartNumber,ColaboradorNome,Marca,Modelo,Tamanho");
                        sql = "SELECT * FROM Monitores";
                        break;

                    case DeviceType.Perifericos:
                        addInClause("Tipo", viewModel.CurrentTiposPeriferico);

                        csvBuilder.AppendLine("ID,ColaboradorNome,Tipo,DataEntrega,PartNumber");
                        sql = "SELECT * FROM Perifericos";
                        break;
                }

                if (whereClauses.Any())
                {
                    sql += " WHERE " + string.Join(" AND ", whereClauses);
                }

                using (var cmd = new SqlCommand(sql, connection))
                {
                    foreach (var p in parameters)
                    {
                        cmd.Parameters.AddWithValue(p.Key, p.Value);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var line = new List<string>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                line.Add(reader[i].ToString());
                            }
                            csvBuilder.AppendLine(string.Join(",", line));
                        }
                    }
                }
            }

            return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", fileName);
        }
    }
}
