using System;
using System.Collections.Generic;

namespace _1150070050_QLPK_GK_LTM.Models.Entities;

public partial class PatientAllergy
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public int MedicineId { get; set; }

    public string? Notes { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;
}
