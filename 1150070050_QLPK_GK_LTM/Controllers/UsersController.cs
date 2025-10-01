using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _1150070050_QLPK_GK_LTM.Models.Entities;
using System;

namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly tuvyContext _context;
        private readonly EmailService _emailService;

        public UsersController(tuvyContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ===============================
        // CRUD USERS
        // ===============================

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return user;
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, User user)
        {
            if (id != user.Id) return BadRequest();
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ===============================
        // LOGIN API
        // ===============================
        public class LoginDto
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Username) || string.IsNullOrEmpty(dto.Password))
                return BadRequest(new { message = "Thiếu thông tin đăng nhập" });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username && u.PasswordHash == dto.Password);

            if (user == null)
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role
            });
        }

        // ===============================
        // REGISTER API
        // ===============================
        public class RegisterDto
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Email { get; set; }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.PasswordHash))
                return BadRequest(new { message = "Thiếu thông tin đăng ký" });

            // Kiểm tra trùng username
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == user.Username);
            if (existingUser != null)
                return Conflict(new { message = "Tên đăng nhập đã tồn tại" });

            // Lưu user mới
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đăng ký thành công",
                user.Id,
                user.Email,
                user.Role
            });
        }

        // ===============================
        // QUÊN MẬT KHẨU (GỬI MÃ OTP)
        // ===============================
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return NotFound(new { message = "Email không tồn tại" });

            // Tạo OTP chỉ có 6 số và gửi qua email
            string otp = GenerateOtp();
            _emailService.SendOtpEmail(user.Email, otp);

            // Lưu OTP vào cơ sở dữ liệu để xác thực sau này
            user.OtpCode = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10); // Hết hạn sau 10 phút
            await _context.SaveChangesAsync();

            return Ok(new { message = "Mã OTP đã được gửi đến email của bạn" });
        }

        // ===============================
        // XÁC THỰC MÃ OTP
        // ===============================
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return NotFound(new { message = "Email không tồn tại" });

            // Log OTP và thời gian hết hạn để kiểm tra
            Console.WriteLine($"Received OTP: {dto.OtpCode}, User OTP: {user.OtpCode}, Expiry: {user.OtpExpiry}");

            if (user.OtpCode != dto.OtpCode || user.OtpExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "Mã OTP không hợp lệ hoặc đã hết hạn" });

            return Ok(new { message = "OTP xác thực thành công" });
        }

        public class VerifyOtpDto
        {
            public string Email { get; set; }
            public string OtpCode { get; set; }
        }

        // ===============================
        // THAY ĐỔI MẬT KHẨU
        // ===============================
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return NotFound(new { message = "Email không tồn tại" });

            // Kiểm tra OTP
            if (user.OtpCode != dto.OtpCode || user.OtpExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "Mã OTP không hợp lệ hoặc đã hết hạn" });

            // Cập nhật mật khẩu mới
            user.PasswordHash = dto.NewPassword; // Hash mật khẩu nếu cần thiết
            user.OtpCode = null; // Xóa mã OTP sau khi sử dụng
            user.OtpExpiry = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Mật khẩu đã được thay đổi thành công" });
        }

        // ===============================
        // Lớp DTOs
        // ===============================
        public class ForgotPasswordDto
        {
            public string Email { get; set; }
        }

        public class ResetPasswordDto
        {
            public string Email { get; set; }
            public string OtpCode { get; set; }
            public string NewPassword { get; set; }
        }

        // ===============================
        // Tạo mã OTP 6 số ngẫu nhiên
        // ===============================
        private string GenerateOtp(int length = 6)
        {
            Random random = new Random();
            string otp = "";
            for (int i = 0; i < length; i++)
            {
                otp += random.Next(0, 10).ToString(); // Chỉ tạo số từ 0 đến 9
            }
            return otp;
        }
    }
}
