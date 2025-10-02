using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Web.Models;
using FirebaseAdmin.Auth;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System;

namespace Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AccountController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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
                try
                {
                    var apiKey = _configuration["Firebase:ApiKey"];
                    if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("your-firebase-web-api-key"))
                    {
                        ModelState.AddModelError(string.Empty, "Firebase API Key is not configured in appsettings.json.");
                        return View(model);
                    }

                    var client = _httpClientFactory.CreateClient();
                    var requestUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";
                    var requestPayload = new
                    {
                        email = model.Login, // Assuming login is the email
                        password = model.Password,
                        returnSecureToken = true
                    };

                    var jsonPayload = JsonConvert.SerializeObject(requestPayload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(requestUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        var firebaseAuthResponse = JsonConvert.DeserializeObject<FirebaseAuthResponse>(responseString);

                        // Verify the ID token using Firebase Admin SDK
                        var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(firebaseAuthResponse.IdToken);
                        var uid = decodedToken.Uid;

                        // Get user record to access custom claims (like roles)
                        var userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);

                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, userRecord.Uid),
                            new Claim(ClaimTypes.Email, userRecord.Email),
                            new Claim(ClaimTypes.Name, userRecord.DisplayName ?? userRecord.Email)
                            // Add role claims from Firebase custom claims
                        };

                        if (userRecord.CustomClaims.TryGetValue("role", out var role))
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
                        }

                        // Add other custom claims as needed
                        if (userRecord.CustomClaims.TryGetValue("ColaboradorCPF", out var colaboradorCpf))
                        {
                            claims.Add(new Claim("ColaboradorCPF", colaboradorCpf.ToString()));
                        }

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties { IsPersistent = true };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        return RedirectToLocal(returnUrl);
                    }
                    else
                    {
                        var errorString = await response.Content.ReadAsStringAsync();
                        // You can parse the errorString to provide a more specific error message
                        ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your credentials.");
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception
                    ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Dashboard");
            }
        }
    }

    // Helper class to deserialize Firebase REST API response
    public class FirebaseAuthResponse
    {
        [JsonProperty("idToken")]
        public string IdToken { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; }

        [JsonProperty("expiresIn")]
        public string ExpiresIn { get; set; }

        [JsonProperty("localId")]
        public string LocalId { get; set; }
    }
}