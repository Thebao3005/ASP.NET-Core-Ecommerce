using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

public class OrderController : Controller
{
    private readonly MuaChungContext _context;

    public OrderController(MuaChungContext context)
    {
        _context = context;
    }

    // Đặt hàng
    [HttpPost]
    public IActionResult PlaceOrder(string paymentMethod, string shippingMethod, double shippingFee, string? note)
    {
        // Lấy giỏ hàng từ session
        var cart = HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY);

        if (cart == null || cart.Count == 0)
        {
            TempData["Error"] = "Giỏ hàng của bạn đang trống!";
            return RedirectToAction("Index", "Cart");
        }

        // Tạo đơn hàng mới
        var order = new HoaDon
        {
            MaKh = HttpContext.Session.GetString("UserId"), // ID người dùng từ session
            NgayDat = DateTime.Now,
            CachThanhToan = paymentMethod,
            CachVanChuyen = shippingMethod,
            PhiVanChuyen = shippingFee,
            MaTrangThai = 0, // Trạng thái "Mới tạo"
            GhiChu = note,
            DiaChi = "Địa chỉ mặc định", // Hoặc lấy từ thông tin người dùng
        };

        _context.HoaDons.Add(order);
        _context.SaveChanges();

        // Lưu chi tiết đơn hàng từ giỏ hàng vào ChiTietHd
        foreach (var item in cart)
        {
            var orderDetail = new ChiTietHd
            {
                MaHd = order.MaHd,
                MaHh = item.MaHh,
                DonGia = item.DonGia,
                SoLuong = item.SoLuong,
                GiamGia = 0 // Nếu có giảm giá, bạn có thể thêm vào đây
            };

            _context.ChiTietHds.Add(orderDetail);
        }

        _context.SaveChanges();

        // Xóa giỏ hàng sau khi đặt hàng thành công
        HttpContext.Session.Remove(MySetting.CART_KEY);
        // Thông báo đặt hàng thành công và quay lại giỏ hàng
        TempData["Success"] = "Đặt hàng thành công!";
        return RedirectToAction("Index", "Cart");
    }
}
