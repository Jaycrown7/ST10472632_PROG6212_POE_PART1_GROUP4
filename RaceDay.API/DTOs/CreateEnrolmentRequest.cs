using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class CreateEnrolmentRequest
    {
        [Range(1, int.MaxValue)]
        public int EventId { get; set; }

    [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }
    }


}
