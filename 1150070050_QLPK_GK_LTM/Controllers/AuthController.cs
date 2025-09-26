using _1150070050_QLPK_GK_LTM.Models.DTOs;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
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
        public AuthController(IConfiguration cfg) => _cfg = cfg;

        // POST: /api/Auth/google
        [HttpPost("google")]
        public async Task<ActionResult<LoginResponse>> Google([FromBody] GoogleTokenDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.IdToken))
                return BadRequest("Missing idToken");

            var webClientId = _cfg["GoogleOAuth:WebClientId"];
            var allowedHd = _cfg["GoogleOAuth:AllowedHd"]; // optional

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    dto.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { webClientId }
                    });
            }
            catch
            {
                return Unauthorized("Invalid Google token");
            }

            if (payload.EmailVerified != true)
                return Unauthorized("Email not verified");

            // (tuỳ chọn) giới hạn domain: dùng HostedDomain (không phải Hd)
            if (!string.IsNullOrEmpty(allowedHd))
            {
                var hd = payload.HostedDomain; // ✅ đúng property cho v1.71.x
                if (!string.Equals(hd, allowedHd, StringComparison.OrdinalIgnoreCase))
                    return Unauthorized("Email domain not allowed");
            }

            // TODO: map payload.Email vào user DB của bạn; lấy role thật
            var userName = payload.Name ?? payload.Email ?? payload.Subject;
            var role = "User";

            var jwt = IssueJwt(userName, role);

            return Ok(new LoginResponse
            {
                AccessToken = jwt,
                UserName = userName,
                Role = role
            });
        }

        private string IssueJwt(string userName, string role)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userName),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _cfg["Jwt:Issuer"],
                audience: _cfg["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(_cfg["Jwt:AccessTokenMinutes"] ?? "30")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
