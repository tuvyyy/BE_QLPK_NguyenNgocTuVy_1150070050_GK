using System;
using System.Collections.Generic;

namespace _1150070050_QLPK_GK_LTM.Models.Entities;

public partial class MedicalRecord
{
    public int RecordId { get; set; }

    public string RecordCode { get; set; } = null!;

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public string? Diagnosis { get; set; }

    public string? Treatment { get; set; }

    public DateTime? VisitDate { get; set; }

    public DateTime? NextAppointment { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsSigned { get; set; }

    public DateTime? SignedAt { get; set; }

    public bool IsResultSent { get; set; }

    public virtual ICollection<DeleteRequest> DeleteRequests { get; set; } = new List<DeleteRequest>();

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}
