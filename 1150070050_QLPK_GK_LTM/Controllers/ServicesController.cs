    using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _1150070050_QLPK_GK_LTM.Models.Entities;
using _1150070050_QLPK_GK_LTM.Models.DTOs;

namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly tuvyContext _context;
        public ServicesController(tuvyContext context) => _context = context;

        // ===== Helpers =====

        private static string NormalizeName(string? s)
            => string.IsNullOrWhiteSpace(s) ? "" : s.Trim();

        private static ServiceDto ToDto(Service e) => new ServiceDto
        {
            Id = e.Id,
            ServiceName = e.ServiceName,
            Price = e.Price
        };

        // ===== GET: /api/Services?page=1&pageSize=20&q=&sort=name_asc =====
        // sort: name_asc | name_desc | price_asc | price_desc  (mặc định: name_asc)
        [HttpGet]
        public async Task<IActionResult> GetServices(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? q = null,
            [FromQuery] string? sort = "name_asc")
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 1000) pageSize = 20;

            var query = _context.Services.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                query = query.Where(s => s.ServiceName.ToLower().Contains(q));
            }

            switch ((sort ?? "").ToLower())
            {
                case "name_desc": query = query.OrderByDescending(s => s.ServiceName); break;
                case "price_asc": query = query.OrderBy(s => s.Price).ThenBy(s => s.ServiceName); break;
                case "price_desc": query = query.OrderByDescending(s => s.Price).ThenBy(s => s.ServiceName); break;
                default: query = query.OrderBy(s => s.ServiceName); break; // name_asc
            }

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                items = items.Select(ToDto),
                total,
                page,
                pageSize,
                hasNext = page * pageSize < total
            });
        }

        // ===== GET: /api/Services/{id} =====
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceDto>> GetService(int id)
        {
            var e = await _context.Services.FindAsync(id);
            if (e == null) return NotFound();
            return ToDto(e);
        }

        // ===== GET: /api/Services/options  (dropdown nhanh) =====
        // Trả về danh sách gọn cho Android: [{id, serviceName}]
        [HttpGet("options")]
        public async Task<IActionResult> GetOptions([FromQuery] string? q = null)
        {
            var query = _context.Services.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                query = query.Where(s => s.ServiceName.ToLower().Contains(q));
            }

            var list = await query
                .OrderBy(s => s.ServiceName)
                .Select(s => new { s.Id, s.ServiceName })
                .ToListAsync();

            return Ok(list);
        }

        // ===== GET: /api/Services/exists/name?name=Khám tổng quát =====
        [HttpGet("exists/name")]
        public async Task<IActionResult> ExistsName([FromQuery] string name)
        {
            var n = NormalizeName(name);
            var exists = !string.IsNullOrEmpty(n)
                         && await _context.Services.AnyAsync(s => s.ServiceName == n);
            return Ok(new { exists, name = n });
        }

        // ===== POST: /api/Services =====
        // Body: { serviceName, price }
        [HttpPost]
        public async Task<ActionResult<ServiceDto>> CreateService([FromBody] ServiceDto dto)
        {
            var name = NormalizeName(dto.ServiceName);
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "ServiceName là bắt buộc." });

            if (dto.Price < 0)
                return BadRequest(new { message = "Price không được âm." });

            // không cho trùng tên dịch vụ (tuỳ bạn muốn nới lỏng hay không)
            var dup = await _context.Services.AnyAsync(s => s.ServiceName == name);
            if (dup) return Conflict(new { message = "Tên dịch vụ đã tồn tại." });

            var e = new Service
            {
                ServiceName = name,
                Price = dto.Price
            };

            _context.Services.Add(e);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetService), new { id = e.Id }, ToDto(e));
        }

        // ===== PUT: /api/Services/{id} =====
        // Body: { id, serviceName, price }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateService(int id, [FromBody] ServiceDto dto)
        {
            var e = await _context.Services.FindAsync(id);
            if (e == null) return NotFound(new { message = "Không tìm thấy dịch vụ." });

            var name = NormalizeName(dto.ServiceName);
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "ServiceName là bắt buộc." });

            if (dto.Price < 0)
                return BadRequest(new { message = "Price không được âm." });

            // chỉ check trùng khi đổi tên hoặc để chắc chắn loại trừ chính mình
            var dup = await _context.Services
                .AnyAsync(s => s.ServiceName == name && s.Id != id);
            if (dup) return Conflict(new { message = "Tên dịch vụ đã tồn tại." });

            e.ServiceName = name;
            e.Price = dto.Price;

            await _context.SaveChangesAsync();
            return Ok(ToDto(e));
        }

        // ===== DELETE: /api/Services/{id}?force=false =====
        // Nếu còn Appointment tham chiếu -> 409 (trừ khi force=true)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteService(int id, [FromQuery] bool force = false)
        {
            var e = await _context.Services
                .Include(s => s.Appointments)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (e == null) return NotFound();

            var hasRef = e.Appointments?.Any() == true;
            if (hasRef && !force)
                return Conflict(new { message = "Không thể xoá vì dịch vụ đang được sử dụng trong lịch hẹn." });

            _context.Services.Remove(e);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
