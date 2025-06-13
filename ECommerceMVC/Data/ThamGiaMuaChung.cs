using ECommerceMVC.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceMVC.Models
{
    public class ThamGiaMuaChung
    {
        [Key]
        public int MaThamGia { get; set; }

        public string MaKh { get; set; }

        public int MaGG{ get; set; }

        public DateTime NgayThamGia { get; set; }

        // Điều hướng
        public virtual KhachHang? KhachHang { get; set; }
        public virtual GiamGia? GiamGia { get; set; }
    }   
}
