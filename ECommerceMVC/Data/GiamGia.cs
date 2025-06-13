using ECommerceMVC.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceMVC.Models
{
    public class GiamGia
    {
        [Key]
        public int MaGG { get; set; } // Mã giảm giá (Primary Key)

        [ForeignKey("HangHoa")]
        public int MaHh { get; set; } // Mã hàng hóa (Foreign Key)

        [Required]
        [Range(0, 100, ErrorMessage = "Tỷ lệ giảm giá phải từ 0% đến 100%")]
        public int TyLeGG { get; set; } // Tỷ lệ giảm giá (%)

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số người tối thiểu phải lớn hơn 0")]
        public int NguoiToiThieu { get; set; } // Số người tối thiểu để áp dụng giảm giá

        [Required]
        public DateTime StartTime { get; set; } // Thời gian bắt đầu

        [Required]
        public DateTime EndTime { get; set; } // Thời gian kết thúc
        public int SoNguoiThamGia { get; set; } = 0;
        public bool IsActive { get; set; }

        // Liên kết với bảng HangHoa
        public virtual HangHoa HangHoa { get; set; }
        public virtual ICollection<ThamGiaMuaChung> ThamGiaMuaChungs { get; set; } = new List<ThamGiaMuaChung>();
    }
}
