namespace ECommerceMVC.ViewModels
{
	public class DanhGiaVM
	{
		public string HoTen { get; set; }  // Tên người dùng hoặc "Khách ẩn danh"
		public string Avatar { get; set; } // Ảnh đại diện (hoặc ảnh mặc định)
		public DateTime? NgayDanhGia { get; set; } // Ngày đánh giá
		public int SoSao { get; set; } // Số sao (tối đa 5)
		public string Comment { get; set; } // Bình luận đánh giá
		public bool AnDanh { get; set; } // Kiểm tra có ẩn danh hay không
	}
}
