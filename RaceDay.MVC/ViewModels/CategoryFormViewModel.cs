using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.ViewModels
{
    public class CategoryFormViewModel
    {
        public int CategoryId { get; set; }

        public int EventId { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Range(0.01, 1000)]
        public decimal Distance { get; set; }

        [Range(0, 1000000)]
        public decimal EntryFee { get; set; }

        [Range(1, 100000)]
        public int MaximumParticipants { get; set; }
    }
}