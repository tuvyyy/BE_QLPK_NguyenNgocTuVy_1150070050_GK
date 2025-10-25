using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _1150070050_QLPK_GK_LTM.Models.Entities;
using _1150070050_QLPK_GK_LTM.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using _1150070050_QLPK_GK_LTM.Models.DTOs;
using _1150070050_QLPK_GK_LTM.Service;

namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly EmailService _emailService;

        public UsersController(ClinicDbContext context, EmailService emailService)
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


        // LOGIN (Username / Email / SĐT)

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Thiếu mật khẩu" });

            // 🔎 Tìm user theo Username hoặc Phone
            User? user = null;
            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                user = await _context.Users
                    .Include(u => u.Patients) // ✅ include ánh xạ sang bệnh nhân
                    .FirstOrDefaultAsync(u => u.Username == dto.Username);
            }
            else if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                user = await _context.Users
                    .Include(u => u.Patients)
                    .FirstOrDefaultAsync(u => u.Phone == dto.Phone);
            }

            if (user == null)
                return Unauthorized(new { message = "❌ Sai tài khoản hoặc mật khẩu" });

            // ✅ Kiểm tra mật khẩu
            if (!PasswordHasher.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "❌ Sai mật khẩu" });

            // ✅ Lấy đúng ID bệnh nhân (nếu có)
            int returnId = user.Patients.FirstOrDefault()?.Id ?? user.Id;

            return Ok(new
            {
                message = "✅ Đăng nhập thành công!",
                id = returnId,                     // ⚙️ Trả về PatientId nếu có
                username = user.Username,
                email = user.Email,
                role = user.Role
            });
        }



        // REGISTER (Tên, Username, Phone, Email)
        public class RegisterDto
        {
            public string FullName { get; set; }
            public string Username { get; set; }   // có thể là tên đăng nhập hoặc sđt
            public string Phone { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Thiếu thông tin đăng ký" });

            // Kiểm tra tài khoản tồn tại
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == dto.Username ||
                    u.Email == dto.Email ||
                    u.Phone == dto.Phone);
            if (existingUser != null)
                return Conflict(new { message = "❌ Tài khoản đã tồn tại!" });

            // 🔍 Kiểm tra hồ sơ bệnh nhân cũ
            var existingPatient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Phone == dto.Phone || p.Phone == dto.Username);

            // 🔒 Hash mật khẩu
            var hashedPassword = PasswordHasher.Hash(dto.Password);

            // ✅ Tạo user mới (role patient)
            var newUser = new User
            {
                FullName = dto.FullName,
                Username = dto.Username,
                Phone = dto.Phone,
                Email = dto.Email,
                PasswordHash = hashedPassword,
                Role = "patient",      // ✅ Bệnh nhân
                LoginProvider = "local"
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // ✅ Đồng bộ hồ sơ bệnh nhân
            if (existingPatient != null)
            {
                if (existingPatient.UserId == null)
                {
                    existingPatient.UserId = newUser.Id; // liên kết
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                _context.Patients.Add(new Patient
                {
                    FullName = dto.FullName,
                    Phone = dto.Phone ?? dto.Username,
                    Email = dto.Email,
                    UserId = newUser.Id
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "✅ Đăng ký thành công!",
                newUser.Id,
                newUser.Username,
                newUser.Phone,
                newUser.Email,
                newUser.Role
            });
        }

        // ===============================
        // QUÊN MẬT KHẨU (OTP)
        // ===============================
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return NotFound(new { message = "Email không tồn tại" });

            string otp = GenerateOtp();
            _emailService.SendOtpEmail(user.Email, otp);

            user.OtpCode = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Mã OTP đã được gửi đến email của bạn" });
        }

        [HttpPost("forgot-password-sms")]
        public async Task<IActionResult> ForgotPasswordSms([FromBody] ForgotPasswordSmsDto dto, [FromServices] SmsService smsService)
        {
            if (string.IsNullOrWhiteSpace(dto.Phone))
                return BadRequest(new { message = "Vui lòng nhập số điện thoại" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Phone == dto.Phone);
            if (user == null)
                return NotFound(new { message = "❌ Số điện thoại không tồn tại" });

            string otp = GenerateOtp();
            smsService.SendOtpSms(user.Phone, otp);

            user.OtpCode = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ OTP đã được gửi qua SMS (giả lập)!", otp });
            // ⚠️ Trong production, KHÔNG trả OTP ra response
        }

        public class ForgotPasswordSmsDto
        {
            public string Phone { get; set; }
        }


        // ===============================
        // XÁC THỰC OTP
        // ===============================
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            User? user = null;

            if (!string.IsNullOrWhiteSpace(dto.Email))
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            else if (!string.IsNullOrWhiteSpace(dto.Phone))
                user = await _context.Users.FirstOrDefaultAsync(u => u.Phone == dto.Phone);

            if (user == null) return NotFound(new { message = "Không tìm thấy tài khoản" });

            if (user.OtpCode != dto.OtpCode || user.OtpExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "Mã OTP không hợp lệ hoặc đã hết hạn" });

            return Ok(new { message = "✅ OTP xác thực thành công" });
        }

        public class VerifyOtpDto
        {
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string OtpCode { get; set; }
        }

        public class ResetPasswordDto
        {
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string OtpCode { get; set; }
            public string NewPassword { get; set; }
        }

        // ĐẶT LẠI MẬT KHẨU
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            User? user = null;

            if (!string.IsNullOrWhiteSpace(dto.Email))
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            else if (!string.IsNullOrWhiteSpace(dto.Phone))
                user = await _context.Users.FirstOrDefaultAsync(u => u.Phone == dto.Phone);

            if (user == null)
                return NotFound(new { message = "Không tìm thấy tài khoản" });

            if (user.OtpCode != dto.OtpCode || user.OtpExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "Mã OTP không hợp lệ hoặc đã hết hạn" });

            user.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
            user.OtpCode = null;
            user.OtpExpiry = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Mật khẩu đã được thay đổi thành công" });
        }

        // DTOs & Helper
        public class ForgotPasswordDto { public string Email { get; set; } }

        private string GenerateOtp(int length = 6)
        {
            var random = new Random();
            return string.Concat(Enumerable.Range(0, length).Select(_ => random.Next(0, 10)));
        }
    }
}
