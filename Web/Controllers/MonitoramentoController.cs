using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MonitoramentoController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<MonitoramentoController> _logger;

        public MonitoramentoController(IConfiguration configuration, ILogger<MonitoramentoController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public IActionResult Index()
        {
            var redes = new List<Rede>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM Rede ORDER BY IP", connection);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            redes.Add(new Rede
                            {
                                Id = (int)reader["Id"],
                                Tipo = reader["Tipo"].ToString(),
                                IP = reader["IP"].ToString(),
                                MAC = reader["MAC"].ToString(),
                                Nome = reader["Nome"].ToString(),
                                Descricao = reader["Descricao"].ToString(),
                                DataInclusao = (DateTime)reader["DataInclusao"],
                                DataAlteracao = reader["DataAlteracao"] as DateTime?,
                                Observacao = reader["Observacao"].ToString(),
                                Status = reader["Status"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting network assets list for monitoring.");
                // Handle error appropriately
            }
            return View(redes);
        }

        [HttpGet]
        public IActionResult GetStatus()
        {
            var redes = new List<Rede>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT Id, IP, Status FROM Rede ORDER BY IP", connection);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            redes.Add(new Rede
                            {
                                Id = (int)reader["Id"],
                                IP = reader["IP"].ToString(),
                                Status = reader["Status"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting network assets status for monitoring.");
                // Handle error appropriately
            }
            return Json(redes);
        }
    }
}
