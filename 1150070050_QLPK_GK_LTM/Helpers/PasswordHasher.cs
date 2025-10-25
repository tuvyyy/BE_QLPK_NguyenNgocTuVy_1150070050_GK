using System.Security.Cryptography;
using System.Text;

namespace _1150070050_QLPK_GK_LTM.Helpers
{
    /// <summary>
    /// 🔐 Lớp hỗ trợ băm (hash) mật khẩu bằng thuật toán SHA-256
    /// Dùng để mã hóa mật khẩu khi đăng ký và kiểm tra khi đăng nhập
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Hàm băm mật khẩu thành chuỗi SHA-256 (64 ký tự hexa)
        /// </summary>
        public static string Hash(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            using (SHA256 sha256 = SHA256.Create())
            {
                // 🔹 Chuyển chuỗi mật khẩu sang byte UTF8 rồi tính toán băm
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                // 🔹 Chuyển từng byte thành 2 ký tự hexa (00-ff)
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));

                return sb.ToString(); // ví dụ: "ef92b778bafe77..."
            }
        }

        /// <summary>
        /// So sánh mật khẩu người dùng nhập vào với mật khẩu đã băm lưu trong DB
        /// </summary>
        public static bool Verify(string inputPassword, string hashedPassword)
        {
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(hashedPassword))
                return false;

            string hashedInput = Hash(inputPassword);
            return hashedInput.Equals(hashedPassword);
        }
    }
}
