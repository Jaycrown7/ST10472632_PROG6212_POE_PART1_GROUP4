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
    [Route("api/results")]
    [Authorize]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // ORGANISER: Record a result for an enrolment in their own event.
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> RecordResult(
            [FromBody] RecordResultRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Unable to identify the logged-in organiser."
                });
            }

            if (request.FinishTime <= TimeOnly.MinValue)
            {
                return BadRequest(new
                {
                    message = "A valid finishing time must be provided."
                });
            }

            var enrolment = await _context.Enrolments
                .Include(e => e.Category)
                .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e =>
                    e.EnrolmentId == request.EnrolmentId);

            if (enrolment == null)
            {
                return NotFound(new
                {
                    message = "Enrolment not found."
                });
            }

            if (enrolment.Category.Event.OrganiserId != userId.Value)
            {
                return Forbid();
            }

            if (enrolment.Status != "Active")
            {
                return BadRequest(new
                {
                    message = "A result can only be recorded for an active enrolment."
                });
            }

            bool resultExists = await _context.Results
                .AnyAsync(r => r.EnrolmentId == request.EnrolmentId);

            if (resultExists)
            {
                return Conflict(new
                {
                    message = "A result has already been recorded for this enrolment."
                });
            }

            string status = request.ResultStatus.Trim();

            if (status.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Result status is required."
                });
            }

            var result = new Result
            {
                EnrolmentId = request.EnrolmentId,
                FinishTime = request.FinishTime,
                Position = request.Position,
                ResultStatus = status
            };

            _context.Results.Add(result);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _context.Entry(result).State = EntityState.Detached;

                bool duplicateNowExists = await _context.Results
                    .AnyAsync(r => r.EnrolmentId == request.EnrolmentId);

                if (duplicateNowExists)
                {
                    return Conflict(new
                    {
                        message = "A result has already been recorded for this enrolment."
                    });
                }

                throw;
            }

            return CreatedAtAction(
                nameof(GetMyResults),
                null,
                new
                {
                    result.ResultId,
                    result.EnrolmentId,
                    result.FinishTime,
                    result.Position,
                    result.ResultStatus,
                    result.RecordedAt
                });
        }

        // PARTICIPANT: View only their own results.
        [HttpGet("my")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyResults()
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Unable to identify the logged-in participant."
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

            var results = await _context.Results
                .AsNoTracking()
                .Where(r => r.Enrolment.ParticipantId == userId.Value)
                .OrderBy(r => r.Position)
                .Select(r => new
                {
                    r.ResultId,
                    r.FinishTime,
                    r.Position,
                    r.ResultStatus,
                    r.RecordedAt,
                    Enrolment = new
                    {
                        r.Enrolment.EnrolmentId,
                        Category = new
                        {
                            r.Enrolment.Category.CategoryId,
                            r.Enrolment.Category.CategoryName,
                            r.Enrolment.Category.Distance
                        },
                        Event = new
                        {
                            r.Enrolment.Category.Event.EventId,
                            r.Enrolment.Category.Event.EventName,
                            r.Enrolment.Category.Event.EventDate,
                            r.Enrolment.Category.Event.Location,
                            r.Enrolment.Category.Event.EventType
                        }
                    }
                })
                .ToListAsync();

            return Ok(results);
        }

        // ORGANISER: View results for an event they own.
        [HttpGet("events/{eventId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEventResults(int eventId)
        {
            var userId = GetUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Unable to identify the logged-in organiser."
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

            var results = await _context.Results
                .AsNoTracking()
                .Where(r => r.Enrolment.Category.EventId == eventId)
                .OrderBy(r => r.Position)
                .Select(r => new
                {
                    r.ResultId,
                    r.FinishTime,
                    r.Position,
                    r.ResultStatus,
                    r.RecordedAt,
                    Participant = new
                    {
                        r.Enrolment.Participant.UserId,
                        r.Enrolment.Participant.User.FirstName,
                        r.Enrolment.Participant.User.LastName,
                        r.Enrolment.Participant.User.Email
                    },
                    Category = new
                    {
                        r.Enrolment.Category.CategoryId,
                        r.Enrolment.Category.CategoryName,
                        r.Enrolment.Category.Distance
                    }
                })
                .ToListAsync();

            return Ok(results);
        }

        private int? GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(userId, out int parsedUserId)
                ? parsedUserId
                : null;
        }
    }
}

