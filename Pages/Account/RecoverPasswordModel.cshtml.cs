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
                // Генерація нового пароля
                var newPassword = GenerateRandomPassword();

                // Логіка для оновлення пароля в базі даних або іншому сховищі
                // Тут, ймовірно, потрібно знайти користувача по email і зберегти новий пароль

                // Відправка електронного листа
                var emailSent = await SendPasswordEmailAsync(Email, newPassword);

                if (emailSent)
                {
                    // Показати повідомлення про успішне відправлення листа
                    return RedirectToPage("/PasswordRecoveryConfirmation");
                }

                // Якщо лист не вдалося надіслати, показати помилку
                ModelState.AddModelError(string.Empty, "Помилка відправки електронного листа.");
            }

            return Page();
        }

        private string GenerateRandomPassword()
        {
            // Генерація випадкового пароля, наприклад, 8-символьний
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
                    Port = 587, // або 465 для SSL
                    Credentials = new NetworkCredential("your-email@example.com", "your-email-password"),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("your-email@example.com"),
                    Subject = "Ваш новий пароль",
                    Body = $"Ваш новий пароль: {newPassword}",
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(email);

                // Відправка листа
                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                // Логування помилки або інші дії
                Console.WriteLine($"Помилка відправки листа: {ex.Message}");
                return false;
            }
        }
    }
}
