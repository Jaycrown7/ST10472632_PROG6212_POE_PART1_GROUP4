using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;
using System.Text.Json;


namespace RaceDay.MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public AuthController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var response = await _apiService.LoginAsync(new
            {
                model.Email,
                model.Password
            });

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");

                return View(model);
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            HttpContext.Session.SetString(
                "UserId",
                result.GetProperty("userId").ToString());

            HttpContext.Session.SetString(
                "FirstName",
                result.GetProperty("firstName").GetString() ?? "");

            HttpContext.Session.SetString(
                "LastName",
                result.GetProperty("lastName").GetString() ?? "");

            HttpContext.Session.SetString(
                "Email",
                result.GetProperty("email").GetString() ?? "");

            var role = result.GetProperty("role").GetString() ?? "";

            HttpContext.Session.SetString("Role", role);

            if (role == "Organiser")
                return RedirectToAction("Dashboard", "Organiser");

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await _apiService.LogoutAsync();

            return RedirectToAction("Index", "Home");

        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var request = new
            {
                model.FirstName,
                model.LastName,
                model.Email,
                model.Password,
                model.PhoneNumber,
                model.City
            };

            var response = await _apiService.RegisterAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(
                    "",
                    "Registration failed. " + error);

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Your account has been created successfully. Please log in.";

            return RedirectToAction("Login");
        }
    }
}