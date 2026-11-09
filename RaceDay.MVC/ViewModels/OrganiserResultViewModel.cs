using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.ViewModels
{
    public class OrganiserResultViewModel
    {
        public int ResultId { get; set; }

        public int EnrolmentId { get; set; }

        public TimeOnly FinishTime { get; set; }

        public int Position { get; set; }

        public string ResultStatus { get; set; } = "Completed";

        public DateTime RecordedAt { get; set; }

        public ResultParticipantViewModel Participant { get; set; } = new();

        public OrganiserResultCategoryViewModel Category { get; set; } = new();
    }

    public class ResultParticipantViewModel
    {
        public int UserId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }

    public class OrganiserResultCategoryViewModel
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public decimal Distance { get; set; }
    }
}