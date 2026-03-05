using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using swd.Settings;

namespace swd.Application.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        /// <summary>
        /// Gửi mã xác minh 6 số đến email người dùng qua Gmail SMTP
        /// </summary>
        public async Task SendVerificationCodeAsync(string toEmail, string toName, string code)
        {
            var message = new MimeMessage();

            // Người gửi
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));

            // Người nhận
            message.To.Add(new MailboxAddress(toName, toEmail));

            message.Subject = $"[SWD] Mã xác minh email của bạn: {code}";

            // Nội dung email HTML đẹp
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = BuildEmailHtml(toName, code)
            };
            message.Body = bodyBuilder.ToMessageBody();

            // Gửi qua Gmail SMTP
            using var client = new SmtpClient();

            // smtp.gmail.com:587 với STARTTLS
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);

            // Google hiển thị App Password có space (vd: "xlrd ooqy yrvt gcep")
            // nhưng SMTP cần chuỗi liền không dấu cách: "xlrdooqyyrvtgcep"
            var appPassword = _settings.AppPassword.Replace(" ", "");
            await client.AuthenticateAsync(_settings.SenderEmail, appPassword);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        /// <summary>
        /// Gửi OTP đặt lại / đổi mật khẩu qua Gmail
        /// </summary>
        public async Task SendPasswordResetCodeAsync(string toEmail, string toName, string code)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = $"[SWD] Mã OTP đặt lại mật khẩu: {code}";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = BuildResetPasswordEmailHtml(toName, code)
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            var appPassword = _settings.AppPassword.Replace(" ", "");
            await client.AuthenticateAsync(_settings.SenderEmail, appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private static string BuildEmailHtml(string name, string code)
        {
            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin:0;padding:0;background:#f4f6fb;font-family:'Segoe UI',Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f4f6fb;padding:40px 0;"">
    <tr>
      <td align=""center"">
        <table width=""520"" cellpadding=""0"" cellspacing=""0""
               style=""background:#ffffff;border-radius:16px;overflow:hidden;
                      box-shadow:0 4px 24px rgba(0,0,0,0.08);"">

          <!-- Header -->
          <tr>
            <td style=""background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);
                        padding:36px 40px;text-align:center;"">
              <h1 style=""color:#fff;margin:0;font-size:26px;font-weight:700;
                          letter-spacing:-0.5px;"">🔐 Xác minh Email</h1>
              <p style=""color:rgba(255,255,255,0.85);margin:8px 0 0;font-size:14px;"">
                SWD Application
              </p>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding:40px;"">
              <p style=""color:#374151;font-size:16px;margin:0 0 16px;"">
                Xin chào <strong>{name}</strong>,
              </p>
              <p style=""color:#6b7280;font-size:15px;line-height:1.6;margin:0 0 28px;"">
                Cảm ơn bạn đã đăng ký tài khoản! Vui lòng dùng mã dưới đây để 
                xác minh địa chỉ email của bạn.
              </p>

              <!-- Verification Code Box -->
              <div style=""background:linear-gradient(135deg,#f0f4ff 0%,#faf0ff 100%);
                           border:2px dashed #a78bfa;border-radius:12px;
                           padding:28px;text-align:center;margin:0 0 28px;"">
                <p style=""color:#7c3aed;font-size:13px;font-weight:600;
                            text-transform:uppercase;letter-spacing:2px;margin:0 0 12px;"">
                  Mã xác minh của bạn
                </p>
                <p style=""color:#1e1b4b;font-size:42px;font-weight:800;
                            letter-spacing:12px;margin:0;font-family:monospace;"">
                  {code}
                </p>
              </div>

              <!-- Expiry Warning -->
              <div style=""background:#fef3c7;border-left:4px solid #f59e0b;
                           border-radius:8px;padding:14px 18px;margin:0 0 28px;"">
                <p style=""color:#92400e;font-size:14px;margin:0;"">
                  ⏱️ Mã này có hiệu lực trong <strong>10 phút</strong>. 
                  Đừng chia sẻ mã này với ai.
                </p>
              </div>

              <p style=""color:#9ca3af;font-size:13px;line-height:1.6;margin:0;"">
                Nếu bạn không thực hiện đăng ký này, hãy bỏ qua email này.
              </p>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background:#f9fafb;padding:20px 40px;
                        border-top:1px solid #e5e7eb;text-align:center;"">
              <p style=""color:#9ca3af;font-size:12px;margin:0;"">
                © 2024 SWD App · Email tự động, vui lòng không trả lời.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }
        private static string BuildResetPasswordEmailHtml(string name, string code)
        {
            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin:0;padding:0;background:#f4f6fb;font-family:'Segoe UI',Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f4f6fb;padding:40px 0;"">
    <tr>
      <td align=""center"">
        <table width=""520"" cellpadding=""0"" cellspacing=""0""
               style=""background:#ffffff;border-radius:16px;overflow:hidden;
                      box-shadow:0 4px 24px rgba(0,0,0,0.08);"">

          <!-- Header màu cam đỏ – phân biệt với email xác minh -->
          <tr>
            <td style=""background:linear-gradient(135deg,#f97316 0%,#dc2626 100%);
                        padding:36px 40px;text-align:center;"">
              <h1 style=""color:#fff;margin:0;font-size:26px;font-weight:700;
                          letter-spacing:-0.5px;"">🔑 Đặt lại mật khẩu</h1>
              <p style=""color:rgba(255,255,255,0.85);margin:8px 0 0;font-size:14px;"">
                SWD Application
              </p>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding:40px;"">
              <p style=""color:#374151;font-size:16px;margin:0 0 16px;"">
                Xin chào <strong>{name}</strong>,
              </p>
              <p style=""color:#6b7280;font-size:15px;line-height:1.6;margin:0 0 28px;"">
                Chúng tôi nhận được yêu cầu đặt lại / đổi mật khẩu cho tài khoản của bạn.
                Sử dụng mã OTP dưới đây để hoàn tất.
              </p>

              <!-- OTP Box -->
              <div style=""background:linear-gradient(135deg,#fff7ed 0%,#fef2f2 100%);
                           border:2px dashed #f97316;border-radius:12px;
                           padding:28px;text-align:center;margin:0 0 28px;"">
                <p style=""color:#ea580c;font-size:13px;font-weight:600;
                            text-transform:uppercase;letter-spacing:2px;margin:0 0 12px;"">
                  Mã OTP của bạn
                </p>
                <p style=""color:#7c2d12;font-size:42px;font-weight:800;
                            letter-spacing:12px;margin:0;font-family:monospace;"">
                  {code}
                </p>
              </div>

              <!-- Warning -->
              <div style=""background:#fef2f2;border-left:4px solid #ef4444;
                           border-radius:8px;padding:14px 18px;margin:0 0 28px;"">
                <p style=""color:#991b1b;font-size:14px;margin:0;"">
                  ⚠️ Mã này có hiệu lực trong <strong>10 phút</strong>. 
                  Nếu bạn không yêu cầu đổi mật khẩu, hãy bỏ qua email này 
                  và kiểm tra bảo mật tài khoản.
                </p>
              </div>

              <p style=""color:#9ca3af;font-size:13px;line-height:1.6;margin:0;"">
                Không chia sẻ mã này với bất kỳ ai, kể cả nhân viên hỗ trợ.
              </p>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background:#f9fafb;padding:20px 40px;
                        border-top:1px solid #e5e7eb;text-align:center;"">
              <p style=""color:#9ca3af;font-size:12px;margin:0;"">
                © 2024 SWD App · Email tự động, vui lòng không trả lời.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }
    }
}
