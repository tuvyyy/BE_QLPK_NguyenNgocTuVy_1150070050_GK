namespace _1150070050_QLPK_GK_LTM.Models.DTOs
{
    public class PrescriptionCreateDto
    {
        public int RecordId { get; set; }
        public int MedicineId { get; set; }   // dùng Id thay vì tên
        public string? Dosage { get; set; }
        public string? Instructions { get; set; }
        public int? Duration { get; set; }
    }

    public class PrescriptionResponseDto
    {
        public int PrescriptionId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = null!;
        public string? Dosage { get; set; }
        public string? Instructions { get; set; }
        public int? Duration { get; set; }
        public string RecordCode { get; set; } = null!;
    }
}
