using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Web.Models;
using Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Controllers
{
    [Authorize]
    public class MonitoresController : Controller
    {
        private readonly MonitorService _monitorService;
        private readonly UserService _userService;
        private readonly PersistentLogService _persistentLogService;
        private readonly ILogger<MonitoresController> _logger;

        public MonitoresController(MonitorService monitorService, UserService userService, PersistentLogService persistentLogService, ILogger<MonitoresController> logger)
        {
            _monitorService = monitorService;
            _userService = userService;
            _persistentLogService = persistentLogService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchString, List<string> currentMarcas, List<string> currentTamanhos, List<string> currentModelos, int pageNumber = 1, int pageSize = 25)
        {
            ViewData["CurrentFilter"] = searchString;
            try
            {
                var (monitores, totalCount) = await _monitorService.GetMonitoresAsync(User, searchString, currentMarcas, currentTamanhos, currentModelos, pageNumber, pageSize);

                var viewModel = new MonitorIndexViewModel
                {
                    Monitores = monitores,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    SearchString = searchString,
                    Marcas = await _monitorService.GetDistinctMonitorValuesAsync("Marca"),
                    Tamanhos = await _monitorService.GetDistinctMonitorValuesAsync("Tamanho"),
                    Modelos = await _monitorService.GetDistinctMonitorValuesAsync("Modelo"),
                    CurrentMarcas = currentMarcas,
                    CurrentTamanhos = currentTamanhos,
                    CurrentModelos = currentModelos
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de monitores.");
                ViewBag.Message = "Ocorreu um erro ao obter a lista de monitores. Por favor, tente novamente mais tarde.";
                return View(new MonitorIndexViewModel());
            }
        }

        // GET: Monitores/Create
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Create()
        {
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome");
            return View(new Web.Models.Monitor());
        }

        // POST: Monitores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Create(Web.Models.Monitor monitor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _monitorService.CreateMonitorAsync(monitor);
                    _persistentLogService.AddLog("Monitor", "Create", User.Identity.Name, $"Monitor '{monitor.PartNumber}' created.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar monitor.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o monitor. Verifique se o PartNumber já existe.");
                }
            }
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", monitor.UserId);
            return View(monitor);
        }

        // GET: Monitores/Edit/5
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var monitor = await _monitorService.FindMonitorByIdAsync(id);

            if (monitor == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", monitor.UserId);
            return View(monitor);
        }

        // POST: Monitores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Edit(string id, Web.Models.Monitor monitor)
        {
            if (id != monitor.PartNumber)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _monitorService.UpdateMonitorAsync(monitor);
                    _persistentLogService.AddLog("Monitor", "Update", User.Identity.Name, $"Monitor '{monitor.PartNumber}' updated.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar monitor.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o monitor.");
                }
            }
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", monitor.UserId);
            return View(monitor);
        }

        // GET: Monitores/Delete/5
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var monitor = await _monitorService.FindMonitorByIdAsync(id);

            if (monitor == null)
            {
                return NotFound();
            }

            return View(monitor);
        }

        // POST: Monitores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                await _monitorService.DeleteMonitorAsync(id);
                _persistentLogService.AddLog("Monitor", "Delete", User.Identity.Name, $"Monitor '{id}' deleted.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir monitor.");
                ViewBag.Message = "Ocorreu um erro ao excluir o monitor.";
                var monitor = await _monitorService.FindMonitorByIdAsync(id);
                return View(monitor);
            }
        }
    }
}
