using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Web.Models;
using Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;

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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Importar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Nenhum arquivo selecionado.";
                return RedirectToAction(nameof(Index));
            }

            var smartphones = new List<Smartphone>();
            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            TempData["ErrorMessage"] = "A planilha do Excel está vazia ou não foi encontrada.";
                            return RedirectToAction(nameof(Index));
                        }

                        int rowCount = worksheet.Dimension.Rows;
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var smartphone = new Smartphone
                            {
                                Modelo = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                                IMEI1 = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                                IMEI2 = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                                Usuario = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                                Filial = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                                ContaGoogle = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                                SenhaGoogle = worksheet.Cells[row, 7].Value?.ToString().Trim(),
                                MAC = worksheet.Cells[row, 8].Value?.ToString().Trim(),
                                DataCriacao = DateTime.Now
                            };

                            if (!string.IsNullOrWhiteSpace(smartphone.Modelo) && !string.IsNullOrWhiteSpace(smartphone.IMEI1))
                            {
                                smartphones.Add(smartphone);
                            }
                        }
                    }
                }

                int adicionados = 0;
                int atualizados = 0;

                var existingSmartphones = await _smartphoneService.GetAllAsync();

                foreach (var smartphone in smartphones)
                {
                    var existente = existingSmartphones.FirstOrDefault(s => s.IMEI1 == smartphone.IMEI1);

                    if (existente != null)
                    {
                        existente.Modelo = smartphone.Modelo;
                        existente.IMEI2 = smartphone.IMEI2;
                        existente.Usuario = smartphone.Usuario;
                        existente.Filial = smartphone.Filial;
                        existente.ContaGoogle = smartphone.ContaGoogle;
                        existente.SenhaGoogle = smartphone.SenhaGoogle;
                        existente.MAC = smartphone.MAC;
                        existente.DataAlteracao = DateTime.Now;

                        await _smartphoneService.UpdateAsync(existente);
                        atualizados++;
                    }
                    else
                    {
                        await _smartphoneService.CreateAsync(smartphone);
                        adicionados++;
                    }
                }

                TempData["SuccessMessage"] = $"{adicionados} smartphones adicionados e {atualizados} atualizados com sucesso.";
            }
            catch (Exception ex)
            {
                // In a real app we might inject a logger to log this.
                TempData["ErrorMessage"] = "Ocorreu um erro durante a importação do arquivo. Verifique se o formato está correto.";
            }

            return RedirectToAction(nameof(Index));
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