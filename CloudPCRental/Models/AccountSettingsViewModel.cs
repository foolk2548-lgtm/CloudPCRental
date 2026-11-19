using System.ComponentModel.DataAnnotations;

namespace CloudPCRental.Models
{
    public class AccountSettingsViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
        [Display(Name = "ชื่อ-นามสกุล")]
        public string FullName { get; set; } = "ธนพล ดีสมเกียรติ";

        [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
        [Display(Name = "ชื่อผู้ใช้ (Username)")]
        public string Username { get; set; } = "thanapol";

        [Required(ErrorMessage = "กรุณากรอกอีเมล")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        [Display(Name = "อีเมล")]
        public string Email { get; set; } = "thanapol@example.com";

        [Required(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์")]
        [RegularExpression(@"^0[0-9]{2}-?[0-9]{3}-?[0-9]{4}$", ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์ 10 หลักให้ถูกต้อง (เช่น 081-234-5678 หรือ 0812345678)")]
        [StringLength(12, ErrorMessage = "เบอร์โทรศัพท์ต้องมีความยาวไม่เกิน 10 หลัก")]
        [Display(Name = "เบอร์โทรศัพท์")]
        public string PhoneNumber { get; set; } = "081-234-5678";

        [Display(Name = "รหัสผ่านปัจจุบัน")]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [Display(Name = "รหัสผ่านใหม่")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Display(Name = "ยืนยันรหัสผ่านใหม่")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "รหัสผ่านใหม่และการยืนยันไม่ตรงกัน")]
        public string? ConfirmNewPassword { get; set; }
    }
}

