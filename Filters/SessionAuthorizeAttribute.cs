using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using mvc7.Controllers;

namespace mvc7.Filters;

/// <summary>
/// Chỉ cho phép truy cập khi đã đăng nhập (có UserId trong Session),
/// nếu chưa thì chuyển về trang đăng nhập.
/// </summary>
public class SessionAuthorizeAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var http = context.HttpContext;
        if (http.Session.GetInt32(AccountController.SessionUserId) == null)
        {
            var tempDataFactory = http.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
            var tempData = tempDataFactory.GetTempData(http);
            tempData["Message"] = "Bạn chưa đăng nhập. Vui lòng đăng nhập để tiếp tục.";

            var returnUrl = http.Request.Path + http.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
        }
    }
}
