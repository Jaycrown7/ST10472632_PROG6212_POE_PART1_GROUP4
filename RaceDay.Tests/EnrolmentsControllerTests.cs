using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
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
            string? userId = null)
        {
            var claims = new ClaimsIdentity();

            if (userId != null)
            {
                claims = new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId)
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

                var request = new RaceDay.API.DTOs.CreateEnrolmentRequest
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

                SetUser(controller, "1");

                var result = await controller.GetEventEnrolments(999);

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }
    }
}