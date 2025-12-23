using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Web.Models;
using Web.Services;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador")]
    public class ManutencoesController : Controller
    {
        private readonly ManutencaoService _manutencaoService;
        private readonly IDatabaseService _databaseService;

        public ManutencoesController(ManutencaoService manutencaoService, PersistentLogService persistentLogService, IDatabaseService databaseService)
        {
            _manutencaoService = manutencaoService;
            _databaseService = databaseService;
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
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "SELECT MAC, Hostname FROM Computadores ORDER BY Hostname";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
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
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "SELECT PartNumber, Modelo FROM Monitores ORDER BY Modelo";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
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
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "SELECT PartNumber, Tipo FROM Perifericos ORDER BY Tipo";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
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
            _manutencaoService.DeleteManutencao(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
