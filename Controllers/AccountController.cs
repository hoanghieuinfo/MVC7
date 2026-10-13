using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc7.Data;
using mvc7.Filters;
using mvc7.Models;
using mvc7.Models.ViewModels;
using mvc7.Services;

namespace mvc7.Controllers;

public class AccountController : Controller
{
    public const string SessionUserId = "UserId";
    public const string SessionUsername = "Username";
    public const string SessionFullName = "FullName";

    private const int MaxFailedAttempts = 3;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly AppDbContext _db;

    public AccountController(AppDbContext db)
    {
        _db = db;
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        model.Username = model.Username?.Trim() ?? string.Empty;

        // Server-side: kiểm tra Username đã tồn tại chưa
        if (ModelState.IsValid && await _db.Users.AnyAsync(u => u.Username == model.Username))
        {
            ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã tồn tại");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new User
        {
            Username = model.Username,
            FullName = model.FullName.Trim(),
            PasswordHashed = PasswordHasher.Hash(model.Password),
            AccessFailedCount = 0,
            IsLocked = false,
            LockoutEnd = null
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Đăng ký thành công! Vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetInt32(SessionUserId) != null)
        {
            return RedirectToAction(nameof(TestLogin));
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var username = model.Username.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng");
            return View(model);
        }

        var now = DateTime.Now;

        // Tài khoản đang bị khóa
        if (user.IsLocked && user.LockoutEnd.HasValue && user.LockoutEnd.Value > now)
        {
            var remaining = user.LockoutEnd.Value - now;
            ModelState.AddModelError(string.Empty,
                $"Tài khoản đã bị khóa do đăng nhập sai quá {MaxFailedAttempts} lần. " +
                $"Vui lòng thử lại sau {Math.Ceiling(remaining.TotalMinutes)} phút " +
                $"(mở khóa lúc {user.LockoutEnd.Value:HH:mm:ss dd/MM/yyyy}).");
            return View(model);
        }

        // Hết thời gian khóa -> mở khóa
        if (user.IsLocked)
        {
            user.IsLocked = false;
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
        }

        if (!PasswordHasher.Verify(model.Password, user.PasswordHashed))
        {
            user.AccessFailedCount++;

            if (user.AccessFailedCount >= MaxFailedAttempts)
            {
                user.IsLocked = true;
                user.LockoutEnd = now.Add(LockoutDuration);
                ModelState.AddModelError(string.Empty,
                    $"Bạn đã đăng nhập sai {MaxFailedAttempts} lần. Tài khoản bị khóa đăng nhập trong " +
                    $"{LockoutDuration.TotalMinutes} phút (đến {user.LockoutEnd.Value:HH:mm:ss dd/MM/yyyy}).");
            }
            else
            {
                ModelState.AddModelError(string.Empty,
                    $"Tên đăng nhập hoặc mật khẩu không đúng. Bạn còn " +
                    $"{MaxFailedAttempts - user.AccessFailedCount} lần thử.");
            }

            await _db.SaveChangesAsync();
            return View(model);
        }

        // Đăng nhập thành công -> reset bộ đếm
        user.AccessFailedCount = 0;
        user.IsLocked = false;
        user.LockoutEnd = null;
        await _db.SaveChangesAsync();

        // Lưu trạng thái đăng nhập vào Session
        HttpContext.Session.SetInt32(SessionUserId, user.UserId);
        HttpContext.Session.SetString(SessionUsername, user.Username);
        HttpContext.Session.SetString(SessionFullName, user.FullName);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction(nameof(TestLogin));
    }

    // GET: /Account/TestLogin - chỉ truy cập được khi đã đăng nhập
    [HttpGet]
    [SessionAuthorize]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult TestLogin()
    {
        ViewBag.UserId = HttpContext.Session.GetInt32(SessionUserId);
        ViewBag.Username = HttpContext.Session.GetString(SessionUsername);
        ViewBag.FullName = HttpContext.Session.GetString(SessionFullName);
        return View();
    }

    // GET: /Account/Logout
    public IActionResult Logout()
    {
        // Xóa toàn bộ dữ liệu Session và cookie Session
        HttpContext.Session.Clear();
        Response.Cookies.Delete(Program.SessionCookieName);

        TempData["Message"] = "Bạn đã đăng xuất.";
        return RedirectToAction(nameof(Login));
    }
}
