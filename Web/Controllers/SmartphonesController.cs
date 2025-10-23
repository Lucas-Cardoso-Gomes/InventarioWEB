using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Web.Models;
using Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria")]
    public class SmartphonesController : Controller
    {
        private readonly SmartphoneService _smartphoneService;

        public SmartphonesController(SmartphoneService smartphoneService)
        {
            _smartphoneService = smartphoneService;
        }

        // GET: Smartphones
        public async Task<IActionResult> Index()
        {
            var smartphones = await _smartphoneService.GetAllAsync();
            return View(smartphones);
        }

        // GET: Smartphones/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var smartphone = await _smartphoneService.GetByIdAsync(id.Value);
            if (smartphone == null)
            {
                return NotFound();
            }

            return View(smartphone);
        }

        // GET: Smartphones/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Smartphones/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Modelo,IMEI1,IMEI2,Usuario,Filial,ContaGoogle,SenhaGoogle,MAC")] Smartphone smartphone)
        {
            if (ModelState.IsValid)
            {
                await _smartphoneService.CreateAsync(smartphone);
                return RedirectToAction(nameof(Index));
            }
            return View(smartphone);
        }

        // GET: Smartphones/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var smartphone = await _smartphoneService.GetByIdAsync(id.Value);
            if (smartphone == null)
            {
                return NotFound();
            }
            return View(smartphone);
        }

        // POST: Smartphones/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Modelo,IMEI1,IMEI2,Usuario,Filial,DataCriacao,ContaGoogle,SenhaGoogle,MAC")] Smartphone smartphone)
        {
            if (id != smartphone.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _smartphoneService.UpdateAsync(smartphone);
                }
                catch
                {
                    // Log the error or handle it appropriately
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(smartphone);
        }

        // GET: Smartphones/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var smartphone = await _smartphoneService.GetByIdAsync(id.Value);
            if (smartphone == null)
            {
                return NotFound();
            }

            return View(smartphone);
        }

        // POST: Smartphones/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _smartphoneService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}