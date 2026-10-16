using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class CreateCategoryRequest
    {
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
