using System;
using System.Collections.Generic;

namespace ECommerceMVC.Data;

public partial class YeuThich
{
    public int MaYt { get; set; }

    public int? MaHh { get; set; }

    public string? MaKh { get; set; }
    public int? MaHd { get; set; }  // Lưu mã hóa đơn mà user đánh giá sản phẩm này
    public DateTime? NgayChon { get; set; }

    public string? MoTa { get; set; }

    // Thêm trường đánh giá
    public int? SoSao { get; set; } // Số sao (1-5)
    public string? Comment { get; set; } // Nội dung đánh giá

    public bool? AnDanh { get; set; } // Đánh giá ẩn danh

    public virtual HangHoa? MaHhNavigation { get; set; }

    public virtual KhachHang? MaKhNavigation { get; set; }
    public virtual HoaDon? MaHdNavigation { get; set; }
    public virtual ICollection<HoiDap> HoiDaps { get; set; } = new List<HoiDap>();
}
