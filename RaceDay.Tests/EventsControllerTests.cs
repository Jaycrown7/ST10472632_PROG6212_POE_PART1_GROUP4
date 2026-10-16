using System;
using System.Threading.Tasks;
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
                var organiser = new User
                {
                    FirstName = "Test",
                    LastName = "Organiser",
                    Email = "organiser@test.com",
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

                var controller = new EventsController(context);

                var result = await controller.GetEventById(eventItem.EventId);

                var okResult = Assert.IsType<OkObjectResult>(result);
                var returnedEvent = Assert.IsType<Event>(okResult.Value);

                Assert.Equal("Test Running Event", returnedEvent.EventName);
            }
        }
    }


}
