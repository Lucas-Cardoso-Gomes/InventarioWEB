using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Web.Models;
using Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserService _userService;
        private readonly IDatabaseService _databaseService;

        public UsersController(UserService userService, PersistentLogService persistentLogService, IDatabaseService databaseService)
        {
            _userService = userService;
            _databaseService = databaseService;
        }

        private async Task<List<Colaborador>> GetAllColaboradoresAsync()
        {
            var colaboradores = new List<Colaborador>();
            using (var connection = _databaseService.CreateConnection())
            {
                // SQLite doesn't support async open properly with IDbConnection interface in older context, but SqliteConnection does.
                // However, standard is synchronous for SQLite usually or Task.CompletedTask wrapper.
                // Since CreateConnection returns IDbConnection, we treat it synchronously or cast.
                // But for consistency let's just Open.
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT CPF, Nome FROM Colaboradores ORDER BY Nome";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            colaboradores.Add(new Colaborador { CPF = reader["CPF"].ToString(), Nome = reader["Nome"].ToString() });
                        }
                    }
                }
            }
            return await Task.FromResult(colaboradores);
        }

        // GET: Users
        public async Task<IActionResult> Index(string sortOrder, string searchString, List<string> currentRoles, int pageNumber = 1, int pageSize = 25)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NomeSortParm"] = string.IsNullOrEmpty(sortOrder) ? "nome_desc" : "";
            ViewData["LoginSortParm"] = sortOrder == "login" ? "login_desc" : "login";
            ViewData["RoleSortParm"] = sortOrder == "role" ? "role_desc" : "role";

            var viewModel = await _userService.GetAllUsersWithColaboradoresAsync(sortOrder, searchString, currentRoles, pageNumber, pageSize);
            return View(viewModel);
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
                    ColaboradorCPF = model.ColaboradorCPF,
                    IsCoordinator = model.IsCoordinator
                };

                await _userService.CreateAsync(user);

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
                ColaboradorCPF = user.ColaboradorCPF,
                IsCoordinator = user.IsCoordinator,
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
                user.ColaboradorCPF = model.ColaboradorCPF;
                user.IsCoordinator = model.IsCoordinator;

                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.PasswordHash = model.Password;
                }
                else
                {
                    user.PasswordHash = null;
                }

                await _userService.UpdateAsync(user);

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
