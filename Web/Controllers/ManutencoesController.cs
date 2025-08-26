using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Web.Models;
using Web.Services;
using System.Threading.Tasks;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManutencoesController : Controller
    {
        private readonly ManutencaoService _manutencaoService;
        private readonly PersistentLogService _persistentLogService;
        private readonly ComputadorService _computadorService;

        public ManutencoesController(ManutencaoService manutencaoService, PersistentLogService persistentLogService, ComputadorService computadorService)
        {
            _manutencaoService = manutencaoService;
            _persistentLogService = persistentLogService;
            _computadorService = computadorService;
        }

        public IActionResult Index()
        {
            var manutencoes = _manutencaoService.GetAllManutencoes();
            return View(manutencoes);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["ComputadorMAC"] = new SelectList(await GetComputadoresAsync(), "MAC", "Hostname");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Manutencao manutencao)
        {
            if (ModelState.IsValid)
            {
                _manutencaoService.AddManutencao(manutencao);
                _persistentLogService.AddLog("Maintenance", "Create", User.Identity.Name, $"Maintenance for computer '{manutencao.ComputadorMAC}' created.");
                return RedirectToAction(nameof(Index));
            }
            ViewData["ComputadorMAC"] = new SelectList(await GetComputadoresAsync(), "MAC", "Hostname", manutencao.ComputadorMAC);
            return View(manutencao);
        }

        private async Task<System.Collections.Generic.List<Computador>> GetComputadoresAsync()
        {
            var (computadores, _) = await _computadorService.GetComputadoresAsync(User, null, null, null, null, null, null, null, null, 1, int.MaxValue);
            return computadores;
        }

        public async Task<IActionResult> Edit(int id)
        {
            var manutencao = _manutencaoService.GetManutencaoById(id);
            if (manutencao == null)
            {
                return NotFound();
            }
            ViewData["ComputadorMAC"] = new SelectList(await GetComputadoresAsync(), "MAC", "Hostname", manutencao.ComputadorMAC);
            return View(manutencao);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Manutencao manutencao)
        {
            if (id != manutencao.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _manutencaoService.UpdateManutencao(manutencao);
                _persistentLogService.AddLog("Maintenance", "Update", User.Identity.Name, $"Maintenance '{manutencao.Id}' for computer '{manutencao.ComputadorMAC}' updated.");
                return RedirectToAction(nameof(Index));
            }
            ViewData["ComputadorMAC"] = new SelectList(await GetComputadoresAsync(), "MAC", "Hostname", manutencao.ComputadorMAC);
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
                _persistentLogService.AddLog("Maintenance", "Delete", User.Identity.Name, $"Maintenance '{manutencao.Id}' for computer '{manutencao.ComputadorMAC}' deleted.");
            }
            _manutencaoService.DeleteManutencao(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
