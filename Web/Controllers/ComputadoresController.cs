using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using web.Models;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Web.Services;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Normal")]
    public class ComputadoresController : Controller
    {
        private readonly ComputadorService _computadorService;
        private readonly UserService _userService;
        private readonly ILogger<ComputadoresController> _logger;
        private readonly PersistentLogService _persistentLogService;

        public ComputadoresController(ComputadorService computadorService, UserService userService, IConfiguration configuration, ILogger<ComputadoresController> logger, PersistentLogService persistentLogService)
        {
            _computadorService = computadorService;
            _userService = userService;
            _logger = logger;
            _persistentLogService = persistentLogService;
        }

        public IActionResult Index(string sortOrder, string searchString,
            List<string> currentFabricantes, List<string> currentSOs, List<string> currentProcessadorFabricantes, List<string> currentRamTipos, List<string> currentProcessadores, List<string> currentRams,
            int pageNumber = 1, int pageSize = 25)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["IpSortParm"] = String.IsNullOrEmpty(sortOrder) ? "ip_desc" : "";
            ViewData["MacSortParm"] = sortOrder == "mac" ? "mac_desc" : "mac";
            ViewData["UserSortParm"] = sortOrder == "user" ? "user_desc" : "user";
            ViewData["HostnameSortParm"] = sortOrder == "hostname" ? "hostname_desc" : "hostname";
            ViewData["OsSortParm"] = sortOrder == "os" ? "os_desc" : "os";
            ViewData["DateSortParm"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["CurrentFilter"] = searchString;

            try
            {
                var (computadores, totalCount) = await _computadorService.GetComputadoresAsync(User, sortOrder, searchString, currentFabricantes, currentSOs, currentProcessadorFabricantes, currentRamTipos, currentProcessadores, currentRams, pageNumber, pageSize);

                var viewModel = new ComputadorIndexViewModel
                {
                    Computadores = computadores,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    SearchString = searchString,
                    CurrentSort = sortOrder,
                    CurrentFabricantes = currentFabricantes,
                    CurrentSOs = currentSOs,
                    CurrentProcessadorFabricantes = currentProcessadorFabricantes,
                    CurrentRamTipos = currentRamTipos,
                    CurrentProcessadores = currentProcessadores,
                    CurrentRams = currentRams,
                    Fabricantes = await _computadorService.GetDistinctComputerValuesAsync("Fabricante"),
                    SOs = await _computadorService.GetDistinctComputerValuesAsync("SO"),
                    ProcessadorFabricantes = await _computadorService.GetDistinctComputerValuesAsync("ProcessadorFabricante"),
                    RamTipos = await _computadorService.GetDistinctComputerValuesAsync("RamTipo"),
                    Processadores = await _computadorService.GetDistinctComputerValuesAsync("Processador"),
                    Rams = await _computadorService.GetDistinctComputerValuesAsync("Ram")
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de computadores.");
                ViewBag.Message = "Ocorreu um erro ao obter a lista de computadores. Por favor, tente novamente mais tarde.";
                return View(new ComputadorIndexViewModel());
            }
        }

        // GET: Computadores/Create
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Create()
        {
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome");
            return View(new Computador());
        }

        // POST: Computadores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Create(Computador computador)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _computadorService.CreateComputadorAsync(computador);
                    _persistentLogService.AddLog("Computer", "Create", User.Identity.Name, $"Computer '{computador.MAC}' created.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar um novo computador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o computador. Verifique se o MAC já existe.");
                }
            }
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", computador.UserId);
            return View(computador);
        }

        // GET: Computadores/Edit/5
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var computador = await _computadorService.FindComputadorByIdAsync(id);

            if (computador == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", computador.UserId);
            return View(computador);
        }

        // POST: Computadores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Edit(string id, Computador computador)
        {
            if (id != computador.MAC)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _computadorService.UpdateComputadorAsync(computador);
                    _persistentLogService.AddLog("Computer", "Update", User.Identity.Name, $"Computer '{computador.MAC}' updated.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar o computador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o computador.");
                }
            }
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", computador.UserId);
            return View(computador);
        }

        // GET: Computadores/Delete/5
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var computador = await _computadorService.FindComputadorByIdAsync(id);

            if (computador == null)
            {
                return NotFound();
            }

            return View(computador);
        }

        // POST: Computadores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                await _computadorService.DeleteComputadorAsync(id);
                _persistentLogService.AddLog("Computer", "Delete", User.Identity.Name, $"Computer '{id}' deleted.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir o computador.");
                ViewBag.Message = "Ocorreu um erro ao excluir o computador. Por favor, tente novamente mais tarde.";
                var computador = await _computadorService.FindComputadorByIdAsync(id);
                return View(computador);
            }
        }
    }
}