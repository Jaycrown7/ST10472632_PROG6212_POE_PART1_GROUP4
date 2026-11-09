namespace RaceDay.MVC.ViewModels
{
    public class ResultViewModel
    {
        public int ResultId { get; set; }

        public TimeOnly FinishTime { get; set; }

        public int Position { get; set; }

        public string ResultStatus { get; set; } = string.Empty;

        public DateTime RecordedAt { get; set; }

        public ResultEnrolmentViewModel Enrolment { get; set; } = new();
    }

    public class ResultEnrolmentViewModel
    {
        public int EnrolmentId { get; set; }

        public ResultCategoryViewModel Category { get; set; } = new();

        public ResultEventViewModel Event { get; set; } = new();
    }

    public class ResultCategoryViewModel
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public decimal Distance { get; set; }
    }

    public class ResultEventViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public DateOnly EventDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;
    }
}