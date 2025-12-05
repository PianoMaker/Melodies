using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Melodies25.Pages.Account
{
    public class AccessDeniedModel : PageModel
    {
        public string? ReturnUrl { get; set; }

        public void OnGet(string? returnUrl)
        {
            Console.WriteLine("AccessDeniedModel OnGet called");
                        
            if(returnUrl != null && returnUrl.Contains("Edit"))
                returnUrl = returnUrl.Replace("Edit", "Details");
            if (returnUrl != null && returnUrl.Contains("Delete"))
                returnUrl = returnUrl.Replace("Delete", "Details");
            ReturnUrl = returnUrl;
            Console.WriteLine($"ReturnUrl set to: {ReturnUrl}");
        }
    }
}
