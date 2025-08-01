﻿using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.ViewComponents
{
	public class MenuLoaiViewComponent : ViewComponent
	{
		private readonly MuaChungContext db;

		public MenuLoaiViewComponent(MuaChungContext context) => db = context;

		public IViewComponentResult Invoke()
		{
			var data = db.Loais.Select(lo => new MenuLoaiVM
			{
				MaLoai = lo.MaLoai,
				TenLoai = lo.TenLoai,
				SoLuong = lo.HangHoas.Count
			}).OrderBy(p => p.TenLoai);

			return View(data); // Default.cshtml
			//return View("Default", data);
		}
	}
}
