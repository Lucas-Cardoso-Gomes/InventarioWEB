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

        public UsersController(UserService userService, PersistentLogService persistentLogService)
        {
            _userService = userService;
            _persistentLogService = persistentLogService;
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
            ViewData["SupervisorId"] = new SelectList(await _userService.GetAllCoordenadoresAsync(), "Id", "Nome");
            return View();
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

            if (model.Role == "Normal" && model.SupervisorId == null)
            {
                ModelState.AddModelError("SupervisorId", "O supervisor é obrigatório para usuários normais.");
            }

            if (ModelState.IsValid)
            {
                var existingUser = await _userService.FindByLoginAsync(model.Login);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Login", "Este login já está em uso.");
                    ViewData["SupervisorId"] = new SelectList(await _userService.GetAllCoordenadoresAsync(), "Id", "Nome", model.SupervisorId);
                    return View(model);
                }

                var user = new User
                {
                    Nome = model.Nome,
                    Login = model.Login,
                    PasswordHash = model.Password, // Placeholder for real hash
                    Role = model.Role,
                    CPF = model.CPF,
                    Email = model.Email,
                    SenhaEmail = model.SenhaEmail,
                    Teams = model.Teams,
                    SenhaTeams = model.SenhaTeams,
                    EDespacho = model.EDespacho,
                    SenhaEDespacho = model.SenhaEDespacho,
                    Genius = model.Genius,
                    SenhaGenius = model.SenhaGenius,
                    Ibrooker = model.Ibrooker,
                    SenhaIbrooker = model.SenhaIbrooker,
                    Adicional = model.Adicional,
                    SenhaAdicional = model.SenhaAdicional,
                    Setor = model.Setor,
                    Smartphone = model.Smartphone,
                    TelefoneFixo = model.TelefoneFixo,
                    Ramal = model.Ramal,
                    Alarme = model.Alarme,
                    Videoporteiro = model.Videoporteiro,
                    Obs = model.Obs,
                    DataInclusao = DateTime.Now,
                    SupervisorId = model.SupervisorId
                };

                await _userService.CreateAsync(user);

                _persistentLogService.AddLog("User", "Create", User.Identity.Name, $"User '{user.Login}' created.");
                
                TempData["SuccessMessage"] = "Usuário criado com sucesso!";

                return RedirectToAction(nameof(Index));
            }
            ViewData["SupervisorId"] = new SelectList(await _userService.GetAllCoordenadoresAsync(), "Id", "Nome", model.SupervisorId);
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
                CPF = user.CPF,
                Email = user.Email,
                SenhaEmail = user.SenhaEmail,
                Teams = user.Teams,
                SenhaTeams = user.SenhaTeams,
                EDespacho = user.EDespacho,
                SenhaEDespacho = user.SenhaEDespacho,
                Genius = user.Genius,
                SenhaGenius = user.SenhaGenius,
                Ibrooker = user.Ibrooker,
                SenhaIbrooker = user.SenhaIbrooker,
                Adicional = user.Adicional,
                SenhaAdicional = user.SenhaAdicional,
                Setor = user.Setor,
                Smartphone = user.Smartphone,
                TelefoneFixo = user.TelefoneFixo,
                Ramal = user.Ramal,
                Alarme = user.Alarme,
                Videoporteiro = user.Videoporteiro,
                Obs = user.Obs,
                SupervisorId = user.SupervisorId
            };
            ViewData["SupervisorId"] = new SelectList(await _userService.GetAllCoordenadoresAsync(), "Id", "Nome", user.SupervisorId);
            return View(model);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, UserViewModel model)
        {
            if (model.Role == "Normal" && model.SupervisorId == null)
            {
                ModelState.AddModelError("SupervisorId", "O supervisor é obrigatório para usuários normais.");
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
                user.CPF = model.CPF;
                user.Email = model.Email;
                user.SenhaEmail = model.SenhaEmail;
                user.Teams = model.Teams;
                user.SenhaTeams = model.SenhaTeams;
                user.EDespacho = model.EDespacho;
                user.SenhaEDespacho = model.SenhaEDespacho;
                user.Genius = model.Genius;
                user.SenhaGenius = model.SenhaGenius;
                user.Ibrooker = model.Ibrooker;
                user.SenhaIbrooker = model.SenhaIbrooker;
                user.Adicional = model.Adicional;
                user.SenhaAdicional = model.SenhaAdicional;
                user.Setor = model.Setor;
                user.Smartphone = model.Smartphone;
                user.TelefoneFixo = model.TelefoneFixo;
                user.Ramal = model.Ramal;
                user.Alarme = model.Alarme;
                user.Videoporteiro = model.Videoporteiro;
                user.Obs = model.Obs;
                user.DataAlteracao = DateTime.Now;
                user.SupervisorId = model.SupervisorId;

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
            ViewData["SupervisorId"] = new SelectList(await _userService.GetAllCoordenadoresAsync(), "Id", "Nome", model.SupervisorId);
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
