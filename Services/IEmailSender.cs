using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class DummyEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        Console.WriteLine($"[DUMMY EMAIL]\nTo: {email}\nSubject: {subject}\n{htmlMessage}");
        return Task.CompletedTask;
    }
}

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly SmtpClient _client;
    private readonly string _from;

    public SmtpEmailSender(IConfiguration config)
    {
        _config = config;
        var host = _config["Smtp:Host"];
        var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
        var user = _config["Smtp:User"];
        var pass = _config["Smtp:Password"];
        _from = user;
        _client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(user, pass)
        };
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var mail = new MailMessage(_from, email)
        {
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };
        await _client.SendMailAsync(mail);
    }
}
