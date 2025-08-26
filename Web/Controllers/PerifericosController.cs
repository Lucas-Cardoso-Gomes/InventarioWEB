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
    public class PerifericosController : Controller
    {
        private readonly PerifericoService _perifericoService;
        private readonly UserService _userService;
        private readonly PersistentLogService _persistentLogService;
        private readonly ILogger<PerifericosController> _logger;

        public PerifericosController(PerifericoService perifericoService, UserService userService, PersistentLogService persistentLogService, ILogger<PerifericosController> logger)
        {
            _perifericoService = perifericoService;
            _userService = userService;
            _persistentLogService = persistentLogService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchString, int pageNumber = 1, int pageSize = 25)
        {
            ViewData["CurrentFilter"] = searchString;
            try
            {
                var (perifericos, totalCount) = await _perifericoService.GetPerifericosAsync(User, searchString, pageNumber, pageSize);
                ViewBag.TotalCount = totalCount;
                ViewBag.PageNumber = pageNumber;
                ViewBag.PageSize = pageSize;
                return View(perifericos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de periféricos.");
                ViewBag.Message = "Ocorreu um erro ao obter a lista de periféricos. Por favor, tente novamente mais tarde.";
                return View(new List<Periferico>());
            }
        }

        // GET: Perifericos/Create
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Create()
        {
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome");
            return View(new Periferico());
        }

        // POST: Perifericos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Create(Periferico periferico)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _perifericoService.CreatePerifericoAsync(periferico);
                    _persistentLogService.AddLog("Periferico", "Create", User.Identity.Name, $"Peripheral '{periferico.Tipo} - {periferico.PartNumber}' created.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar periférico.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o periférico.");
                }
            }
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", periferico.UserId);
            return View(periferico);
        }

        // GET: Perifericos/Edit/5
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Edit(int id)
        {
            var periferico = await _perifericoService.FindPerifericoByIdAsync(id);
            if (periferico == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", periferico.UserId);
            return View(periferico);
        }

        // POST: Perifericos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Edit(int id, Periferico periferico)
        {
            if (id != periferico.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _perifericoService.UpdatePerifericoAsync(periferico);
                    _persistentLogService.AddLog("Periferico", "Update", User.Identity.Name, $"Peripheral '{periferico.Tipo} - {periferico.PartNumber}' updated.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar periférico.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o periférico.");
                }
            }
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", periferico.UserId);
            return View(periferico);
        }

        // GET: Perifericos/Delete/5
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Delete(int id)
        {
            var periferico = await _perifericoService.FindPerifericoByIdAsync(id);
            if (periferico == null)
            {
                return NotFound();
            }
            return View(periferico);
        }

        // POST: Perifericos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var periferico = await _perifericoService.FindPerifericoByIdAsync(id);
                if (periferico != null)
                {
                    await _perifericoService.DeletePerifericoAsync(id);
                    _persistentLogService.AddLog("Periferico", "Delete", User.Identity.Name, $"Peripheral '{periferico.Tipo} - {periferico.PartNumber}' deleted.");
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir periférico.");
                ViewBag.ErrorMessage = "Ocorreu um erro ao excluir o periférico.";
                var periferico = await _perifericoService.FindPerifericoByIdAsync(id);
                return View(periferico);
            }
        }
    }
}
