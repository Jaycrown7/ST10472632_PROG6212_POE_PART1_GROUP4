using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.ViewModels
{
    public class RecordOrganiserResultViewModel
    {
        public int EventId { get; set; }

        [Range(1, int.MaxValue)]
        public int EnrolmentId { get; set; }

        [Required]
        public TimeOnly FinishTime { get; set; }

        [Range(1, int.MaxValue)]
        public int Position { get; set; }

        [Required]
        [StringLength(30)]
        public string ResultStatus { get; set; } = "Completed";
    }
}