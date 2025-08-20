using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Models;

namespace Web.Controllers
{
    [Authorize]
    public class ComputadoresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ComputadoresController> _logger;

        public ComputadoresController(ApplicationDbContext context, ILogger<ComputadoresController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Computadores
        public async Task<IActionResult> Index(string sortOrder, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["IpSortParm"] = String.IsNullOrEmpty(sortOrder) ? "ip_desc" : "";
            ViewData["HostnameSortParm"] = sortOrder == "hostname" ? "hostname_desc" : "hostname";
            ViewData["DateSortParm"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["CurrentFilter"] = searchString;

            var computadores = from c in _context.Computadores
                           select c;

            if (!String.IsNullOrEmpty(searchString))
            {
                computadores = computadores.Where(s => s.Hostname.Contains(searchString)
                                       || s.IP.Contains(searchString)
                                       || s.Usuario.Contains(searchString)
                                       || s.MAC.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "ip_desc":
                    computadores = computadores.OrderByDescending(c => c.IP);
                    break;
                case "hostname":
                    computadores = computadores.OrderBy(c => c.Hostname);
                    break;
                case "hostname_desc":
                    computadores = computadores.OrderByDescending(c => c.Hostname);
                    break;
                case "date":
                    computadores = computadores.OrderBy(c => c.DataColeta);
                    break;
                case "date_desc":
                    computadores = computadores.OrderByDescending(c => c.DataColeta);
                    break;
                default:
                    computadores = computadores.OrderBy(c => c.IP);
                    break;
            }

            int pageSize = 25;
            return View(await PaginatedList<Computador>.CreateAsync(computadores.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Computadores/Details/AB-CD-EF-12-34-56
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var computador = await _context.Computadores
                .Include(c => c.Discos)
                .Include(c => c.GPUs)
                .Include(c => c.AdaptadoresRede)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MAC == id);

            if (computador == null)
            {
                return NotFound();
            }

            return View(computador);
        }

        // GET: Computadores/Create
        [Authorize(Roles = "Administrador")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Computadores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([Bind("MAC,IP,Usuario,Hostname,Fabricante,SO,ProcessadorNome,ProcessadorFabricante,ProcessadorCores,ProcessadorThreads,ProcessadorClock,RamTotal,RamTipo,RamVelocidade,RamVoltagem,RamPorModulo,ConsumoCPU")] Computador computador)
        {
            if (ModelState.IsValid)
            {
                computador.DataColeta = DateTime.Now;
                _context.Add(computador);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(computador);
        }

        // GET: Computadores/Edit/AB-CD-EF-12-34-56
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var computador = await _context.Computadores.FindAsync(id);
            if (computador == null)
            {
                return NotFound();
            }
            return View(computador);
        }

        // POST: Computadores/Edit/AB-CD-EF-12-34-56
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(string id, [Bind("MAC,IP,Usuario,Hostname,Fabricante,SO,DataColeta,ProcessadorNome,ProcessadorFabricante,ProcessadorCores,ProcessadorThreads,ProcessadorClock,RamTotal,RamTipo,RamVelocidade,RamVoltagem,RamPorModulo,ConsumoCPU")] Computador computador)
        {
            if (id != computador.MAC)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(computador);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ComputadorExists(computador.MAC))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(computador);
        }

        // GET: Computadores/Delete/AB-CD-EF-12-34-56
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var computador = await _context.Computadores
                .FirstOrDefaultAsync(m => m.MAC == id);
            if (computador == null)
            {
                return NotFound();
            }

            return View(computador);
        }

        // POST: Computadores/Delete/AB-CD-EF-12-34-56
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var computador = await _context.Computadores.FindAsync(id);
            if (computador != null)
            {
                _context.Computadores.Remove(computador);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ComputadorExists(string id)
        {
            return _context.Computadores.Any(e => e.MAC == id);
        }
    }
}