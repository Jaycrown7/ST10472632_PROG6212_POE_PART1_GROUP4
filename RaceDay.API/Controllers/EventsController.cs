using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/events")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // Both Participants and Organisers can view events.
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events.ToListAsync();

            return Ok(events);
        }

        // Only Organisers can view the organiser-only test endpoint.
        [HttpGet("organiser-test")]
        [Authorize(Roles = "Organiser")]
        public IActionResult OrganiserTest()
        {
            return Ok(new
            {
                message = "Access granted to the Organiser.",
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                role = User.FindFirstValue(ClaimTypes.Role)
            });
        }

        // Only authenticated Participants can access this test endpoint.
        [HttpGet("participant-test")]
        [Authorize(Roles = "Participant")]
        public IActionResult ParticipantTest()
        {
            return Ok(new
            {
                message = "Access granted to the Participant.",
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                role = User.FindFirstValue(ClaimTypes.Role)
            });
        }
    }
}