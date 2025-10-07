using System.ComponentModel.DataAnnotations;

namespace RoomCast.Models
{
    public class User
    {
        [Key]
        public int User_id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Password { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;
    }
}
