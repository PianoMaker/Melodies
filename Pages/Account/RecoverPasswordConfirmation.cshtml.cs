using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Text;
using static Music.Messages;
using Music;

namespace Melodies25.Pages
{
    public class RecoverPasswordModel : PageModel
    {
        [BindProperty]
        public string Email { get; set; }

        private readonly UserManager<IdentityUser> _userManager;

        public RecoverPasswordModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> OnPost()
        {
            MessageL(COLORS.yellow, $"RecoverPassword OnPost method: {Email}");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Користувача з таким email не знайдено.");
                ErrorMessageL("Користувача з таким email не знайдено.");
                return Page();
            }

            // Генерація нового пароля
            var newPassword = GenerateRandomPassword();

            // Створення токена і скидання пароля
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            Console.WriteLine($"Generated token: {token}");

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Не вдалося оновити пароль.");
                Console.WriteLine("Password reset failed.");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"Error: {error.Code} - {error.Description}");
                }
                return Page();
            }

            // Відправка листа
            var emailSent = await SendPasswordEmailAsync(Email, newPassword);
            if (emailSent)
            {
                Console.WriteLine("Password reset email sent successfully.");
                return RedirectToPage("/Account/RecoverPasswordConfirmation");
            }

            ModelState.AddModelError(string.Empty, "Помилка відправки листа.");
            Console.WriteLine("Error sending email.");
            return Page();
        }

        private string GenerateRandomPassword()
        {
            var length = 12; // Мінімальна довжина пароля
            var random = new Random();
            var password = new StringBuilder();

            bool containsUppercase = false;
            bool containsLowercase = false;
            bool containsDigit = false;
            bool containsSpecialChar = false;

            while (password.Length < length)
            {
                var character = (char)random.Next(33, 126); // Генерація випадкового символу
                password.Append(character);

                if (char.IsUpper(character)) containsUppercase = true;
                if (char.IsLower(character)) containsLowercase = true;
                if (char.IsDigit(character)) containsDigit = true;
                if (!char.IsLetterOrDigit(character)) containsSpecialChar = true;
            }

            // Додаємо символи, якщо вони відсутні
            if (!containsUppercase) password.Append('A'); // Додаємо велику літеру
            if (!containsLowercase) password.Append('a'); // Додаємо малу літеру
            if (!containsDigit) password.Append('1'); // Додаємо цифру
            if (!containsSpecialChar) password.Append('!'); // Додаємо спеціальний символ

            return password.ToString();
        }


        private async Task<bool> SendPasswordEmailAsync(string email, string newPassword)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587, // або 465 для SSL 587 для 
                    Credentials = new NetworkCredential("melody.pianomaker@gmail.com", "gfel ktvy ribw katy"),
                    EnableSsl = true,
                }; 

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("melody.pianomaker@gmail.com"),
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
