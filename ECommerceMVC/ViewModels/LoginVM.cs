using System.ComponentModel.DataAnnotations;

namespace ECommerceMVC.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; }

        public bool RememberMe { get; set; }
    }
}
