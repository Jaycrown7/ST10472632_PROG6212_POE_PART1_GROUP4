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

        // PARTICIPANT: Enrol in a category
        [HttpPost]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> CreateEnrolment(
            [FromBody] CreateEnrolmentRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Unable to identify the logged-in user."
                });
            }

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.UserId == userId.Value);

            if (participant == null)
            {
                return BadRequest(new
                {
                    message = "A participant profile was not found for this account."
                });
            }

            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Category not found."
                });
            }

            // Prevent duplicate enrolments.
            var alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e =>
                    e.ParticipantId == participant.UserId &&
                    e.CategoryId == category.CategoryId);

            if (alreadyEnrolled)
            {
                return Conflict(new
                {
                    message = "You are already enrolled in this category."
                });
            }

            // Check the number of active enrolments in this category.
            var activeEnrolments = await _context.Enrolments
                .CountAsync(e =>
                    e.CategoryId == category.CategoryId &&
                    e.Status == "Active");

            if (activeEnrolments >= category.MaximumParticipants)
            {
                return Conflict(new
                {
                    message = "This category has reached its maximum capacity."
                });
            }

            var enrolment = new Enrolment
            {
                ParticipantId = participant.UserId,
                CategoryId = category.CategoryId,
                EnrolmentDate = DateTime.UtcNow,
                Status = "Active"
            };

            _context.Enrolments.Add(enrolment);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // The database's unique constraint protects against
                // duplicate enrolments, including concurrent requests.
                _context.Entry(enrolment).State = EntityState.Detached;

                var duplicateExists = await _context.Enrolments
                    .AnyAsync(e =>
                        e.ParticipantId == participant.UserId &&
                        e.CategoryId == category.CategoryId);

                if (duplicateExists)
                {
                    return Conflict(new
                    {
                        message = "You are already enrolled in this category."
                    });
                }

                throw;
            }

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

        // PARTICIPANT: View only their own enrolments.
        [HttpGet("my")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Unable to identify the logged-in user."
                });
            }

            var participantExists = await _context.Participants
                .AnyAsync(p => p.UserId == userId.Value);

            if (!participantExists)
            {
                return NotFound(new
                {
                    message = "Participant profile not found."
                });
            }

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

        // ORGANISER: View enrolments for an event they own.
        [HttpGet("events/{eventId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Unable to identify the logged-in user."
                });
            }

            var eventItem = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            if (eventItem.OrganiserId != userId.Value)
            {
                return Forbid();
            }

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

        private int? GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userId, out int parsedUserId))
            {
                return parsedUserId;
            }

            return null;
        }
    }
}