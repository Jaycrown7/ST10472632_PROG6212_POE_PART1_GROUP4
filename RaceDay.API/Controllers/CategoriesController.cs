using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/events/{eventId:int}/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;


    public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        // Retrieve categories for an event.
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetCategories(int eventId)
        {
            var eventExists = await _context.Events
                .AnyAsync(e => e.EventId == eventId);

            if (!eventExists)
            {
                return NotFound(new { message = "Event not found." });
            }

            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.EventId == eventId)
                .OrderBy(c => c.Distance)
                .ToListAsync();

            return Ok(categories);
        }

        // Retrieve one category using the approved route.
        [HttpGet("/api/categories/{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            return Ok(category);
        }

        // Create a category for an event owned by the organiser.
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateCategory(
            int eventId,
            [FromBody] CreateCategoryRequest request)
        {
            var organiserId = GetOrganiserId();

            if (organiserId == null)
            {
                return Unauthorized(new
                {
                    message = "Unable to identify the logged-in organiser."
                });
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (eventItem == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (eventItem.OrganiserId != organiserId.Value)
            {
                return Forbid();
            }

            var category = new Category
            {
                EventId = eventId,
                CategoryName = request.CategoryName.Trim(),
                Distance = request.Distance,
                EntryFee = request.EntryFee,
                MaximumParticipants = request.MaximumParticipants,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCategoryById),
                new { id = category.CategoryId },
                category);
        }

        // Update a category belonging to an event owned by the organiser.
        [HttpPut("/api/categories/{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateCategory(
            int id,
            [FromBody] CreateCategoryRequest request)
        {
            var organiserId = GetOrganiserId();

            if (organiserId == null)
            {
                return Unauthorized(new
                {
                    message = "Unable to identify the logged-in organiser."
                });
            }

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            if (category.Event.OrganiserId != organiserId.Value)
            {
                return Forbid();
            }

            category.CategoryName = request.CategoryName.Trim();
            category.Distance = request.Distance;
            category.EntryFee = request.EntryFee;
            category.MaximumParticipants = request.MaximumParticipants;

            await _context.SaveChangesAsync();

            return Ok(category);
        }

        // Delete a category belonging to an event owned by the organiser.
        [HttpDelete("/api/categories/{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var organiserId = GetOrganiserId();

            if (organiserId == null)
            {
                return Unauthorized(new
                {
                    message = "Unable to identify the logged-in organiser."
                });
            }

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            if (category.Event.OrganiserId != organiserId.Value)
            {
                return Forbid();
            }

            if (await _context.Enrolments.AnyAsync(
                e => e.CategoryId == id))
            {
                return Conflict(new
                {
                    message = "Cannot delete a category that has enrolments."
                });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private int? GetOrganiserId()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(userId, out int organiserId)
                ? organiserId
                : null;
        }
    }


}
