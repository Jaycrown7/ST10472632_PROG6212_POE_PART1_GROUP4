
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class RecordResultRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid enrolment ID is required.")]
        public int EnrolmentId { get; set; }

        [Required]
        public TimeOnly FinishTime { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Position must be at least 1.")]
        public int Position { get; set; }

        [Required]
        [StringLength(30)]
        public string ResultStatus { get; set; } = "Completed";
    }
}

