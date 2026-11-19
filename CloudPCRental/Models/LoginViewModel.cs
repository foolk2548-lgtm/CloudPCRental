using System.ComponentModel.DataAnnotations;

namespace CloudPCRental.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "กรุณากรอก Username หรือ Email")]
        [Display(Name = "Username หรือ Email")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอก Password")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }
}

