using System.ComponentModel.DataAnnotations;

namespace CloudPCRental.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้ (Username)")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username ต้องมีความยาว 3 - 50 ตัวอักษร")]
        [Display(Name = "ชื่อผู้ใช้ (Username)")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกอีเมล (Email)")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        [Display(Name = "อีเมล (Email)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "รหัสผ่านต้องมีความยาวอย่างน้อย 4 ตัวอักษร")]
        [DataType(DataType.Password)]
        [Display(Name = "รหัสผ่าน (Password)")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณายืนยันรหัสผ่าน")]
        [DataType(DataType.Password)]
        [Display(Name = "ยืนยันรหัสผ่าน")]
        [Compare("Password", ErrorMessage = "รหัสผ่านและการยืนยันรหัสผ่านไม่ตรงกัน")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

