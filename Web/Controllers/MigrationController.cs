using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MigrationController : Controller
    {
        private readonly UserService _userService;

        public MigrationController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateColaboradorUsers()
        {
            var colaboradores = await _userService.GetAllColaboradoresAsync();
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

            TempData["MigrationResult"] = $"{usersCreated} usuários de colaboradores criados. {usersAlreadyExist} já existiam. {usersWithNoEmail} colaboradores ignorados por falta de e-mail/senha.";
            return RedirectToAction("Index", "Users");
        }
    }
}