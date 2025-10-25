using _1150070050_QLPK_GK_LTM.Models.DTOs;
using _1150070050_QLPK_GK_LTM.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicalRecordsController : ControllerBase
    {
        private readonly ClinicDbContext _context;

        public MedicalRecordsController(ClinicDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var records = await _context.MedicalRecords
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .Select(r => new
                {
                    r.RecordId,
                    r.RecordCode,
                    r.PatientId,
                    PatientName = r.Patient.FullName,
                    r.DoctorId,
                    DoctorName = r.Doctor.FullName,
                    r.Diagnosis,
                    r.Treatment,
                    r.VisitDate,
                    r.NextAppointment,
                    r.Status,
                    r.CreatedAt,
                    r.IsResultSent
                })
                .ToListAsync();

            return Ok(records);
        }

        // ✅ 3️⃣ Lấy hồ sơ cơ bản (chỉ dữ liệu chính)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBasic(int id)
        {
            var record = await _context.MedicalRecords
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.RecordId == id);

            if (record == null)
                return NotFound(new { message = $"Không tìm thấy hồ sơ id={id}" });

            var dto = new MedicalRecordResponseDto
            {
                RecordId = record.RecordId,
                RecordCode = record.RecordCode,
                PatientId = record.PatientId,
                PatientName = record.Patient.FullName,
                DoctorId = record.DoctorId,
                DoctorName = record.Doctor.FullName,
                Diagnosis = record.Diagnosis,
                Treatment = record.Treatment,
                VisitDate = record.VisitDate,
                NextAppointment = record.NextAppointment,
                Status = record.Status,
                CreatedAt = record.CreatedAt,
                IsResultSent = record.IsResultSent
            };

            return Ok(dto);
        }


        // =========================================================
        // ✅ 3️⃣ Tạo mới hồ sơ (có Appointment + Notification)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MedicalRecordCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Thiếu/không hợp lệ dữ liệu hồ sơ.", modelState = ModelState });

            // Kiểm tra FK trước
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == dto.PatientId);
            if (patient == null)
                return BadRequest(new { message = $"PatientId={dto.PatientId} không tồn tại." });

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == dto.DoctorId);
            if (doctor == null)
                return BadRequest(new { message = $"DoctorId={dto.DoctorId} không tồn tại." });

            // 🔹 Sinh mã hồ sơ MR-YYYYMMDD-000X
            string today = DateTime.Now.ToString("yyyyMMdd");
            var lastCode = await _context.MedicalRecords
                .Where(r => r.RecordCode.StartsWith("MR-" + today))
                .OrderByDescending(r => r.RecordCode)
                .Select(r => r.RecordCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastCode))
            {
                var parts = lastCode.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int currentNumber))
                    nextNumber = currentNumber + 1;
            }
            string recordCode = $"MR-{today}-{nextNumber:D4}";

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 🔹 Tạo hồ sơ chính
                var record = new MedicalRecord
                {
                    RecordCode = recordCode,
                    PatientId = dto.PatientId,
                    DoctorId = dto.DoctorId,
                    Diagnosis = dto.Diagnosis,
                    Treatment = dto.Treatment,
                    VisitDate = dto.VisitDate == default ? DateTime.Now : dto.VisitDate,
                    NextAppointment = dto.NextAppointment,
                    Status = string.IsNullOrWhiteSpace(dto.Status) ? "Đang điều trị" : dto.Status,
                    CreatedAt = DateTime.Now
                };

                _context.MedicalRecords.Add(record);
                await _context.SaveChangesAsync();

                bool appointmentCreated = false;
                bool notificationCreated = false;

                // 🔹 Tạo lịch hẹn nếu có NextAppointment
                if (dto.NextAppointment.HasValue)
                {
                    var appointment = new Appointment
                    {
                        PatientId = dto.PatientId,
                        DoctorId = dto.DoctorId,
                        ServiceId = dto.ServiceId > 0 ? dto.ServiceId : 1,
                        AppointmentDate = dto.NextAppointment.Value,
                        Status = "Scheduled",
                        CreatedAt = DateTime.Now
                    };
                    _context.Appointments.Add(appointment);
                    await _context.SaveChangesAsync();
                    appointmentCreated = true;
                }

                // 🔹 Tạo thông báo cho bệnh nhân nếu có UserId
                if (patient.UserId.HasValue)
                {
                    var notification = new Notification
                    {
                        ReceiverId = patient.UserId.Value,
                        Title = "Hồ sơ bệnh án mới",
                        Message = $"Bạn vừa có hồ sơ bệnh án {recordCode}.",
                        NotificationType = "MedicalRecord",
                        CreatedAt = DateTime.Now,
                        IsRead = false
                    };
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                    notificationCreated = true;
                }

                await tx.CommitAsync();

                return Ok(new
                {
                    message = "✅ Tạo hồ sơ thành công!",
                    recordId = record.RecordId,
                    recordCode,
                    appointmentCreated,
                    notificationCreated
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "❌ Lỗi tạo hồ sơ", detail = ex.Message });
            }
        }

        // =========================================================
        // ✅ Cập nhật hồ sơ
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] MedicalRecordUpdateDto dto)
        {
            var record = await _context.MedicalRecords.FindAsync(id);
            if (record == null)
                return NotFound(new { message = $"Không tìm thấy hồ sơ id={id}" });

            record.Diagnosis = dto.Diagnosis ?? record.Diagnosis;
            record.Treatment = dto.Treatment ?? record.Treatment;
            record.NextAppointment = dto.NextAppointment ?? record.NextAppointment;
            record.Status = dto.Status ?? record.Status;

            await _context.SaveChangesAsync();
            return Ok(new { message = "✅ Cập nhật hồ sơ thành công!" });
        }

        // =========================================================
        // ✅ Xóa hồ sơ
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.MedicalRecords.FindAsync(id);
            if (record == null)
                return NotFound(new { message = "Không tìm thấy hồ sơ cần xóa." });

            _context.MedicalRecords.Remove(record);
            await _context.SaveChangesAsync();
            return Ok(new { message = "🗑️ Đã xóa hồ sơ bệnh án!" });
        }


        // ✅ Ký toa thuốc (chốt hồ sơ, không cho sửa)
        [HttpPost("sign/{recordId}")]
        public async Task<IActionResult> SignPrescription(int recordId)
        {
            var record = await _context.MedicalRecords.FindAsync(recordId);
            if (record == null)
                return NotFound(new { message = "❌ Không tìm thấy hồ sơ!" });

            if (record.IsSigned == true)
                return BadRequest(new { message = "⚠️ Hồ sơ đã được ký!" });

            record.IsSigned = true;
            record.SignedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Hồ sơ đã được bác sĩ ký xác nhận!", recordId, record.SignedAt });
        }

        // ✅ Hủy ký (cho phép sửa/xóa lại)
        [HttpPost("unsign/{recordId}")]
        public async Task<IActionResult> UnsignPrescription(int recordId)
        {
            var record = await _context.MedicalRecords.FindAsync(recordId);
            if (record == null)
                return NotFound(new { message = "❌ Không tìm thấy hồ sơ!" });

            if (record.IsSigned == false)
                return BadRequest(new { message = "⚠️ Hồ sơ chưa được ký!" });

            record.IsSigned = false;
            record.SignedAt = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "🌀 Đã hủy ký, cho phép chỉnh sửa toa thuốc!", recordId });
        }


        // =========================================================
        // ✅ Lấy tất cả hồ sơ của một bệnh nhân
        [HttpGet("by-patient/{patientId}")]
        public async Task<IActionResult> GetByPatient(int patientId)
        {
            var records = await _context.MedicalRecords
                .Include(r => r.Doctor)
                .Where(r => r.PatientId == patientId)
                .Select(r => new
                {
                    r.RecordId,
                    r.RecordCode,
                    r.PatientId,
                    DoctorName = r.Doctor.FullName,
                    r.Diagnosis,
                    r.Treatment,
                    r.VisitDate,
                    r.NextAppointment,
                    r.Status,
                    r.CreatedAt
                })
                .ToListAsync();

            if (!records.Any())
                return NotFound(new { message = "Không có hồ sơ nào cho bệnh nhân này." });

            return Ok(records);
        }
    }
}
