using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using web.Models;
using Web.Services;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManutencoesController : Controller
    {
        private readonly ManutencaoService _manutencaoService;
        private readonly PersistentLogService _persistentLogService;
        private readonly string _connectionString;

        public ManutencoesController(ManutencaoService manutencaoService, PersistentLogService persistentLogService, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _manutencaoService = manutencaoService;
            _persistentLogService = persistentLogService;
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
            ViewData["MonitorPartNumber"] = new SelectList(GetMonitores(), "PartNumber", "Modelo");
            ViewData["PerifericoPartNumber"] = new SelectList(GetPerifericos(), "PartNumber", "Tipo");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Manutencao manutencao)
        {
            if (ModelState.IsValid)
            {
                _manutencaoService.AddManutencao(manutencao);
                _persistentLogService.AddLog("Maintenance", "Create", User.Identity.Name, $"Maintenance for item created.");
                return RedirectToAction(nameof(Index));
            }
            ViewData["ComputadorMAC"] = new SelectList(GetComputadores(), "MAC", "Hostname", manutencao.ComputadorMAC);
            ViewData["MonitorPartNumber"] = new SelectList(GetMonitores(), "PartNumber", "Modelo", manutencao.MonitorPartNumber);
            ViewData["PerifericoPartNumber"] = new SelectList(GetPerifericos(), "PartNumber", "Tipo", manutencao.PerifericoPartNumber);
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

        private List<web.Models.Monitor> GetMonitores()
        {
            var monitores = new List<web.Models.Monitor>();
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
                            monitores.Add(new web.Models.Monitor
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

        public IActionResult Edit(int id)
        {
            var manutencao = _manutencaoService.GetManutencaoById(id);
            if (manutencao == null)
            {
                return NotFound();
            }
            ViewData["ComputadorMAC"] = new SelectList(GetComputadores(), "MAC", "Hostname", manutencao.ComputadorMAC);
            ViewData["MonitorPartNumber"] = new SelectList(GetMonitores(), "PartNumber", "Modelo", manutencao.MonitorPartNumber);
            ViewData["PerifericoPartNumber"] = new SelectList(GetPerifericos(), "PartNumber", "Tipo", manutencao.PerifericoPartNumber);
            return View(manutencao);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Manutencao manutencao)
        {
            if (id != manutencao.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _manutencaoService.UpdateManutencao(manutencao);
                _persistentLogService.AddLog("Maintenance", "Update", User.Identity.Name, $"Maintenance '{manutencao.Id}' updated.");
                return RedirectToAction(nameof(Index));
            }
            ViewData["ComputadorMAC"] = new SelectList(GetComputadores(), "MAC", "Hostname", manutencao.ComputadorMAC);
            ViewData["MonitorPartNumber"] = new SelectList(GetMonitores(), "PartNumber", "Modelo", manutencao.MonitorPartNumber);
            ViewData["PerifericoPartNumber"] = new SelectList(GetPerifericos(), "PartNumber", "Tipo", manutencao.PerifericoPartNumber);
            return View(manutencao);
        }

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
        public IActionResult DeleteConfirmed(int id)
        {
            var manutencao = _manutencaoService.GetManutencaoById(id);
            if (manutencao != null)
            {
                _persistentLogService.AddLog("Maintenance", "Delete", User.Identity.Name, $"Maintenance '{manutencao.Id}' deleted.");
            }
            _manutencaoService.DeleteManutencao(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
