using System;
using System.Collections.Generic;

namespace _1150070050_QLPK_GK_LTM.Models.Entities;

public partial class DeleteRequest
{
    public int RequestId { get; set; }

    public int RecordId { get; set; }

    public int RequestedBy { get; set; }

    public string? Reason { get; set; }

    public DateTime RequestedAt { get; set; }

    public string Status { get; set; } = null!;

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual MedicalRecord Record { get; set; } = null!;

    public virtual User RequestedByNavigation { get; set; } = null!;
}
