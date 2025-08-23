using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.Text;
using static Music.Messages;
using Music;
using System.ComponentModel.DataAnnotations;

namespace Melodies25.Pages
{
    public class RecoverPasswordModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Вкажіть email.")]
        [EmailAddress(ErrorMessage = "Невірний формат email.")]
        public string Email { get; set; } = string.Empty;

        [TempData]
        public string? PwdResetMsg { get; set; }

        private readonly UserManager<IdentityUser> _userManager;

        public RecoverPasswordModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public void OnGet()
        {
            // just render page; TempData message (PwdResetMsg) shown in cshtml
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
                // Do not reveal that user does not exist (to prevent enumeration)
                PwdResetMsg = "Якщо обліковий запис існує, новий пароль надіслано на пошту.";
                return RedirectToPage();
            }

            // Generate new strong password
            var newPassword = GenerateRandomPassword();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            var emailSent = await SendPasswordEmailAsync(Email, newPassword);
            if (emailSent)
            {
                PwdResetMsg = "Новий пароль надіслано на вашу електронну адресу.";
                return RedirectToPage();
            }

            ModelState.AddModelError(string.Empty, "Сталася помилка під час надсилання листа. Спробуйте пізніше.");
            return Page();
        }

        private string GenerateRandomPassword()
        {
            var length = 12;
            var random = new Random();
            var password = new StringBuilder();

            bool containsUppercase = false;
            bool containsLowercase = false;
            bool containsDigit = false;
            bool containsSpecialChar = false;

            while (password.Length < length)
            {
                var character = (char)random.Next(33, 126);
                password.Append(character);

                if (char.IsUpper(character)) containsUppercase = true;
                if (char.IsLower(character)) containsLowercase = true;
                if (char.IsDigit(character)) containsDigit = true;
                if (!char.IsLetterOrDigit(character)) containsSpecialChar = true;
            }

            if (!containsUppercase) password.Append('A');
            if (!containsLowercase) password.Append('a');
            if (!containsDigit) password.Append('1');
            if (!containsSpecialChar) password.Append('!');

            return password.ToString();
        }

        private async Task<bool> SendPasswordEmailAsync(string email, string newPassword)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("melody.pianomaker@gmail.com", "gfel ktvy ribw katy"), // TODO: move to secrets
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

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при надсиланні листа: {ex.Message}");
                return false;
            }
        }
    }
}
