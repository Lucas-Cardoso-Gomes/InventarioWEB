using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Web.Models;
using Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserService _userService;
        private readonly PersistentLogService _persistentLogService;

        public UsersController(UserService userService, PersistentLogService persistentLogService)
        {
            _userService = userService;
            _persistentLogService = persistentLogService;
        }

        // GET: Users
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var users = await _userService.GetAllUsersAsync(searchString);
            return View(users);
        }

        // GET: Users/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewData["CoordenadorId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome");
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(User user, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("Password", "A senha é obrigatória ao criar um novo usuário.");
            }

            if (ModelState.IsValid)
            {
                var existingUser = await _userService.FindByLoginAsync(user.Login);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Login", "Este login já está em uso.");
                    ViewData["CoordenadorId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", user.CoordenadorId);
                    return View(user);
                }

                await _userService.CreateAsync(user, password);

                _persistentLogService.AddLog("User", "Create", User.Identity.Name, $"User '{user.Login}' created.");
                
                TempData["SuccessMessage"] = "Usuário criado com sucesso!";

                return RedirectToAction(nameof(Index));
            }
            ViewData["CoordenadorId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", user.CoordenadorId);
            return View(user);
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
            ViewData["CoordenadorId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", user.CoordenadorId);
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, User user, string password)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _userService.UpdateAsync(user, password);

                _persistentLogService.AddLog("User", "Update", User.Identity.Name, $"User '{user.Login}' updated.");
                
                TempData["SuccessMessage"] = "Usuário atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CoordenadorId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", user.CoordenadorId);
            return View(user);
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
