using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Web.Models;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly FirestoreDb _firestoreDb;

        public UsersController(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        private async Task<List<Colaborador>> GetAllColaboradoresAsync()
        {
            var colaboradores = new List<Colaborador>();
            var snapshot = await _firestoreDb.Collection("colaboradores").OrderBy("Nome").GetSnapshotAsync();
            foreach (var document in snapshot.Documents)
            {
                var colab = document.ConvertTo<Colaborador>();
                colab.CPF = document.Id; // The document ID is the CPF
                colaboradores.Add(colab);
            }
            return colaboradores;
        }

        public async Task<IActionResult> Index()
        {
            var userRecords = new List<UserViewModel>();
            var pagedEnumerable = FirebaseAuth.DefaultInstance.ListUsersAsync(null);
            var enumerator = pagedEnumerable.GetEnumerator();
            while (await enumerator.MoveNext())
            {
                var user = enumerator.Current;
                var role = user.CustomClaims.ContainsKey("role") ? user.CustomClaims["role"].ToString() : "N/A";
                userRecords.Add(new UserViewModel
                {
                    Uid = user.Uid,
                    Nome = user.DisplayName,
                    Login = user.Email,
                    Role = role
                });
            }
            return View(userRecords);
        }

        public async Task<IActionResult> Create()
        {
            var model = new UserViewModel
            {
                Colaboradores = new SelectList(await GetAllColaboradoresAsync(), "CPF", "Nome")
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "A senha é obrigatória.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userArgs = new UserRecordArgs
                    {
                        Email = model.Login,
                        Password = model.Password,
                        DisplayName = model.Nome,
                        Disabled = false
                    };
                    var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(userArgs);

                    var claims = new Dictionary<string, object>
                    {
                        { "role", model.Role },
                        { "ColaboradorCPF", model.ColaboradorCPF ?? string.Empty }
                    };
                    await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(userRecord.Uid, claims);

                    TempData["SuccessMessage"] = "Usuário criado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (FirebaseAuthException ex)
                {
                    ModelState.AddModelError(string.Empty, $"Erro ao criar usuário: {ex.Message}");
                }
            }
            model.Colaboradores = new SelectList(await GetAllColaboradoresAsync(), "CPF", "Nome", model.ColaboradorCPF);
            return View(model);
        }

        public async Task<IActionResult> Edit(string id) // id is UID
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(id);
            if (userRecord == null) return NotFound();

            var model = new UserViewModel
            {
                Uid = userRecord.Uid,
                Nome = userRecord.DisplayName,
                Login = userRecord.Email,
                Role = userRecord.CustomClaims.ContainsKey("role") ? userRecord.CustomClaims["role"].ToString() : null,
                ColaboradorCPF = userRecord.CustomClaims.ContainsKey("ColaboradorCPF") ? userRecord.CustomClaims["ColaboradorCPF"].ToString() : null,
                Colaboradores = new SelectList(await GetAllColaboradoresAsync(), "CPF", "Nome", userRecord.CustomClaims.ContainsKey("ColaboradorCPF") ? userRecord.CustomClaims["ColaboradorCPF"].ToString() : null)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserViewModel model)
        {
            if (id != model.Uid) return BadRequest();

            if (ModelState.IsValid)
            {
                try
                {
                    var args = new UserRecordArgs
                    {
                        Uid = id,
                        Email = model.Login,
                        DisplayName = model.Nome,
                    };

                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        args.Password = model.Password;
                    }

                    await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);

                    var claims = new Dictionary<string, object>
                    {
                        { "role", model.Role },
                        { "ColaboradorCPF", model.ColaboradorCPF ?? string.Empty }
                    };
                    await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(id, claims);

                    TempData["SuccessMessage"] = "Usuário atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (FirebaseAuthException ex)
                {
                    ModelState.AddModelError(string.Empty, $"Erro ao atualizar usuário: {ex.Message}");
                }
            }
            model.Colaboradores = new SelectList(await GetAllColaboradoresAsync(), "CPF", "Nome", model.ColaboradorCPF);
            return View(model);
        }

        public async Task<IActionResult> Delete(string id) // id is UID
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(id);
            if (userRecord == null) return NotFound();

            var model = new UserViewModel
            {
                Uid = userRecord.Uid,
                Nome = userRecord.DisplayName,
                Login = userRecord.Email
            };

            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(id);
                TempData["SuccessMessage"] = "Usuário excluído com sucesso!";
            }
            catch (FirebaseAuthException ex)
            {
                TempData["ErrorMessage"] = $"Erro ao excluir usuário: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}