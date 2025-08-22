using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Web.Models;
using Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userService.FindByLoginAsync(model.Login);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Login", "Este login já está em uso.");
                    return View(model);
                }

                var user = new User
                {
                    Nome = model.Nome,
                    Login = model.Login,
                    // IMPORTANT: This is a simple string assignment.
                    // In a real application, use a secure password hashing library like BCrypt.
                    // Example: PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    PasswordHash = model.Password, // Placeholder for real hash
                    Role = model.Role
                };

                await _userService.CreateAsync(user);
                
                // Optionally, you can add a success message.
                TempData["SuccessMessage"] = "Usuário criado com sucesso!";

                return RedirectToAction("Index", "Computadores");
            }
            return View(model);
        }
    }
}
