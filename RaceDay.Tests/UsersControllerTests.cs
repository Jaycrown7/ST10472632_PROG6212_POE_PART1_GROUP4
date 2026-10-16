using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests
{
    public class UsersControllerTests
    {
        private static async Task<(RaceDayDbContext Context, SqliteConnection Connection)>
            CreateContextAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            connection.CreateFunction<string>(
                "getdate",
                () => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new RaceDayDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return (context, connection);
        }

        private static void SetUser(
            UsersController controller,
            string? userId = null,
            string? role = "Participant")
        {
            var claims = new System.Collections.Generic.List<Claim>();

            if (userId != null)
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

            if (role != null)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var identity = new ClaimsIdentity(claims, "TestAuthentication");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }

        private static async Task<User> CreateUserAsync(
            RaceDayDbContext context,
            string email)
        {
            var user = new User
            {
                FirstName = "Test",
                LastName = "User",
                Email = email,
                PasswordHash = "test-password-hash",
                Role = "Participant",
                PhoneNumber = "0712345678",
                City = "Johannesburg"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return user;
        }

        [Fact]
        public async Task GetProfile_ShouldReturnUnauthorized_WhenUserIdIsMissing()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var controller = new UsersController(context);
                SetUser(controller, null);

                var result = await controller.GetProfile();

                Assert.IsType<UnauthorizedResult>(result);
            }
        }

        [Fact]
        public async Task GetProfile_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var controller = new UsersController(context);
                SetUser(controller, "999");

                var result = await controller.GetProfile();

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }

        [Fact]
        public async Task GetProfile_ShouldReturnOk_WhenUserExists()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var user = await CreateUserAsync(
                    context, "profile.get@test.com");

                var controller = new UsersController(context);
                SetUser(controller, user.UserId.ToString());

                var result = await controller.GetProfile();

                var okResult = Assert.IsType<OkObjectResult>(result);
                Assert.NotNull(okResult.Value);
            }
        }

        [Fact]
        public async Task UpdateProfile_ShouldReturnUnauthorized_WhenUserIdIsMissing()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var controller = new UsersController(context);
                SetUser(controller, null);

                var request = new UpdateUserProfileRequest
                {
                    PhoneNumber = "0722222222",
                    City = "Tembisa",
                    EmergencyContact = "0711111111",
                    ProfileImageUrl = "https://example.com/profile.jpg"
                };

                var result = await controller.UpdateProfile(request);

                Assert.IsType<UnauthorizedResult>(result);
            }
        }

        [Fact]
        public async Task UpdateProfile_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var controller = new UsersController(context);
                SetUser(controller, "999");

                var request = new UpdateUserProfileRequest
                {
                    PhoneNumber = "0722222222",
                    City = "Tembisa"
                };

                var result = await controller.UpdateProfile(request);

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }

        [Fact]
        public async Task UpdateProfile_ShouldSaveChanges_WhenUserExists()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var user = await CreateUserAsync(
                    context, "profile.update@test.com");

                var controller = new UsersController(context);
                SetUser(controller, user.UserId.ToString());

                var request = new UpdateUserProfileRequest
                {
                    PhoneNumber = "0722222222",
                    City = "Tembisa",
                    EmergencyContact = "0711111111",
                    ProfileImageUrl = "https://example.com/profile.jpg"
                };

                var result = await controller.UpdateProfile(request);

                Assert.IsType<OkObjectResult>(result);

                var updatedUser = await context.Users
                    .Include(u => u.UserProfile)
                    .SingleAsync(u => u.UserId == user.UserId);

                Assert.Equal("0722222222", updatedUser.PhoneNumber);
                Assert.Equal("Tembisa", updatedUser.City);
                Assert.NotNull(updatedUser.UserProfile);
                Assert.Equal(
                    "0711111111",
                    updatedUser.UserProfile!.EmergencyContact);
                Assert.Equal(
                    "https://example.com/profile.jpg",
                    updatedUser.UserProfile.ProfileImageUrl);
            }
        }

        [Fact]
        public async Task UpdateProfile_ShouldUpdateExistingProfile()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var user = await CreateUserAsync(
                    context, "profile.existing@test.com");

                var profile = new UserProfile
                {
                    UserId = user.UserId,
                    EmergencyContact = "0700000000",
                    ProfileImageUrl = "https://example.com/old.jpg",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.UserProfiles.Add(profile);
                await context.SaveChangesAsync();

                var controller = new UsersController(context);
                SetUser(controller, user.UserId.ToString());

                var request = new UpdateUserProfileRequest
                {
                    PhoneNumber = "0733333333",
                    City = "Pretoria",
                    EmergencyContact = "0744444444",
                    ProfileImageUrl = "https://example.com/new.jpg"
                };

                var result = await controller.UpdateProfile(request);

                Assert.IsType<OkObjectResult>(result);

                var updatedProfile = await context.UserProfiles
                    .SingleAsync(p => p.UserId == user.UserId);

                Assert.Equal("0744444444", updatedProfile.EmergencyContact);
                Assert.Equal(
                    "https://example.com/new.jpg",
                    updatedProfile.ProfileImageUrl);
            }
        }
    }
}