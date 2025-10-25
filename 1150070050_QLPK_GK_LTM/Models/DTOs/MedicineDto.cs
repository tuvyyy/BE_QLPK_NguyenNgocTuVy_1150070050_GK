namespace _1150070050_QLPK_GK_LTM.Models.DTOs
{
    public class MedicineDto
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string? Description { get; set; }
        public string? Unit { get; set; }
        public int? MaxDosagePerDay { get; set; }
        public bool? IsActive { get; set; }
    }

}
