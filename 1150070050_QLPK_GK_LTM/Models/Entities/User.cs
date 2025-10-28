using System;
using System.Collections.Generic;

namespace _1150070050_QLPK_GK_LTM.Models.Entities;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Email { get; set; }

    public string? Role { get; set; }

    public string? OtpCode { get; set; }

    public DateTime? OtpExpiry { get; set; }

    public string? GoogleId { get; set; }

    public string? LoginProvider { get; set; }

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public virtual ICollection<DeleteRequest> DeleteRequestApprovedByNavigations { get; set; } = new List<DeleteRequest>();

    public virtual ICollection<DeleteRequest> DeleteRequestRequestedByNavigations { get; set; } = new List<DeleteRequest>();

    public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();

    public virtual ICollection<Notification> NotificationReceivers { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> NotificationSenders { get; set; } = new List<Notification>();

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
