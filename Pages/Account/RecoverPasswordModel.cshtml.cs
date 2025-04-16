using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Melodies25.Pages
{
    public class RecoverPasswordModel : PageModel
    {
        [BindProperty]
        public string Email { get; set; }

        public async Task<IActionResult> OnPost()
        {
            if (ModelState.IsValid)
            {
                // ��������� ������ ������
                var newPassword = GenerateRandomPassword();

                // ����� ��� ��������� ������ � ��� ����� ��� ������ �������
                // ���, �������, ������� ������ ����������� �� email � �������� ����� ������

                // ³������� ������������ �����
                var emailSent = await SendPasswordEmailAsync(Email, newPassword);

                if (emailSent)
                {
                    // �������� ����������� ��� ������ ����������� �����
                    return RedirectToPage("/PasswordRecoveryConfirmation");
                }

                // ���� ���� �� ������� ��������, �������� �������
                ModelState.AddModelError(string.Empty, "������� �������� ������������ �����.");
            }

            return Page();
        }

        private string GenerateRandomPassword()
        {
            // ��������� ����������� ������, ���������, 8-����������
            var random = new System.Random();
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            var password = new char[8];
            for (int i = 0; i < password.Length; i++)
            {
                password[i] = validChars[random.Next(validChars.Length)];
            }
            return new string(password);
        }

        private async Task<bool> SendPasswordEmailAsync(string email, string newPassword)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.yourmailserver.com")
                {
                    Port = 587, // ��� 465 ��� SSL
                    Credentials = new NetworkCredential("your-email@example.com", "your-email-password"),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("your-email@example.com"),
                    Subject = "��� ����� ������",
                    Body = $"��� ����� ������: {newPassword}",
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(email);

                // ³������� �����
                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                // ��������� ������� ��� ���� 䳿
                Console.WriteLine($"������� �������� �����: {ex.Message}");
                return false;
            }
        }
    }
}
