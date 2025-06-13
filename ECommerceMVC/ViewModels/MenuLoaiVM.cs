namespace ECommerceMVC.ViewModels
{
    public class MenuLoaiVM
    {
        public int MaLoai { get; set; }
        public string TenLoai { get; set; }
        public int SoLuong { get; set; }
        public List<SanPhamVM> SanPhams { get; set; } = new();
    }
    public class SanPhamVM
    {
        public int MaHh { get; set; }
        public string TenHh { get; set; }
        public double? DonGia { get; set; }
        public string Hinh { get; set; }
    }
}
