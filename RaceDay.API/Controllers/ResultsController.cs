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

        // POST /api/results
        // Organiser records a result for an enrolment in their own event.
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> RecordResult(
            [FromBody] RecordResultRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized(new { message = "Unable to identify the logged-in organiser." });

            if (request.FinishTime <= TimeOnly.MinValue)
                return BadRequest(new { message = "A valid finishing time must be provided." });

            if (request.Position < 1)
                return BadRequest(new { message = "Position must be greater than zero." });

            if (string.IsNullOrWhiteSpace(request.ResultStatus))
                return BadRequest(new { message = "Result status is required." });

            var enrolment = await _context.Enrolments
                .Include(e => e.Category)
                .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);

            if (enrolment == null)
                return NotFound(new { message = "Enrolment not found." });

            if (enrolment.Category.Event.OrganiserId != userId.Value)
                return Forbid();

            if (enrolment.Status != "Active")
                return BadRequest(new
                {
                    message = "A result can only be recorded for an active enrolment."
                });

            var resultExists = await _context.Results
                .AnyAsync(r => r.EnrolmentId == request.EnrolmentId);

            if (resultExists)
                return Conflict(new
                {
                    message = "A result has already been recorded for this enrolment."
                });

            var result = new Result
            {
                EnrolmentId = request.EnrolmentId,
                FinishTime = request.FinishTime,
                Position = request.Position,
                ResultStatus = request.ResultStatus.Trim()
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

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

        // GET /api/results/my
        // Participant views their own results.
        [HttpGet("my")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyResults()
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized(new { message = "Unable to identify the logged-in participant." });

            var participantExists = await _context.Participants
                .AnyAsync(p => p.UserId == userId.Value);

            if (!participantExists)
                return NotFound(new { message = "Participant profile not found." });

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

        // GET /api/events/{eventId}/results
        // Organiser views results for their own event.
        [HttpGet("/api/events/{eventId:int}/results")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEventResults(int eventId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized(new { message = "Unable to identify the logged-in organiser." });

            var eventItem = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (eventItem == null)
                return NotFound(new { message = "Event not found." });

            if (eventItem.OrganiserId != userId.Value)
                return Forbid();

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

        // PUT /api/results/{id}
        // Organiser updates a result belonging to their own event.
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateResult(
            int id,
            [FromBody] RecordResultRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized(new { message = "Unable to identify the logged-in organiser." });

            if (request.FinishTime <= TimeOnly.MinValue)
                return BadRequest(new { message = "A valid finishing time must be provided." });

            if (request.Position < 1)
                return BadRequest(new { message = "Position must be greater than zero." });

            if (string.IsNullOrWhiteSpace(request.ResultStatus))
                return BadRequest(new { message = "Result status is required." });

            var result = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e.Category)
                        .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
                return NotFound(new { message = "Result not found." });

            if (result.Enrolment.Category.Event.OrganiserId != userId.Value)
                return Forbid();

            if (result.EnrolmentId != request.EnrolmentId)
                return BadRequest(new
                {
                    message = "The enrolment associated with a result cannot be changed."
                });

            result.FinishTime = request.FinishTime;
            result.Position = request.Position;
            result.ResultStatus = request.ResultStatus.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                result.ResultId,
                result.EnrolmentId,
                result.FinishTime,
                result.Position,
                result.ResultStatus,
                result.RecordedAt
            });
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
