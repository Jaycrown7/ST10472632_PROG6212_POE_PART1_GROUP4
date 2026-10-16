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
    public class ResultsControllerTests
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

       
    [Fact]
        public async Task GetEventResults_ShouldReturnForbid_WhenEventBelongsToAnotherOrganiser()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var owner = await CreateUserAsync(
                    context, "results.owner@test.com", "Organiser");

                var otherOrganiser = await CreateUserAsync(
                    context, "results.other@test.com", "Organiser");

                var raceEvent = await CreateEventAsync(
                    context, owner.UserId);

                var controller = new ResultsController(context);
                SetUser(controller, otherOrganiser.UserId.ToString(), "Organiser");

                var result = await controller.GetEventResults(raceEvent.EventId);

                Assert.IsType<ForbidResult>(result);
            }
        }

        [Fact]
        public async Task UpdateResult_ShouldReturnNotFound_WhenResultDoesNotExist()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var organiser = await CreateUserAsync(
                    context, "results.update.missing@test.com", "Organiser");

                var controller = new ResultsController(context);
                SetUser(controller, organiser.UserId.ToString(), "Organiser");

                var request = new RaceDay.API.DTOs.RecordResultRequest
                {
                    EnrolmentId = 1,
                    FinishTime = new TimeOnly(0, 30, 0),
                    Position = 1,
                    ResultStatus = "Completed"
                };

                var result = await controller.UpdateResult(999, request);

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }

        [Fact]
        public async Task UpdateResult_ShouldReturnOk_WhenOrganiserOwnsResult()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var organiser = await CreateUserAsync(
                    context, "results.update.owner@test.com", "Organiser");

                var setup = await CreateEnrolmentAsync(
                    context, organiser.UserId, "results.update.participant@test.com");

                var savedResult = new Result
                {
                    EnrolmentId = setup.Enrolment.EnrolmentId,
                    FinishTime = new TimeOnly(0, 35, 0),
                    Position = 2,
                    ResultStatus = "Completed",
                    RecordedAt = DateTime.UtcNow
                };

                context.Results.Add(savedResult);
                await context.SaveChangesAsync();

                var controller = new ResultsController(context);
                SetUser(controller, organiser.UserId.ToString(), "Organiser");

                var request = new RaceDay.API.DTOs.RecordResultRequest
                {
                    EnrolmentId = setup.Enrolment.EnrolmentId,
                    FinishTime = new TimeOnly(0, 32, 0),
                    Position = 1,
                    ResultStatus = "Completed"
                };

                var result = await controller.UpdateResult(savedResult.ResultId, request);

                Assert.IsType<OkObjectResult>(result);

                var updated = await context.Results
                    .SingleAsync(r => r.ResultId == savedResult.ResultId);

                Assert.Equal(new TimeOnly(0, 32, 0), updated.FinishTime);
                Assert.Equal(1, updated.Position);
            }
        }

        [Fact]
        public async Task UpdateResult_ShouldReturnForbid_WhenAnotherOrganiserOwnsResult()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var owner = await CreateUserAsync(
                    context, "results.update.actualowner@test.com", "Organiser");

                var otherOrganiser = await CreateUserAsync(
                    context, "results.update.otherowner@test.com", "Organiser");

                var setup = await CreateEnrolmentAsync(
                    context, owner.UserId, "results.update.forbid.participant@test.com");

                var savedResult = new Result
                {
                    EnrolmentId = setup.Enrolment.EnrolmentId,
                    FinishTime = new TimeOnly(0, 35, 0),
                    Position = 2,
                    ResultStatus = "Completed",
                    RecordedAt = DateTime.UtcNow
                };

                context.Results.Add(savedResult);
                await context.SaveChangesAsync();

                var controller = new ResultsController(context);
                SetUser(controller, otherOrganiser.UserId.ToString(), "Organiser");

                var request = new RaceDay.API.DTOs.RecordResultRequest
                {
                    EnrolmentId = setup.Enrolment.EnrolmentId,
                    FinishTime = new TimeOnly(0, 32, 0),
                    Position = 1,
                    ResultStatus = "Completed"
                };

                var result = await controller.UpdateResult(savedResult.ResultId, request);

                Assert.IsType<ForbidResult>(result);
            }
        }



        private static void SetUser(
            ResultsController controller,
            string? userId = null,
            string? role = null)
        {
            var claims = new System.Collections.Generic.List<Claim>();

            if (userId != null)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            if (role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(
                claims,
                "TestAuthentication");

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
                PasswordHash = "test-password-hash",
                Role = role,
                City = "Johannesburg",
                PhoneNumber = "0712345678"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return user;
        }

        private static async Task<Event> CreateEventAsync(
            RaceDayDbContext context,
            int organiserId,
            string eventName = "Test Race")
        {
            var raceEvent = new Event
            {
                OrganiserId = organiserId,
                EventName = eventName,
                Description = "Test running event",
                EventDate = DateOnly.FromDateTime(
                    DateTime.UtcNow.AddDays(30)),
                Location = "Johannesburg",
                Distance = 10m,
                EventType = "Run"
            };

            context.Events.Add(raceEvent);
            await context.SaveChangesAsync();

            return raceEvent;
        }

        private static async Task<(Participant Participant, Category Category, Enrolment Enrolment)>
            CreateEnrolmentAsync(
                RaceDayDbContext context,
                int organiserId,
                string participantEmail,
                string eventName = "Test Race")
        {
            var participantUser = await CreateUserAsync(
                context,
                participantEmail,
                "Participant");

            var participant = new Participant
            {
                UserId = participantUser.UserId
            };

            context.Participants.Add(participant);
            await context.SaveChangesAsync();

            var raceEvent = await CreateEventAsync(
                context,
                organiserId,
                eventName);

            var category = new Category
            {
                EventId = raceEvent.EventId,
                CategoryName = "10 km Open",
                Distance = 10,
                EntryFee = 100,
                MaximumParticipants = 100
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var enrolment = new Enrolment
            {
                ParticipantId = participantUser.UserId,
                CategoryId = category.CategoryId,
                EnrolmentDate = DateTime.UtcNow,
                Status = "Active"
            };

            context.Enrolments.Add(enrolment);
            await context.SaveChangesAsync();

            return (participant, category, enrolment);
        }

        [Fact]
        public async Task GetMyResults_ShouldReturnUnauthorized_WhenUserIdIsMissing()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var controller = new ResultsController(context);

                SetUser(controller);

                var result = await controller.GetMyResults();

                Assert.IsType<UnauthorizedObjectResult>(result);
            }
        }

        [Fact]
        public async Task RecordResult_ShouldReturnUnauthorized_WhenUserIdIsMissing()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var controller = new ResultsController(context);

                SetUser(controller);

                var request = new RaceDay.API.DTOs.RecordResultRequest
                {
                    EnrolmentId = 1,
                    FinishTime = new TimeOnly(0, 30, 0),
                    Position = 1,
                    ResultStatus = "Completed"
                };

                var result = await controller.RecordResult(request);

                Assert.IsType<UnauthorizedObjectResult>(result);
            }
        }

        [Fact]
        public async Task GetEventResults_ShouldReturnNotFound_WhenEventDoesNotExist()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var controller = new ResultsController(context);

                SetUser(controller, "1", "Organiser");

                var result = await controller.GetEventResults(999);

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }

        [Fact]
        public async Task RecordResult_ShouldReturnCreated_WhenOrganiserOwnsEvent()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var organiser = await CreateUserAsync(
                    context,
                    "organiser@example.com",
                    "Organiser");

                var setup = await CreateEnrolmentAsync(
                    context,
                    organiser.UserId,
                    "participant@example.com");

                var controller = new ResultsController(context);

                SetUser(
                    controller,
                    organiser.UserId.ToString(),
                    "Organiser");

                var request = new RaceDay.API.DTOs.RecordResultRequest
                {
                    EnrolmentId = setup.Enrolment.EnrolmentId,
                    FinishTime = new TimeOnly(0, 32, 45),
                    Position = 1,
                    ResultStatus = "Completed"
                };

                var result = await controller.RecordResult(request);

                Assert.IsType<CreatedAtActionResult>(result);
                Assert.Equal(1, await context.Results.CountAsync());
            }
        }

        [Fact]
        public async Task RecordResult_ShouldReturnForbid_WhenEventBelongsToAnotherOrganiser()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var eventOwner = await CreateUserAsync(
                    context,
                    "owner@example.com",
                    "Organiser");

                var otherOrganiser = await CreateUserAsync(
                    context,
                    "otherorganiser@example.com",
                    "Organiser");

                var setup = await CreateEnrolmentAsync(
                    context,
                    eventOwner.UserId,
                    "participant@example.com");

                var controller = new ResultsController(context);

                SetUser(
                    controller,
                    otherOrganiser.UserId.ToString(),
                    "Organiser");

                var request = new RaceDay.API.DTOs.RecordResultRequest
                {
                    EnrolmentId = setup.Enrolment.EnrolmentId,
                    FinishTime = new TimeOnly(0, 35, 0),
                    Position = 1,
                    ResultStatus = "Completed"
                };

                var result = await controller.RecordResult(request);

                Assert.IsType<ForbidResult>(result);
                Assert.Empty(await context.Results.ToListAsync());
            }
        }

        [Fact]
        public async Task RecordResult_ShouldReturnConflict_WhenResultAlreadyExists()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var organiser = await CreateUserAsync(
                    context,
                    "organiser@example.com",
                    "Organiser");

                var setup = await CreateEnrolmentAsync(
                    context,
                    organiser.UserId,
                    "participant@example.com");

                context.Results.Add(new Result
                {
                    EnrolmentId = setup.Enrolment.EnrolmentId,
                    FinishTime = new TimeOnly(0, 32, 45),
                    Position = 1,
                    ResultStatus = "Completed",
                    RecordedAt = DateTime.UtcNow
                });

                await context.SaveChangesAsync();

                var controller = new ResultsController(context);

                SetUser(
                    controller,
                    organiser.UserId.ToString(),
                    "Organiser");

                var request = new RaceDay.API.DTOs.RecordResultRequest
                {
                    EnrolmentId = setup.Enrolment.EnrolmentId,
                    FinishTime = new TimeOnly(0, 33, 0),
                    Position = 2,
                    ResultStatus = "Completed"
                };

                var result = await controller.RecordResult(request);

                Assert.IsType<ConflictObjectResult>(result);
                Assert.Equal(1, await context.Results.CountAsync());
            }
        }

        [Fact]
        public async Task GetMyResults_ShouldReturnNotFound_WhenUserIsNotParticipant()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var user = await CreateUserAsync(
                    context,
                    "organiser@example.com",
                    "Organiser");

                var controller = new ResultsController(context);

                SetUser(
                    controller,
                    user.UserId.ToString(),
                    "Participant");

                var result = await controller.GetMyResults();

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }

        [Fact]
        public async Task GetMyResults_ShouldReturnOk_WhenParticipantExists()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var organiser = await CreateUserAsync(
                    context,
                    "organiser@example.com",
                    "Organiser");

                var setup = await CreateEnrolmentAsync(
                    context,
                    organiser.UserId,
                    "participant@example.com");

                context.Results.Add(new Result
                {
                    EnrolmentId = setup.Enrolment.EnrolmentId,
                    FinishTime = new TimeOnly(0, 32, 45),
                    Position = 1,
                    ResultStatus = "Completed",
                    RecordedAt = DateTime.UtcNow
                });

                await context.SaveChangesAsync();

                var controller = new ResultsController(context);

                SetUser(
                    controller,
                    setup.Participant.UserId.ToString(),
                    "Participant");

                var result = await controller.GetMyResults();

                Assert.IsType<OkObjectResult>(result);
            }
        }
    }
}