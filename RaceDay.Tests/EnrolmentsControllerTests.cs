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
            ClaimsIdentity identity;

            if (userId != null)
            {
                identity = new ClaimsIdentity(
                    new[]
                    {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Role, role ?? "Participant")
                    },
                    "TestAuthentication");
            }
            else
            {
                identity = new ClaimsIdentity();
            }

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
                EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
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
                    EventId = 1,
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
                var (_, eventItem, _) = await CreateEventAndCategoryAsync(
                    context,
                    "enrolment.missing.category@test.com");

                var participant = await CreateParticipantAsync(
                    context,
                    "enrolment.missing.participant@test.com");

                var controller = new EnrolmentsController(context);
                SetUser(controller, participant.UserId.ToString());

                var request = new CreateEnrolmentRequest
                {
                    EventId = eventItem.EventId,
                    CategoryId = 999
                };

                var result = await controller.CreateEnrolment(request);

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }

        [Fact]
        public async Task CreateEnrolment_ShouldReturnNotFound_WhenEventDoesNotExist()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var participant = await CreateParticipantAsync(
                    context,
                    "enrolment.missing.event@test.com");

                var controller = new EnrolmentsController(context);
                SetUser(controller, participant.UserId.ToString());

                var request = new CreateEnrolmentRequest
                {
                    EventId = 999,
                    CategoryId = 1
                };

                var result = await controller.CreateEnrolment(request);

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }

        [Fact]
        public async Task CreateEnrolment_ShouldReturnNotFound_WhenCategoryBelongsToAnotherEvent()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var (_, eventItem, category) = await CreateEventAndCategoryAsync(
                    context,
                    "enrolment.wrong.event.organiser@test.com");

                var (_, otherEvent, _) = await CreateEventAndCategoryAsync(
                    context,
                    "enrolment.other.event.organiser@test.com");

                var participant = await CreateParticipantAsync(
                    context,
                    "enrolment.wrong.event.participant@test.com");

                var controller = new EnrolmentsController(context);
                SetUser(controller, participant.UserId.ToString());

                var request = new CreateEnrolmentRequest
                {
                    EventId = otherEvent.EventId,
                    CategoryId = category.CategoryId
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
                var (_, eventItem, category) = await CreateEventAndCategoryAsync(
                    context,
                    "enrolment.valid.organiser@test.com");

                var participant = await CreateParticipantAsync(
                    context,
                    "enrolment.valid.participant@test.com");

                var controller = new EnrolmentsController(context);
                SetUser(controller, participant.UserId.ToString());

                var request = new CreateEnrolmentRequest
                {
                    EventId = eventItem.EventId,
                    CategoryId = category.CategoryId
                };

                var result = await controller.CreateEnrolment(request);

                Assert.IsType<CreatedAtActionResult>(result);

                var savedEnrolment = await context.Enrolments.SingleAsync(
                    e => e.ParticipantId == participant.UserId
                         && e.CategoryId == category.CategoryId);

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
                var (_, eventItem, category) = await CreateEventAndCategoryAsync(
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
                    EventId = eventItem.EventId,
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
        public async Task CreateEnrolment_ShouldReturnConflict_WhenCategoryIsFull()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var (_, eventItem, category) = await CreateEventAndCategoryAsync(
                    context,
                    "enrolment.full.organiser@test.com",
                    1);

                var firstParticipant = await CreateParticipantAsync(
                    context,
                    "enrolment.full.first@test.com");

                var secondParticipant = await CreateParticipantAsync(
                    context,
                    "enrolment.full.second@test.com");

                context.Enrolments.Add(new Enrolment
                {
                    ParticipantId = firstParticipant.UserId,
                    CategoryId = category.CategoryId,
                    EnrolmentDate = DateTime.UtcNow,
                    Status = "Active"
                });

                await context.SaveChangesAsync();

                var controller = new EnrolmentsController(context);
                SetUser(controller, secondParticipant.UserId.ToString());

                var request = new CreateEnrolmentRequest
                {
                    EventId = eventItem.EventId,
                    CategoryId = category.CategoryId
                };

                var result = await controller.CreateEnrolment(request);

                Assert.IsType<ConflictObjectResult>(result);
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

                var result = await controller.GetEventEnrolments(eventItem.EventId);

                Assert.IsType<ForbidResult>(result);
            }
        }

        [Fact]
        public async Task GetEventEnrolments_ShouldReturnOk_WhenOrganiserOwnsEvent()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var (organiser, eventItem, category) =
                    await CreateEventAndCategoryAsync(
                        context,
                        "enrolment.own.event.organiser@test.com");

                var participant = await CreateParticipantAsync(
                    context,
                    "enrolment.own.event.participant@test.com");

                context.Enrolments.Add(new Enrolment
                {
                    ParticipantId = participant.UserId,
                    CategoryId = category.CategoryId,
                    EnrolmentDate = DateTime.UtcNow,
                    Status = "Active"
                });

                await context.SaveChangesAsync();

                var controller = new EnrolmentsController(context);
                SetUser(controller, organiser.UserId.ToString(), "Organiser");

                var result = await controller.GetEventEnrolments(eventItem.EventId);

                Assert.IsType<OkObjectResult>(result);
            }
        }

        [Fact]
        public async Task CancelEnrolment_ShouldReturnNoContent_WhenParticipantOwnsEnrolment()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var (_, _, category) = await CreateEventAndCategoryAsync(
                    context,
                    "enrolment.cancel.organiser@test.com");

                var participant = await CreateParticipantAsync(
                    context,
                    "enrolment.cancel.participant@test.com");

                var enrolment = new Enrolment
                {
                    ParticipantId = participant.UserId,
                    CategoryId = category.CategoryId,
                    EnrolmentDate = DateTime.UtcNow,
                    Status = "Active"
                };

                context.Enrolments.Add(enrolment);
                await context.SaveChangesAsync();

                var enrolmentId = enrolment.EnrolmentId;

                var controller = new EnrolmentsController(context);
                SetUser(controller, participant.UserId.ToString());

                var result = await controller.CancelEnrolment(enrolmentId);

                Assert.IsType<NoContentResult>(result);
                Assert.False(await context.Enrolments.AnyAsync(
                    e => e.EnrolmentId == enrolmentId));
            }
        }

        [Fact]
        public async Task CancelEnrolment_ShouldReturnNotFound_WhenEnrolmentDoesNotExist()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var participant = await CreateParticipantAsync(
                    context,
                    "enrolment.cancel.missing@test.com");

                var controller = new EnrolmentsController(context);
                SetUser(controller, participant.UserId.ToString());

                var result = await controller.CancelEnrolment(999);

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }

        [Fact]
        public async Task CancelEnrolment_ShouldReturnForbid_WhenEnrolmentBelongsToAnotherParticipant()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var (_, _, category) = await CreateEventAndCategoryAsync(
                    context,
                    "enrolment.cancel.ownership.organiser@test.com");

                var owner = await CreateParticipantAsync(
                    context,
                    "enrolment.cancel.owner@test.com");

                var otherParticipant = await CreateParticipantAsync(
                    context,
                    "enrolment.cancel.other@test.com");

                var enrolment = new Enrolment
                {
                    ParticipantId = owner.UserId,
                    CategoryId = category.CategoryId,
                    EnrolmentDate = DateTime.UtcNow,
                    Status = "Active"
                };

                context.Enrolments.Add(enrolment);
                await context.SaveChangesAsync();

                var controller = new EnrolmentsController(context);
                SetUser(controller, otherParticipant.UserId.ToString());

                var result = await controller.CancelEnrolment(enrolment.EnrolmentId);

                Assert.IsType<ForbidResult>(result);
            }
        }
    }


}
