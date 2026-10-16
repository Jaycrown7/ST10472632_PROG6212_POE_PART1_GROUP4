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
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests
{
    public class EnrolmentsControllerTests
    {
        private static async Task<(RaceDayDbContext Context, SqliteConnection Connection)>
            CreateContextAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            connection.CreateFunction<string?>(
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
            EnrolmentsController controller,
            string? userId = null,
            string? role = null)
        {
            var claims = new ClaimsIdentity();

            if (userId != null)
            {
                claims = new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId),
                        new Claim(ClaimTypes.Role, role ?? "Participant")
                    },
                    "TestAuthentication");
            }

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(claims)
                }
            };
        }

        private static async Task<User> CreateUserAsync(
            RaceDayDbContext context,
            string email,
            string role)
        {
            var user = new User
            {
                FirstName = "Test",
                LastName = "User",
                Email = email,
                PasswordHash = "TestPasswordHash",
                Role = role,
                City = "Johannesburg",
                PhoneNumber = "0712345678"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return user;
        }

        private static async Task<(User Organiser, Event EventItem, Category CategoryItem)>
            CreateEventAndCategoryAsync(
                RaceDayDbContext context,
                string email,
                int maximumParticipants = 100)
        {
            var organiser = await CreateUserAsync(
                context,
                email,
                "Organiser");

            var eventItem = new Event
            {
                OrganiserId = organiser.UserId,
                EventName = "Test Running Event",
                Description = "Event for enrolment unit tests.",
                EventDate = DateOnly.FromDateTime(
                    DateTime.UtcNow.AddDays(30)),
                Location = "Johannesburg",
                Distance = 10,
                EventType = "Run",
                CreatedAt = DateTime.UtcNow
            };

            context.Events.Add(eventItem);
            await context.SaveChangesAsync();

            var category = new Category
            {
                EventId = eventItem.EventId,
                CategoryName = "10 km Open",
                Distance = 10,
                EntryFee = 150,
                MaximumParticipants = maximumParticipants,
                CreatedAt = DateTime.UtcNow
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            return (organiser, eventItem, category);
        }

        private static async Task<User> CreateParticipantAsync(
            RaceDayDbContext context,
            string email)
        {
            var participantUser = await CreateUserAsync(
                context,
                email,
                "Participant");

            context.Participants.Add(new Participant
            {
                UserId = participantUser.UserId
            });

            await context.SaveChangesAsync();

            return participantUser;
        }

        [Fact]
        public async Task GetMyEnrolments_ShouldReturnUnauthorized_WhenUserIdIsMissing()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var controller = new EnrolmentsController(context);

                SetUser(controller);

                var result = await controller.GetMyEnrolments();

                Assert.IsType<UnauthorizedObjectResult>(result);
            }
        }

        [Fact]
        public async Task CreateEnrolment_ShouldReturnUnauthorized_WhenUserIdIsMissing()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var controller = new EnrolmentsController(context);

                SetUser(controller);

                var request = new CreateEnrolmentRequest
                {
                    CategoryId = 1
                };

                var result = await controller.CreateEnrolment(request);

                Assert.IsType<UnauthorizedObjectResult>(result);
            }
        }

        [Fact]
        public async Task GetEventEnrolments_ShouldReturnNotFound_WhenEventDoesNotExist()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var controller = new EnrolmentsController(context);

                SetUser(controller, "1", "Organiser");

                var result = await controller.GetEventEnrolments(999);

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }

        [Fact]
        public async Task CreateEnrolment_ShouldReturnNotFound_WhenCategoryDoesNotExist()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var participant = await CreateParticipantAsync(
                    context,
                    "enrolment.missing.category@test.com");

                var controller = new EnrolmentsController(context);

                SetUser(controller, participant.UserId.ToString());

                var request = new CreateEnrolmentRequest
                {
                    CategoryId = 999
                };

                var result = await controller.CreateEnrolment(request);

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }

        [Fact]
        public async Task CreateEnrolment_ShouldReturnCreated_WhenRequestIsValid()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var (_, _, category) = await CreateEventAndCategoryAsync(
                    context,
                    "enrolment.valid.organiser@test.com");

                var participant = await CreateParticipantAsync(
                    context,
                    "enrolment.valid.participant@test.com");

                var controller = new EnrolmentsController(context);

                SetUser(controller, participant.UserId.ToString());

                var request = new CreateEnrolmentRequest
                {
                    CategoryId = category.CategoryId
                };

                var result = await controller.CreateEnrolment(request);

                Assert.IsType<CreatedAtActionResult>(result);

                var savedEnrolment = await context.Enrolments
                    .SingleAsync(e =>
                        e.ParticipantId == participant.UserId &&
                        e.CategoryId == category.CategoryId);

                Assert.Equal("Active", savedEnrolment.Status);
            }
        }

        [Fact]
        public async Task CreateEnrolment_ShouldReturnConflict_WhenParticipantIsAlreadyEnrolled()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var (_, _, category) = await CreateEventAndCategoryAsync(
                    context,
                    "enrolment.duplicate.organiser@test.com");

                var participant = await CreateParticipantAsync(
                    context,
                    "enrolment.duplicate.participant@test.com");

                context.Enrolments.Add(new Enrolment
                {
                    ParticipantId = participant.UserId,
                    CategoryId = category.CategoryId,
                    EnrolmentDate = DateTime.UtcNow,
                    Status = "Active"
                });

                await context.SaveChangesAsync();

                var controller = new EnrolmentsController(context);

                SetUser(controller, participant.UserId.ToString());

                var request = new CreateEnrolmentRequest
                {
                    CategoryId = category.CategoryId
                };

                var result = await controller.CreateEnrolment(request);

                Assert.IsType<ConflictObjectResult>(result);

                var enrolmentCount = await context.Enrolments.CountAsync(
                    e => e.ParticipantId == participant.UserId);

                Assert.Equal(1, enrolmentCount);
            }
        }

        [Fact]
        public async Task GetEventEnrolments_ShouldReturnForbid_WhenEventBelongsToAnotherOrganiser()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var (_, eventItem, _) = await CreateEventAndCategoryAsync(
                    context,
                    "enrolment.ownership.organiser@test.com");

                var otherOrganiser = await CreateUserAsync(
                    context,
                    "enrolment.ownership.other@test.com",
                    "Organiser");

                var controller = new EnrolmentsController(context);

                SetUser(controller, otherOrganiser.UserId.ToString(), "Organiser");

                var result = await controller.GetEventEnrolments(
                    eventItem.EventId);

                Assert.IsType<ForbidResult>(result);
            }
        }
    }
}