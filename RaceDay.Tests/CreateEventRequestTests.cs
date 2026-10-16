using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using RaceDay.API.DTOs;
using Xunit;

namespace RaceDay.Tests
{
    public class CreateEventRequestTests
    {
        private static IList<ValidationResult> ValidateModel(
            CreateEventRequest request)
        {
            var validationResults = new List<ValidationResult>();

            var validationContext = new ValidationContext(request);

            Validator.TryValidateObject(
                request,
                validationContext,
                validationResults,
                validateAllProperties: true);

            return validationResults;
        }

        private static CreateEventRequest CreateValidRequest()
        {
            return new CreateEventRequest
            {
                EventName = "Johannesburg Road Race",
                Description = "A local running event.",
                EventDate = DateOnly.FromDateTime(
                    DateTime.Today.AddDays(30)),
                Location = "Johannesburg",
                Distance = 10m,
                EventType = "Run"
            };
        }

        [Fact]
        public void Validate_ShouldPass_WhenRequestIsValid()
        {
            var request = CreateValidRequest();

            var errors = ValidateModel(request);

            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_ShouldFail_WhenEventNameIsMissing()
        {
            var request = CreateValidRequest();
            request.EventName = string.Empty;

            var errors = ValidateModel(request);

            Assert.Contains(
                errors,
                error => error.MemberNames.Contains("EventName"));
        }

        [Fact]
        public void Validate_ShouldFail_WhenDescriptionIsMissing()
        {
            var request = CreateValidRequest();
            request.Description = string.Empty;

            var errors = ValidateModel(request);

            Assert.Contains(
                errors,
                error => error.MemberNames.Contains("Description"));
        }

        [Fact]
        public void Validate_ShouldFail_WhenLocationIsMissing()
        {
            var request = CreateValidRequest();
            request.Location = string.Empty;

            var errors = ValidateModel(request);

            Assert.Contains(
                errors,
                error => error.MemberNames.Contains("Location"));
        }

        [Fact]
        public void Validate_ShouldFail_WhenDistanceIsZero()
        {
            var request = CreateValidRequest();
            request.Distance = 0m;

            var errors = ValidateModel(request);

            Assert.Contains(
                errors,
                error => error.MemberNames.Contains("Distance"));
        }

        [Fact]
        public void Validate_ShouldFail_WhenDistanceIsNegative()
        {
            var request = CreateValidRequest();
            request.Distance = -5m;

            var errors = ValidateModel(request);

            Assert.Contains(
                errors,
                error => error.MemberNames.Contains("Distance"));
        }

        [Fact]
        public void Validate_ShouldFail_WhenDistanceExceedsMaximum()
        {
            var request = CreateValidRequest();
            request.Distance = 1001m;

            var errors = ValidateModel(request);

            Assert.Contains(
                errors,
                error => error.MemberNames.Contains("Distance"));
        }

        [Theory]
        [InlineData("Swim")]
        [InlineData("RUN")]
        [InlineData("running")]
        [InlineData("")]
        public void Validate_ShouldFail_WhenEventTypeIsInvalid(
            string eventType)
        {
            var request = CreateValidRequest();
            request.EventType = eventType;

            var errors = ValidateModel(request);

            Assert.Contains(
                errors,
                error => error.MemberNames.Contains("EventType"));
        }

        [Theory]
        [InlineData("Run")]
        [InlineData("Walk")]
        [InlineData("Cycle")]
        public void Validate_ShouldPass_WhenEventTypeIsSupported(
            string eventType)
        {
            var request = CreateValidRequest();
            request.EventType = eventType;

            var errors = ValidateModel(request);

            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_ShouldFail_WhenEventNameExceedsMaximumLength()
        {
            var request = CreateValidRequest();
            request.EventName = new string('A', 101);

            var errors = ValidateModel(request);

            Assert.Contains(
                errors,
                error => error.MemberNames.Contains("EventName"));
        }
    }
}