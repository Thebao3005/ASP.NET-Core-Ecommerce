using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
namespace ECommerceMVC.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly MuaChungContext db;
        private readonly IEmailService _emailService;
        public KhachHangController(MuaChungContext context, IEmailService emailService)
        {
            db = context;
            _emailService = emailService;
        }
        // Kiểm tra trùng Email, SĐT
        private bool CheckExist(string email, string dienThoai)
        {
            return db.KhachHangs.Any(kh => kh.Email == email || kh.DienThoai == dienThoai);
        }
        // Hiển thị form đăng nhập
        [HttpGet]
        public IActionResult DangNhap()
        {
            return View();
        }

        // Xử lý đăng nhập
        [HttpPost]
        public IActionResult DangNhap(LoginVM model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.Email == model.Email);
            var nhanVien = db.NhanViens.FirstOrDefault(nv => nv.Email == model.Email);

            if (khachHang == null && nhanVien == null)
            {
                ModelState.AddModelError("", "❌ Tài khoản chưa được đăng ký.");
                return View(model);
            }
            if (khachHang != null)
            {
                // So sánh trực tiếp mật khẩu (Không mã hóa)
                if (khachHang.MatKhau != model.MatKhau)
                {
                    ModelState.AddModelError("", "❌ Mật khẩu không đúng.");
                    return View(model);
                }
                else if (khachHang.HieuLuc == false)
                {
                    ModelState.AddModelError("", "❌ Tài khoản hiện đang bị khóa vui lòng liên hệ Admin để mở khóa.");
                    return View(model);
                }
                if (khachHang.VaiTro == 0)
                {
                    // Lưu thông tin vào Session
                    HttpContext.Session.SetString("UserId", khachHang.MaKh);
                    HttpContext.Session.SetString("UserName", khachHang.HoTen);
                    HttpContext.Session.SetString("UserRole", khachHang.VaiTro.ToString());

                    return RedirectToAction("Index", "Admin");
                }
                else if (khachHang.VaiTro == 2)
                {
                    // Lưu thông tin vào Session
                    HttpContext.Session.SetString("UserId", khachHang.MaKh);
                    HttpContext.Session.SetString("UserName", khachHang.HoTen);
                    HttpContext.Session.SetString("UserRole", khachHang.VaiTro.ToString());


                    TempData["SuccessMessage"] = "✅ Đăng nhập thành công!";
                    Console.WriteLine("✅ Đăng nhập thành công, chuyển hướng đến trang chủ");
                    // Nếu có returnUrl, ưu tiên quay lại đó
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    // Nếu có sản phẩm chờ, thêm vào giỏ hàng
                    if (HttpContext.Session.GetInt32("PendingProductId").HasValue)
                    {
                        return RedirectToAction("ConfirmPendingCart", "Cart");
                    }
                    // Kiểm tra nếu có ReturnUrl lưu trong session và điều hướng về đó
                    string returnUrlFromSession = HttpContext.Session.GetString("ReturnUrl");
                    if (!string.IsNullOrEmpty(returnUrlFromSession))
                    {
                        HttpContext.Session.Remove("ReturnUrl"); // Xóa session ReturnUrl sau khi đã dùng
                        return Redirect(returnUrlFromSession); // Quay lại trang trước đó 
                    }
                }
            }
            if (nhanVien != null)
            {
                if (nhanVien.MatKhau != model.MatKhau)
                {
                    ModelState.AddModelError("", "❌ Mật khẩu không đúng.");
                    return View(model);
                }
                else if (nhanVien.HieuLuc == false)
                {
                    ModelState.AddModelError("", "❌ Tài khoản hiện đang bị khóa vui lòng liên hệ Admin để mở khóa.");
                    return View(model);
                }

                // Lưu session cho nhân viên
                HttpContext.Session.SetString("UserId", nhanVien.MaNv ?? ""); 
                HttpContext.Session.SetString("UserName", nhanVien.HoTen ?? "");
                HttpContext.Session.SetString("UserRole", "NhanVien");

                return RedirectToAction("Index", "Admin");
            }
            return RedirectToAction("Index", "HangHoa"); // Chuyển về trang chủ
        }


        // Xử lý đăng xuất
        public IActionResult DangXuat()
        {
            // Chỉ xóa thông tin đăng nhập, KHÔNG xóa giỏ hàng
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Clear(); // Xóa toàn bộ session
            return RedirectToAction("DangNhap");
        }

        // Hàm tạo mã khách hàng (5 ký tự in hoa, không trùng)
        private string GenerateCustomerCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var random = new Random();
            string newCode;

            do
            {
                newCode = new string(Enumerable.Repeat(chars, 5)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (db.KhachHangs.Any(kh => kh.MaKh == newCode));

            return newCode;
        }

        // Hiển thị form đăng ký
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }


        // Đăng ký khách hàng
        [HttpPost]
        public IActionResult DangKy(RegisterVM model, IFormFile? Hinh)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra số điện thoại hoặc email có tồn tại không
                if (db.KhachHangs.Any(kh => kh.DienThoai == model.DienThoai))
                {
                    ModelState.AddModelError("DienThoai", "Số điện thoại đã được sử dụng!");
                    return View(model);
                }

                if (db.KhachHangs.Any(kh => kh.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng!");
                    return View(model);
                }

                // Tạo khách hàng mới
                var newCustomer = new KhachHang
                {
                    MaKh = GenerateCustomerCode(), // Tạo mã tự động
                    MatKhau = model.MatKhau, // Chưa hash
                    HoTen = model.HoTen,
                    GioiTinh = model.GioiTinh,
                    NgaySinh = model.NgaySinh ?? DateTime.Now,
                    DiaChi = model.DiaChi,
                    DienThoai = model.DienThoai,
                    Email = model.Email,
                    HieuLuc = true,
                    VaiTro = 2, // Mặc định: khách hàng thường
                    RandomKey = Guid.NewGuid().ToString()
                };

                // Xử lý ảnh đại diện nếu có tải lên
                if (Hinh != null && Hinh.Length > 0)
                {
                    string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(Hinh.FileName)}";
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");

                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    string filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        Hinh.CopyTo(stream);
                    }

                    newCustomer.Hinh = "/images/avatars/" + fileName;
                }
                else
                {
                    // Nếu không chọn ảnh thì dùng ảnh mặc định
                    newCustomer.Hinh = "default.jpg";
                }

                // Thêm vào database và lưu
                db.KhachHangs.Add(newCustomer);
                db.SaveChanges();

                // Hiển thị thông báo thành công
                TempData["Success"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay.";

                return RedirectToAction("DangNhap");
            }

            return View(model);
        }
        // POST: Xử lý đăng ký
        [HttpPost]
        public IActionResult DangKyNV(NhanVien model, string XacNhanMatKhau)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra mã nhân viên đã tồn tại chưa
            model.MaNv = GenerateUniqueCode(5);

            // Kiểm tra email đã tồn tại chưa (nếu muốn)
            if (db.NhanViens.Any(nv => nv.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email đã được đăng ký.");
                return View(model);
            }

            // Kiểm tra mật khẩu trùng (có thể thêm ở client rồi nhưng kiểm tra thêm)
            if (model.MatKhau != XacNhanMatKhau)
            {
                ModelState.AddModelError("XacNhanMatKhau", "Mật khẩu xác nhận không khớp.");
                return View(model);
            }

            // Mã hóa mật khẩu (nên mã hóa, không lưu thẳng)
            model.MatKhau = model.MatKhau;

            // Mặc định hiệu lực = false
            model.HieuLuc = false;

            // Thêm nhân viên mới
            db.NhanViens.Add(model);
            db.SaveChanges();

            // Có thể redirect hoặc hiện thông báo thành công
            TempData["SuccessMessage"] = "Đăng ký nhân viên thành công! Vui lòng chờ xét duyệt.";
            return RedirectToAction("DangNhap"); // chuyển đến trang đăng nhập hoặc trang khác
        }

        // Phương thức ghi log thông tin
        private void LogInfo(string logPath, string message)
        {
            System.IO.File.AppendAllText(logPath, $"[INFO] {message}\n");
        }

        // Phương thức ghi log lỗi
        private void LogError(string logPath, string message)
        {
            System.IO.File.AppendAllText(logPath, $"[ERROR] {message}\n");
        }

        // Phương thức ghi log cảnh báo
        private void LogWarning(string logPath, string message)
        {
            System.IO.File.AppendAllText(logPath, $"[WARNING] {message}\n");
        }

        // Hiển thị chi tiết khách hàng
        public IActionResult ChiTiet(string id)
        {
            var khachHang = db.KhachHangs
                .Where(kh => kh.MaKh == id)
                .Select(kh => new KhachHangVM
                {
                    MaKh = kh.MaKh,
                    HoTen = kh.HoTen,
                    GioiTinh = kh.GioiTinh,
                    NgaySinh = kh.NgaySinh,
                    DiaChi = kh.DiaChi,
                    DienThoai = kh.DienThoai,
                    Email = kh.Email,
                    Hinh = kh.Hinh
                })
                .FirstOrDefault();

            if (khachHang == null)
            {
                return NotFound();
            }

            return View(khachHang);
        }

        // Xử lý AJAX cập nhật thông tin khách hàng
        [HttpPost]
        public async Task<IActionResult> Update(KhachHangVM model, IFormFile Hinh)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Điền thông tin cần cập nhập" });
            }

            var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.MaKh == model.MaKh);
            if (khachHang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khách hàng!" });
            }

            // Cập nhật thông tin
            khachHang.HoTen = model.HoTen;
            khachHang.GioiTinh = model.GioiTinh;
            khachHang.NgaySinh = model.NgaySinh ?? khachHang.NgaySinh;
            khachHang.DiaChi = model.DiaChi;
            khachHang.DienThoai = model.DienThoai;
            khachHang.Email = model.Email;

            string imageUrl = null;
            if (Hinh != null && Hinh.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Uploads/KhachHang");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Lấy tên gốc của file, bỏ khoảng trắng và ký tự đặc biệt
                string originalFileName = Path.GetFileNameWithoutExtension(Hinh.FileName).Trim().Replace(" ", "_"); // Thay dấu cách bằng dấu gạch dưới
                string fileExtension = Path.GetExtension(Hinh.FileName);

                // Đặt tên file theo format MaKh_tenfilegoc.dinhdang
                string uniqueFileName = $"{model.MaKh}_{originalFileName}{fileExtension}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Hinh.CopyToAsync(stream);
                }

                imageUrl = uniqueFileName;
                khachHang.Hinh = imageUrl;
            }


            db.SaveChanges();
            return Json(new { success = true, message = "Cập nhật thành công!", imageUrl });
        }

        // Hiển thị form quên mật khẩu
        [HttpGet]
        public IActionResult QuenMatKhau()
        {
            return View();
        }
        // Gửi mã xác nhận (OTP) qua email
        [HttpPost]
        public IActionResult GuiMaXacNhan([FromBody] Dictionary<string, string> data)
        {
            if (!data.ContainsKey("email"))
                return Json(new { success = false, message = "❌ Thiếu thông tin email!" });

            string email = data["email"];

            var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.Email == email);
            if (khachHang == null)
            {
                return Json(new { success = false, message = "❌ Email không tồn tại!" });
            }

            // Kiểm tra nếu OTP đã gửi và còn hiệu lực
            string lastSentTime = HttpContext.Session.GetString("LastOtpTime");
            if (!string.IsNullOrEmpty(lastSentTime))
            {
                DateTime lastTime = DateTime.Parse(lastSentTime);
                if ((DateTime.Now - lastTime).TotalMinutes < 2)
                {
                    return Json(new { success = false, message = "⚠️ Vui lòng thử lại sau 2 phút!" });
                }
            }

            // Tạo mã OTP mới
            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("EmailReset", email);
            HttpContext.Session.SetString("LastOtpTime", DateTime.Now.ToString());

            // Gửi email OTP
            string subject = "Mã xác nhận khôi phục mật khẩu";
            string body = $"Mã OTP của bạn là: <b>{otp}</b>. Mã có hiệu lực trong 5 phút.";
            _emailService.SendEmail(email, subject, body);

            return Json(new { success = true, message = "✅ Mã OTP đã được gửi đến email của bạn!" });
        }
        //Xác thực mã OTP
        [HttpPost]
        public IActionResult XacThucOTP([FromBody] Dictionary<string, string> data)
        {
            if (!data.ContainsKey("otp"))
                return Json(new { success = false, message = "❌ Thiếu mã OTP!" });

            string otp = data["otp"];
            string sessionOtp = HttpContext.Session.GetString("OTP");

            if (otp != sessionOtp)
            {
                return Json(new { success = false, message = "❌ Mã OTP không chính xác!" });
            }

            return Json(new { success = true, message = "✅ Mã OTP hợp lệ!" });
        }
        //Đặt lại mật khẩu
        [HttpPost]
        public IActionResult DatLaiMatKhau([FromBody] Dictionary<string, string> data)
        {
            if (!data.ContainsKey("newPassword"))
                return Json(new { success = false, message = "❌ Thiếu mật khẩu mới!" });

            string newPassword = data["newPassword"];
            string email = HttpContext.Session.GetString("EmailReset");

            var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.Email == email);
            if (khachHang == null)
            {
                return Json(new { success = false, message = "❌ Email không hợp lệ!" });
            }

            // Cập nhật mật khẩu (mã hóa trước khi lưu)
            khachHang.MatKhau = newPassword;
            db.SaveChanges();

            return Json(new { success = true, message = "✅ Mật khẩu đã được đặt lại!" });
        }


        // Hàm mã hóa MD5
        private string ToMd5Hash(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return Convert.ToHexString(hashBytes);
            }
        }
        // Hàm sinh mã nhân viên 5 chữ cái không trùng lặp
        private string GenerateUniqueCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var random = new Random();

            while (true)
            {
                var code = new StringBuilder();
                var usedChars = new HashSet<char>();

                while (code.Length < length)
                {
                    var c = chars[random.Next(chars.Length)];
                    if (!usedChars.Contains(c))
                    {
                        code.Append(c);
                        usedChars.Add(c);
                    }
                }

                var codeStr = code.ToString();

                // Kiểm tra mã chưa tồn tại trong db
                if (!db.NhanViens.Any(nv => nv.MaNv == codeStr))
                {
                    return codeStr;
                }
                // Nếu đã tồn tại, lặp tạo lại
            }
        }
    }
}
