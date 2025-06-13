using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using ECommerceMVC.Helpers;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;

namespace ECommerceMVC.Controllers
{
    public class CartController : BaseController
    {
        private readonly MuaChungContext db;

        public CartController(MuaChungContext context)
        {
            db = context;
        }

        // Lấy danh sách sản phẩm trong giỏ hàng từ Session
        public List<CartItem> Cart
        {
            get => HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();
            set => HttpContext.Session.Set(MySetting.CART_KEY, value);
        }

        // Hiển thị giỏ hàng
        public IActionResult Index()
        {
            // Lấy giỏ hàng từ session
            var cart = HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();
            // Kiểm tra nếu có thông báo từ TempData
            ViewBag.SuccessMessage = TempData["Success"];
            return View(Cart);
        }

        // Thêm sản phẩm vào giỏ hàng (Kiểm tra đăng nhập)
        public IActionResult AddToCart(int id, int quantity = 1)
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                // Lưu sản phẩm vào Session để thêm sau khi đăng nhập
                HttpContext.Session.SetInt32("PendingProductId", id);
                HttpContext.Session.SetInt32("PendingQuantity", quantity);

                // Chuyển hướng đến trang đăng nhập, sau đó trở lại ConfirmPendingCart
                return RedirectToAction("DangNhap", "KhachHang", new { returnUrl = Url.Action("ConfirmPendingCart", "Cart") });
            }
            // Nếu đã đăng nhập, thêm sản phẩm vào giỏ hàng như bình thường
            return ConfirmAddToCart(id, quantity);
        }

        // Xác nhận thêm sản phẩm vào giỏ hàng
        public IActionResult ConfirmAddToCart(int id, int quantity)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHh == id);

            if (item == null)
            {
                var hangHoa = db.HangHoas.SingleOrDefault(p => p.MaHh == id);
                if (hangHoa != null)
                {
                    item = new CartItem
                    {
                        MaHh = hangHoa.MaHh,
                        TenHH = hangHoa.TenHh,
                        DonGia = hangHoa.DonGia ?? 0,
                        Hinh = hangHoa.Hinh ?? string.Empty,
                        SoLuong = quantity
                    };
                    gioHang.Add(item);
                }
            }
            else
            {
                item.SoLuong += quantity;
            }

            // Cập nhật giỏ hàng vào Session
            Cart = gioHang;
            // Lưu giỏ hàng vào session
            HttpContext.Session.Set("Cart", Cart);

            return RedirectToAction("Index");
        }

        //  Xác nhận thêm sản phẩm vào giỏ hàng sau khi đăng nhập
        public IActionResult ConfirmPendingCart()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("DangNhap", "KhachHang");
            }

            // Lấy sản phẩm từ Session
            var pendingProductId = HttpContext.Session.GetInt32("PendingProductId");
            var pendingQuantity = HttpContext.Session.GetInt32("PendingQuantity");

            if (pendingProductId.HasValue && pendingQuantity.HasValue)
            {
                // Xóa dữ liệu tạm lưu
                HttpContext.Session.Remove("PendingProductId");
                HttpContext.Session.Remove("PendingQuantity");

                // 🛒 Gọi `ConfirmAddToCart` để thêm sản phẩm vào giỏ hàng
                return ConfirmAddToCart(pendingProductId.Value, pendingQuantity.Value);
            }

            return RedirectToAction("Index");
        }

        // Cập nhật số lượng sản phẩm trong giỏ hàng
        [HttpPost]
        public JsonResult UpdateCart(int id, int quantity)
        {
            var cart = Cart;
            var item = cart.SingleOrDefault(p => p.MaHh == id);

            // Nếu sản phẩm tồn tại trong giỏ hàng
            if (item != null)
            {
                if (quantity > 0)
                    item.SoLuong = quantity;  // Cập nhật số lượng
                else
                    cart.Remove(item);  // Nếu quantity <= 0 thì xóa sản phẩm khỏi giỏ hàng
            }

            // Cập nhật lại Cart
            Cart = cart;

            return Json(new
            {
                totalCart = cart.Sum(p => p.ThanhTien).ToString("N0"),  // Tổng tiền của giỏ hàng
                itemTotal = item != null ? (item.DonGia * item.SoLuong).ToString("N0") : "0",  // Tổng tiền của sản phẩm cụ thể
                cartCount = cart.Count(),  // Số lượng sản phẩm trong giỏ hàng
                cartDetails = cart.Select(c => new
                {
                    c.MaHh,
                    c.SoLuong
                }).ToList()  // Chi tiết sản phẩm trong giỏ hàng
            });
        }
        [HttpPost]
        public IActionResult PlaceOrder(string ReceiverName, string ShippingAddress, string Note, DateTime? DeliveryDate, string paymentMethod)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("DangNhap", "KhachHang");
            }

            var khachHang = db.KhachHangs.Find(userId);
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction("Index");
            }

            var hoaDon = new HoaDon
            {
                MaKh = khachHang.MaKh,
                NgayDat = DateTime.Now,
                HoTen = string.IsNullOrEmpty(ReceiverName) ? khachHang.HoTen : ReceiverName,
                DiaChi = string.IsNullOrEmpty(ShippingAddress) ? khachHang.DiaChi : ShippingAddress,
                GhiChu = Note,
                NgayCan = DeliveryDate,
                MaTrangThai = 0,
                CachThanhToan = paymentMethod,
            };

            db.HoaDons.Add(hoaDon);
            db.SaveChanges();

            foreach (var item in cart)
            {
                var chiTiet = new ChiTietHd
                {
                    MaHd = hoaDon.MaHd,
                    MaHh = item.MaHh,
                    SoLuong = item.SoLuong,
                    DonGia = item.DonGia,
                };
                db.ChiTietHds.Add(chiTiet);
            }

            db.SaveChanges();
            HttpContext.Session.Remove("Cart");
            ClearCart();
            TempData["Success"] = "Đơn hàng của bạn đã được đặt thành công!";
            return RedirectToAction("Index");
        }

        // Xóa sản phẩm khỏi giỏ hàng
        public IActionResult RemoveCart(int id)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHh == id);
            if (item != null)
            {
                gioHang.Remove(item);
                Cart = gioHang;
            }
            return RedirectToAction("Index");
        }

        // Xóa toàn bộ giỏ hàng
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove(MySetting.CART_KEY);
            return RedirectToAction("Index");
        }
        public IActionResult CartSummary()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            return ViewComponent("Cart", cart);
        }

    }
}
