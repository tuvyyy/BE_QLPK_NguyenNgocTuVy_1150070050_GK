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
}
