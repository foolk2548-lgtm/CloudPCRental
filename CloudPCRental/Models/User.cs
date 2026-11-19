using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudPCRental.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "กรุณากรอก Username")]
        [StringLength(50)]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอก Email")]
        [EmailAddress(ErrorMessage = "รูปแบบ Email ไม่ถูกต้อง")]
        [StringLength(100)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอก Password")]
        [StringLength(255)]
        [DataType(DataType.Password)]
        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Column("role")]
        public string Role { get; set; } = "User"; // "Admin", "Manager", "Staff", "Accountant" หรือ "User"
    }
}

