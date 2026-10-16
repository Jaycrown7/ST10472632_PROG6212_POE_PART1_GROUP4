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

        // Anyone can view categories for an event.
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories(int eventId)
        {
            var eventExists = await _context.Events
                .AnyAsync(e => e.EventId == eventId);

            if (!eventExists)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.EventId == eventId)
                .OrderBy(c => c.Distance)
                .ToListAsync();

            return Ok(categories);
        }

        // Anyone can view one category for an event.
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryById(
            int eventId,
            int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.EventId == eventId &&
                    c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Category not found."
                });
            }

            return Ok(category);
        }

        // Only the organiser who owns the event can create categories.
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
                return NotFound(new
                {
                    message = "Event not found."
                });
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
    new
    {
        eventId = eventId,
        id = category.CategoryId
    },
    new
    {
        category.CategoryId,
        category.EventId,
        category.CategoryName,
        category.Distance,
        category.EntryFee,
        category.MaximumParticipants,
        category.CreatedAt
    });
        }

        private int? GetOrganiserId()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (int.TryParse(userId, out int organiserId))
            {
                return organiserId;
            }

            return null;
        }
    }


}
