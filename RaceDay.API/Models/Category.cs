using System;
using System.Collections.Generic;

namespace RaceDay.API.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public int EventId { get; set; }

    public string CategoryName { get; set; } = null!;

    public decimal Distance { get; set; }

    public decimal EntryFee { get; set; }

    public int MaximumParticipants { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();

    public virtual Event Event { get; set; } = null!;
}
