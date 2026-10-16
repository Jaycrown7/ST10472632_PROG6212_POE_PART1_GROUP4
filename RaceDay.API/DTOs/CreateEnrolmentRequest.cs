using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class CreateEnrolmentRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid category ID is required.")]
        public int CategoryId { get; set; }
    }
}
