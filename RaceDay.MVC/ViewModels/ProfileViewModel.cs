using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.ViewModels
{
    public class ProfileViewModel
    {
        public int UserId { get; set; }

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";

        [Phone]
        public string? PhoneNumber { get; set; }

        public string? City { get; set; }

        public string? EmergencyContact { get; set; }

        public string? ProfileImageUrl { get; set; }

        public IFormFile? ProfileImage { get; set; }
    }
}