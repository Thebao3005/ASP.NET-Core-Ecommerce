using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace ECommerceMVC.ViewComponents
{
	public class ViewSieuSaleViewComponent : ViewComponent
	{
		private readonly MuaChungContext _context;

		public ViewSieuSaleViewComponent(MuaChungContext context)
		{
			_context = context;
		}

		public IViewComponentResult Invoke()
		{
			var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
			string userId = userIdClaim?.Value;

			var sanPhamMuaChung = _context.GiamGias
				.Where(gg => gg.IsActive && gg.EndTime > DateTime.Now)
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
					DaThamGia = !string.IsNullOrEmpty(userId) &&
				_context.ThamGiaMuaChungs.Any(t => t.MaGG == gg.MaGG && t.MaKh == userId)
				})
				.Where(sp => sp.IsGiamGia && !sp.DaThamGia)
				.Take(5)
				.ToList();

			return View(sanPhamMuaChung);
		}
	}
}
