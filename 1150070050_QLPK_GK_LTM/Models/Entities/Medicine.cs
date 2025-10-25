using System;
using System.Collections.Generic;

namespace _1150070050_QLPK_GK_LTM.Models.Entities;

public partial class Medicine
{
    public int MedicineId { get; set; }

    public string MedicineName { get; set; } = null!;

    public string? Description { get; set; }

    public string? Unit { get; set; }

    public int? MaxDosagePerDay { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<DrugInteraction> DrugInteractionMedicineId1Navigations { get; set; } = new List<DrugInteraction>();

    public virtual ICollection<DrugInteraction> DrugInteractionMedicineId2Navigations { get; set; } = new List<DrugInteraction>();

    public virtual ICollection<MedicineAlternative> MedicineAlternativeAlternativeMedicines { get; set; } = new List<MedicineAlternative>();

    public virtual ICollection<MedicineAlternative> MedicineAlternativeMedicines { get; set; } = new List<MedicineAlternative>();

    public virtual ICollection<PatientAllergy> PatientAllergies { get; set; } = new List<PatientAllergy>();

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
