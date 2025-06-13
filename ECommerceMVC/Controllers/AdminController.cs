using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.Models;
using ECommerceMVC.Models.ViewModels;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Mail;
using System.Net;
using Microsoft.CodeAnalysis;

namespace ECommerceMVC.Controllers
{
    public class AdminController : BaseController
    {
        private readonly MuaChungContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        public AdminController(MuaChungContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _context = context;
            _env = env;
            _configuration = configuration;
        }
        public IActionResult Index()
        {

            var donHangs = _context.HoaDons
                .Where(d => d.MaTrangThai == 5)  // Hoặc kiểm tra theo status hoàn tất
                .Include(d => d.ChiTietHds)
                .ToList();

            double? tongDoanhThu = 0;
            int tongSoLuong = 0;

            foreach (var dh in donHangs)
            {
                foreach (var ct in dh.ChiTietHds)
                {
                    tongDoanhThu += ct.SoLuong * ct.DonGia;
                    tongSoLuong += ct.SoLuong;
                }
            }
            // Truy vấn dữ liệu doanh thu theo tháng/năm
            var doanhThuData = _context.HoaDons
                .Where(h => h.MaTrangThai == 5) // Chỉ lấy các đơn hàng đã thành công
                .SelectMany(
                    h => _context.ChiTietHds
                        .Where(c => c.MaHd == h.MaHd),
                    (h, c) => new { HoaDon = h, ChiTietHd = c })
                .AsEnumerable() // Chuyển dữ liệu về client-side
                .GroupBy(
                    x => new { x.HoaDon.NgayDat.Year, x.HoaDon.NgayDat.Month })
                .Select(g => new
                {
                    ThoiGian = $"{g.Key.Month:D2}/{g.Key.Year}",
                    TongDoanhThu = g.Sum(e => e.ChiTietHd.DonGia * e.ChiTietHd.SoLuong * (1 - e.ChiTietHd.GiamGia))
                })
                .OrderBy(e => e.ThoiGian)
                .ToList();
            // Lấy top 5 sản phẩm có lượt xem cao nhất
            var topSanPham = _context.HangHoas
                .OrderByDescending(h => h.SoLanXem)
                .Select(h => new
                {
                    h.TenHh,
                    h.Hinh,
                    h.DonGia,
                    h.SoLanXem
                })
                .ToList();
            // Lấy danh sách khách hàng với tổng giá trị tất cả các hóa đơn
            // Lấy tổng tiền tất cả các hóa đơn của khách hàng
            var topKhachHang = _context.HoaDons
                .Where(d => d.MaTrangThai == 5) // Hoặc bạn có thể lọc theo điều kiện khác nếu cần
                .SelectMany(d => d.ChiTietHds, (d, c) => new
                {
                    MaKh = d.MaKh,
                    TenKh = d.MaKhNavigation.HoTen, // Lấy tên khách hàng
                    Email = d.MaKhNavigation.Email, // Lấy email
                    AnhDaiDien = d.MaKhNavigation.Hinh, // Lấy ảnh đại diện
                    TongTienHoaDon = c.DonGia * c.SoLuong * (1 - c.GiamGia / 100) // Tính tổng tiền cho từng hóa đơn
                })
                .GroupBy(x => x.MaKh) // Nhóm theo mã khách hàng
                .Select(group => new
                {
                    MaKh = group.Key,
                    TenKh = group.FirstOrDefault().TenKh,
                    Email = group.FirstOrDefault().Email,
                    AnhDaiDien = group.FirstOrDefault().AnhDaiDien,
                    TongHoaDon = group.Sum(g => g.TongTienHoaDon) // Tính tổng doanh thu của mỗi khách hàng
                })
                .OrderByDescending(kh => kh.TongHoaDon) // Sắp xếp theo tổng hóa đơn từ cao đến thấp 
                .ToList();
            //Du lieuj hoa don
            // Lấy danh sách hóa đơn có trạng thái hoàn tất
            var hoaDonList = _context.HoaDons
                .Where(hd => hd.MaTrangThai == 5)
                .Select(hd => new
                {
                    MaHd = hd.MaHd,
                    HoTen = hd.MaKhNavigation.HoTen,
                    Hinh = hd.MaKhNavigation.Hinh,
                    NgayDat = hd.NgayDat,
                    ChiTietHds = hd.ChiTietHds.Select(ct => new
                    {
                        TenHh = ct.MaHhNavigation.TenHh,
                        Hinh = ct.MaHhNavigation.Hinh,
                        GiamGia = ct.GiamGia,
                        DonGia = ct.DonGia ?? 0, // Nếu không có đơn giá, mặc định là 0
                        SoLuong = ct.SoLuong
                    }).ToList()
                })
                .OrderByDescending(hd => hd.NgayDat) // Sắp xếp theo ngày đặt hóa đơn
                .ToList();

            // Dữ liệu sản phẩm đã bán (modal sản phẩm)
            var sanPhamDaBan = _context.ChiTietHds
                .Where(c => c.MaHdNavigation.MaTrangThai == 5)
                .Include(c => c.MaHhNavigation)
                .ThenInclude(h => h.MaLoaiNavigation)
                .GroupBy(c => new
                    {
                        c.MaHh,
                        c.MaHhNavigation.TenHh,
                        c.MaHhNavigation.DonGia,
                        c.MaHhNavigation.Hinh,
                        Loai = c.MaHhNavigation.MaLoaiNavigation.TenLoai
                    })
                .Select(g => new
                    {
                        TenHh = g.Key.TenHh,
                        DonGia = g.Key.DonGia,
                        Hinh = g.Key.Hinh,
                        Loai = g.Key.Loai,
                        TongSoLuong = g.Sum(x => x.SoLuong)
                    })
                .OrderByDescending(x => x.TongSoLuong)
            .ToList();
            // Thống kê loại hàng
            var thongKe = _context.Loais.Select(loai => new MenuLoaiVM
            {
                MaLoai = loai.MaLoai,
                TenLoai = loai.TenLoai,
                SoLuong = loai.HangHoas.Count(),
                SanPhams = loai.HangHoas.Select(sp => new SanPhamVM
                {
                    MaHh = sp.MaHh,
                    TenHh = sp.TenHh,
                    DonGia = sp.DonGia,
                    Hinh = sp.Hinh
                }).ToList()
            }).ToList();
            var dsDon = _context.HoaDons.ToList();
            var donXuLy = dsDon.Count(d => d.MaTrangThai == 0); //"Đang xử lý"
            var donDaGiao = dsDon.Count(d => d.MaTrangThai == 3); //"Đã giao"
            var donBiHuy = dsDon.Count(d => d.MaTrangThai == -1);//"Đã hủy"
            var tong = dsDon.Count();
            var hoaDonListTong = _context.HoaDons
                .Select(hd => new
                {
                    MaHd = hd.MaHd,
                    HoTen = hd.MaKhNavigation.HoTen,
                    Hinh = hd.MaKhNavigation.Hinh,
                    NgayDat = hd.NgayDat,
                    TenTrangThai = hd.MaTrangThaiNavigation.TenTrangThai,
                    ChiTietHds = hd.ChiTietHds.Select(ct => new
                    {
                        TenHh = ct.MaHhNavigation.TenHh,
                        Hinh = ct.MaHhNavigation.Hinh,
                        GiamGia = ct.GiamGia,
                        DonGia = ct.DonGia ?? 0, // Nếu không có đơn giá, mặc định là 0
                        SoLuong = ct.SoLuong
                    }).ToList()
                })
                .OrderByDescending(hd => hd.NgayDat) // Sắp xếp theo ngày đặt hóa đơn
                .ToList();
            var hoaDonListHT = _context.HoaDons
                 .Where(hd => hd.MaTrangThai == 3)
                .Select(hd => new
                {
                    MaHd = hd.MaHd,
                    HoTen = hd.MaKhNavigation.HoTen,
                    Hinh = hd.MaKhNavigation.Hinh,
                    NgayDat = hd.NgayDat,
                    TenTrangThai = hd.MaTrangThaiNavigation.TenTrangThai,
                    ChiTietHds = hd.ChiTietHds.Select(ct => new
                    {
                        TenHh = ct.MaHhNavigation.TenHh,
                        Hinh = ct.MaHhNavigation.Hinh,
                        GiamGia = ct.GiamGia,
                        DonGia = ct.DonGia ?? 0, // Nếu không có đơn giá, mặc định là 0
                        SoLuong = ct.SoLuong
                    }).ToList()
                })
                .OrderByDescending(hd => hd.NgayDat) // Sắp xếp theo ngày đặt hóa đơn
                .ToList();
                        var hoaDonListHuy = _context.HoaDons
                .Where(hd => hd.MaTrangThai == -1)
                .Select(hd => new
                {
                    MaHd = hd.MaHd,
                    HoTen = hd.MaKhNavigation.HoTen,
                    Hinh = hd.MaKhNavigation.Hinh,
                    NgayDat = hd.NgayDat,
                    TenTrangThai = hd.MaTrangThaiNavigation.TenTrangThai,
                    ChiTietHds = hd.ChiTietHds.Select(ct => new
                    {
                        TenHh = ct.MaHhNavigation.TenHh,
                        Hinh = ct.MaHhNavigation.Hinh,
                        GiamGia = ct.GiamGia,
                        DonGia = ct.DonGia ?? 0, // Nếu không có đơn giá, mặc định là 0
                        SoLuong = ct.SoLuong
                    }).ToList()
                })
                .OrderByDescending(hd => hd.NgayDat) // Sắp xếp theo ngày đặt hóa đơn
                .ToList();
                        var hoaDonListXL = _context.HoaDons
                .Where(hd => hd.MaTrangThai == 0)
                .Select(hd => new
                {
                    MaHd = hd.MaHd,
                    HoTen = hd.MaKhNavigation.HoTen,
                    Hinh = hd.MaKhNavigation.Hinh,
                    NgayDat = hd.NgayDat,
                    TenTrangThai = hd.MaTrangThaiNavigation.TenTrangThai,
                    ChiTietHds = hd.ChiTietHds.Select(ct => new
                    {
                        TenHh = ct.MaHhNavigation.TenHh,
                        Hinh = ct.MaHhNavigation.Hinh,
                        GiamGia = ct.GiamGia,
                        DonGia = ct.DonGia ?? 0, // Nếu không có đơn giá, mặc định là 0
                        SoLuong = ct.SoLuong
                    }).ToList()
                })
                .OrderByDescending(hd => hd.NgayDat) // Sắp xếp theo ngày đặt hóa đơn
                .ToList();
            double tyLeHuy = tong > 0 ? (double)donBiHuy / tong * 100 : 0;

            var thoiGianTB = dsDon
                .Where(d => d.MaTrangThai == 5 && d.NgayGiao != null && d.NgayGiao.HasValue
                            && d.NgayGiao > d.NgayDat
                            && d.NgayGiao.Value.Year >= 2020)
                .Select(d => (d.NgayGiao.Value - d.NgayDat).TotalHours)
                .DefaultIfEmpty(0)
                .Average();

            var thongKeTinhTrang = new ThongKeDonHangVM
            {
                TongDonHang = tong,
                DonDangXuLy = donXuLy,
                DonDaGiao = donDaGiao,
                DonBiHuy = donBiHuy,
                TyLeHuy = Math.Round(tyLeHuy, 2),
                ThoiGianXuLyTrungBinh = Math.Round(thoiGianTB, 2)
            };
            // Làm tròn và chuyển sang ngày & giờ
            var tbTimeSpan = TimeSpan.FromHours(Math.Round(thoiGianTB, 0));
            var days = tbTimeSpan.Days;
            var hours = tbTimeSpan.Hours;
            // Lấy ngày hiện tại, đầu tuần, đầu tháng
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var hoaDonGiao = _context.HoaDons
                .Include(hd => hd.ChiTietHds)
                .Where(hd => hd.MaTrangThai == 5)
                .ToList(); // ToList đảm bảo dữ liệu đã load để dùng LINQ to Object

            double? TinhTongTien(HoaDon hd) =>
                hd.ChiTietHds.Sum(ct =>
                    ct.SoLuong *( ct.DonGia * (1 - (ct.GiamGia / 100)))
                );

            var doanhThuToday = hoaDonGiao
                .Where(hd => hd.NgayGiao == today)
                .Sum(hd => TinhTongTien(hd));

            var doanhThuWeek = hoaDonGiao
                .Where(hd => hd.NgayGiao >= startOfWeek && hd.NgayGiao <= today)
                .Sum(hd => TinhTongTien(hd));

            var doanhThuMonth = hoaDonGiao
                .Where(hd => hd.NgayGiao >= startOfMonth && hd.NgayGiao <= today)
                .Sum(hd => TinhTongTien(hd));

            var soSpToday = hoaDonGiao
                .Where(hd => hd.NgayGiao == today )
                .SelectMany(hd => hd.ChiTietHds)
                .Sum(ct => ct.SoLuong);

            var soSpWeek = hoaDonGiao
                .Where(hd => hd.NgayGiao >= startOfWeek && hd.NgayDat.Date <= today)
                .SelectMany(hd => hd.ChiTietHds)
                .Sum(ct => ct.SoLuong);

            var soSpMonth = hoaDonGiao
                .Where(hd => hd.NgayGiao >= startOfMonth && hd.NgayGiao <= today)
                .SelectMany(hd => hd.ChiTietHds)
                .Sum(ct => ct.SoLuong);

            // Truyền dữ liệu doanh thu vào View
            // Truyền sang ViewBag
            ViewBag.TopKhachHang = topKhachHang;
            ViewBag.TopSanPham = topSanPham;
            ViewBag.DoanhThuData = doanhThuData;
            ViewBag.TongDoanhThu = tongDoanhThu;
            ViewBag.HoaDonList = hoaDonList;
            ViewBag.SanPhamDaBan = sanPhamDaBan;
            ViewBag.TongSoLuong = tongSoLuong;
            ViewBag.ThongKeLoaiHang = thongKe;
            ViewBag.TinhTrangDonHang = thongKeTinhTrang;
            ViewBag.ThoiGianTrungBinh = $"{days} ngày {hours} giờ";
            ViewBag.TongHoaDon = hoaDonListTong;
            ViewBag.HoaDonHT = hoaDonListHT;
            ViewBag.HoaDonHuy = hoaDonListHuy;
            ViewBag.HoaDonXL = hoaDonListXL;
            ViewBag.DoanhThuToday = doanhThuToday;
            ViewBag.DoanhThuWeek = doanhThuWeek;
            ViewBag.DoanhThuMonth = doanhThuMonth;
            ViewBag.SoSpToday = soSpToday;
            ViewBag.SoSpWeek = soSpWeek;
            ViewBag.SoSpMonth = soSpMonth;

            return View();
        }

