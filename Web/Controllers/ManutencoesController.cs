using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using web.Models;
using Web.Services;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManutencoesController : Controller
    {
        private readonly ManutencaoService _manutencaoService;
        private readonly string _connectionString;

        public ManutencoesController(ManutencaoService manutencaoService, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _manutencaoService = manutencaoService;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index()
        {
            var manutencoes = _manutencaoService.GetAllManutencoes();
            return View(manutencoes);
        }

        public IActionResult Create()
        {
            ViewData["ComputadorMAC"] = new SelectList(GetComputadores(), "MAC", "Hostname");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Manutencao manutencao)
        {
            if (ModelState.IsValid)
            {
                _manutencaoService.AddManutencao(manutencao);
                return RedirectToAction(nameof(Index));
            }
            ViewData["ComputadorMAC"] = new SelectList(GetComputadores(), "MAC", "Hostname", manutencao.ComputadorMAC);
            return View(manutencao);
        }

        private System.Collections.Generic.List<Computador> GetComputadores()
        {
            var computadores = new System.Collections.Generic.List<Computador>();
            using (var connection = new System.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT MAC, Hostname FROM Computadores ORDER BY Hostname";
                using (var command = new System.Data.SqlClient.SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            computadores.Add(new Computador
                            {
                                MAC = reader.GetString(0),
                                Hostname = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return computadores;
        }
    }
}
