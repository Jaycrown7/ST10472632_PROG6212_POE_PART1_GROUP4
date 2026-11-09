using System;

namespace RaceDay.MVC.ViewModels
{
    public class OrganiserEnrolmentViewModel
    {
        public int EnrolmentId { get; set; }

        public DateTime EnrolmentDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public EnrolmentCategoryViewModel Category { get; set; } = new();

        public EnrolmentParticipantViewModel Participant { get; set; } = new();
    }

    public class EnrolmentCategoryViewModel
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public decimal Distance { get; set; }
    }

    public class EnrolmentParticipantViewModel
    {
        public int UserId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}