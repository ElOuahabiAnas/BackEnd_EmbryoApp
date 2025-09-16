
using Microsoft.AspNetCore.Identity;

namespace EmbryoApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName  { get; set; }
        public bool IsActive { get; set; } = true; // valeur par défaut côté CLR
    }
}
