using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Web.Models;
using Web.Services;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador")]
    public class ManutencoesController : Controller
    {
        private readonly ManutencaoService _manutencaoService;
        private readonly DatabaseLogService _databaseLogService;
        private readonly string _connectionString;

        public ManutencoesController(ManutencaoService manutencaoService, DatabaseLogService databaseLogService, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _manutencaoService = manutencaoService;
            _databaseLogService = databaseLogService;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index(string partNumber, string colaborador, string hostname)
        {
            var manutencoes = _manutencaoService.GetAllManutencoes(partNumber, colaborador, hostname);

            var viewModel = new ManutencaoIndexViewModel
            {
                Manutencoes = manutencoes,
                PartNumber = partNumber,
                Colaborador = colaborador,
                Hostname = hostname
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["ComputadorMAC"] = new SelectList(GetComputadores().Select(c => new { Value = c.MAC, Text = $"{c.Hostname} ({c.MAC})" }), "Value", "Text");
            ViewData["MonitorPartNumber"] = new SelectList(GetMonitores().Select(m => new { Value = m.PartNumber, Text = $"{m.Modelo} ({m.PartNumber})" }), "Value", "Text");
            ViewData["PerifericoPartNumber"] = new SelectList(GetPerifericos().Select(p => new { Value = p.PartNumber, Text = $"{p.Tipo} ({p.PartNumber})" }), "Value", "Text");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(Manutencao manutencao)
        {
            if (ModelState.IsValid)
            {
                _manutencaoService.AddManutencao(manutencao);
                _databaseLogService.AddLog("Maintenance", "Create", User.Identity.Name, $"Maintenance for item created.");
                return RedirectToAction(nameof(Index));
            }
            ViewData["ComputadorMAC"] = new SelectList(GetComputadores().Select(c => new { Value = c.MAC, Text = $"{c.Hostname} ({c.MAC})" }), "Value", "Text", manutencao.ComputadorMAC);
            ViewData["MonitorPartNumber"] = new SelectList(GetMonitores().Select(m => new { Value = m.PartNumber, Text = $"{m.Modelo} ({m.PartNumber})" }), "Value", "Text", manutencao.MonitorPartNumber);
            ViewData["PerifericoPartNumber"] = new SelectList(GetPerifericos().Select(p => new { Value = p.PartNumber, Text = $"{p.Tipo} ({p.PartNumber})" }), "Value", "Text", manutencao.PerifericoPartNumber);
            return View(manutencao);
        }

        private List<Computador> GetComputadores()
        {
            var computadores = new List<Computador>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT MAC, Hostname FROM Computadores ORDER BY Hostname";
                using (var command = new SqlCommand(sql, connection))
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

        private List<Web.Models.Monitor> GetMonitores()
        {
            var monitores = new List<Web.Models.Monitor>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT PartNumber, Modelo FROM Monitores ORDER BY Modelo";
                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            monitores.Add(new Web.Models.Monitor
                            {
                                PartNumber = reader.GetString(0),
                                Modelo = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return monitores;
        }

        private List<Periferico> GetPerifericos()
        {
            var perifericos = new List<Periferico>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT PartNumber, Tipo FROM Perifericos ORDER BY Tipo";
                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            perifericos.Add(new Periferico
                            {
                                PartNumber = reader.GetString(0),
                                Tipo = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return perifericos;
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var manutencao = _manutencaoService.GetManutencaoById(id);
            if (manutencao == null)
            {
                return NotFound();
            }
            ViewData["ComputadorMAC"] = new SelectList(GetComputadores().Select(c => new { Value = c.MAC, Text = $"{c.Hostname} ({c.MAC})" }), "Value", "Text", manutencao.ComputadorMAC);
            ViewData["MonitorPartNumber"] = new SelectList(GetMonitores().Select(m => new { Value = m.PartNumber, Text = $"{m.Modelo} ({m.PartNumber})" }), "Value", "Text", manutencao.MonitorPartNumber);
            ViewData["PerifericoPartNumber"] = new SelectList(GetPerifericos().Select(p => new { Value = p.PartNumber, Text = $"{p.Tipo} ({p.PartNumber})" }), "Value", "Text", manutencao.PerifericoPartNumber);
            return View(manutencao);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id, Manutencao manutencao)
        {
            if (id != manutencao.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _manutencaoService.UpdateManutencao(manutencao);
                _databaseLogService.AddLog("Maintenance", "Update", User.Identity.Name, $"Maintenance '{manutencao.Id}' updated.");
                return RedirectToAction(nameof(Index));
            }
            ViewData["ComputadorMAC"] = new SelectList(GetComputadores().Select(c => new { Value = c.MAC, Text = $"{c.Hostname} ({c.MAC})" }), "Value", "Text", manutencao.ComputadorMAC);
            ViewData["MonitorPartNumber"] = new SelectList(GetMonitores().Select(m => new { Value = m.PartNumber, Text = $"{m.Modelo} ({m.PartNumber})" }), "Value", "Text", manutencao.MonitorPartNumber);
            ViewData["PerifericoPartNumber"] = new SelectList(GetPerifericos().Select(p => new { Value = p.PartNumber, Text = $"{p.Tipo} ({p.PartNumber})" }), "Value", "Text", manutencao.PerifericoPartNumber);
            return View(manutencao);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var manutencao = _manutencaoService.GetManutencaoById(id);
            if (manutencao == null)
            {
                return NotFound();
            }
            return View(manutencao);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteConfirmed(int id)
        {
            var manutencao = _manutencaoService.GetManutencaoById(id);
            if (manutencao != null)
            {
                _databaseLogService.AddLog("Maintenance", "Delete", User.Identity.Name, $"Maintenance '{manutencao.Id}' deleted.");
            }
            _manutencaoService.DeleteManutencao(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
