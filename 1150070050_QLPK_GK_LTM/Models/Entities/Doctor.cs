using System;
using System.Collections.Generic;

namespace _1150070050_QLPK_GK_LTM.Models.Entities;

public partial class Doctor
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string? Specialty { get; set; }

    public string? Phone { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
