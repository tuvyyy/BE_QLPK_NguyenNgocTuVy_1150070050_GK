namespace _1150070050_QLPK_GK_LTM.Models.DTOs
{
    public class AppointmentResponseDto
    {
        public int Id { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string? Status { get; set; }

        public string PatientName { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string ServiceName { get; set; } = "";
    }
}
