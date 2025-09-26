namespace _1150070050_QLPK_GK_LTM.Models.DTOs
{
    public class GoogleTokenDto
    {
        public string IdToken { get; set; }
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }
    }
}
