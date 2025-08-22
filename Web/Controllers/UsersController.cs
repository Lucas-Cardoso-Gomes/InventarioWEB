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

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersAsync();
            return View(users);
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
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "A senha é obrigatória ao criar um novo usuário.");
            }

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

                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userService.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new UserViewModel
            {
                Nome = user.Nome,
                Login = user.Login,
                Role = user.Role
                // Password is not loaded for editing
            };

            return View(model);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userService.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                user.Nome = model.Nome;
                user.Login = model.Login;
                user.Role = model.Role;

                // Only update password if a new one is provided
                if (!string.IsNullOrEmpty(model.Password))
                {
                    // In a real app, hash this password
                    user.PasswordHash = model.Password;
                }
                else
                {
                    user.PasswordHash = null; // Tell the service not to update the password
                }

                await _userService.UpdateAsync(user);
                
                TempData["SuccessMessage"] = "Usuário atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userService.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _userService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Usuário excluído com sucesso!";
            return RedirectToAction(nameof(Index));
        }
    }
}
