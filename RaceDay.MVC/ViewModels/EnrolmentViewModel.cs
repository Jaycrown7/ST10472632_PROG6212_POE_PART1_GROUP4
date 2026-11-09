using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.ViewModels
{
    public class EnrolmentViewModel
    {
        [Range(1, int.MaxValue)]
        public int EventId { get; set; }

        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }
    }
}