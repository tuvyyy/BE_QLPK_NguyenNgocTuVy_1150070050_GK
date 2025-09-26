using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace _1150070050_QLPK_GK_LTM.Controllers.Doctors
{
    public class DoctorCreateDto
    {
        [Required, MaxLength(150)]
        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = null!;

        [MaxLength(100)]
        [JsonPropertyName("specialty")]
        public string? Specialty { get; set; }

        [MaxLength(20)]
        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [MaxLength(20)]
        [JsonPropertyName("roomCode")]
        public string? RoomCode { get; set; }

        [JsonPropertyName("maxDailyAppointments")]
        public int? MaxDailyAppointments { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;
    }

    public class DoctorUpdateDto : DoctorCreateDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class DoctorListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = null!;

        [JsonPropertyName("specialty")]
        public string? Specialty { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("roomCode")]
        public string? RoomCode { get; set; }

        [JsonPropertyName("maxDailyAppointments")]
        public int? MaxDailyAppointments { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }

    public class PagedResult<T>
    {
        [JsonPropertyName("items")]
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }
    }
}
