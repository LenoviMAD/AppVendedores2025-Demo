using System;
using System.Threading;
using System.Threading.Tasks;

namespace AppVendedores2025.Services
{
    public class EmailScheduler
    {
        private Timer _timer;
        private readonly EmailRetryService _emailRetryService;

        public EmailScheduler(EmailRetryService emailRetryService)
        {
            _emailRetryService = emailRetryService;
            _timer = new Timer(async _ => await RetryEmails(), null, TimeSpan.Zero, TimeSpan.FromMinutes(5)); // Inicia inmediatamente y repite cada 5 minutos
        }

        private async Task RetryEmails()
        {
            await _emailRetryService.RetryFailedEmails();
        }

        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, 0); // Detener el timer
        }
    }
}
