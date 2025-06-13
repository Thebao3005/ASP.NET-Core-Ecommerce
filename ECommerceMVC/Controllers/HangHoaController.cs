using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMVC.Controllers
{
	public class HangHoaController : BaseController
	{
		private readonly MuaChungContext db;

		public HangHoaController(MuaChungContext conetxt)
		{
			db = conetxt;
		}

        public IActionResult Index(int? loai, int page = 1)
        {
            int pageSize = 12;

            var hangHoas = db.HangHoas.AsQueryable();

            if (loai.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.MaLoai == loai.Value);
            }

            int totalItems = hangHoas.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var result = hangHoas
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new HangHoaVM
                {
                    MaHh = p.MaHh,
                    TenHH = p.TenHh,
                    DonGia = p.DonGia ?? 0,
                    Hinh = p.Hinh ?? "",
                    MoTaDonVi = p.MoTaDonVi ?? "",
                    TenLoai = p.MaLoaiNavigation.TenLoai
                })
                .ToList();

            // Gửi thông tin phân trang về View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SelectedCategory = loai;

            return View(result);
        }



        public IActionResult Search(string? query)
		{
			var hangHoas = db.HangHoas.AsQueryable();

			if (query != null)
			{
				hangHoas = hangHoas.Where(p => p.TenHh.Contains(query));
			}

			var result = hangHoas.Select(p => new HangHoaVM
			{
				MaHh = p.MaHh,
				TenHH = p.TenHh,
				DonGia = p.DonGia ?? 0,
				Hinh = p.Hinh ?? "",
				MoTaDonVi = p.MoTaDonVi ?? "",
				TenLoai = p.MaLoaiNavigation.TenLoai
			});
			return View(result);
		}


		public IActionResult Detail(int id)
		{
			var data = db.HangHoas
				.Include(p => p.MaLoaiNavigation)
				.SingleOrDefault(p => p.MaHh == id);

			if (data == null)
			{
				TempData["Message"] = $"Không thấy sản phẩm có mã {id}";
				return Redirect("/404");
			}
            // Cập nhật số lần xem
            data.SoLanXem = (data.SoLanXem ?? 0) + 1;
            db.SaveChanges(); // Lưu thay đổi vào database
            // Lấy danh sách đánh giá từ bảng YeuThich
            var danhGia = db.YeuThiches
				.Where(y => y.MaHh == id && y.SoSao.HasValue) // Chỉ lấy đánh giá có số sao
				.Select(y => new
				{
					HoTen = y.AnDanh == true ? "Khách ẩn danh" : y.MaKhNavigation.HoTen,
					Avatar = y.AnDanh == true ? "/img/avatar.jpg"
			: (string.IsNullOrEmpty(y.MaKhNavigation.Hinh) ? "/img/avatar.jpg" : "/Uploads/KhachHang/" + y.MaKhNavigation.Hinh),
					NgayDanhGia = y.NgayChon,
					SoSao = y.SoSao,
					Comment = y.Comment,
					AnDanh = y.AnDanh,
                    TraLoi = db.HoiDaps
                        .Where(hd => hd.MaYt == y.MaYt) // Lấy câu trả lời theo MaYT
                        .Select(hd => new
                        {
                            MaHd = hd.MaHdap,
                            CauHoi = hd.CauHoi,
                            TraLoi = hd.TraLoi,
                            NgayDua = hd.NgayDua,
                            NhanVien = hd.MaNvNavigation.HoTen // Lấy tên nhân viên trả lời
                        }).ToList()

                }).ToList();

			// Đếm số lượng đánh giá cho sản phẩm có mã id
			int soLuongDanhGia = db.YeuThiches.Count(y => y.MaHh == id && y.SoSao.HasValue);

			// Tính tổng số sao của sản phẩm
			int tongSoSao = danhGia.Sum(y => y.SoSao ?? 0);

			// Tính điểm trung bình số sao (tránh lỗi chia cho 0)
			int diemDanhGia = soLuongDanhGia > 0 ? (int)tongSoSao / soLuongDanhGia : 0;

			var result = new ChiTietHangHoaVM
			{
				MaHh = data.MaHh,
				TenHH = data.TenHh,
				DonGia = data.DonGia ?? 0,
				ChiTiet = data.MoTa ?? string.Empty,
				Hinh = data.Hinh ?? string.Empty,
				MoTaDonVi = data.MoTaDonVi ?? string.Empty,
				TenLoai = data.MaLoaiNavigation.TenLoai,
				SoLuongTon = 10, // Tạm thời đặt cứng
				DiemDanhGia = (int)(diemDanhGia), // Làm tròn số sao
                SoLanXem = data.SoLanXem ?? 0 // Truyền số lượt xem ra View
            };
			// Lấy danh sách sản phẩm giảm giá (Mua chung)
			var sanPhamMuaChung = db.GiamGias
				.Where(gg => gg.IsActive && gg.EndTime > DateTime.Now)
				.Join(db.HangHoas, gg => gg.MaHh, hh => hh.MaHh, (gg, hh) => new HangHoaVM
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
					DaThamGia = db.ThamGiaMuaChungs
						.Any(t => t.MaGG == gg.MaGG && t.MaKh == UserId)
				})
				.Where(sp => sp.IsGiamGia && !sp.DaThamGia)
				.Take(5) // Giới hạn số lượng sản phẩm hiển thị
				.ToList();
			var sanPhamNoiBat = db.HangHoas
		   .OrderByDescending(hh => hh.SoLanXem) // Lọc theo số lần xem nhiều nhất
		   .Take(8) // Lấy 8 sản phẩm nổi bật
		   .ToList();
			ViewBag.FeaturedProducts = sanPhamNoiBat;
			ViewBag.DanhGia = danhGia;
			ViewBag.DiemDanhGia = diemDanhGia; // Truyền điểm đánh giá qua ViewBag
			ViewBag.SoLuongDanhGia = soLuongDanhGia; // Truyền số lượng đánh giá
			ViewBag.SanPhamMuaChung = sanPhamMuaChung;
			return View(result);
		}

        // Hành động để lọc sản phẩm theo giá
        public IActionResult FilterByPrice(decimal price, int page = 1)
        {
            int pageSize = 12;

            double maxPrice = Convert.ToDouble(price);

            var query = db.HangHoas
                          .Where(p => p.DonGia <= maxPrice);

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var data = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new HangHoaVM
                {
                    MaHh = p.MaHh,
                    TenHH = p.TenHh,
                    DonGia = p.DonGia ?? 0,
                    Hinh = p.Hinh ?? "",
                    MoTaDonVi = p.MoTaDonVi ?? "",
                    TenLoai = p.MaLoaiNavigation.TenLoai
                }).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.FilterPrice = price;

            return PartialView("ProductItem", data);
        }
		public IActionResult SearchSuggestions(string query)
		{
			if (string.IsNullOrEmpty(query))
			{
				return Json(new List<object>());
			}

			var suggestions = db.HangHoas
				.Where(h => EF.Functions.Like(h.TenHh.ToLower(), $"%{query.ToLower()}%"))
				.Select(h => new 
				{
					  h.MaHh,
					  h.TenHh,
					  h.Hinh,
					  h.DonGia
				})
				.Take(5)
				.ToList();
            return Json(suggestions);  // Trả về gợi ý tìm kiếm dưới dạng JSON
		}




	}
}
