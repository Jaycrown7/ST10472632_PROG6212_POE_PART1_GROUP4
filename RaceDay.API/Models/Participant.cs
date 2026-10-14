using System;
using System.Collections.Generic;

namespace RaceDay.API.Models;

public partial class Participant
{
    public int UserId { get; set; }

    public virtual ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();

    public virtual User User { get; set; } = null!;
}
