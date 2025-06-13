using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.Models;
using ECommerceMVC.Models.ViewModels;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace ECommerceMVC.Controllers
{
	public class MuaChungController : BaseController
	{
		private readonly MuaChungContext _context;
        private readonly IEmailService _emailService;
        public MuaChungController(MuaChungContext context, IEmailService emailService)
		{
			_context = context;
            _emailService = emailService;

        }
		public IActionResult Index(int price = 0, int page = 1)
		{
			int pageSize = 9;

			var query = _context.GiamGias
				.Where(gg => gg.IsActive == true && gg.EndTime >= DateTime.Now && gg.StartTime <= DateTime.Now)
				.Join(_context.HangHoas, gg => gg.MaHh, hh => hh.MaHh, (gg, hh) => new HangHoaVM
				{
					MaHh = hh.MaHh,
					TenHh = hh.TenHh,
					DonGia = hh.DonGia,
					Hinh = hh.Hinh,
					MoTa = hh.MoTa,
					GiamGia = gg.TyLeGG,
                    MaGG = gg.MaGG,
					IsGiamGia = true,
					EndTime = gg.EndTime,
                    DaThamGia = _context.ThamGiaMuaChungs
                .Any(t => t.MaGG == gg.MaGG && t.MaKh == UserId) // Kiểm tra nếu đã tham gia chương trình giảm giá
                });

			if (price > 0)
				query = query.Where(p => p.DonGia * (1 - p.GiamGia / 100.0) <= price);

			var totalItems = query.Count(); // Đếm số sản phẩm hợp lệ
			var hangHoas = query
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToList();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
			ViewBag.TotalCount = totalItems; // Truyền số lượng sản phẩm đang giảm giá
			ViewBag.HideSieuSale = true;
			return View(hangHoas);
		}

        [HttpPost]
        public async Task<IActionResult> ThamGia(int maGG)
        {
            try
            {
                var maKh = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(maKh))
                {
                    return RedirectToAction("DangNhap", "KhachHang");
                }

                var giamGia = await _context.GiamGias
                    .Include(g => g.ThamGiaMuaChungs)
                    .Include(g => g.HangHoa)
                    .FirstOrDefaultAsync(g => g.MaGG == maGG);

                if (giamGia == null || !giamGia.IsActive || DateTime.Now < giamGia.StartTime || DateTime.Now > giamGia.EndTime)
                {
                    TempData["Error"] = "Chương trình giảm giá không hợp lệ.";
                    return RedirectToAction("Index");
                }

                var daMuaHang = await _context.HoaDons
                    .AnyAsync(h => h.MaKh == maKh && h.MaTrangThai == 5); // trạng thái đã giao

                if (!daMuaHang)
                {
                    TempData["Error"] = "Bạn cần đã mua ít nhất một đơn hàng để tham gia chương trình.";
                    return RedirectToAction("Index");
                }

                var daThamGia = await _context.ThamGiaMuaChungs
                    .AnyAsync(t => t.MaGG == maGG && t.MaKh == maKh);

                if (daThamGia)
                {
                    TempData["Error"] = "Bạn đã tham gia chương trình này rồi.";
                    return RedirectToAction("Index");
                }

                // Thêm mới tham gia
                var thamGia = new ThamGiaMuaChung
                {
                    MaGG = maGG,
                    MaKh = maKh,
                    NgayThamGia = DateTime.Now
                };

                _context.ThamGiaMuaChungs.Add(thamGia);
                giamGia.SoNguoiThamGia += 1;

                await _context.SaveChangesAsync();

                
                if (giamGia.SoNguoiThamGia >= giamGia.NguoiToiThieu)
                {
                    giamGia.IsActive = false; // ngừng chương trình
                    await _context.SaveChangesAsync();
                    var nguoiThamGia = await _context.ThamGiaMuaChungs
                        .Include(t => t.KhachHang)
                        .Where(t => t.MaGG == maGG)
                        .ToListAsync();

                    foreach (var thamGiaItem in nguoiThamGia)
                    {
                        var kh = thamGiaItem.KhachHang;

                        var hoaDon = new HoaDon
                        {
                            MaKh = kh.MaKh,
                            NgayDat = DateTime.Now,
                            HoTen = kh.HoTen,
                            DiaChi = kh.DiaChi,
                            GhiChu = "Tự động tạo từ chương trình mua chung",
                            NgayCan = DateTime.Now.AddDays(3),
                            MaTrangThai = 0, // chờ duyệt
                            CachThanhToan = "Thanh toán khi nhận"
                        };

                        _context.HoaDons.Add(hoaDon);
                        await _context.SaveChangesAsync(); // để lấy được MaHd

                        var chiTiet = new ChiTietHd
                        {
                            MaHd = hoaDon.MaHd,
                            MaHh = giamGia.MaHh,
                            SoLuong = 1,
                            DonGia = TinhDonGiaSauGiam(giamGia.HangHoa?.DonGia ?? 0, giamGia.TyLeGG), // fallback tránh null
                            GiamGia = giamGia.TyLeGG 
                        };
                        _context.ChiTietHds.Add(chiTiet);

                        // Gửi email thông báo
                        if (!string.IsNullOrEmpty(kh.Email))
                        {
                            var subject = "Chúc mừng! Bạn đã tham gia chương trình mua chung thành công";
                            var body = $@"
                        <h3>Xin chào {kh.HoTen},</h3>
                        <p>Chương trình mua chung bạn tham gia đã đủ người.</p>
                        <p>Sản phẩm đã được thêm vào đơn hàng của bạn với trạng thái <b>chờ duyệt</b>.</p>
                        <p>Vui lòng vào hệ thống để kiểm tra đơn hàng.</p>
                        <br/>
                        <p>Trân trọng,<br/>WEBMUAHANGCHUNG</p>";

                            _emailService.SendEmail(kh.Email, subject, body);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Bạn đã tham gia chương trình mua chung thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        public IActionResult ChiTietGiamGia(int maHh)
        {
            var giamGia = _context.GiamGias
                .Where(gg => gg.MaHh == maHh)
                .OrderByDescending(gg => gg.StartTime)
                .FirstOrDefault();

            if (giamGia == null)
            {
                return Json(new { success = false, message = "Không có chương trình giảm giá nào cho sản phẩm này!" });
            }


            // Kiểm tra trạng thái chương trình giảm giá (đã kết thúc hay chưa)
            bool isEnded = DateTime.Now > giamGia.EndTime;

            return Json(new
            {
                success = true,
                maHh = giamGia.MaHh,
                tyLeGG = giamGia.TyLeGG,
                nguoiToiThieu = giamGia.NguoiToiThieu,
                soNguoiThamGia = giamGia.SoNguoiThamGia,
                startTime = giamGia.StartTime.ToString("yyyy-MM-dd HH:mm"),
                endTime = giamGia.EndTime.ToString("yyyy-MM-dd HH:mm"),
                isActive = giamGia.IsActive,
                isEnded = isEnded
            });
        }
        public double TinhDonGiaSauGiam(double donGiaGoc, double? tyLeGiamGia)
        {
            // Kiểm tra nếu tỷ lệ giảm giá là null thì trả về giá gốc
            if (tyLeGiamGia == null)
            {
                return donGiaGoc;  // Nếu không có giảm giá thì dùng giá gốc
            }

            // Tính giá sau khi áp dụng giảm giá
            return donGiaGoc * (1 - tyLeGiamGia.Value/100);
        }
    }
}
