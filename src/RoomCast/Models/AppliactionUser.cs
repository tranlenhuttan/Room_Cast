// File: Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

namespace RoomCast.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        public string DisplayName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
            ? FullName
            : $"{FirstName} {LastName}".Trim();
    }
}
