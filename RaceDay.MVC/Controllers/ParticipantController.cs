using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;
using System.Net.Http.Json;


namespace RaceDay.MVC.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public ParticipantController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> MyEvents()
        {
            var response = await _apiService.SendAsync(
                HttpMethod.Get,
                "api/enrolments/my");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage =
                    "Unable to load your events. Please log in again.";

                return View(new List<MyEnrolmentViewModel>());
            }

            var enrolments = await response.Content
                .ReadFromJsonAsync<List<MyEnrolmentViewModel>>();

            return View(enrolments ?? new List<MyEnrolmentViewModel>());
        }

        public async Task<IActionResult> MyResults()
        {
            var response = await _apiService.SendAsync(
                HttpMethod.Get,
                "api/results/my");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage =
                    "Unable to load your results. Please try again.";

                return View(new List<ResultViewModel>());
            }

            var results = await response.Content
                .ReadFromJsonAsync<List<ResultViewModel>>();

            return View(results ?? new List<ResultViewModel>());

        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var response = await _apiService.SendAsync(
                HttpMethod.Get,
                "api/users/profile");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction("Login", "Auth");

            var profile = await response.Content
                .ReadFromJsonAsync<ProfileViewModel>();

            return View(profile ?? new ProfileViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            string? imageUrl = model.ProfileImageUrl;

            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var uploadResponse = await _apiService.UploadFileAsync(
                    model.ProfileImage);

                if (!uploadResponse.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        "ProfileImage",
                        "Image upload failed. Please try again.");

                    return View(model);
                }

                var uploadResult = await uploadResponse.Content
                    .ReadFromJsonAsync<UploadImageResponse>();

                imageUrl = uploadResult?.ImageUrl;
            }

            var updateRequest = new
            {
                model.PhoneNumber,
                model.City,
                model.EmergencyContact,
                ProfileImageUrl = imageUrl
            };

            var response = await _apiService.SendAsync(
                HttpMethod.Put,
                "api/users/profile",
                updateRequest);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to update your profile.");

                return View(model);
            }

            TempData["SuccessMessage"] = "Profile updated successfully!";

            return RedirectToAction(nameof(Profile));
        }

        public class UploadImageResponse
        {
            public string? ImageUrl { get; set; }
        }

    }
}