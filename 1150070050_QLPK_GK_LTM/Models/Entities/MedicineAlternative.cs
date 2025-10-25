using System;
using System.Collections.Generic;

namespace _1150070050_QLPK_GK_LTM.Models.Entities;

public partial class MedicineAlternative
{
    public int Id { get; set; }

    public int MedicineId { get; set; }

    public int AlternativeMedicineId { get; set; }

    public string? Notes { get; set; }

    public virtual Medicine AlternativeMedicine { get; set; } = null!;

    public virtual Medicine Medicine { get; set; } = null!;
}
