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
    [Route("api/enrolments")]
    [Authorize]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;


    public EnrolmentsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // POST /api/enrolments
        // Participant enrols in a category belonging to the specified event.
        [HttpPost]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> CreateEnrolment(
            [FromBody] CreateEnrolmentRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized(new { message = "Unable to identify the logged-in user." });

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.UserId == userId.Value);

            if (participant == null)
                return BadRequest(new { message = "Participant profile not found." });

            var eventExists = await _context.Events
                .AnyAsync(e => e.EventId == request.EventId);

            if (!eventExists)
                return NotFound(new { message = "Event not found." });

            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.CategoryId == request.CategoryId &&
                    c.EventId == request.EventId);

            if (category == null)
                return NotFound(new
                {
                    message = "The category was not found for the specified event."
                });

            var alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e =>
                    e.ParticipantId == participant.UserId &&
                    e.CategoryId == category.CategoryId);

            if (alreadyEnrolled)
                return Conflict(new { message = "You are already enrolled in this category." });

            var activeEnrolments = await _context.Enrolments
                .CountAsync(e =>
                    e.CategoryId == category.CategoryId &&
                    e.Status == "Active");

            if (activeEnrolments >= category.MaximumParticipants)
                return Conflict(new { message = "This category has reached its maximum capacity." });

            var enrolment = new Enrolment
            {
                ParticipantId = participant.UserId,
                CategoryId = category.CategoryId,
                EnrolmentDate = DateTime.UtcNow,
                Status = "Active"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMyEnrolments),
                null,
                new
                {
                    enrolment.EnrolmentId,
                    enrolment.ParticipantId,
                    enrolment.CategoryId,
                    enrolment.EnrolmentDate,
                    enrolment.Status
                });
        }

        // GET /api/enrolments/my
        [HttpGet("my")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized(new { message = "Unable to identify the logged-in user." });

            var participantExists = await _context.Participants
                .AnyAsync(p => p.UserId == userId.Value);

            if (!participantExists)
                return NotFound(new { message = "Participant profile not found." });

            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .Where(e => e.ParticipantId == userId.Value)
                .OrderByDescending(e => e.EnrolmentDate)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.EnrolmentDate,
                    e.Status,
                    Category = new
                    {
                        e.Category.CategoryId,
                        e.Category.CategoryName,
                        e.Category.Distance,
                        e.Category.EntryFee
                    },
                    Event = new
                    {
                        e.Category.Event.EventId,
                        e.Category.Event.EventName,
                        e.Category.Event.EventDate,
                        e.Category.Event.Location,
                        e.Category.Event.EventType
                    }
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // GET /api/events/{eventId}/enrolments
        [HttpGet("/api/events/{eventId:int}/enrolments")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized(new { message = "Unable to identify the logged-in user." });

            var eventItem = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (eventItem == null)
                return NotFound(new { message = "Event not found." });

            if (eventItem.OrganiserId != userId.Value)
                return Forbid();

            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .Where(e => e.Category.EventId == eventId)
                .OrderBy(e => e.EnrolmentDate)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.EnrolmentDate,
                    e.Status,
                    Category = new
                    {
                        e.Category.CategoryId,
                        e.Category.CategoryName,
                        e.Category.Distance
                    },
                    Participant = new
                    {
                        e.Participant.UserId,
                        e.Participant.User.FirstName,
                        e.Participant.User.LastName,
                        e.Participant.User.Email
                    }
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // DELETE /api/enrolments/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized(new { message = "Unable to identify the logged-in user." });

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (enrolment == null)
                return NotFound(new { message = "Enrolment not found." });

            if (enrolment.ParticipantId != userId.Value)
                return Forbid();

            _context.Enrolments.Remove(enrolment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private int? GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(userId, out var parsedUserId)
                ? parsedUserId
                : null;
        }
    }


}
