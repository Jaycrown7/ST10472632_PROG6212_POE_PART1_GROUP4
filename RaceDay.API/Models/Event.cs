using System;
using System.Collections.Generic;

namespace RaceDay.API.Models;

public partial class Event
{
    public int EventId { get; set; }

    public int OrganiserId { get; set; }

    public string EventName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateOnly EventDate { get; set; }

    public string Location { get; set; } = null!;

    public decimal Distance { get; set; }

    public string EventType { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual User Organiser { get; set; } = null!;
}
