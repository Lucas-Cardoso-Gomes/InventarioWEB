using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserService _userService;

        public AccountController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var user = await _userService.FindByLoginAsync(model.Login);

                bool isValid = false;
                if (user != null)
                {
                    // Check if password matches stored hash
                    var inputHash = ComputeSha256Hash(model.Password);
                    if (inputHash == user.PasswordHash)
                    {
                        isValid = true;
                    }
                    // Fallback for legacy plain text passwords (migrate on login)
                    else if (model.Password == user.PasswordHash)
                    {
                        isValid = true;
                        // Update to hash
                        user.PasswordHash = inputHash;
                        await _userService.UpdateAsync(user);
                    }
                }

                if (isValid)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Nome),
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim("Login", user.Login) // Custom claim for login
                    };

                    if (!string.IsNullOrEmpty(user.ColaboradorCPF))
                    {
                        claims.Add(new Claim("ColaboradorCPF", user.ColaboradorCPF));
                    }

                    claims.Add(new Claim("IsCoordinator", user.IsCoordinator.ToString()));

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        //AllowRefresh = <bool>,
                        // Refreshing the authentication session should be allowed.
                        IsPersistent = true // Make the cookie persistent
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        if (user.Role == "Colaborador" && returnUrl == "/")
                        {
                            return RedirectToAction("Index", "Chamados");
                        }
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        if (user.Role == "Colaborador")
                        {
                            return RedirectToAction("Index", "Chamados");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Dashboard");
                        }
                    }
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Computadores");
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