        // 📌 Hiển thị danh sách khách hàng
        public IActionResult KhachHang()
        {
            var khachHangs = _context.KhachHangs.ToList();
            return View(khachHangs);
        }

        // 📌 Cập nhật trạng thái tài khoản (AJAX)
        [HttpPost]
        public IActionResult CapNhatHieuLuc(String id, int trangThai)
        {
            var khachHang = _context.KhachHangs.Find(id);
            if (khachHang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khách hàng!" });
            }

            khachHang.HieuLuc = trangThai == 1; // Chuyển đổi về bool
            _context.SaveChanges(); // Lưu vào database

            return Json(new { success = true, message = "Cập nhật thành công!" });
        }
        // 📌 Hiển thị danh sách nhân viên
        public IActionResult NhanVien()
        {
            // Danh sách nhân viên đã kích hoạt
            var nhanVienKichHoat = _context.NhanViens.Where(nv => nv.KichHoat == true).ToList();

            // Danh sách nhân viên chưa kích hoạt (đang chờ duyệt)
            var nhanVienChoDuyet = _context.NhanViens.Where(nv => nv.KichHoat == false).ToList();

            // Truyền 2 danh sách vào ViewModel hoặc ViewBag
            ViewBag.NhanVienKichHoat = nhanVienKichHoat;
            ViewBag.NhanVienChoDuyet = nhanVienChoDuyet;
            return View(nhanVienKichHoat);
        }
        [HttpGet]
        public IActionResult ChiTietNhanVien(string id)
        {
            var nv = _context.NhanViens.FirstOrDefault(x => x.MaNv == id);
            if (nv == null)
                return Content("Không tìm thấy nhân viên");

            return PartialView("_ChiTietNhanVien", nv);
        }

