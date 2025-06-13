using System;
using System.Collections.Generic;

namespace ECommerceMVC.Models.ViewModels
{
    public class DonHangViewModel
    {
        // Thông tin đơn hàng
        public int MaHd { get; set; }
        public string HoTen { get; set; }
        public DateTime NgayDat { get; set; }
        public string CachThanhToan { get; set; }
        public string DiaChi { get; set; }
        public string? GhiChu { get; set; }
        public DateTime? NgayGiao { get; set; }
        public int MaTrangThai { get; set; } // 0: Chờ duyệt, -1: Hủy, 3: Đang giao, 5: Hoàn thành

        // Danh sách chi tiết hóa đơn
        public List<ChiTietDonHangViewModel> ChiTietHds { get; set; } = new List<ChiTietDonHangViewModel>();
    }

    public class ChiTietDonHangViewModel
    {
        public string TenHh { get; set; }
        public string Hinh { get; set; }
        public int SoLuong { get; set; }
        public double? DonGia { get; set; }
        public double? ThanhTien => SoLuong * DonGia; // Tính tổng tiền của mỗi sản phẩm
    }
}
