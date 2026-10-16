using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Services;
using Xunit;

namespace RaceDay.Tests
{
    public class AuthServiceTests
    {
        private static RaceDayDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var context = new RaceDayDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            return context;
        }

        [Fact]
        
        public async Task RegisterAsync_ShouldRejectDuplicateEmail()
        {
            await using var context = CreateContext();
            var service = new AuthService(context);

            var request = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "Runner",
                Email = "runner@example.com",
                Password = "RaceDayTest123!",
                City = "Johannesburg"
            };

            // Arrange: Register the account successfully first.
            await service.RegisterAsync(request);

            // Act and Assert: Registering the same email again must fail.
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.RegisterAsync(request));

            // Confirm only one user was created.
            Assert.Equal(1, await context.Users.CountAsync());
        }

        [Fact]
        public async Task RegisterAsync_ShouldCreateParticipantAndNormalizeEmail()
        {
            await using var context = CreateContext();
            var service = new AuthService(context);

            var request = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "Runner",
                Email = " RUNNER@EXAMPLE.COM ",
                Password = "RaceDayTest123!",
                City = "Johannesburg"
            };

            var response = await service.RegisterAsync(request);

            Assert.Equal("runner@example.com", response.Email);
            Assert.Equal("Participant", response.Role);
            Assert.NotNull(await context.Participants.FindAsync(response.UserId));

            var user = await context.Users.SingleAsync(
                u => u.UserId == response.UserId);

            Assert.NotEqual(request.Password, user.PasswordHash);
        }

        [Fact]
        public async Task LoginAsync_ShouldSucceedWithCorrectCredentials()
        {
            await using var context = CreateContext();
            var service = new AuthService(context);

            await service.RegisterAsync(new RegisterRequest
            {
                FirstName = "Test",
                LastName = "Runner",
                Email = "runner@example.com",
                Password = "RaceDayTest123!"
            });

            var response = await service.LoginAsync(new LoginRequest
            {
                Email = "RUNNER@EXAMPLE.COM",
                Password = "RaceDayTest123!"
            });

            Assert.NotNull(response);
            Assert.Equal("runner@example.com", response.Email);
        }

        [Fact]
        public async Task LoginAsync_ShouldRejectIncorrectPassword()
        {
            await using var context = CreateContext();
            var service = new AuthService(context);

            await service.RegisterAsync(new RegisterRequest
            {
                FirstName = "Test",
                LastName = "Runner",
                Email = "runner@example.com",
                Password = "RaceDayTest123!"
            });

            var response = await service.LoginAsync(new LoginRequest
            {
                Email = "runner@example.com",
                Password = "WrongPassword123!"
            });

            Assert.Null(response);
        }

        [Fact]
        public async Task LoginAsync_ShouldRejectUnknownEmail()
        {
            await using var context = CreateContext();
            var service = new AuthService(context);

            var response = await service.LoginAsync(new LoginRequest
            {
                Email = "unknown@example.com",
                Password = "RaceDayTest123!"
            });

            Assert.Null(response);
        }
    }

}
