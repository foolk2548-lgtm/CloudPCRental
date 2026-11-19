using System.ComponentModel.DataAnnotations;

namespace CloudPCRental.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "กรุณากรอกอีเมลที่ใช้ลงทะเบียน")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        [Display(Name = "อีเมลของคุณ (Registered Email)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกรหัสผ่านใหม่")]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "รหัสผ่านต้องมีความยาวอย่างน้อย 4 ตัวอักษร")]
        [DataType(DataType.Password)]
        [Display(Name = "รหัสผ่านใหม่ (New Password)")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณายืนยันรหัสผ่านใหม่")]
        [DataType(DataType.Password)]
        [Display(Name = "ยืนยันรหัสผ่านใหม่ (Confirm Password)")]
        [Compare("NewPassword", ErrorMessage = "รหัสผ่านใหม่และการยืนยันไม่ตรงกัน")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}

