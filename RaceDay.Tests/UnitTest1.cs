using Microsoft.AspNetCore.Identity;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void HashPassword_ShouldNotStorePlainTextPassword()
        {
            // Arrange
            var user = new User();
            var passwordHasher = new PasswordHasher<User>();
            string password = "RaceDayTest123!";


        // Act
        string passwordHash = passwordHasher.HashPassword(user, password);

            // Assert
            Assert.NotEqual(password, passwordHash);
            Assert.False(string.IsNullOrWhiteSpace(passwordHash));
        }

        [Fact]
        public void VerifyHashedPassword_ShouldSucceedForCorrectPassword()
        {
            // Arrange
            var user = new User();
            var passwordHasher = new PasswordHasher<User>();
            string password = "RaceDayTest123!";

            string passwordHash = passwordHasher.HashPassword(user, password);

            // Act
            var result = passwordHasher.VerifyHashedPassword(
                user,
                passwordHash,
                password);

            // Assert
            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Fact]
        public void VerifyHashedPassword_ShouldFailForIncorrectPassword()
        {
            // Arrange
            var user = new User();
            var passwordHasher = new PasswordHasher<User>();

            string passwordHash = passwordHasher.HashPassword(
                user,
                "RaceDayTest123!");

            // Act
            var result = passwordHasher.VerifyHashedPassword(
                user,
                passwordHash,
                "WrongPassword123!");

            // Assert
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }
    }


}
