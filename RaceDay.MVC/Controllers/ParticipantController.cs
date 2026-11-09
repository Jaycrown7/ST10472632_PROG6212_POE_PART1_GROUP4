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
    }
}