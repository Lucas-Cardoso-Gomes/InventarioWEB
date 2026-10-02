using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MigrationController : Controller
    {
        private readonly UserService _userService;
        private readonly ILogger<MigrationController> _logger;

        public MigrationController(UserService userService, ILogger<MigrationController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateColaboradorUsers()
        {
            _logger.LogInformation("Starting CreateColaboradorUsers process.");
            try
            {
                var colaboradores = await _userService.GetAllColaboradoresAsync();
                _logger.LogInformation($"Found {colaboradores} collaborators.");

                var usersCreated = 0;
                var usersAlreadyExist = 0;
                var usersWithNoEmail = 0;

                foreach (var colaborador in colaboradores)
                {
                    if (!string.IsNullOrEmpty(colaborador.Email) && !string.IsNullOrEmpty(colaborador.SenhaEmail))
                    {
                        var existingUser = await _userService.FindByLoginAsync(colaborador.Email);
                        if (existingUser == null)
                        {
                            var newUser = new User
                            {
                                Nome = colaborador.Nome,
                                Login = colaborador.Email,
                                PasswordHash = colaborador.SenhaEmail,
                                Role = "Colaborador",
                                ColaboradorCPF = colaborador.CPF,
                                IsCoordinator = false
                            };
                            await _userService.CreateAsync(newUser);
                            usersCreated++;
                        }
                        else
                        {
                            usersAlreadyExist++;
                        }
                    }
                    else
                    {
                        usersWithNoEmail++;
                    }
                }

                string message = $"{usersCreated} usuários de colaboradores criados. {usersAlreadyExist} já existiam. {usersWithNoEmail} colaboradores ignorados por falta de e-mail/senha.";
                _logger.LogInformation(message);
                TempData["SuccessMessage"] = message; // Changed to SuccessMessage to match layout
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating users from collaborators.");
                TempData["ErrorMessage"] = "Erro ao criar usuários: " + ex.Message;
            }

            // Redirect to UsersController Index action. The controller name is "Users", not "User"
            return RedirectToAction("Index", "Users");
        }
    }
}
