using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Web.Models;
using Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserService _userService;
        private readonly PersistentLogService _persistentLogService;
        private readonly ColaboradoresController _colaboradoresController;

        public UsersController(UserService userService, PersistentLogService persistentLogService, IConfiguration configuration, ILogger<ColaboradoresController> logger)
        {
            _userService = userService;
            _persistentLogService = persistentLogService;
            _colaboradoresController = new ColaboradoresController(configuration, logger, persistentLogService);
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        // GET: Users/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewData["CoordenadorId"] = new SelectList(await _userService.GetAllCoordenadoresAsync(), "Id", "Nome");
            ViewData["ColaboradorCPF"] = new SelectList(_colaboradoresController.GetColaboradores(), "CPF", "Nome");
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            ModelState.AddModelError("", "Este é um erro de teste.");

            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "A senha é obrigatória ao criar um novo usuário.");
            }

            if (model.Login == "admin")
            {
                ModelState.Remove("ColaboradorCPF");
            }

            if (model.Role == "Normal" && model.CoordenadorId == null)
            {
                ModelState.AddModelError("CoordenadorId", "O coordenador é obrigatório para usuários normais.");
            }

            if (ModelState.IsValid)
            {
                var existingUser = await _userService.FindByLoginAsync(model.Login);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Login", "Este login já está em uso.");
                    ViewData["CoordenadorId"] = new SelectList(await _userService.GetAllCoordenadoresAsync(), "Id", "Nome", model.CoordenadorId);
                    ViewData["ColaboradorCPF"] = new SelectList(_colaboradoresController.GetColaboradores(), "CPF", "Nome", model.ColaboradorCPF);
                    return View(model);
                }

                var user = new User
                {
                    Nome = model.Nome,
                    Login = model.Login,
                    PasswordHash = model.Password, // Placeholder for real hash
                    Role = model.Role,
                    ColaboradorCPF = model.ColaboradorCPF,
                    CoordenadorId = model.CoordenadorId
                };

                await _userService.CreateAsync(user);

                _persistentLogService.AddLog("User", "Create", User.Identity.Name, $"User '{user.Login}' created.");
                
                TempData["SuccessMessage"] = "Usuário criado com sucesso!";

                return RedirectToAction(nameof(Index));
            }
            ViewData["CoordenadorId"] = new SelectList(await _userService.GetAllCoordenadoresAsync(), "Id", "Nome", model.CoordenadorId);
            ViewData["ColaboradorCPF"] = new SelectList(_colaboradoresController.GetColaboradores(), "CPF", "Nome", model.ColaboradorCPF);
            return View(model);
        }

        // GET: Users/Edit/5
        [Authorize(Roles = "Admin")]
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
                Role = user.Role,
                ColaboradorCPF = user.ColaboradorCPF,
                CoordenadorId = user.CoordenadorId
            };
            ViewData["CoordenadorId"] = new SelectList(await _userService.GetAllCoordenadoresAsync(), "Id", "Nome", user.CoordenadorId);
            ViewData["ColaboradorCPF"] = new SelectList(_colaboradoresController.GetColaboradores(), "CPF", "Nome", user.ColaboradorCPF);
            return View(model);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, UserViewModel model)
        {
            if (model.Login == "admin")
            {
                ModelState.Remove("ColaboradorCPF");
            }

            if (model.Role == "Normal" && model.CoordenadorId == null)
            {
                ModelState.AddModelError("CoordenadorId", "O coordenador é obrigatório para usuários normais.");
            }

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
                user.ColaboradorCPF = model.ColaboradorCPF;
                user.CoordenadorId = model.CoordenadorId;

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

                _persistentLogService.AddLog("User", "Update", User.Identity.Name, $"User '{user.Login}' updated.");
                
                TempData["SuccessMessage"] = "Usuário atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CoordenadorId"] = new SelectList(await _userService.GetAllCoordenadoresAsync(), "Id", "Nome", model.CoordenadorId);
            ViewData["ColaboradorCPF"] = new SelectList(_colaboradoresController.GetColaboradores(), "CPF", "Nome", model.ColaboradorCPF);
            return View(model);
        }

        // GET: Users/Delete/5
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userService.FindByIdAsync(id);
            if (user != null)
            {
                await _userService.DeleteAsync(id);
                _persistentLogService.AddLog("User", "Delete", User.Identity.Name, $"User '{user.Login}' deleted.");
                TempData["SuccessMessage"] = "Usuário excluído com sucesso!";
            }
            else
            {
                TempData["ErrorMessage"] = "Usuário não encontrado.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
