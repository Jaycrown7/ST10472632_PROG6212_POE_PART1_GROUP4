using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public UsersController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/users/profile
        // Returns the logged-in user's profile.
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
                return NotFound(new { message = "User profile not found." });

            return Ok(new
            {
                user.UserId,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.PhoneNumber,
                user.City,
                EmergencyContact = user.UserProfile?.EmergencyContact,
                ProfileImageUrl = user.UserProfile?.ProfileImageUrl
            });
        }

        // PUT: api/users/profile
        // Updates the logged-in user's profile.
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateUserProfileRequest request)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
                return NotFound(new { message = "User profile not found." });

            user.PhoneNumber = request.PhoneNumber;
            user.City = request.City;

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile
                {
                    UserId = user.UserId,
                    EmergencyContact = request.EmergencyContact,
                    ProfileImageUrl = request.ProfileImageUrl
                };

                _context.UserProfiles.Add(user.UserProfile);
            }
            else
            {
                user.UserProfile.EmergencyContact = request.EmergencyContact;
                user.UserProfile.ProfileImageUrl = request.ProfileImageUrl;
                user.UserProfile.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully.",
                user.UserId,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.PhoneNumber,
                user.City,
                EmergencyContact = user.UserProfile.EmergencyContact,
                ProfileImageUrl = user.UserProfile.ProfileImageUrl
            });
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (int.TryParse(userIdValue, out var userId))
                return userId;

            return null;
        }
    }

    public class UpdateUserProfileRequest
    {
        public string? PhoneNumber { get; set; }

        public string? City { get; set; }

        public string? EmergencyContact { get; set; }

        public string? ProfileImageUrl { get; set; }
    }
}