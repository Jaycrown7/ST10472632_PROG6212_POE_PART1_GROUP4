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

        public async Task<IActionResult> Events()
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
                ViewBag.ErrorMessage = "Unable to load your events.";
                return View(new List<EventViewModel>());
            }

            var events = await response.Content
                .ReadFromJsonAsync<List<EventViewModel>>()
                ?? new List<EventViewModel>();

            int.TryParse(
                HttpContext.Session.GetString("UserId"),
                out int organiserId);

            var organiserEvents = events
                .Where(e => e.OrganiserId == organiserId)
                .ToList();

            return View(organiserEvents);
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            return View(new EventFormViewModel
            {
                EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(EventFormViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var request = new
            {
                model.EventName,
                model.Description,
                model.EventDate,
                model.Location,
                model.Distance,
                model.EventType
            };

            var response = await _apiService.SendAsync(
                HttpMethod.Post,
                "api/events",
                request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(
                    string.Empty,
                    $"Event creation failed: {error}");

                return View(model);
            }

            TempData["SuccessMessage"] = "Event created successfully!";

            return RedirectToAction(nameof(Events));
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            var response = await _apiService.SendAsync(
                HttpMethod.Get,
                $"api/events/{id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Unable to find this event.";
                return RedirectToAction(nameof(Events));
            }

            var race = await response.Content.ReadFromJsonAsync<EventViewModel>();

            if (race == null)
            {
                return RedirectToAction(nameof(Events));
            }

            int.TryParse(
                HttpContext.Session.GetString("UserId"),
                out int organiserId);

            if (race.OrganiserId != organiserId)
            {
                return Forbid();
            }

            var model = new EventFormViewModel
            {
                EventId = race.EventId,
                EventName = race.EventName,
                Description = race.Description,
                EventDate = race.EventDate,
                Location = race.Location,
                Distance = race.Distance,
                EventType = race.EventType
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(EventFormViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var request = new
            {
                model.EventName,
                model.Description,
                model.EventDate,
                model.Location,
                model.Distance,
                model.EventType
            };

            var response = await _apiService.SendAsync(
                HttpMethod.Put,
                $"api/events/{model.EventId}",
                request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(
                    string.Empty,
                    $"Event update failed: {error}");

                return View(model);
            }

            TempData["SuccessMessage"] = "Event updated successfully!";

            return RedirectToAction(nameof(Events));
        }

        [HttpGet]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            var response = await _apiService.SendAsync(
                HttpMethod.Get,
                $"api/events/{id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Unable to find this event.";
                return RedirectToAction(nameof(Events));
            }

            var race = await response.Content.ReadFromJsonAsync<EventViewModel>();

            if (race == null)
            {
                return RedirectToAction(nameof(Events));
            }

            int.TryParse(
                HttpContext.Session.GetString("UserId"),
                out int organiserId);

            if (race.OrganiserId != organiserId)
            {
                return Forbid();
            }

            return View(race);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEventConfirmed(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            var response = await _apiService.SendAsync(
                HttpMethod.Delete,
                $"api/events/{id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Unable to delete this event.";
                return RedirectToAction(nameof(Events));
            }

            TempData["SuccessMessage"] = "Event deleted successfully!";

            return RedirectToAction(nameof(Events));
        }
        // Display categories belonging to an event.
        public async Task<IActionResult> Categories(int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Auth");

            var eventResponse = await _apiService.SendAsync(
                HttpMethod.Get, $"api/events/{eventId}");

            if (!eventResponse.IsSuccessStatusCode)
                return RedirectToAction(nameof(Events));

            var race = await eventResponse.Content.ReadFromJsonAsync<EventViewModel>();

            if (race == null)
                return RedirectToAction(nameof(Events));

            int.TryParse(HttpContext.Session.GetString("UserId"), out int organiserId);

            if (race.OrganiserId != organiserId)
                return Forbid();

            var response = await _apiService.SendAsync(
                HttpMethod.Get, $"api/events/{eventId}/categories");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Unable to load categories.";
                return View(new List<CategoryViewModel>());
            }

            ViewBag.EventId = eventId;
            ViewBag.EventName = race.EventName;

            var categories = await response.Content.ReadFromJsonAsync<List<CategoryViewModel>>()
                ?? new List<CategoryViewModel>();

            return View(categories);
        }

        // Display the create category form.
        [HttpGet]
        public async Task<IActionResult> CreateCategory(int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Auth");

            var response = await _apiService.SendAsync(
                HttpMethod.Get, $"api/events/{eventId}");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction(nameof(Events));

            var race = await response.Content.ReadFromJsonAsync<EventViewModel>();

            if (race == null)
                return RedirectToAction(nameof(Events));

            int.TryParse(HttpContext.Session.GetString("UserId"), out int organiserId);

            if (race.OrganiserId != organiserId)
                return Forbid();

            return View(new CategoryFormViewModel { EventId = eventId });
        }

        // Create a category.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CategoryFormViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            var request = new
            {
                model.CategoryName,
                model.Distance,
                model.EntryFee,
                model.MaximumParticipants
            };

            var response = await _apiService.SendAsync(
                HttpMethod.Post,
                $"api/events/{model.EventId}/categories",
                request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(
                    string.Empty, $"Category creation failed: {error}");

                return View(model);
            }

            TempData["SuccessMessage"] = "Category created successfully!";

            return RedirectToAction(nameof(Categories), new { eventId = model.EventId });
        }

        // Display the edit category form.
        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Auth");

            var response = await _apiService.SendAsync(
                HttpMethod.Get, $"api/categories/{id}");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction(nameof(Events));

            var category = await response.Content.ReadFromJsonAsync<CategoryViewModel>();

            if (category == null)
                return RedirectToAction(nameof(Events));

            var eventResponse = await _apiService.SendAsync(
                HttpMethod.Get, $"api/events/{category.EventId}");

            if (!eventResponse.IsSuccessStatusCode)
                return RedirectToAction(nameof(Events));

            var race = await eventResponse.Content.ReadFromJsonAsync<EventViewModel>();

            int.TryParse(HttpContext.Session.GetString("UserId"), out int organiserId);

            if (race == null || race.OrganiserId != organiserId)
                return Forbid();

            var model = new CategoryFormViewModel
            {
                CategoryId = category.CategoryId,
                EventId = category.EventId,
                CategoryName = category.CategoryName,
                Distance = category.Distance,
                EntryFee = category.EntryFee,
                MaximumParticipants = category.MaximumParticipants
            };

            return View(model);
        }

        // Update a category.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(CategoryFormViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            var request = new
            {
                model.CategoryName,
                model.Distance,
                model.EntryFee,
                model.MaximumParticipants
            };

            var response = await _apiService.SendAsync(
                HttpMethod.Put,
                $"api/categories/{model.CategoryId}",
                request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(
                    string.Empty, $"Category update failed: {error}");

                return View(model);
            }

            TempData["SuccessMessage"] = "Category updated successfully!";

            return RedirectToAction(nameof(Categories), new { eventId = model.EventId });
        }

        // Display category deletion confirmation.
        [HttpGet]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Auth");

            var response = await _apiService.SendAsync(
                HttpMethod.Get, $"api/categories/{id}");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction(nameof(Events));

            var category = await response.Content.ReadFromJsonAsync<CategoryViewModel>();

            if (category == null)
                return RedirectToAction(nameof(Events));

            var eventResponse = await _apiService.SendAsync(
                HttpMethod.Get, $"api/events/{category.EventId}");

            if (!eventResponse.IsSuccessStatusCode)
                return RedirectToAction(nameof(Events));

            var race = await eventResponse.Content.ReadFromJsonAsync<EventViewModel>();

            int.TryParse(HttpContext.Session.GetString("UserId"), out int organiserId);

            if (race == null || race.OrganiserId != organiserId)
                return Forbid();

            return View(category);
        }

        // Delete a category.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategoryConfirmed(int id, int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Auth");

            var response = await _apiService.SendAsync(
                HttpMethod.Delete, $"api/categories/{id}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                TempData["ErrorMessage"] =
                    $"Unable to delete category. {error}";

                return RedirectToAction(nameof(Categories), new { eventId });
            }

            TempData["SuccessMessage"] = "Category deleted successfully!";

            return RedirectToAction(nameof(Categories), new { eventId });
        }
        // Display participants enrolled in an event.
        public async Task<IActionResult> Enrolments(int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Auth");

            // Verify event ownership.
            var eventResponse = await _apiService.SendAsync(
                HttpMethod.Get,
                $"api/events/{eventId}");

            if (!eventResponse.IsSuccessStatusCode)
                return RedirectToAction(nameof(Events));

            var race = await eventResponse.Content
                .ReadFromJsonAsync<EventViewModel>();

            if (race == null)
                return RedirectToAction(nameof(Events));

            int.TryParse(
                HttpContext.Session.GetString("UserId"),
                out int organiserId);

            if (race.OrganiserId != organiserId)
                return Forbid();

            // Retrieve event enrolments.
            var response = await _apiService.SendAsync(
                HttpMethod.Get,
                $"api/events/{eventId}/enrolments");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Unable to load event enrolments.";
                return View(new List<OrganiserEnrolmentViewModel>());
            }

            var enrolments = await response.Content
                .ReadFromJsonAsync<List<OrganiserEnrolmentViewModel>>()
                ?? new List<OrganiserEnrolmentViewModel>();

            ViewBag.EventId = eventId;
            ViewBag.EventName = race.EventName;

            return View(enrolments);
        }

    }

}