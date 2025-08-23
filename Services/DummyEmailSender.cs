using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Melodies25.Services
{
    public class DummyEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // No-op email sender. Logging could be added if needed.
            return Task.CompletedTask;
        }
    }
}