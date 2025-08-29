using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Web.Models;
using Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserService _userService;
        private readonly PersistentLogService _persistentLogService;
        private readonly string _connectionString;

        public UsersController(UserService userService, PersistentLogService persistentLogService, IConfiguration configuration)
        {
            _userService = userService;
            _persistentLogService = persistentLogService;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private async Task<List<Colaborador>> GetAllColaboradoresAsync()
        {
            var colaboradores = new List<Colaborador>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT CPF, Nome FROM Colaboradores ORDER BY Nome", connection);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        colaboradores.Add(new Colaborador { CPF = reader.GetString(0), Nome = reader.GetString(1) });
                    }
                }
            }
            return colaboradores;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersWithColaboradoresAsync();
            return View(users);
        }

        // GET: Users/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var model = new UserViewModel
            {
                Colaboradores = new SelectList(await GetAllColaboradoresAsync(), "CPF", "Nome")
            };
            return View(model);
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
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
                    model.Colaboradores = new SelectList(await GetAllColaboradoresAsync(), "CPF", "Nome", model.ColaboradorCPF);
                    return View(model);
                }

                var user = new User
                {
                    Nome = model.Nome,
                    Login = model.Login,
                    PasswordHash = model.Password, // Placeholder for real hash
                    Role = model.Role,
                    Diretoria = model.Diretoria,
                    ColaboradorCPF = model.ColaboradorCPF
                };

                await _userService.CreateAsync(user);

                _persistentLogService.AddLog("User", "Create", User.Identity.Name, $"User '{user.Login}' created.");
                
                TempData["SuccessMessage"] = "Usuário criado com sucesso!";

                return RedirectToAction(nameof(Index));
            }
            model.Colaboradores = new SelectList(await GetAllColaboradoresAsync(), "CPF", "Nome", model.ColaboradorCPF);
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
                Id = user.Id,
                Nome = user.Nome,
                Login = user.Login,
                Role = user.Role,
                Diretoria = user.Diretoria,
                ColaboradorCPF = user.ColaboradorCPF,
                Colaboradores = new SelectList(await GetAllColaboradoresAsync(), "CPF", "Nome", user.ColaboradorCPF)
            };

            return View(model);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
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
                user.Diretoria = model.Diretoria;
                user.ColaboradorCPF = model.ColaboradorCPF;

                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.PasswordHash = model.Password;
                }
                else
                {
                    user.PasswordHash = null;
                }

                await _userService.UpdateAsync(user);

                _persistentLogService.AddLog("User", "Update", User.Identity.Name, $"User '{user.Login}' updated.");
                
                TempData["SuccessMessage"] = "Usuário atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            model.Colaboradores = new SelectList(await GetAllColaboradoresAsync(), "CPF", "Nome", model.ColaboradorCPF);
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
