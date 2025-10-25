using System;
using System.Collections.Generic;

namespace _1150070050_QLPK_GK_LTM.Models.Entities;

public partial class TestResult
{
    public int ResultId { get; set; }

    public int RecordId { get; set; }

    public string? TestType { get; set; }

    public string? ResultSummary { get; set; }

    public string? FileUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? DoctorId { get; set; }

    public bool IsSigned { get; set; }

    public DateTime? SignedAt { get; set; }

    public string? IndicatorsJson { get; set; }

    public bool IsResultSent { get; set; }

    public virtual MedicalRecord Record { get; set; } = null!;
}
