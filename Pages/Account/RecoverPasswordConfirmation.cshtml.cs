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
                ModelState.AddModelError(string.Empty, "����������� � ����� email �� ��������.");
                ErrorMessageL("����������� � ����� email �� ��������.");
                return Page();
            }

            // ��������� ������ ������
            var newPassword = GenerateRandomPassword();

            // ��������� ������ � �������� ������
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            Console.WriteLine($"Generated token: {token}");

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "�� ������� ������� ������.");
                Console.WriteLine("Password reset failed.");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"Error: {error.Code} - {error.Description}");
                }
                return Page();
            }

            // ³������� �����
            var emailSent = await SendPasswordEmailAsync(Email, newPassword);
            if (emailSent)
            {
                Console.WriteLine("Password reset email sent successfully.");
                return RedirectToPage("/Account/RecoverPasswordConfirmation");
            }

            ModelState.AddModelError(string.Empty, "������� �������� �����.");
            Console.WriteLine("Error sending email.");
            return Page();
        }

        private string GenerateRandomPassword()
        {
            var length = 12; // ̳������� ������� ������
            var random = new Random();
            var password = new StringBuilder();

            bool containsUppercase = false;
            bool containsLowercase = false;
            bool containsDigit = false;
            bool containsSpecialChar = false;

            while (password.Length < length)
            {
                var character = (char)random.Next(33, 126); // ��������� ����������� �������
                password.Append(character);

                if (char.IsUpper(character)) containsUppercase = true;
                if (char.IsLower(character)) containsLowercase = true;
                if (char.IsDigit(character)) containsDigit = true;
                if (!char.IsLetterOrDigit(character)) containsSpecialChar = true;
            }

            // ������ �������, ���� ���� ������
            if (!containsUppercase) password.Append('A'); // ������ ������ �����
            if (!containsLowercase) password.Append('a'); // ������ ���� �����
            if (!containsDigit) password.Append('1'); // ������ �����
            if (!containsSpecialChar) password.Append('!'); // ������ ����������� ������

            return password.ToString();
        }


        private async Task<bool> SendPasswordEmailAsync(string email, string newPassword)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587, // ��� 465 ��� SSL 587 ��� 
                    Credentials = new NetworkCredential("melody.pianomaker@gmail.com", "gfel ktvy ribw katy"),
                    EnableSsl = true,
                }; 

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("melody.pianomaker@gmail.com"),
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
