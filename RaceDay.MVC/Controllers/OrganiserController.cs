using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public OrganiserController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Dashboard()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            var response = await _apiService.SendAsync(
                HttpMethod.Get,
                "api/events");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Unable to load dashboard information.";
                return View(new OrganiserDashboardViewModel());
            }

            var events = await response.Content
                .ReadFromJsonAsync<List<EventViewModel>>()
                ?? new List<EventViewModel>();

            int.TryParse(
                HttpContext.Session.GetString("UserId"),
                out int organiserId);

            events = events
                .Where(e => e.OrganiserId == organiserId)
                .ToList();

            int totalEnrolments = 0;

            foreach (var race in events)
            {
                var enrolmentResponse = await _apiService.SendAsync(
                    HttpMethod.Get,
                    $"api/events/{race.EventId}/enrolments");

                if (enrolmentResponse.IsSuccessStatusCode)
                {
                    using var document = JsonDocument.Parse(
                        await enrolmentResponse.Content.ReadAsStringAsync());

                    if (document.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        totalEnrolments +=
                            document.RootElement.GetArrayLength();
                    }
                }
            }

            var dashboard = new OrganiserDashboardViewModel
            {
                TotalEvents = events.Count,
                UpcomingEvents = events.Count(e =>
                    e.EventDate >= DateOnly.FromDateTime(DateTime.Today)),
                TotalEnrolments = totalEnrolments,
                Events = events
            };

            return View(dashboard);
        }
    }
}