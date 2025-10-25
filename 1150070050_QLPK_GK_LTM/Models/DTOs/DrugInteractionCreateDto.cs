namespace _1150070050_QLPK_GK_LTM.Models.DTOs
{
    public class DrugInteractionCreateDto
    {
        public int MedicineId1 { get; set; }
        public int MedicineId2 { get; set; }
        public string? Severity { get; set; }
        public string? Description { get; set; }
    }

}
