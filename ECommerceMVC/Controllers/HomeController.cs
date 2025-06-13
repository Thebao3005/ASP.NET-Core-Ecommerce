using ECommerceMVC.Data;
using ECommerceMVC.Models;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;

namespace ECommerceMVC.Controllers
{
	public class HomeController : BaseController
	{
		private readonly ILogger<HomeController> _logger;
        private readonly MuaChungContext _context;
        public HomeController(ILogger<HomeController> logger, MuaChungContext context)
		{
			_logger = logger;
			_context = context;
		}
        public IActionResult Index()
		{
            var sanPhamNoiBat = _context.HangHoas
            .OrderByDescending(hh => hh.SoLanXem) // Lọc theo số lần xem nhiều nhất
            .Take(8) // Lấy 8 sản phẩm nổi bật
            .ToList();
            // Lấy 3 đánh giá mới nhất đã được duyệt
            var danhGias = _context.YeuThiches
                .Include(dg => dg.MaKhNavigation)
                .Where(dg => dg.AnDanh == false)
                .ToList()
                .GroupBy(dg => dg.MaKh) // nhóm theo khách hàng
                .Select(g => g.First()) // lấy bình luận đầu tiên (đã sắp xếp theo ngày hoặc sao)
                .OrderByDescending(dg => dg.SoSao) // sắp xếp lại theo số sao nếu cần
                .Take(5) // lấy 5 bình luận duy nhất từ 5 khách khác nhau
                .ToList();

            ViewBag.DanhGiaNguoiDung = danhGias;
            ViewBag.FeaturedProducts = sanPhamNoiBat;
            return View();
		}

		[Route("/404")]
        public IActionResult PageNotFound()
        {
            return View();
        }

        public IActionResult Privacy()
		{
			return View();
		}

		public IActionResult LienHe()
		{
            ViewBag.ChuDeList = _context.ChuDes
                            .Select(cd => new SelectListItem
                            {
                                Value = cd.MaCd.ToString(),
                                Text = cd.TenCd
                            })
                            .ToList();
            return View();
		}
        [HttpPost]
        public IActionResult Submit(ContactFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ChuDeList = _context.ChuDes
                                            .Select(cd => new SelectListItem
                                            {
                                                Value = cd.MaCd.ToString(),
                                                Text = cd.TenCd
                                            })
                                            .ToList();
                return View("LienHe", model);
            }

            // Kiểm tra MaCd có tồn tại không
            var chuDeExists = _context.ChuDes.Any(cd => cd.MaCd == model.Macd);
            if (!chuDeExists)
            {
                ModelState.AddModelError("MaCd", "Chủ đề không hợp lệ.");
                ViewBag.ChuDeList = _context.ChuDes.Select(cd => new SelectListItem
                { Value = cd.MaCd.ToString(), Text = cd.TenCd }).ToList();
                return View("LienHe", model);
            }

            var gopY = new GopY
            {
                MaGy = Guid.NewGuid().ToString(),
                MaCd = model.Macd,
                HoTen = model.HoTen,
                Email = model.Email,
                DienThoai = model.DienThoai,
                NoiDung = model.NoiDung,
                NgayGy = DateOnly.FromDateTime(DateTime.Now),
                CanTraLoi = false
            };

            try
            {
                _context.Gopies.Add(gopY);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất có thể.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi gửi góp ý: " + ex.Message;
            }

            return RedirectToAction("LienHe");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
        public IActionResult ChinhSachBaoMat()
        {
            return View();
        }
        public IActionResult DieuKhoanSuDung()
        {
            return View();
        }
        public IActionResult BanHangVaHoanTien()
        {
            return View();
        }

    }
}
