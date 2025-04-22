using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

public class DummyEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // У production тут буде код для надсилання реального email
        Console.WriteLine($"Email to {email}: {subject}\n{htmlMessage}");
        return Task.CompletedTask;
    }
}
