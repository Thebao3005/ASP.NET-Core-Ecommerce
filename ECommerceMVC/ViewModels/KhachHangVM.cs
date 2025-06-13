using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerceMVC.ViewModels
{
    public class KhachHangVM
    {
        [Display(Name = "Mã Khách Hàng")]
        public string MaKh { get; set; }

        [Display(Name = "Họ tên")]
        [Required(ErrorMessage = "Họ tên không được để trống!")]
        [MaxLength(50, ErrorMessage = "Tối đa 50 ký tự")]
        public string HoTen { get; set; }

        [Display(Name = "Giới tính")]
        public bool GioiTinh { get; set; } = true;

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? NgaySinh { get; set; }

        [Display(Name = "Địa chỉ")]
        [MaxLength(60, ErrorMessage = "Tối đa 60 ký tự")]
        public string DiaChi { get; set; }

        [Display(Name = "Điện thoại")]
        [Required(ErrorMessage = "Số điện thoại không được để trống!")]
        [MaxLength(10, ErrorMessage = "Số điện thoại phải có đúng 10 chữ số")]
        [RegularExpression(@"^(0[3|5|7|8|9])([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ!")]
        public string DienThoai { get; set; }

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Email không được để trống!")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ!")]
        public string Email { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string? Hinh { get; set; }

        [Display(Name = "Tải ảnh đại diện")]
        public IFormFile? HinhFile { get; set; }
        [Display(Name = "Vai trò")]
        public int VaiTro { get; set; } = 2; // 2: Khách hàng, 1: Admin

        [Display(Name = "Trạng thái")]
        public bool HieuLuc { get; set; } = true;
    }
}
