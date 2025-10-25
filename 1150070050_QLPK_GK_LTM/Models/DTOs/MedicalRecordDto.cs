namespace _1150070050_QLPK_GK_LTM.Models.DTOs
{
    // ✅ Dùng khi tạo hồ sơ bệnh án mới
    public class MedicalRecordCreateDto
    {
        public string? RecordCode { get; set; }   // Cho phép null -> server tự sinh
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int ServiceId { get; set; }   // 🔹 thêm để tạo Appointment
        public string? Diagnosis { get; set; }
        public string? Treatment { get; set; }
        public DateTime? VisitDate { get; set; }
        public DateTime? NextAppointment { get; set; }
        public string? Status { get; set; }
    }

    // ✅ Dùng khi cập nhật hồ sơ
    public class MedicalRecordUpdateDto
    {
        public string? Diagnosis { get; set; }
        public string? Treatment { get; set; }
        public DateTime? NextAppointment { get; set; }
        public string? Status { get; set; }
    }

    // ✅ Dùng khi trả response -> cần có RecordCode
    public class MedicalRecordResponseDto
    {
        public int RecordId { get; set; }
        public string RecordCode { get; set; } = null!;  // Bổ sung property này
        public int PatientId { get; set; }
        public string PatientName { get; set; } = null!;
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = null!;
        public string? Diagnosis { get; set; }
        public string? Treatment { get; set; }
        public DateTime? VisitDate { get; set; }
        public DateTime? NextAppointment { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool IsResultSent { get; set; }

    }
}
