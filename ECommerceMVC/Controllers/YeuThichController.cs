using System;
using System.Linq;
using ECommerceMVC.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMVC.Controllers
{
    public class YeuThichController : BaseController
    {
        private readonly MuaChungContext _context;

        public YeuThichController(MuaChungContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult DanhGiaSanPham(int MaHh, int MaHd, int SoSao, string Comment, bool AnDanh)
        {
            if (string.IsNullOrEmpty(UserId))
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để đánh giá sản phẩm!" });
            }

            if (SoSao < 1 || SoSao > 5)
            {
                return Json(new { success = false, message = "Số sao đánh giá không hợp lệ!" });
            }

            var hoaDon = _context.HoaDons
                .Include(hd => hd.ChiTietHds)
                .FirstOrDefault(hd => hd.MaHd == MaHd && hd.MaKh == UserId && hd.MaTrangThai == 5);

            if (hoaDon == null)
            {
                return Json(new { success = false, message = "❌ Không tìm thấy hóa đơn hoặc hóa đơn chưa hoàn thành!" });
            }

            var danhGia = new YeuThich
            {
                MaHh = MaHh,
                MaKh = UserId,
                MaHd = MaHd,
                NgayChon = DateTime.Now,
                SoSao = SoSao,  // ⭐ Kiểm tra giá trị này có đúng không
                Comment = Comment.Trim(),
                AnDanh = AnDanh
            };

            _context.YeuThiches.Add(danhGia);
            _context.SaveChanges();

            return Json(new { success = true, message = "🎉 Đánh giá đã được gửi thành công!" });
        }



    }
}
