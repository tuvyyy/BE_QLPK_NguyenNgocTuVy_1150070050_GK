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
                return BadRequest(new { message = "Missing idToken" });

            var webClientId = _cfg["GoogleOAuth:WebClientId"];
            if (string.IsNullOrWhiteSpace(webClientId))
                return StatusCode(500, new { message = "GoogleOAuth:WebClientId not configured" });

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
            catch (InvalidJwtException)
            {
                // Gỡ lỗi nhanh: in aud/iss ra console
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(dto.IdToken);
                    var aud = string.Join(",", jwt.Audiences);
                    var iss = jwt.Issuer;
                    Console.WriteLine($"[GoogleLogin] Reject token aud={aud} iss={iss} cfgAud={webClientId}");
                }
                catch { /* ignore */ }

                return Unauthorized(new { message = "Invalid Google token" });
            }

            if (payload.EmailVerified != true)
                return Unauthorized(new { message = "Email not verified" });

            // (tuỳ chọn) giới hạn domain
            if (!string.IsNullOrWhiteSpace(allowedHd))
            {
                var hd = payload.HostedDomain; // đúng property
                if (!string.Equals(hd, allowedHd, StringComparison.OrdinalIgnoreCase))
                    return Unauthorized(new { message = "Email domain not allowed" });
            }

            // Map payload -> thông tin trả về (nếu cần lưu DB, thêm ở đây)
            var userName = payload.Name ?? (payload.Email ?? payload.Subject);
            var role = "User";

            var token = IssueJwt(userName, role);

            return Ok(new LoginResponse
            {
                AccessToken = token,
                UserName = userName,
                Role = role
            });
        }

        private string IssueJwt(string userName, string role)
        {
            var issuer = _cfg["Jwt:Issuer"];
            var audience = _cfg["Jwt:Audience"];
            var keyStr = _cfg["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(keyStr))
                throw new InvalidOperationException("Jwt config is missing (Issuer/Audience/Key)");

            var lifetimeMins = int.TryParse(_cfg["Jwt:AccessTokenMinutes"], out var m) ? m : 30;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userName),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr)),
                SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(lifetimeMins),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
