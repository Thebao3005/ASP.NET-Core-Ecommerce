
namespace ECommerceMVC.ViewModels
{
	public class HangHoaVM
	{
		public int MaHh { get; set; }
		public string? TenHH { get; set; }
        public string TenHh { get; internal set; }
        public string? Hinh { get; set; }
		public double? DonGia { get; set; }
		public string? MoTaDonVi { get; set; }
		public string? TenLoai { get; set; }
		public string? TenAlias { get; set; }
        public int MaLoai { get; set; }
        public string? MoTa { get; set; }
        public string? MaNcc { get; set; }
        public DateTime NgaySx { get; set; }
        public double GiamGia { get; set; }
        public int? SoLanXem { get; set; }
		public bool IsGiamGia { get; set; }
		public DateTime? EndTime { get; set; }
		public int MaGG { get; set; }
        public bool DaThamGia { get; set; } // Thêm thuộc tính này
    }

	public class ChiTietHangHoaVM
	{
		public int MaHh { get; set; }
		public string TenHH { get; set; }
		public string Hinh { get; set; }
		public double DonGia { get; set; }
		public string MoTa { get; set; }
		public string TenLoai { get; set; }
		public string ChiTiet { get; set; }
		public double DiemDanhGia { get; set; }
		public int SoLuongTon { get; set; }
        public int SoLanXem { get; set; } // Thêm thuộc tính này để lưu số lần xem
        public string MoTaDonVi { get; set; }

        public bool IsGiamGia { get; set; }
    }
}
