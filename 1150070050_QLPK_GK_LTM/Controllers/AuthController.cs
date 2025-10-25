using _1150070050_QLPK_GK_LTM.Models.Entities;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _cfg;
        private readonly tuvyContext _context;

        public AuthController(IConfiguration cfg, tuvyContext context)
        {
            _cfg = cfg;
            _context = context;
        }

        [HttpPost("google")]
        public async Task<IActionResult> Google([FromBody] GoogleTokenDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.IdToken))
                return BadRequest(new { message = "Thiếu idToken" });

            var webClientId = _cfg["GoogleOAuth:WebClientId"];
            GoogleJsonWebSignature.Payload payload;

            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { webClientId } });
            }
            catch
            {
                return Unauthorized(new { message = "Token Google không hợp lệ!" });
            }

            if (payload.EmailVerified != true)
                return Unauthorized(new { message = "Email chưa được xác thực!" });

            // ✅ Tìm user có email trùng hoặc GoogleId trùng
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == payload.Email || u.GoogleId == payload.Subject);

            if (user == null)
            {
                // Nếu chưa có → tạo mới
                user = new User
                {
                    FullName = payload.Name ?? payload.Email,
                    Email = payload.Email,
                    GoogleId = payload.Subject,
                    Role = "user",
                    LoginProvider = "google"
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Liên kết với bệnh nhân cũ (nếu email trùng)
                var oldPatient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == payload.Email);
                if (oldPatient != null)
                    oldPatient.UserId = user.Id;
                else
                    _context.Patients.Add(new Patient { FullName = user.FullName, Email = user.Email, UserId = user.Id });

                await _context.SaveChangesAsync();
            }
            else
            {
                if (string.IsNullOrEmpty(user.GoogleId))
                {
                    user.GoogleId = payload.Subject;
                    user.LoginProvider = "google";
                    await _context.SaveChangesAsync();
                }
            }

            var token = IssueJwt(user.FullName, user.Role);

            return Ok(new
            {
                message = "✅ Đăng nhập Google thành công!",
                user = new { user.Id, user.FullName, user.Email },
                accessToken = token
            });
        }

        private string IssueJwt(string userName, string role)
        {
            var issuer = _cfg["Jwt:Issuer"];
            var audience = _cfg["Jwt:Audience"];
            var key = _cfg["Jwt:Key"];

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, role)
            };

            var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public class GoogleTokenDto
        {
            public string IdToken { get; set; }
        }
    }
}
