using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Modules.Auth.Infrastructure.Services
{
    public interface IEmailService
    {
        Task<bool> SendOtpEmailAsync(string email, string otpCode);
        Task<bool> SendEmailAsync(string email, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly SmtpClient _smtpClient;
        private readonly IConfiguration _configuration;
        private readonly SmtpOptions _smtpOptions;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration, IOptions<SmtpOptions> smtpOptions)
        {
            _logger = logger;
            _configuration = configuration;
            _smtpOptions = smtpOptions.Value;

            var smtpHost = _smtpOptions.Host;
            var smtpPort = _smtpOptions.Port;
            var smtpUser = _smtpOptions.UserName;
            var smtpPassword = _smtpOptions.Password;
            var enableSsl = _smtpOptions.SmtpEnableSsl;

            if (string.IsNullOrWhiteSpace(smtpHost)
                || string.IsNullOrWhiteSpace(smtpUser)
                || string.IsNullOrWhiteSpace(smtpPassword))
            {
                _logger.LogError("SMTP configuration is incomplete.");
            }

            _smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = enableSsl
            };
        }

        public async Task<bool> SendOtpEmailAsync(string email, string otpCode)
        {
            try
            {
                var appName = _configuration["Email:Smtp:FromName"] ?? "E-Verland";

                var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background-color: #f9f9f9;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 4px 8px rgba(0,0,0,0.1);
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(90deg, #667eea, #764ba2);
            color: white;
            padding: 25px 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
        }}
        .content {{
            padding: 30px 25px;
            color: #333333;
            line-height: 1.6;
        }}
        .content h2 {{
            color: #444444;
        }}
        .otp-box {{
            background-color: #f0f0f0;
            border: 2px solid #667eea;
            border-radius: 8px;
            padding: 20px;
            text-align: center;
            margin: 20px 0;
        }}
        .otp-code {{
            font-size: 32px;
            font-weight: bold;
            color: #667eea;
            letter-spacing: 5px;
            font-family: 'Courier New', monospace;
        }}
        .otp-info {{
            background-color: #fffbea;
            border-left: 4px solid #ffa500;
            padding: 12px 15px;
            margin: 15px 0;
            border-radius: 4px;
            font-size: 14px;
        }}
        .footer {{
            text-align: center;
            padding: 15px;
            font-size: 13px;
            color: #999999;
            border-top: 1px solid #eeeeee;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{appName}</h1>
        </div>
        <div class='content'>
            <h2>Email Verification</h2>
            <p>Thank you for registering! Please use the following code to verify your email address:</p>
            
            <div class='otp-box'>
                <div class='otp-code'>{otpCode}</div>
            </div>
            
            <div class='otp-info'>
                <strong> This code will expire in 10 minutes</strong>
            </div>
            
            <p>If you didn't request this code, please ignore this email.</p>
            <p>For security reasons, never share this code with anyone.</p>
        </div>
        <div class='footer'>
            &copy; {DateTime.Now.Year} {appName}. All rights reserved.
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(email, "Email Verification Code", htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending OTP email to {email}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(string email, string subject, string htmlBody)
        {
            try
            {
                var senderEmail = _smtpOptions.UserName;

                if (string.IsNullOrWhiteSpace(senderEmail))
                {
                    _logger.LogError($"Sender email is not configured.");
                    return false;
                }

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "E-Verland"),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await _smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully to {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email to {email}: {ex.Message}");
                return false;
            }
        }
    }
}
