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
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;


    public EventsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // Anyone can view all events.
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events
                .AsNoTracking()
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            return Ok(events);
        }

        // Anyone can view a specific event.
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEventById(int id)
        {
            var eventItem = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            return Ok(eventItem);
        }

        // Only Organisers can create events.
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateEvent(
            [FromBody] CreateEventRequest request)
        {
            if (!TryGetOrganiserId(out int organiserId))
            {
                return Unauthorized(new
                {
                    message = "Unable to identify the logged-in organiser."
                });
            }

            var eventItem = new Event
            {
                OrganiserId = organiserId,
                EventName = request.EventName.Trim(),
                Description = request.Description.Trim(),
                EventDate = request.EventDate,
                Location = request.Location.Trim(),
                Distance = request.Distance,
                EventType = request.EventType,
                CreatedAt = DateTime.UtcNow
            };

            _context.Events.Add(eventItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEventById),
                new { id = eventItem.EventId },
                eventItem);
        }

        // Only the Organiser who owns the event can update it.
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateEvent(
            int id,
            [FromBody] UpdateEventRequest request)
        {
            if (!TryGetOrganiserId(out int organiserId))
            {
                return Unauthorized(new
                {
                    message = "Unable to identify the logged-in organiser."
                });
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            if (eventItem.OrganiserId != organiserId)
            {
                return Forbid();
            }

            eventItem.EventName = request.EventName.Trim();
            eventItem.Description = request.Description.Trim();
            eventItem.EventDate = request.EventDate;
            eventItem.Location = request.Location.Trim();
            eventItem.Distance = request.Distance;
            eventItem.EventType = request.EventType;

            await _context.SaveChangesAsync();

            return Ok(eventItem);
        }

        // Only the Organiser who owns the event can delete it.
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (!TryGetOrganiserId(out int organiserId))
            {
                return Unauthorized(new
                {
                    message = "Unable to identify the logged-in organiser."
                });
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            if (eventItem.OrganiserId != organiserId)
            {
                return Forbid();
            }

            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Test endpoint: only Organisers can access it.
        [HttpGet("organiser-test")]
        [Authorize(Roles = "Organiser")]
        public IActionResult OrganiserTest()
        {
            return Ok(new
            {
                message = "Access granted to the Organiser.",
                userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier),
                role = User.FindFirstValue(ClaimTypes.Role)
            });
        }

        // Test endpoint: only Participants can access it.
        [HttpGet("participant-test")]
        [Authorize(Roles = "Participant")]
        public IActionResult ParticipantTest()
        {
            return Ok(new
            {
                message = "Access granted to the Participant.",
                userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier),
                role = User.FindFirstValue(ClaimTypes.Role)
            });
        }

        private bool TryGetOrganiserId(out int organiserId)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out organiserId);
        }
    }


}
