using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _1150070050_QLPK_GK_LTM.Models.Entities;

namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly ClinicDbContext _context;

        public NotificationsController(ClinicDbContext context)
        {
            _context = context;
        }

        // ==============================================
        // ✅ Dành cho app bệnh nhân
        // ==============================================
        // GET /api/Notifications/by-patient/{patientId}
        [HttpGet("by-patient/{patientId:int}")]
        public async Task<IActionResult> GetByPatient(int patientId)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == patientId);
            if (patient == null)
                return NotFound(new { message = "Không tìm thấy bệnh nhân." });

            int? userId = patient.UserId;
            if (userId == null)
                return BadRequest(new { message = "Bệnh nhân chưa được gắn tài khoản người dùng." });

            var list = await _context.Notifications
                .Where(n => n.ReceiverId == userId.Value)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.NotificationId,
                    n.Title,
                    n.Message,
                    n.NotificationType,
                    n.CreatedAt,
                    n.IsRead
                })
                .ToListAsync();

            if (!list.Any())
                return NotFound(new { message = "Bệnh nhân chưa có thông báo nào." });

            return Ok(list);
        }


        // ==============================================
        // ⚙️ Dành cho phía phòng khám (admin)
        // ==============================================
        [HttpGet("by-user/{userId:int}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var list = await _context.Notifications
                .Where(n => n.ReceiverId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.NotificationId,
                    n.Title,
                    n.Message,
                    n.NotificationType,
                    n.CreatedAt,
                    n.IsRead
                })
                .ToListAsync();

            if (!list.Any())
                return NotFound(new { message = "Người dùng chưa có thông báo nào." });

            return Ok(list);
        }

        // ==============================================
        // POST /api/Notifications
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Notification n)
        {
            if (n == null || n.ReceiverId <= 0)
                return BadRequest(new { message = "Thiếu dữ liệu thông báo." });

            n.CreatedAt = DateTime.Now;
            n.IsRead = false;

            _context.Notifications.Add(n);
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Gửi thông báo thành công!", n });
        }

        // PUT /api/Notifications/mark-read/{id}
        [HttpPut("mark-read/{id:int}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var noti = await _context.Notifications.FindAsync(id);
            if (noti == null)
                return NotFound(new { message = "Không tìm thấy thông báo." });

            noti.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã đánh dấu đã đọc!" });
        }

        // DELETE /api/Notifications/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var noti = await _context.Notifications.FindAsync(id);
            if (noti == null)
                return NotFound(new { message = "Không tìm thấy thông báo." });

            _context.Notifications.Remove(noti);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa thông báo!" });
        }
    }
}
