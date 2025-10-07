// File: Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

namespace RoomCast.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public DateTime Date { get; set; }
    }
}
