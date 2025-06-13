using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class BaseController : Controller
{
	protected string UserId { get; private set; }
	protected string UserName { get; private set; }
	protected string UserRole { get; private set; }

	public override void OnActionExecuting(ActionExecutingContext context)
	{
		// Kiểm tra HttpContext và Session trước khi truy cập
		if (context.HttpContext?.Session != null)
		{
			UserId = context.HttpContext.Session.GetString("UserId");
			UserName = context.HttpContext.Session.GetString("UserName");
			UserRole = context.HttpContext.Session.GetString("UserRole");

			if (!string.IsNullOrEmpty(UserId))
			{
				Console.WriteLine($"✅ Phiên đăng nhập: {UserName} (ID: {UserId}, Vai trò: {UserRole})");
				ViewBag.UserName = UserName;
			}
			else
			{
				Console.WriteLine("⚠️ Không có phiên đăng nhập.");
			}
		}
		else
		{
			Console.WriteLine("⚠️ Session chưa được khởi tạo.");
		}

		base.OnActionExecuting(context);
	}
}