        [HttpPost]
        public IActionResult DuyetNhanVien(string id)
        {
            var nv = _context.NhanViens.Find(id);
            if (nv == null) return Json(new { success = false });

            nv.KichHoat = true; // kích hoạt tài khoản
            _context.SaveChanges();
            return Json(new { success = true });
        }
        // Thêm nhân viên
        [HttpPost]
        public IActionResult ThemNhanVien(NhanVien nv)
        {
            if (_context.NhanViens.Any(x => x.MaNv == nv.MaNv))
                return Json(new { success = false, message = "Mã nhân viên đã tồn tại!" });

            _context.NhanViens.Add(nv);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        // Xóa nhân viên
        [HttpPost]
        public IActionResult XoaNhanVien(string id)
        {
            var nv = _context.NhanViens.Find(id);
            if (nv == null) return Json(new { success = false });

            _context.NhanViens.Remove(nv);
            _context.SaveChanges();
            return Json(new { success = true });
        }
        [HttpPost]
        public IActionResult CapNhatHieuLucNhanVien(string id, int trangThai)
        {
            var nv = _context.NhanViens.Find(id);
            if (nv == null)
            {
                return Json(new { success = false, message = "Không tìm thấy nhân viên!" });
            }

            nv.HieuLuc = trangThai == 1;
            _context.SaveChanges();

            return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
        }
        //Xem chi tiet san pham
        [HttpGet]
        public JsonResult ChiTietSanPham(int id)
        {
            var sp = _context.HangHoas
                             .Where(h => h.MaHh == id)
                             .Select(h => new
                             {
                                 tenHh = h.TenHh,
                                 hinh = h.Hinh,
                                 donGia = h.DonGia,
                                 giamGia = h.GiamGia,
                                 moTaDonVi = h.MoTaDonVi,
                                 moTa = h.MoTa,
                                 danhMuc = h.MaLoaiNavigation.TenLoai,
                                 nhaCungCap = h.MaNccNavigation.TenCongTy,
                                 soLanXem = h.SoLanXem
                             }).FirstOrDefault();

            if (sp == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });
            }

