using System;
using System.Collections.Generic;

namespace _1150070050_QLPK_GK_LTM.Models.Entities;

public partial class DrugInteraction
{
    public int Id { get; set; }

    public int MedicineId1 { get; set; }

    public int MedicineId2 { get; set; }

    public string? Severity { get; set; }

    public string? Description { get; set; }

    public virtual Medicine MedicineId1Navigation { get; set; } = null!;

    public virtual Medicine MedicineId2Navigation { get; set; } = null!;
}
