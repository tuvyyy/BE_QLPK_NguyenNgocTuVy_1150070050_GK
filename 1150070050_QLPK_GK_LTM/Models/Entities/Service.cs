using System;
using System.Collections.Generic;

namespace _1150070050_QLPK_GK_LTM.Models.Entities;

public partial class Service
{
    public int Id { get; set; }

    public string ServiceName { get; set; } = null!;

    public decimal Price { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
