namespace RaceDay.MVC.ViewModels
{
    public class MyEnrolmentViewModel
    {
        public int EnrolmentId { get; set; }

        public DateTime EnrolmentDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public CategoryViewModel Category { get; set; } = new();

        public EnrolledEventViewModel Event { get; set; } = new();
    }

    public class EnrolledEventViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public DateOnly EventDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;
    }
}