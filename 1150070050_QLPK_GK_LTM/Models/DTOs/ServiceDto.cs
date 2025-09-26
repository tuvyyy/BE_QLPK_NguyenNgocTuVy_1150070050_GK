namespace _1150070050_QLPK_GK_LTM.Models.DTOs
{
    public class ServiceDto
    {
        public int Id { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string PriceFormatted => string.Format("{0:#,##0} VNĐ", Price);
    }

}
