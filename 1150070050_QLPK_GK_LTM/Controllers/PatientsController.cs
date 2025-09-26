using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using _1150070050_QLPK_GK_LTM.Models.Entities;

namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly tuvyContext _context;
        public PatientsController(tuvyContext context) => _context = context;

        // ==== Helpers: chuẩn hoá & validate SĐT VN ====
        private static string NormalizeVnPhone(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var digits = Regex.Replace(input, @"\D", "");       // giữ số
            if (digits.StartsWith("84") && digits.Length == 11) // 84xxxxxxxxx -> 0xxxxxxxxx
                digits = "0" + digits.Substring(2);
            return digits;
        }
        private static bool IsValidVnMobile(string phone) => Regex.IsMatch(phone, @"^0\d{9}$");

        // ==== GET: /api/Patients?page=1&pageSize=20&sort=name_asc&q=...
        [HttpGet]
        public async Task<IActionResult> GetPatients([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
                                                     [FromQuery] string? sort = "name_asc", [FromQuery] string? q = null)
        {
            var query = _context.Patients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                query = query.Where(x =>
                    x.FullName.ToLower().Contains(q) ||
                    (x.Phone != null && x.Phone.Contains(q)) ||
                    (x.Address != null && x.Address.ToLower().Contains(q)));
            }

            switch ((sort ?? "").ToLower())
            {
                case "name_desc": query = query.OrderByDescending(x => x.FullName); break;
                default: query = query.OrderBy(x => x.FullName); break;
            }

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new
            {
                items,
                total,
                page,
                pageSize,
                hasNext = page * pageSize < total
            });
        }

        // ==== GET: /api/Patients/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatient(int id)
        {
            var p = await _context.Patients.FindAsync(id);
            if (p == null) return NotFound();
            return p;
        }

        // ==== GET: /api/Patients/exists/phone?phone=09...
        [HttpGet("exists/phone")]
        public async Task<IActionResult> ExistsPhone([FromQuery] string phone)
        {
            var norm = NormalizeVnPhone(phone);
            var valid = !string.IsNullOrEmpty(norm) && IsValidVnMobile(norm);
            var exists = valid && await _context.Patients.AnyAsync(x => x.Phone == norm);
            return Ok(new { valid, exists, phone = norm });
        }

        // ==== POST: /api/Patients
        [HttpPost]
        public async Task<ActionResult<Patient>> CreatePatient([FromBody] Patient dto)
        {
            var fullName = (dto.FullName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                return BadRequest(new { message = "FullName là bắt buộc." });

            var phoneNorm = NormalizeVnPhone(dto.Phone);
            if (!string.IsNullOrEmpty(phoneNorm) && !IsValidVnMobile(phoneNorm))
                return BadRequest(new { message = "Số điện thoại không hợp lệ (10 số, bắt đầu 0)." });

            if (!string.IsNullOrEmpty(phoneNorm) &&
                await _context.Patients.AnyAsync(x => x.Phone == phoneNorm))
                return Conflict(new { message = "SĐT đã tồn tại." });

            var entity = new Patient
            {
                FullName = fullName,
                Dob = dto.Dob,
                Gender = string.IsNullOrWhiteSpace(dto.Gender) ? null : dto.Gender.Trim(),
                Phone = string.IsNullOrEmpty(phoneNorm) ? null : phoneNorm,
                Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address!.Trim()
            };

            _context.Patients.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPatient), new { id = entity.Id }, entity);
        }

        // ==== PUT: /api/Patients/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(int id, [FromBody] Patient dto)
        {
            var entity = await _context.Patients.FindAsync(id);
            if (entity == null) return NotFound(new { message = "Không tìm thấy bệnh nhân." });

            var fullName = (dto.FullName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                return BadRequest(new { message = "FullName là bắt buộc." });

            var phoneNorm = NormalizeVnPhone(dto.Phone);
            if (!string.IsNullOrEmpty(phoneNorm) && !IsValidVnMobile(phoneNorm))
                return BadRequest(new { message = "Số điện thoại không hợp lệ (10 số, bắt đầu 0)." });

            if (!string.IsNullOrEmpty(phoneNorm) &&
                await _context.Patients.AnyAsync(x => x.Phone == phoneNorm && x.Id != id))
                return Conflict(new { message = "SĐT đã tồn tại." });

            entity.FullName = fullName;
            entity.Dob = dto.Dob;
            entity.Gender = string.IsNullOrWhiteSpace(dto.Gender) ? null : dto.Gender.Trim();
            entity.Phone = string.IsNullOrEmpty(phoneNorm) ? null : phoneNorm;
            entity.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address!.Trim();

            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        // ==== DELETE: /api/Patients/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(int id, [FromQuery] bool force = false)
        {
            var entity = await _context.Patients
                .Include(p => p.Appointments)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entity == null) return NotFound();

            var hasAppointments = entity.Appointments?.Any() == true;
            if (hasAppointments && !force)
                return Conflict(new
                {
                    message = "Không thể xoá vì bệnh nhân còn lịch/phim khám tham chiếu."
                });

            _context.Patients.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
