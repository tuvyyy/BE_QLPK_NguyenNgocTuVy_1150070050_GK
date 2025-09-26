using System;
using System.Collections.Generic;

namespace _1150070050_QLPK_GK_LTM.Models.Entities;

public partial class Patient
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public DateOnly? Dob { get; set; }

    public string? Gender { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
