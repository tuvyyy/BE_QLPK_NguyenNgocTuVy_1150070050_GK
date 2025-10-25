using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using _1150070050_QLPK_GK_LTM.Models.Entities;
using _1150070050_QLPK_GK_LTM.Models.DTOs;

namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly ClinicDbContext _context;

        public AppointmentsController(ClinicDbContext context)
        {
            _context = context;
        }

        // Helper: map sang DTO hiển thị
        private static AppointmentResponseDto ToDto(Appointment a) => new AppointmentResponseDto
        {
            Id = a.Id,
            AppointmentDate = a.AppointmentDate,
            Status = a.Status,
            PatientName = a.Patient?.FullName ?? "",
            DoctorName = a.Doctor?.FullName ?? "",
            ServiceName = a.Service?.ServiceName ?? ""
        };

        // Helper: chuẩn hoá status null => "Scheduled"
        private static string NormalizeStatus(string? s) =>
            string.IsNullOrWhiteSpace(s) ? "Scheduled" : s.Trim();

        // ================== GET LIST ==================
        [HttpGet]
        public async Task<IActionResult> GetAppointments(
            [FromQuery] DateTime? date = null,
            [FromQuery] int? doctorId = null,
            [FromQuery] int? patientId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 50;

            var q = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .AsQueryable();

            if (date.HasValue)
            {
                var d0 = date.Value.Date;
                var d1 = d0.AddDays(1);
                q = q.Where(a => a.AppointmentDate >= d0 && a.AppointmentDate < d1);
            }
            if (doctorId.HasValue) q = q.Where(a => a.DoctorId == doctorId.Value);
            if (patientId.HasValue) q = q.Where(a => a.PatientId == patientId.Value);

            q = q.OrderBy(a => a.AppointmentDate).ThenBy(a => a.Id);

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new
            {
                items = items.Select(ToDto),
                total,
                page,
                pageSize,
                hasNext = page * pageSize < total
            });
        }

        // ================== ✅ NEW ENDPOINT ==================
        // GET /api/Appointments/by-patient/{patientId}
        [HttpGet("by-patient/{patientId:int}")]
        public async Task<IActionResult> GetByPatient(int patientId)
        {
            var list = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            if (!list.Any())
                return NotFound(new { message = "Bệnh nhân này chưa có lịch hẹn." });

            return Ok(list.Select(ToDto));
        }

        // ================== GET BY ID ==================
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AppointmentResponseDto>> GetById(int id)
        {
            var a = await _context.Appointments
                .Include(x => x.Patient)
                .Include(x => x.Doctor)
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();
            return ToDto(a);
        }

        // ================== CREATE ==================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AppointmentDto dto)
        {
            if (dto.PatientId <= 0 || dto.DoctorId <= 0 || dto.ServiceId <= 0)
                return BadRequest(new { message = "Thiếu PatientId/DoctorId/ServiceId." });

            var okPatient = await _context.Patients.AnyAsync(x => x.Id == dto.PatientId);
            var okDoctor = await _context.Doctors.AnyAsync(x => x.Id == dto.DoctorId);
            var okService = await _context.Services.AnyAsync(x => x.Id == dto.ServiceId);
            if (!okPatient || !okDoctor || !okService)
                return BadRequest(new { message = "Bệnh nhân/Bác sĩ/Dịch vụ không tồn tại." });

            var conflict = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == dto.DoctorId &&
                a.AppointmentDate == dto.AppointmentDate &&
                a.Status != "Canceled");
            if (conflict)
                return Conflict(new { message = "Bác sĩ đã có lịch tại thời điểm này." });

            var entity = new Appointment
            {
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                ServiceId = dto.ServiceId,
                AppointmentDate = dto.AppointmentDate,
                Status = NormalizeStatus(dto.Status)
            };

            _context.Appointments.Add(entity);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsSqlUniqueConflict(ex))
            {
                return Conflict(new { message = "Bác sĩ đã có lịch tại thời điểm này." });
            }

            await _context.Entry(entity).Reference(e => e.Patient).LoadAsync();
            await _context.Entry(entity).Reference(e => e.Doctor).LoadAsync();
            await _context.Entry(entity).Reference(e => e.Service).LoadAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
        }

        // ================== UPDATE ==================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] AppointmentDto dto)
        {
            var a = await _context.Appointments.FindAsync(id);
            if (a == null) return NotFound(new { message = "Không tìm thấy lịch hẹn." });

            var okPatient = await _context.Patients.AnyAsync(x => x.Id == dto.PatientId);
            var okDoctor = await _context.Doctors.AnyAsync(x => x.Id == dto.DoctorId);
            var okService = await _context.Services.AnyAsync(x => x.Id == dto.ServiceId);
            if (!okPatient || !okDoctor || !okService)
                return BadRequest(new { message = "Bệnh nhân/Bác sĩ/Dịch vụ không tồn tại." });

            var conflict = await _context.Appointments.AnyAsync(x =>
                x.Id != id &&
                x.DoctorId == dto.DoctorId &&
                x.AppointmentDate == dto.AppointmentDate &&
                x.Status != "Canceled");
            if (conflict)
                return Conflict(new { message = "Bác sĩ đã có lịch tại thời điểm này." });

            a.PatientId = dto.PatientId;
            a.DoctorId = dto.DoctorId;
            a.ServiceId = dto.ServiceId;
            a.AppointmentDate = dto.AppointmentDate;
            a.Status = NormalizeStatus(dto.Status);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsSqlUniqueConflict(ex))
            {
                return Conflict(new { message = "Bác sĩ đã có lịch tại thời điểm này." });
            }

            return Ok(new { message = "Cập nhật thành công." });
        }

        // ================== UPDATE STATUS ==================
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return BadRequest(new { message = "Thiếu trạng thái." });

            var a = await _context.Appointments.FindAsync(id);
            if (a == null) return NotFound(new { message = "Không tìm thấy lịch hẹn." });

            a.Status = value.Trim();
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã cập nhật trạng thái." });
        }

        // ================== DELETE ==================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var a = await _context.Appointments.FindAsync(id);
            if (a == null) return NotFound();

            _context.Appointments.Remove(a);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ================== EXTRAS ==================
        [HttpGet("statuses")]
        public IActionResult GetStatuses() =>
            Ok(new[] { "Scheduled", "CheckedIn", "InExam", "Completed", "Canceled" });

        [HttpGet("today")]
        public async Task<IActionResult> Today([FromQuery] int? doctorId = null)
        {
            var d0 = DateTime.Today;
            var d1 = d0.AddDays(1);
            var q = _context.Appointments
                .Include(x => x.Patient).Include(x => x.Doctor).Include(x => x.Service)
                .Where(x => x.AppointmentDate >= d0 && x.AppointmentDate < d1);

            if (doctorId.HasValue) q = q.Where(x => x.DoctorId == doctorId.Value);

            var list = await q.OrderBy(x => x.AppointmentDate).ToListAsync();
            return Ok(list.Select(ToDto));
        }

        // ================== Utilities ==================
        private static bool IsSqlUniqueConflict(DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx)
                return sqlEx.Number == 2601 || sqlEx.Number == 2627;
            return false;
        }
    }
}
