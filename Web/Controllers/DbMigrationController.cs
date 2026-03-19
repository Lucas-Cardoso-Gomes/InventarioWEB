using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Services;
using System.Threading.Tasks;
using System;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DbMigrationController : Controller
    {
        private readonly DataMigrationService _migrationService;

        public DbMigrationController(DataMigrationService migrationService)
        {
            _migrationService = migrationService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Migrate(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                ViewBag.Message = "Por favor, forneça uma string de conexão válida.";
                return View("Index");
            }

            try
            {
                await _migrationService.MigrateAsync(connectionString);
                ViewBag.Message = "Migração concluída com sucesso!";
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Erro durante a migração: {ex.Message}";
            }

            return View("Index");
        }
    }
}
