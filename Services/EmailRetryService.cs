using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppVendedores2025.Services
{
    public class EmailRetryService
    {
        private readonly FailedEmailService _failedEmailService;

        public EmailRetryService(FailedEmailService failedEmailService)
        {
            _failedEmailService = failedEmailService;
        }

        public async Task RetryFailedEmails()
        {
            var failedEmails = await _failedEmailService.GetAllFailedEmails();

            foreach (var failedEmail in failedEmails)
            {
                try
                {
                    await EnviarEmail.EnviarEmailApp(failedEmail.Email, failedEmail.Subject, failedEmail.Body);

                    // Si el envío es exitoso, elimina el registro
                    await _failedEmailService.DeleteFailedEmail(failedEmail.Id);
                }
                catch (Exception ex)
                {
                    // Manejar el error, por ejemplo, registrar el error
                    Console.WriteLine($"Error al enviar correo a {failedEmail.Email}: {ex.Message}");
                }
            }
        }
    }
}
