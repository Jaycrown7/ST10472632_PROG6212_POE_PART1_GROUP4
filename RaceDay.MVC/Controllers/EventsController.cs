using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;
using System.Net.Http.Json;

namespace RaceDay.MVC.Controllers
{
    public class EventsController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public EventsController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var response = await _apiService.SendAsync(
                HttpMethod.Get,
                "api/events");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage =
                    "Unable to load events. Please try again later.";

                return View(new List<EventViewModel>());
            }

            var events = await response.Content
                .ReadFromJsonAsync<List<EventViewModel>>();

            return View(events ?? new List<EventViewModel>());
        }

        public async Task<IActionResult> Details(int id)
        {
            var eventResponse = await _apiService.SendAsync(
                HttpMethod.Get,
                $"api/events/{id}");

            if (!eventResponse.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var eventDetails = await eventResponse.Content
                .ReadFromJsonAsync<EventViewModel>();

            if (eventDetails == null)
            {
                return NotFound();
            }

            var categoryResponse = await _apiService.SendAsync(
                HttpMethod.Get,
                $"api/events/{id}/categories");

            var categories = new List<CategoryViewModel>();

            if (categoryResponse.IsSuccessStatusCode)
            {
                categories = await categoryResponse.Content
                    .ReadFromJsonAsync<List<CategoryViewModel>>()
                    ?? new List<CategoryViewModel>();
            }

            var model = new EventDetailsViewModel
            {
                Event = eventDetails,
                Categories = categories
            };

            return View(model);


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(EnrolmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Details", new { id = model.EventId });
            }

            var response = await _apiService.SendAsync(
                HttpMethod.Post,
                "api/enrolments",
                new
                {
                    model.EventId,
                    model.CategoryId
                });

            if (!response.IsSuccessStatusCode)
            {
                var errorDetails = await response.Content.ReadAsStringAsync();

                TempData["ErrorMessage"] =
                    $"Enrolment failed. API Status: {(int)response.StatusCode}. Details: {errorDetails}";

                return RedirectToAction("Details", new { id = model.EventId });
            }

            TempData["SuccessMessage"] =
                "You have successfully enrolled in this event!";

            return RedirectToAction("MyEvents", "Participant");
        }
    }
}