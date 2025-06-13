using System.ComponentModel.DataAnnotations;

namespace ECommerceMVC.ViewModels
{
    public class RegisterVM
    {
        [Key]
        [Display(Name = "Mã Khách Hàng")]
        [MaxLength(20, ErrorMessage = "Tối đa 20 kí tự")]
        public string? MaKh { get; set; } // Cho phép null, sẽ tự sinh


        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; }

        [Display(Name = "Họ tên")]
        [Required(ErrorMessage = "Họ Tên không được để trống")]
        [MaxLength(50, ErrorMessage = "Tối đa 50 kí tự")]
        public string HoTen { get; set; }

        public bool GioiTinh { get; set; } = true;

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? NgaySinh { get; set; }

        [Display(Name = "Địa chỉ")]
        [MaxLength(60, ErrorMessage = "Tối đa 60 kí tự")]
        public string DiaChi { get; set; }

        [Display(Name = "Điện thoại")]
        [MaxLength(10, ErrorMessage = "Số điện thoại phải có đúng 10 chữ số")]
        [RegularExpression(@"^(0[3|5|7|8|9])([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ!")]
        public string DienThoai { get; set; }

        [EmailAddress(ErrorMessage = "Chưa đúng định dạng email")]
        public string Email { get; set; }

        public string? Hinh { get; set; }
    }
}