            return Json(new { success = true, data = sp });
        }

        // 📌 Xóa khách hàng (AJAX)
        [HttpPost]
        public async Task<IActionResult> XoaKhachHang(string id)
        {
            var khachHang = await _context.KhachHangs.FindAsync(id);
            if (khachHang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khách hàng!" });
            }

            _context.KhachHangs.Remove(khachHang);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa khách hàng thành công!" });
        }



        // 📌 Hiển thị danh sách sản phẩm
        public IActionResult SanPham()
        {
            var sanPhams = _context.HangHoas
       .Include(hh => hh.MaLoaiNavigation) // Load danh mục sản phẩm nếu có
       .ToList();

            if (sanPhams == null || !sanPhams.Any())
            {
                return View(new List<HangHoa>()); // Tránh lỗi NullReferenceException
            }
            ViewBag.DanhMuc = _context.Loais.ToList();
            ViewBag.NhaCungCap = _context.NhaCungCaps.ToList();
            return View(sanPhams);
        }

        // 📌 Controller: Thêm sản phẩm (AJAX)
        [HttpPost]
        public async Task<IActionResult> ThemSanPham(HangHoaVM model, IFormFile? HinhFile)
        {
            if (string.IsNullOrEmpty(model.TenHH))
                return Json(new { success = false, message = "Tên sản phẩm không được để trống!" });

            var hangHoa = new HangHoa
            {
                TenHh = model.TenHH,
                DonGia = model.DonGia,
                GiamGia = model.GiamGia,
                MoTaDonVi = model.MoTaDonVi ?? "",
                MoTa = model.MoTa ?? "",
                MaLoai = model.MaLoai,
                MaNcc = model.MaNcc,
                SoLanXem = 0, // Mặc định số lần xem = 0
                TenAlias = "" // Mặc định TenAlias rỗng
            };

            if (HinhFile != null && HinhFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Hinh/HangHoa");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string originalFileName = Path.GetFileNameWithoutExtension(HinhFile.FileName).Trim().Replace(" ", "_");
                string fileExtension = Path.GetExtension(HinhFile.FileName);
                string uniqueFileName = $"{DateTime.Now.Ticks}_{originalFileName}{fileExtension}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await HinhFile.CopyToAsync(stream);
                }

                hangHoa.Hinh = uniqueFileName;
            }

