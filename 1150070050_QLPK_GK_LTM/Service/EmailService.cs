using System;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    // Tạo mã OTP ngẫu nhiên (chỉ gồm 6 chữ số)
    // Tạo OTP chỉ có số
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


    // Gửi mã OTP qua email
    public void SendOtpEmail(string toEmail, string otp)
    {
        var smtpSettings = _config.GetSection("SmtpSettings");
        var fromEmail = smtpSettings["FromEmail"];
        var password = smtpSettings["Password"];
        var host = smtpSettings["Host"];
        var port = int.Parse(smtpSettings["Port"]);

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail),
            Subject = "Mã OTP thay đổi mật khẩu",
            Body = $"Mã OTP của bạn là: {otp}",
            IsBodyHtml = false
        };

        mailMessage.To.Add(toEmail);

        var smtpClient = new SmtpClient
        {
            Host = host,
            Port = port,
            Credentials = new NetworkCredential(fromEmail, password),
            EnableSsl = true
        };

        smtpClient.Send(mailMessage);
    }

}
