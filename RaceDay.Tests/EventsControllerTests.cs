using System;
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
    public class EventsControllerTests
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

        private static void SetOrganiser(EventsController controller, int organiserId)
        {
            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, organiserId.ToString()),
                    new Claim(ClaimTypes.Role, "Organiser")
                },
                "TestAuthentication");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }

        private static async Task<(User Organiser, Event EventItem)> CreateEventAsync(
            RaceDayDbContext context,
            string email)
        {
            var organiser = new User
            {
                FirstName = "Test",
                LastName = "Organiser",
                Email = email,
                PasswordHash = "TestPasswordHash",
                Role = "Organiser",
                City = "Johannesburg",
                PhoneNumber = "0712345678"
            };

            context.Users.Add(organiser);
            await context.SaveChangesAsync();

            var eventItem = new Event
            {
                OrganiserId = organiser.UserId,
                EventName = "Test Running Event",
                Description = "An event created for unit testing.",
                EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                Location = "Johannesburg",
                Distance = 10,
                EventType = "Run",
                CreatedAt = DateTime.UtcNow
            };

            context.Events.Add(eventItem);
            await context.SaveChangesAsync();

            return (organiser, eventItem);
        }

        [Fact]
        public async Task GetEvents_ShouldReturnOk()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var controller = new EventsController(context);

                var result = await controller.GetEvents();

                Assert.IsType<OkObjectResult>(result);
            }
        }

        [Fact]
        public async Task GetEventById_ShouldReturnNotFoundForMissingEvent()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var controller = new EventsController(context);

                var result = await controller.GetEventById(999);

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }

        [Fact]
        public async Task GetEventById_ShouldReturnEventWhenItExists()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var (_, eventItem) = await CreateEventAsync(
                    context, "event.read@test.com");

                var controller = new EventsController(context);

                var result = await controller.GetEventById(eventItem.EventId);

                var okResult = Assert.IsType<OkObjectResult>(result);
                var returnedEvent = Assert.IsType<Event>(okResult.Value);

                Assert.Equal("Test Running Event", returnedEvent.EventName);
            }
        }

        [Fact]
        public async Task UpdateEvent_ShouldReturnForbid_WhenEventBelongsToAnotherOrganiser()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var (_, eventItem) = await CreateEventAsync(
                    context, "event.owner@test.com");

                var controller = new EventsController(context);

                SetOrganiser(controller, eventItem.OrganiserId + 1000);

                var request = new UpdateEventRequest
                {
                    EventName = "Changed Event",
                    Description = "Changed description",
                    EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)),
                    Location = "Pretoria",
                    Distance = 21,
                    EventType = "Run"
                };

                var result = await controller.UpdateEvent(
                    eventItem.EventId, request);

                Assert.IsType<ForbidResult>(result);
                Assert.Equal("Test Running Event", eventItem.EventName);
            }
        }

        [Fact]
        public async Task DeleteEvent_ShouldReturnForbid_WhenEventBelongsToAnotherOrganiser()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var (_, eventItem) = await CreateEventAsync(
                    context, "event.delete.owner@test.com");

                var controller = new EventsController(context);

                SetOrganiser(controller, eventItem.OrganiserId + 1000);

                var result = await controller.DeleteEvent(eventItem.EventId);

                Assert.IsType<ForbidResult>(result);

                var eventStillExists = await context.Events
                    .AnyAsync(e => e.EventId == eventItem.EventId);

                Assert.True(eventStillExists);
            }
        }

        [Fact]
        public async Task UpdateEvent_ShouldReturnOk_WhenEventBelongsToOrganiser()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var (organiser, eventItem) = await CreateEventAsync(
                    context, "event.update.owner@test.com");

                var controller = new EventsController(context);

                SetOrganiser(controller, organiser.UserId);

                var request = new UpdateEventRequest
                {
                    EventName = "Updated Running Event",
                    Description = "Updated description.",
                    EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
                    Location = "Pretoria",
                    Distance = 21,
                    EventType = "Run"
                };

                var result = await controller.UpdateEvent(
                    eventItem.EventId, request);

                Assert.IsType<OkObjectResult>(result);
                Assert.Equal("Updated Running Event", eventItem.EventName);
            }
        }
    }
}