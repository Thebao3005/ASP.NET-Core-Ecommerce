using ECommerceMVC.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMVC.Controllers
{
    public class HoaDonController : BaseController
    {
        private readonly MuaChungContext _context;

        public HoaDonController(MuaChungContext context)
        {
            _context = context;
        }

        // Danh sách hóa đơn đã đặt của khách hàng (Trạng thái = 0)
        public IActionResult DonDaDat()
        {
            if (string.IsNullOrEmpty(UserId))
            {
                // Lưu URL của trang này để sau khi đăng nhập có thể quay lại
                HttpContext.Session.SetString("ReturnUrl", Url.Action("DonDaDat", "HoaDon"));
                return RedirectToAction("DangNhap", "KhachHang");
            }

            var orders = _context.HoaDons
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.MaKhNavigation) // Đảm bảo load thông tin khách hàng
                .Where(h => h.MaKh == UserId && h.MaTrangThai == 0)
                .OrderByDescending(h => h.NgayDat)
                .ToList();

            return View("DonDaDat", orders);
        }

        // Chi tiết hóa đơn đã đặt
        public IActionResult Details(int id)
        {
            // Kiểm tra người dùng đã đăng nhập chưa
            if (string.IsNullOrEmpty(UserId))
            {
                return RedirectToAction("DangNhap", "KhachHang");
            }

            // Truy vấn hóa đơn và các quan hệ liên quan
            var order = _context.HoaDons
                .Include(h => h.ChiTietHds)
                    .ThenInclude(c => c.MaHhNavigation)
                        .ThenInclude(hh => hh.YeuThiches)
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .FirstOrDefault(h => h.MaHd == id && h.MaKh == UserId);

            if (order == null)
            {
                return NotFound();
            }

            // Lấy danh sách sản phẩm trong hóa đơn
            var danhSachSanPham = order.ChiTietHds.Select(c => c.MaHh).ToList();

            // Xác định các sản phẩm chưa được đánh giá trong hóa đơn này
            var danhGiaCuaToi = danhSachSanPham
                .Where(sanPham => !order.ChiTietHds
                    .Any(c => c.MaHh == sanPham && c.MaHhNavigation.YeuThiches.Any(y => y.MaKh == UserId && y.MaHd == id)))
                .ToList();

            // Xác định các sản phẩm đã được đánh giá trong hóa đơn này
            var danhGiaDaCo = danhSachSanPham
                .Where(sanPham => order.ChiTietHds
                    .Any(c => c.MaHh == sanPham && c.MaHhNavigation.YeuThiches.Any(y => y.MaKh == UserId && y.MaHd == id)))
                .ToList();

            // Truyền danh sách sản phẩm có thể đánh giá và đã đánh giá vào ViewBag
            ViewBag.DanhGiaCuaToi = danhGiaCuaToi;
            ViewBag.DanhGiaDaCo = danhGiaDaCo;

            return View("Details", order);
        }


        public IActionResult HuyDon(int MaHd)
        {
            // Kiểm tra nếu người dùng chưa đăng nhập
            if (string.IsNullOrEmpty(UserId))
            {
                // Lưu URL của trang này để sau khi đăng nhập có thể quay lại
                HttpContext.Session.SetString("ReturnUrl", Url.Action("DonDaHuy", "HoaDon"));
                return RedirectToAction("DangNhap", "KhachHang");
            }

            // Lấy đơn hàng với trạng thái chưa xử lý (MaTrangThai == 0) và thuộc về người dùng hiện tại
            var order = _context.HoaDons
                .Include(h => h.MaTrangThaiNavigation)  // Load thông tin trạng thái đơn hàng
                .Include(h => h.MaKhNavigation)        // Load thông tin khách hàng
                .FirstOrDefault(h => h.MaHd == MaHd && h.MaKh == UserId && h.MaTrangThai == 0);

            if (order == null)
            {
                TempData["Error"] = "Không thể hủy đơn hàng. Đơn hàng không hợp lệ hoặc đã được xử lý.";
                return RedirectToAction("DonDaDat");  // Quay lại trang danh sách đơn hàng đã đặt
            }

            // Cập nhật trạng thái đơn hàng thành -1 (Đã hủy)
            order.MaTrangThai = -1;
            _context.SaveChanges();

            TempData["Success"] = "Đơn hàng đã được hủy thành công.";
            return RedirectToAction("DonDaHuy");  // Chuyển hướng đến trang đơn hàng đã hủy
        }


        // 📌 Đơn hàng đang vận chuyển
        public IActionResult DangVanChuyen()
        {
            if (string.IsNullOrEmpty(UserId))
            {
                HttpContext.Session.SetString("ReturnUrl", Url.Action("DangVanChuyen", "HoaDon"));
                return RedirectToAction("DangNhap", "KhachHang");
            }
            var orders = _context.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.ChiTietHds)
                .Where(h => h.MaKh == UserId && h.MaTrangThai == 3) // Trạng thái 3: Đang vận chuyển
                .OrderByDescending(h => h.NgayDat) // Sắp xếp theo ngày đặt mới nhất
                .ToList();

            return View(orders);
        }

        // 📌 Đơn hàng đã hủy
        public IActionResult DonDaHuy()
        {
            if (string.IsNullOrEmpty(UserId))
            {
                // Lưu URL của trang này để sau khi đăng nhập có thể quay lại
                HttpContext.Session.SetString("ReturnUrl", Url.Action("DonDaHuy", "HoaDon"));
                return RedirectToAction("DangNhap", "KhachHang");
            }

            var orders = _context.HoaDons
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.MaKhNavigation)
                .Where(h => h.MaKh == UserId && h.MaTrangThai == -1)  // Chỉ lấy đơn hàng đã hủy của người dùng
                .OrderByDescending(h => h.NgayDat)
                .ToList();


            return View(orders);
        }

        // 📌 Đơn hàng đã nhận
        public IActionResult DonDaNhan()
        {
            if (string.IsNullOrEmpty(UserId))
            {
                HttpContext.Session.SetString("ReturnUrl", Url.Action("DonDaNhan", "HoaDon"));
                return RedirectToAction("DangNhap", "KhachHang");
            }
            var orders = _context.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.ChiTietHds)
                .Where(h => h.MaKh == UserId && h.MaTrangThai == 5) // Kiểm tra NULL trước
                .OrderByDescending(h => h.NgayDat)  // Trả về DateTime.MinValue nếu NULL
                .ToList();

            return View(orders);
        }
        // Hiển thị danh sách đơn hàng chờ xác nhận (Trạng thái = 4)
        public IActionResult ChoXacNhan()
        {
            if (string.IsNullOrEmpty(UserId))
            {
                HttpContext.Session.SetString("ReturnUrl", Url.Action("ChoXacNhan", "HoaDon"));
                return RedirectToAction("DangNhap", "KhachHang");
            }

            var orders = _context.HoaDons
                .Include(h => h.MaKhNavigation)
                .Where(h => h.MaKh == UserId && h.MaTrangThai == 4)
                .OrderByDescending(h => h.NgayDat)
                .ToList();

            return View("ChoXacNhan", orders);
        }

        // Xác nhận đơn hàng đã nhận (Chuyển từ trạng thái = 4 -> 5)
        [HttpPost]
        public IActionResult XacNhanDaNhan(int id)
        {
            var hoaDon = _context.HoaDons.FirstOrDefault(h => h.MaHd == id && h.MaTrangThai == 4);
            if (hoaDon == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng hoặc đơn hàng không hợp lệ." });
            }

            hoaDon.MaTrangThai = 5; // Cập nhật trạng thái thành "Đã nhận"
            _context.SaveChanges();

            return Json(new { success = true, message = "Đã xác nhận đơn hàng thành công!" });
        }

    }
}
