using System;
using System.Collections.Generic;

namespace _1150070050_QLPK_GK_LTM.Models.Entities;

public partial class Prescription
{
    public int PrescriptionId { get; set; }

    public int RecordId { get; set; }

    public string? Dosage { get; set; }

    public string? Instructions { get; set; }

    public int? Duration { get; set; }

    public int MedicineId { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual MedicalRecord Record { get; set; } = null!;
}
