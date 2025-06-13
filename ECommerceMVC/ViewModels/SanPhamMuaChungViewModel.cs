using ECommerceMVC.Models;
using System.ComponentModel.DataAnnotations;

namespace ECommerceMVC.ViewModels
{
    public class SanPhamMuaChungViewModel
    {
            public int MaHh { get; set; }
            public string TenHh { get; set; }
            public double? DonGia { get; set; }
            public string? Hinh { get; set; }

            public string TrangThaiGiamGia { get; set; } = "Chưa thiết lập";

            // Thêm nếu muốn dùng trong View
            public GiamGia? GiamGia { get; set; }
            
            public bool CanSetupGiamGia { get; set; }
            public bool CanEditGiamGia { get; set; }
		public bool CanTatGiamGia { get; set; }

		public int TyLeGG { get; set; }
        [Range(1, int.MaxValue)]
        public int NguoiToiThieu { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; }
    }
}
