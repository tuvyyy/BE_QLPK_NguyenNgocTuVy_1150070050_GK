namespace _1150070050_QLPK_GK_LTM.Models.DTOs
{
    // 🧾 Dành cho nhân viên gửi yêu cầu xóa
    public class DeleteRequestDto
    {
        public int RequestedBy { get; set; }     // Id user (bác sĩ / điều dưỡng)
        public string Reason { get; set; }       // Lý do xóa
    }

    // ✅ Dành cho admin duyệt
    public class ApproveDeleteDto
    {
        public int AdminId { get; set; }         // Id admin duyệt
        public bool IsApproved { get; set; }     // true = duyệt, false = từ chối
    }
}
