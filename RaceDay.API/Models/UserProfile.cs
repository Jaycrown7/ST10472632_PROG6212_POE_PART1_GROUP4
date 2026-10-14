using System;
using System.Collections.Generic;

namespace RaceDay.API.Models;

public partial class UserProfile
{
    public int ProfileId { get; set; }

    public int UserId { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? EmergencyContact { get; set; }

    public string? ProfileImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