            _context.HangHoas.Add(hangHoa);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Thêm sản phẩm thành công!" });
        }


        // 📌 Cập nhật sản phẩm (AJAX)
        [HttpPost]
        public async Task<IActionResult> SuaSanPham(HangHoaVM model, IFormFile? HinhFile)
        {
            if (model.MaHh == 0)
                return Json(new { success = false, message = "Thiếu ID sản phẩm!" });

            var hangHoa = await _context.HangHoas.FindAsync(model.MaHh);
            if (hangHoa == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });

            // 🟡 Cập nhật thông tin sản phẩm
            hangHoa.TenHh = model.TenHH;
            hangHoa.DonGia = model.DonGia;
            hangHoa.GiamGia = model.GiamGia;
            hangHoa.MoTaDonVi = model.MoTaDonVi ?? "";
            hangHoa.MoTa = model.MoTa ?? "";
            hangHoa.MaLoai = model.MaLoai;
            hangHoa.MaNcc = model.MaNcc;

            string imageUrl = null;
            if (HinhFile != null && HinhFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Hinh/HangHoa");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Lấy tên file gốc, bỏ khoảng trắng và ký tự đặc biệt
                string originalFileName = Path.GetFileNameWithoutExtension(HinhFile.FileName).Trim().Replace(" ", "_");
                string fileExtension = Path.GetExtension(HinhFile.FileName);

                // Tạo tên file theo format MaHh_tenfile.dinhdang
                string uniqueFileName = $"{model.MaHh}_{originalFileName}{fileExtension}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Xóa hình cũ nếu có
                if (!string.IsNullOrEmpty(hangHoa.Hinh))
                {
                    string oldFilePath = Path.Combine(uploadsFolder, hangHoa.Hinh);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Lưu hình mới
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await HinhFile.CopyToAsync(stream);
                }

                imageUrl = "/Hinh/HangHoa/" + uniqueFileName;
                hangHoa.Hinh = uniqueFileName;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật sản phẩm thành công!" });
        }


        // 📌 Xóa sản phẩm (AJAX)
        [HttpPost]
        public async Task<IActionResult> XoaSanPham(int id)
        {
            var hangHoa = await _context.HangHoas.FindAsync(id);
            if (hangHoa == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });

            _context.HangHoas.Remove(hangHoa);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa sản phẩm thành công!" });
        }
        [HttpPost]
        public IActionResult CapNhatGiamGia(int maHh, bool isGiamGia)
        {
            var sanPham = _context.HangHoas.Find(maHh);
            if (sanPham == null)
            {
                return Json(new { success = false });
            }

            sanPham.IsGiamGia = isGiamGia;
            _context.SaveChanges();

            return Json(new { success = true });
        }
        // Hiển thị danh sách đơn hàng theo trạng thái
        public IActionResult DonHang()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetDonHangTheoTrangThai(int trangThai)
        {
            var donHangs = _context.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .Where(h => h.MaTrangThai == trangThai)
                .OrderByDescending(h => h.NgayDat)
                .Select(h => new
                {
                    MaHd = h.MaHd,
                    HoTen = h.HoTen ?? h.MaKhNavigation.HoTen,
                    NgayDat = h.NgayDat.ToString("dd/MM/yyyy"),
                    NgayCan = h.NgayCan.HasValue ? h.NgayCan.Value.ToString("dd/MM/yyyy") : "Chưa có",
                    NgayGiao = h.NgayGiao.HasValue ? h.NgayGiao.Value.ToString("dd/MM/yyyy") : "Chưa có",
                    MaTrangThai = h.MaTrangThai
                })
                .ToList();

            // Kiểm tra dữ liệu
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(donHangs));

            return Json(donHangs);
        }
        public IActionResult GetChiTietDonHang(int maHd)
        {
            var donHang = _context.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.ChiTietHds)
                .ThenInclude(ct => ct.MaHhNavigation)
                .FirstOrDefault(h => h.MaHd == maHd);

            if (donHang == null)
                return NotFound();
            var tongTien = donHang.ChiTietHds.Sum(ct => ct.SoLuong * ct.DonGia);
            var result = new
            {
                maHd = donHang.MaHd,
                hoTen = donHang.MaKhNavigation.HoTen,
                ngayDat = donHang.NgayDat.ToString("dd/MM/yyyy"),
                ngayCan = donHang.NgayCan?.ToString("dd/MM/yyyy"),
                ngayGiao = donHang.NgayGiao?.ToString("dd/MM/yyyy"),
                maTrangThai = donHang.MaTrangThai,
                ghiChu = donHang.GhiChu ?? "Không có ghi chú" ?? "Chưa có địa chỉ",
                diaChi = donHang.DiaChi,
                tongTien = tongTien,
                chiTietHds = donHang.ChiTietHds.Select(ct => new
                {
                    hinh = ct.MaHhNavigation.Hinh,
                    tenSp = ct.MaHhNavigation.TenHh,
                    soLuong = ct.SoLuong,
                    gia = ct.DonGia,
                    thanhTien = ct.SoLuong * ct.DonGia
                })
            };

            return Json(result);
        }
        [HttpPost]
        public IActionResult DuyetDonHang(int maHd, DateTime ngayGiao)
        {
            var donHang = _context.HoaDons.FirstOrDefault(h => h.MaHd == maHd);
            if (donHang == null)
            {
                return NotFound();
            }
            var today = DateTime.Today;
            donHang.MaTrangThai = 3; // Chuyển sang trạng thái "Đang vận chuyển"
            donHang.NgayGiao = today; // Cập nhật ngày giao hàng

            _context.SaveChanges();
            return Ok();
        }
        [HttpPost]
        public IActionResult CapNhatTrangThaiDonHang(int maHd, int trangThaiMoi)
        {
            var donHang = _context.HoaDons.FirstOrDefault(h => h.MaHd == maHd);
            if (donHang == null)
            {
                return NotFound(new { message = "Không tìm thấy đơn hàng!" });
            }

            donHang.MaTrangThai = trangThaiMoi; // Cập nhật trạng thái mới
            _context.SaveChanges();

            return Ok(new { message = "Cập nhật trạng thái thành công!" });
        }
        //Quản lý danh mục
        public IActionResult DanhMuc(string selectedType = "Loai")
        {

            ViewBag.SelectedType = selectedType;
            ViewBag.LoaiHangs = _context.Loais.ToList();
            ViewBag.NhaCungCaps = _context.NhaCungCaps.ToList();
            return View();
        }
        // Thêm Loại Hàng
        [HttpPost]
        public async Task<IActionResult> ThemLoai(Loai model, IFormFile Hinh)
        {
            if (ModelState.IsValid)
            {
                if (Hinh != null && Hinh.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Hinh/Loai");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    string originalFileName = Path.GetFileNameWithoutExtension(Hinh.FileName).Trim().Replace(" ", "_");
                    string fileExtension = Path.GetExtension(Hinh.FileName);
                    string uniqueFileName = $"{model.MaLoai}_{originalFileName}{fileExtension}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await Hinh.CopyToAsync(stream);
                    }
                    model.Hinh = $"/Hinh/Loai/{uniqueFileName}";
                }
                _context.Loais.Add(model);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // Sửa Loại Hàng
        [HttpPost]
        public async Task<IActionResult> SuaLoai(Loai model, IFormFile Hinh)
        {
            var loai = _context.Loais.Find(model.MaLoai);
            if (loai != null)
            {
                loai.TenLoai = model.TenLoai;
                loai.TenLoaiAlias = model.TenLoaiAlias;
                loai.MoTa = model.MoTa;
                if (Hinh != null && Hinh.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Hinh/Loai");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    string originalFileName = Path.GetFileNameWithoutExtension(Hinh.FileName).Trim().Replace(" ", "_");
                    string fileExtension = Path.GetExtension(Hinh.FileName);
                    string uniqueFileName = $"{model.MaLoai}_{originalFileName}{fileExtension}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await Hinh.CopyToAsync(stream);
                    }
                    loai.Hinh = $"/Hinh/Loai/{uniqueFileName}";
                }
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // Xóa Loại Hàng
        [HttpPost]
        public IActionResult XoaLoai(int id)
        {
            var loai = _context.Loais.Find(id);
            if (loai != null)
            {
                _context.Loais.Remove(loai);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // Thêm Nhà Cung Cấp
        [HttpPost]
        public async Task<IActionResult> ThemNCC([FromForm] NhaCungCap model, [FromForm] IFormFile Logo)
        {
            if (string.IsNullOrWhiteSpace(model.MaNcc))
            {
                return Json(new { success = false, error = "Mã NCC không được để trống!" });
            }

            // Chuyển mã NCC thành chữ hoa
            model.MaNcc = model.MaNcc.Trim().ToUpper();

            // Kiểm tra nếu Mã NCC đã tồn tại
            if (_context.NhaCungCaps.Any(n => n.MaNcc == model.MaNcc))
            {
                return Json(new { success = false, error = "Mã NCC đã tồn tại!" });
            }

            // Xử lý lưu ảnh Logo nếu có
            if (Logo != null && Logo.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Hinh/NCC");
                Directory.CreateDirectory(uploadsFolder); // Tạo thư mục nếu chưa có

                string uniqueFileName = $"{model.MaNcc}_{Path.GetFileName(Logo.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Logo.CopyToAsync(stream);
                }

                model.Logo = $"/Hinh/NCC/{uniqueFileName}";
            }

            // Lưu vào Database
            _context.NhaCungCaps.Add(model);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }



        // Sửa Nhà Cung Cấp
        [HttpPost]
        public async Task<IActionResult> SuaNCC(NhaCungCap model, IFormFile Logo)
        {
            var ncc = _context.NhaCungCaps.Find(model.MaNcc);
            if (ncc != null)
            {
                ncc.TenCongTy = model.TenCongTy;
                ncc.NguoiLienLac = model.NguoiLienLac;
                ncc.Email = model.Email;
                ncc.DienThoai = model.DienThoai;
                ncc.DiaChi = model.DiaChi;
                ncc.MoTa = model.MoTa;
                if (Logo != null && Logo.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Hinh/NCC");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    string originalFileName = Path.GetFileNameWithoutExtension(Logo.FileName).Trim().Replace(" ", "_");
                    string fileExtension = Path.GetExtension(Logo.FileName);
                    string uniqueFileName = $"{model.MaNcc}_{originalFileName}{fileExtension}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await Logo.CopyToAsync(stream);
                    }
                    ncc.Logo = $"/Hinh/NCC/{uniqueFileName}";
                }
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
        [HttpPost]
        public IActionResult XoaNCC(string maNcc)
        {
            if (string.IsNullOrWhiteSpace(maNcc))
            {
                return Json(new { success = false, error = "Mã NCC không hợp lệ!" });
            }

            // Chuyển mã NCC thành chữ hoa để tránh lỗi không tìm thấy
            maNcc = maNcc.Trim().ToUpper();

            // Tìm NCC trong database
            var nhaCungCap = _context.NhaCungCaps.FirstOrDefault(n => n.MaNcc == maNcc);
            if (nhaCungCap == null)
            {
                return Json(new { success = false, error = "Không tìm thấy nhà cung cấp!" });
            }

            // Xóa ảnh logo nếu có
            if (!string.IsNullOrEmpty(nhaCungCap.Logo))
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", nhaCungCap.Logo.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            // Xóa nhà cung cấp khỏi database
            _context.NhaCungCaps.Remove(nhaCungCap);
            _context.SaveChanges();

            return Json(new { success = true });
        }

        public IActionResult PhanHoi()
        {
            var danhGiaSpham = _context.HangHoas
        .Where(hh => hh.YeuThiches.Any(yt => yt.SoSao.HasValue))
        .Select(hh => new
        {
            MaHh = hh.MaHh,
            TenHh = hh.TenHh,
            Hinh = hh.Hinh,
            TongSoSao = hh.YeuThiches.Where(yt => yt.SoSao.HasValue).Sum(yt => yt.SoSao.Value),
            DiemTrungBinh = hh.YeuThiches.Where(yt => yt.SoSao.HasValue).Average(yt => yt.SoSao.Value),
            DanhGia = hh.YeuThiches
                .Where(yt => yt.SoSao.HasValue)
                .Select(yt => new
                {
                    MaYT = yt.MaYt, // Thêm Mã Yêu Thích để liên kết
                    HoTen = yt.AnDanh == true ? "Khách ẩn danh" : yt.MaKhNavigation.HoTen,
                    HinhKh = yt.AnDanh == true ? "default.jpg" : yt.MaKhNavigation.Hinh,
                    NgayDanhGia = yt.NgayChon,
                    SoSao = yt.SoSao,
                    Comment = yt.Comment,
                    AnDanh = yt.AnDanh,
                    TraLoi = _context.HoiDaps
                        .Where(hd => hd.MaYt == yt.MaYt) // Lấy câu trả lời theo MaYT
                        .Select(hd => new
                        {
                            MaHd = hd.MaHdap,
                            CauHoi = hd.CauHoi,
                            TraLoi = hd.TraLoi,
                            NgayDua = hd.NgayDua,
                            NhanVien = hd.MaNvNavigation.HoTen // Lấy tên nhân viên trả lời
                        }).ToList()
                }).ToList()
        }).ToList();

            ViewBag.DanhGiaSpham = danhGiaSpham;
            ViewBag.GopY = _context.Gopies.ToList();
            return View();
        }
        // Xử lý phản hồi
        [HttpPost]
        public IActionResult PhanHoiGY(string id, string traLoi)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "NhanVien")
            {
                return Json(new { success = false, message = "Chỉ nhân viên mới có quyền gửi phản hồi!" });
            }
            var gopY = _context.Gopies.FirstOrDefault(g => g.MaGy == id);
            if (gopY != null)
            {
                gopY.CanTraLoi = true;
                gopY.TraLoi = traLoi;
                gopY.NgayTl = DateOnly.FromDateTime(DateTime.Now);
                _context.SaveChanges();

                // Gửi email phản hồi
                SendReplyEmail(gopY.Email, traLoi);
            }
            return RedirectToAction("PhanHoi");
        }

        // Hàm gửi email phản hồi
        private void SendReplyEmail(string to, string replyMessage)
        {
            var smtpSettings = _configuration.GetSection("Smtp");

            using (var smtpClient = new SmtpClient(smtpSettings["Server"]))
            {
                smtpClient.Port = int.Parse(smtpSettings["Port"]);
                smtpClient.Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]);
                smtpClient.EnableSsl = true;

                using (var mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(smtpSettings["From"]);
                    mailMessage.To.Add(to);
                    mailMessage.Subject = "Phản hồi từ ADMIN WEBMUAHANGCHUNG";
                    mailMessage.Body = $@"
                <h3>Xin chào!</h3>
                <p>Chúng tôi đã nhận được góp ý của bạn và đây là phản hồi từ đội ngũ quản trị:</p>
                <blockquote style='border-left: 4px solid #007bff; padding-left: 10px; color: #333;'>{replyMessage}</blockquote>
                <p>Cảm ơn bạn đã đóng góp ý kiến. Nếu có thêm thắc mắc, vui lòng liên hệ lại!</p>
                <br>
                <p>Trân trọng,</p>
                <p><b>ADMIN WEBMUAHANGCHUNG</b></p>
                ";
                    mailMessage.IsBodyHtml = true;

                    smtpClient.Send(mailMessage);
                }
            }
        }

        [HttpPost]
        public IActionResult TraLoiDanhGia(int MaYt, string TraLoi)
        {
            var danhGia = _context.YeuThiches.FirstOrDefault(yt => yt.MaYt == MaYt);
            if (danhGia == null)
            {
                return Json(new { success = false, message = "Đánh giá không tồn tại!" });
            }

            var nhanVienId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(nhanVienId))
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập!" });
            }
            if (userRole != "NhanVien")
            {
                return Json(new { success = false, message = "Chỉ nhân viên mới có quyền gửi phản hồi!" });
            }

            var phanHoi = new HoiDap
            {
                MaYt = MaYt,
                CauHoi = danhGia.Comment,
                TraLoi = TraLoi,
                NgayDua = DateTime.Now,
                MaNv = nhanVienId
            };

            _context.HoiDaps.Add(phanHoi);
            _context.SaveChanges();

            return Json(new { success = true });
        }
        [HttpPost]
        public IActionResult CapNhatTraLoi(int MaHdap, string NoiDungMoi)
        {
            var phanHoi = _context.HoiDaps.FirstOrDefault(hd => hd.MaHdap == MaHdap);
            if (phanHoi != null)
            {
                phanHoi.TraLoi = NoiDungMoi;
                phanHoi.NgayDua = DateTime.Now; // Cập nhật ngày chỉnh sửa
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
        /////////////////////////
        // Hiển thị danh sách sản phẩm trong chương trình Mua Chung
        public IActionResult MuaChung()
        { // Bước 1: Lấy dữ liệu từ database về RAM
            var hangHoas = _context.HangHoas
                .Where(hh => hh.IsGiamGia == true)
                .ToList();

            var sanPhamMuaChung = hangHoas.Select(hh =>
            {
                var giamGia = _context.GiamGias
                    .Where(gg => gg.MaHh == hh.MaHh)
                    .OrderByDescending(gg => gg.StartTime)
                    .FirstOrDefault();

                string trangThai = "Chưa thiết lập";
                bool canSetupGiamGia = false;
                bool canEditGiamGia = false;
                bool canTatGiamGia = false;
                if (giamGia != null)
                {
                    if (giamGia.SoNguoiThamGia >= giamGia.NguoiToiThieu)
                    {
                        // Nếu đã đủ người, thì kết thúc luôn
                        trangThai = "Đã kết thúc";
                        canSetupGiamGia = true;
                        if (giamGia.IsActive)
                        {
                            giamGia.IsActive = false;
                            _context.Update(giamGia);
                            _context.SaveChanges();
                        }
                    }
                    else if (DateTime.Now < giamGia.StartTime || giamGia.IsActive == false)
                    {
                        trangThai = "Chưa bắt đầu";
                        canEditGiamGia = true; // Cho phép sửa giảm giá khi chưa bắt đầu
                    }
                    else if (DateTime.Now >= giamGia.StartTime && DateTime.Now <= giamGia.EndTime && giamGia.IsActive == true)
                    {
                        trangThai = "Đang giảm giá";
                    }


                }
                else
                {
                    trangThai = "Chưa thiết lập";
                    canSetupGiamGia = true; // Cho phép setup giảm giá khi chưa có chương trình giảm giá nào
                }

                return new SanPhamMuaChungViewModel
                {
                    MaHh = hh.MaHh,
                    TenHh = hh.TenHh,
                    DonGia = hh.DonGia,
                    Hinh = hh.Hinh,
                    GiamGia = giamGia,
                    TrangThaiGiamGia = trangThai,
                    CanSetupGiamGia = canSetupGiamGia,
                    CanEditGiamGia = canEditGiamGia,
                    CanTatGiamGia = canTatGiamGia,
                };
            }).ToList();
            // Chia thành 2 nhóm
            var sanPhamDangChay = sanPhamMuaChung
                .Where(sp => sp.TrangThaiGiamGia == "Đang giảm giá" || sp.TrangThaiGiamGia == "Chưa bắt đầu")
                .ToList();

            var sanPhamKetThuc = _context.GiamGias
    .Where(gg => gg.SoNguoiThamGia >= gg.NguoiToiThieu && gg.IsActive == false || gg.EndTime < DateTime.Now && gg.IsActive == true)
    .Join(_context.HangHoas,
          gg => gg.MaHh,
          hh => hh.MaHh,
          (gg, hh) => new SanPhamMuaChungViewModel
          {
              MaHh = hh.MaHh,
              TenHh = hh.TenHh,
              DonGia = hh.DonGia,
              Hinh = hh.Hinh,
              GiamGia = gg,
              TrangThaiGiamGia = "Đã kết thúc",
              CanSetupGiamGia = true,
              CanEditGiamGia = false
          }).ToList();
            ViewBag.SanPhamDangChay = sanPhamDangChay;
            ViewBag.SanPhamKetThuc = sanPhamKetThuc;
            return View(sanPhamMuaChung);
        }

        [HttpPost]
        public IActionResult SetupGiamGia(GiamGia model)
        {
            if (model.StartTime >= model.EndTime)
            {
                return Json(new { success = false, message = "Thời gian kết thúc phải lớn hơn thời gian bắt đầu" });
            }
            // Kiểm tra nếu ngày kết thúc của chương trình giảm giá hiện tại đã qua
            var existingGiamGia = _context.GiamGias
                .Where(gg => gg.MaHh == model.MaHh && gg.IsActive)
                .OrderByDescending(gg => gg.StartTime)
                .FirstOrDefault();

            if (existingGiamGia != null && DateTime.Now < existingGiamGia.EndTime)
            {
                return Json(new { success = false, message = "Chương trình giảm giá hiện tại chưa hết hạn. Không thể tạo thêm chương trình giảm giá." });
            }
            var giamGia = new GiamGia
            {
                MaGG = model.MaGG,
                MaHh = model.MaHh,
                TyLeGG = model.TyLeGG,
                NguoiToiThieu = model.NguoiToiThieu,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                IsActive = model.IsActive
            };

            _context.GiamGias.Add(giamGia);
            _context.SaveChanges();

            return Json(new { success = true });
        }
        [HttpGet]
        public IActionResult ChiTietGiamGia(int maHh, int maGg)
        {
            var giamGia = _context.GiamGias
                .FirstOrDefault(gg => gg.MaGG == maGg && gg.MaHh == maHh);

            if (giamGia == null)
                return Json(new { success = false, message = "Không tìm thấy chương trình giảm giá." });

            var hangHoa = _context.HangHoas.FirstOrDefault(h => h.MaHh == maHh);
            if (hangHoa == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

            var danhSachThamGia = _context.ThamGiaMuaChungs
                .Where(t => t.MaGG == maGg)
                .Include(t => t.KhachHang)
                .ToList();

            var khachHangVMs = danhSachThamGia.Select(t => new KhachHangGiamGiaViewModel
            {
                Makh = t.MaKh,
                HoTen = t.KhachHang?.HoTen ?? "Không có tên",
                Email = t.KhachHang?.Email ?? "Chưa có email",
                Anh = t.KhachHang?.Hinh ?? "default.png",
                NgayThamGia = t.NgayThamGia,
                SoLuongDaMua = _context.HoaDons.Count(hd => hd.MaKh == t.MaKh && hd.MaTrangThai == 5)
            }).ToList();

            var vm = new ChiTietGiamGiaVM
            {
                MaHh = hangHoa.MaHh,
                TenHh = hangHoa.TenHh,
                DonGia = hangHoa.DonGia,
                HinhAnhSanPham = hangHoa.Hinh,
                NguoiToiThieu = giamGia.NguoiToiThieu,
                TyLeGG = giamGia.TyLeGG,
                StartTime = giamGia.StartTime,
                EndTime = giamGia.EndTime,
                SoNguoiThamGia = khachHangVMs.Count,
                KhachHangs = khachHangVMs
            };

            return Json(new { success = true, data = vm });
        }


        [HttpPost]
        public IActionResult SuaGiamGia(GiamGia model)
        {
            if (model.StartTime >= model.EndTime)
            {
                return Json(new { success = false, message = "Thời gian kết thúc phải lớn hơn thời gian bắt đầu" });
            }

            var existingGiamGia = _context.GiamGias
                .FirstOrDefault(gg => gg.MaGG == model.MaGG);

            if (existingGiamGia == null)
            {
                return Json(new { success = false, message = "Không tìm thấy chương trình giảm giá cần sửa!" });
            }

            // Cập nhật lại thông tin
            existingGiamGia.TyLeGG = model.TyLeGG;
            existingGiamGia.NguoiToiThieu = model.NguoiToiThieu;
            existingGiamGia.StartTime = model.StartTime;
            existingGiamGia.EndTime = model.EndTime;
            existingGiamGia.IsActive = model.IsActive;

            _context.SaveChanges();

            return Json(new { success = true });
        }



    }
}
