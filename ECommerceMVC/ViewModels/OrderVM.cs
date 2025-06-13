using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerceMVC.ViewModels
{
    public class OrderVM
    {
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string DiaChi { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày giao hàng")]
        [DataType(DataType.Date)]
        public DateTime NgayGiao { get; set; }

        public string GhiChu { get; set; }

        public List<CartItem> GioHang { get; set; }
    }
}
