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

        private static void SetUser(
            ResultsController controller,
            string? userId = null)
        {
            ClaimsIdentity identity;

            if (userId == null)
            {
                identity = new ClaimsIdentity();
            }
            else
            {
                identity = new ClaimsIdentity(
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
                    User = new ClaimsPrincipal(identity)
                }
            };
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

                SetUser(controller, "1");

                var result = await controller.GetEventResults(999);

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }
    }
}