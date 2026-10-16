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
    public class CategoriesControllerTests
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

        private static void SetOrganiser(
            CategoriesController controller,
            int organiserId)
        {
            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        organiserId.ToString()),
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

        private static async Task<(User Organiser, Event EventItem)>
            CreateEventAsync(
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
                EventName = "Test Categories Event",
                Description = "Test event for categories.",
                EventDate = DateOnly.FromDateTime(
                    DateTime.UtcNow.AddDays(30)),
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
        public async Task GetCategories_ShouldReturnNotFound_WhenEventDoesNotExist()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var controller = new CategoriesController(context);

                var result = await controller.GetCategories(999);

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }

        [Fact]
        public async Task GetCategoryById_ShouldReturnNotFound_WhenCategoryDoesNotExist()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var controller = new CategoriesController(context);

                var result = await controller.GetCategoryById(1, 999);

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }

        [Fact]
        public async Task GetCategories_ShouldReturnOk_WhenEventExists()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var (_, eventItem) = await CreateEventAsync(
                    context,
                    "categories.read@test.com");

                var controller = new CategoriesController(context);

                var result = await controller.GetCategories(
                    eventItem.EventId);

                Assert.IsType<OkObjectResult>(result);
            }
        }

        [Fact]
        public async Task CreateCategory_ShouldReturnForbid_WhenEventBelongsToAnotherOrganiser()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var (_, eventItem) = await CreateEventAsync(
                    context,
                    "categories.owner@test.com");

                var controller = new CategoriesController(context);

                SetOrganiser(
                    controller,
                    eventItem.OrganiserId + 1000);

                var request = new CreateCategoryRequest
                {
                    CategoryName = "5 km Open",
                    Distance = 5,
                    EntryFee = 150,
                    MaximumParticipants = 100
                };

                var result = await controller.CreateCategory(
                    eventItem.EventId,
                    request);

                Assert.IsType<ForbidResult>(result);

                Assert.False(
                    await context.Categories.AnyAsync(
                        c => c.EventId == eventItem.EventId));
            }
        }

        [Fact]
        public async Task CreateCategory_ShouldReturnNotFound_WhenEventDoesNotExist()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var controller = new CategoriesController(context);

                SetOrganiser(controller, 1);

                var request = new CreateCategoryRequest
                {
                    CategoryName = "5 km Open",
                    Distance = 5,
                    EntryFee = 150,
                    MaximumParticipants = 100
                };

                var result = await controller.CreateCategory(
                    999,
                    request);

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }

        [Fact]
        public async Task CreateCategory_ShouldReturnCreated_WhenOrganiserOwnsEvent()
        {
            var (context, connection) = await CreateContextAsync();

            await using (context)
            await using (connection)
            {
                var (organiser, eventItem) = await CreateEventAsync(
                    context,
                    "categories.create@test.com");

                var controller = new CategoriesController(context);

                SetOrganiser(controller, organiser.UserId);

                var request = new CreateCategoryRequest
                {
                    CategoryName = "5 km Open",
                    Distance = 5,
                    EntryFee = 150,
                    MaximumParticipants = 100
                };

                var result = await controller.CreateCategory(
                    eventItem.EventId,
                    request);

                Assert.IsType<CreatedAtActionResult>(result);

                var category = await context.Categories
                    .SingleAsync(c => c.EventId == eventItem.EventId);

                Assert.Equal("5 km Open", category.CategoryName);
                Assert.Equal(5, category.Distance);
            }
        }
    }
}