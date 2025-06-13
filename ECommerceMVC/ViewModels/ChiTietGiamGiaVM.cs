namespace ECommerceMVC.ViewModels
{
    public class ChiTietGiamGiaVM
    {
        public int MaHh { get; set; }
        public string TenHh { get; set; }
        public double? DonGia { get; set; }
        public string HinhAnhSanPham { get; set; }
        public int NguoiToiThieu { get; set; }
        public int SoNguoiThamGia { get; set; }
        public int TyLeGG { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public List<KhachHangGiamGiaViewModel> KhachHangs { get; set; } = new();
    }
    public class KhachHangGiamGiaViewModel
    {
        public string Makh { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string Anh { get; set; }
        public DateTime NgayThamGia { get; set; }
        public int SoLuongDaMua { get; set; }
    }
}
