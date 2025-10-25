using System;
using System.Collections.Generic;

namespace _1150070050_QLPK_GK_LTM.Models.Entities;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int? SenderId { get; set; }

    public int? ReceiverId { get; set; }

    public string? Title { get; set; }

    public string? Message { get; set; }

    public string? NotificationType { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsRead { get; set; }

    public virtual User? Receiver { get; set; }

    public virtual User? Sender { get; set; }
}
