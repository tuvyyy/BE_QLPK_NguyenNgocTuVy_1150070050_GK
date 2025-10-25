using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _1150070050_QLPK_GK_LTM.Models.Entities;
using System.Text.RegularExpressions;

namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorsController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        public DoctorsController(ClinicDbContext context) { _context = context; }
        private static string NormalizeVnPhone(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            // giữ lại chỉ chữ số
            var digits = Regex.Replace(input, @"\D", "");
            // +84 / 84xxxxx -> 0xxxxx
            if (digits.StartsWith("84") && digits.Length == 11) digits = "0" + digits.Substring(2);
            return digits;
        }

        private static bool IsValidVnMobile(string phone)
            => Regex.IsMatch(phone, @"^0\d{9}$");  // đúng 10 số, bắt đầu 0

        // ==================== LIST (search + paging + sort) ====================
        // GET api/Doctors?q=&page=&pageSize=&sort=
        // sort: created_desc|created_asc|name_asc|name_desc
        [HttpGet]
        public async Task<ActionResult<object>> GetDoctors(
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sort = "created_desc")
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var query = _context.Doctors.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim();
                query = query.Where(d =>
                    d.FullName.Contains(kw) ||
                    (d.Phone != null && d.Phone.Contains(kw)) ||
                    (d.Specialty != null && d.Specialty.Contains(kw)));
            }

            query = sort switch
            {
                "name_asc" => query.OrderBy(d => d.FullName),
                "name_desc" => query.OrderByDescending(d => d.FullName),
                "created_asc" => query.OrderBy(d => d.Id),      // dùng Id thay CreatedAt
                _ => query.OrderByDescending(d => d.Id)         // created_desc
            };

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new {
                    id = d.Id,
                    fullName = d.FullName,
                    specialty = d.Specialty,
                    phone = d.Phone
                })
                .ToListAsync();

            return Ok(new { items, total, page, pageSize });
        }

        // ==================== DETAIL ====================
        // GET api/Doctors/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> GetDoctor(int id)
        {
            var d = await _context.Doctors.FindAsync(id);
            if (d == null) return NotFound();
            return Ok(new
            {
                id = d.Id,
                fullName = d.FullName,
                specialty = d.Specialty,
                phone = d.Phone
            });
        }

        // ==================== CREATE ====================
        // POST api/Doctors
        [HttpPost]
        public async Task<ActionResult<Doctor>> CreateDoctor([FromBody] Doctor dto)
        {
            var fullName = (dto.FullName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                return BadRequest(new { message = "FullName là bắt buộc." });

            var phoneNorm = NormalizeVnPhone(dto.Phone);

            if (!string.IsNullOrEmpty(phoneNorm) && !IsValidVnMobile(phoneNorm))
                return BadRequest(new { message = "Số điện thoại không hợp lệ (10 số, bắt đầu 0)." });

            if (!string.IsNullOrEmpty(phoneNorm) &&
                await _context.Doctors.AnyAsync(x => x.Phone == phoneNorm))
                return Conflict(new { message = "SĐT đã tồn tại." });

            var entity = new Doctor
            {
                FullName = fullName,
                Specialty = string.IsNullOrWhiteSpace(dto.Specialty) ? null : dto.Specialty!.Trim(),
                Phone = string.IsNullOrEmpty(phoneNorm) ? null : phoneNorm
            };

            _context.Doctors.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetDoctor), new { id = entity.Id }, entity);
        }


        // ==================== UPDATE ====================
        // PUT api/Doctors/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, [FromBody] Doctor dto)
        {
            var entity = await _context.Doctors.FindAsync(id);
            if (entity == null) return NotFound(new { message = "Không tìm thấy bác sĩ." });

            var fullName = (dto.FullName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                return BadRequest(new { message = "FullName là bắt buộc." });

            var phoneNorm = NormalizeVnPhone(dto.Phone);

            if (!string.IsNullOrEmpty(phoneNorm) && !IsValidVnMobile(phoneNorm))
                return BadRequest(new { message = "Số điện thoại không hợp lệ (10 số, bắt đầu 0)." });

            if (!string.IsNullOrEmpty(phoneNorm) &&
                await _context.Doctors.AnyAsync(x => x.Phone == phoneNorm && x.Id != id))
                return Conflict(new { message = "SĐT đã tồn tại." });

            entity.FullName = fullName;
            entity.Specialty = string.IsNullOrWhiteSpace(dto.Specialty) ? null : dto.Specialty!.Trim();
            entity.Phone = string.IsNullOrEmpty(phoneNorm) ? null : phoneNorm;

            await _context.SaveChangesAsync();
            return Ok(entity);
        }
        // ==================== DELETE (SAFE) ====================
        // DELETE api/Doctors/{id}
        // Luôn chặn xoá nếu còn Appointments tham chiếu → trả 409.
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var d = await _context.Doctors.FindAsync(id);
            if (d == null) return NotFound();

            var hasAppointments = await _context.Appointments
                .AsNoTracking()
                .AnyAsync(a => a.DoctorId == id);

            if (hasAppointments)
            {
                return Conflict(new
                {
                    message = "Không thể xoá vì bác sĩ đang có lịch hẹn/phiên khám tham chiếu. " +
                              "Hãy chuyển lịch sang bác sĩ khác bằng: PATCH /api/Doctors/{fromId}/reassign?toDoctorId=..."
                });
            }

            _context.Doctors.Remove(d);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ==================== REASSIGN APPOINTMENTS ====================
        // PATCH api/Doctors/{fromId}/reassign?toDoctorId=123
        [HttpPatch("{fromId:int}/reassign")]
        public async Task<IActionResult> ReassignAppointments(int fromId, [FromQuery] int toDoctorId)
        {
            if (fromId == toDoctorId)
                return BadRequest(new { message = "fromId và toDoctorId phải khác nhau." });

            var from = await _context.Doctors.FindAsync(fromId);
            var to = await _context.Doctors.FindAsync(toDoctorId);
            if (from == null || to == null)
                return NotFound(new { message = "Bác sĩ nguồn hoặc bác sĩ đích không tồn tại." });

            var affected = await _context.Appointments.Where(a => a.DoctorId == fromId).ToListAsync();
            foreach (var ap in affected) ap.DoctorId = toDoctorId;

            await _context.SaveChangesAsync();
            return Ok(new { moved = affected.Count });
        }

        // ==================== EXPORT CSV ====================
        // GET api/Doctors/export.csv?q=
        [HttpGet("export.csv")]
        public async Task<IActionResult> ExportCsv([FromQuery] string? q)
        {
            var query = _context.Doctors.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim();
                query = query.Where(d =>
                    d.FullName.Contains(kw) ||
                    (d.Phone != null && d.Phone.Contains(kw)) ||
                    (d.Specialty != null && d.Specialty.Contains(kw)));
            }

            var list = await query.OrderByDescending(d => d.Id).ToListAsync();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Id,FullName,Specialty,Phone");
            foreach (var x in list)
                sb.AppendLine($"{x.Id},\"{x.FullName}\",\"{x.Specialty}\",{x.Phone}");

            var bytes = System.Text.Encoding.UTF8.GetBytes("\uFEFF" + sb.ToString()); // BOM để Excel nhận UTF-8
            return File(bytes, "text/csv; charset=utf-8", "doctors.csv");
        }

        // ==================== SPECIALTIES DROPDOWN ====================
        // GET api/Doctors/specialties
        [HttpGet("specialties")]
        public async Task<ActionResult<IEnumerable<string>>> GetSpecialties()
        {
            var list = await _context.Doctors
                .AsNoTracking()
                .Where(d => d.Specialty != null && d.Specialty != "")
                .Select(d => d.Specialty!)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            return Ok(list);
        }

        // ==================== EXISTS PHONE ====================
        // GET api/Doctors/exists/phone?phone=0912...
        [HttpGet("exists/phone")]
        public async Task<ActionResult<object>> ExistsPhone([FromQuery] string? phone)
        {
            var normalized = NormalizePhone(phone ?? "");
            var valid = Regex.IsMatch(normalized, @"^0\\d{9}$");

            if (!valid)
                return Ok(new { exists = false, valid = false, normalized });

            var exists = await _context.Doctors.AsNoTracking().AnyAsync(x => x.Phone == normalized);
            return Ok(new { exists, valid = true, normalized });
        }

        // ==================== Helpers ====================
        private static string NormalizePhone(string phone) =>
            Regex.Replace(phone ?? "", "[^0-9]", "");
    }
}
