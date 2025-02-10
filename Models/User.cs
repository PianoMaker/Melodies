using Microsoft.AspNetCore.Identity;
namespace Melodies25.Models
{
    public class ApplicationUser : IdentityUser
    {
            public string? FullName { get; set; }
       

    }


    public class SharedResource
    {
    }
}