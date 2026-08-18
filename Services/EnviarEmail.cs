using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AppVendedores2025.Services
{
    
    public class EnviarEmail
    {
        private static readonly string smtpServer = "smtp.empresademo.example";
        private static readonly int smtpPort = 587;
        private static readonly string smtpUser = "demo@empresademo.example";
        private static readonly string smtpPass = "CHANGE_ME_SMTP_PASSWORD";
        private static readonly string fromEmail = "demo@empresademo.example";

        private static readonly FailedEmailService _failedEmailService = new FailedEmailService();

        public static async Task EnviarEmailApp(string destinatario, string asunto, string cuerpo)
        {
            var mensaje = new MailMessage
            {
                From = new MailAddress(fromEmail),
                Subject = asunto,
                Body = cuerpo,
                IsBodyHtml = true
            };

            mensaje.To.Add(destinatario);

            using (var smtp = new SmtpClient(smtpServer, smtpPort))
            {
                smtp.Credentials = new NetworkCredential(fromEmail, smtpPass);
                smtp.EnableSsl = true;

                try
                {
                    await smtp.SendMailAsync(mensaje);
                }
                catch (SmtpException ex)
                {
                    // Guardar en la base de datos si falla
                    var failedEmail = new FailedEmail
                    {
                        Email = destinatario,
                        Status = "Failed",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _failedEmailService.AddFailedEmail(failedEmail);
                    throw new Exception("Error al enviar el correo: " + ex.Message);
                }
            }
        }
    }
}
