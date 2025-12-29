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
        private readonly PersistentLogService _persistentLogService;

        public SmartphonesController(SmartphoneService smartphoneService, PersistentLogService persistentLogService)
        {
            _smartphoneService = smartphoneService;
            _persistentLogService = persistentLogService;
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

                await _persistentLogService.LogChangeAsync(
                    User.Identity.Name,
                    "CREATE",
                    "Smartphone",
                    $"Created smartphone: {smartphone.Modelo}",
                    $"IMEI: {smartphone.IMEI1}, User: {smartphone.Usuario}"
                );

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

                    await _persistentLogService.LogChangeAsync(
                        User.Identity.Name,
                        "EDIT",
                        "Smartphone",
                        $"Updated smartphone: {smartphone.Modelo}",
                        $"ID: {smartphone.Id}, IMEI: {smartphone.IMEI1}"
                    );
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
            var smartphone = await _smartphoneService.GetByIdAsync(id);
            await _smartphoneService.DeleteAsync(id);

            if (smartphone != null)
            {
                await _persistentLogService.LogChangeAsync(
                    User.Identity.Name,
                    "DELETE",
                    "Smartphone",
                    $"Deleted smartphone: {smartphone.Modelo}",
                    $"ID: {id}, IMEI: {smartphone.IMEI1}"
                );
            }

            return RedirectToAction(nameof(Index));
        }
    }
}