using EmoTagger.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EmoTagger.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context; // ✅ **Eksik context tanımlandı!**

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(); // Views/Dashboard/Register.cshtml
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Lütfen tüm alanları eksiksiz doldurun!" });
            }

            if (_context.Users.Any(u => u.Email == user.Email))
            {
                return Json(new { success = false, message = "Bu e-posta zaten kayıtlı!" });
            }

            if (string.IsNullOrEmpty(user.Password) || user.Password.Length < 6)
            {
                return Json(new { success = false, message = "Şifreniz en az 6 karakter olmalıdır." });
            }

            if (user.Password != user.ConfirmPassword)
            {
                return Json(new { success = false, message = "Şifreler uyuşmuyor!" });
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            user.CreatedAt = DateTime.UtcNow;

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // ✅ Başarılıysa Login sayfasına yönlendir
                return Json(new { success = true, redirectUrl = Url.Action("Login", "Dashboard") });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Bir hata oluştu: " + (ex.InnerException?.Message ?? ex.Message)
                });
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(); // Views/Dashboard/Login.cshtml sayfasını açar
        }

        [HttpPost]
        public IActionResult Login([FromBody] User loginUser)
        {
            if (loginUser == null || string.IsNullOrEmpty(loginUser.Email) || string.IsNullOrEmpty(loginUser.Password))
            {
                return Json(new { success = false, message = "Lütfen e-posta ve şifre giriniz!" });
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == loginUser.Email);

            if (user == null)
            {
                return Json(new { success = false, message = "Bu e-posta ile kayıtlı kullanıcı bulunamadı!" });
            }

            if (!BCrypt.Net.BCrypt.Verify(loginUser.Password, user.Password))
            {
                return Json(new { success = false, message = "Şifreniz yanlış!" });
            }

            // **Kullanıcı bilgilerini session'a ekle**
            HttpContext.Session.SetString("UserName", user.FirstName);
            HttpContext.Session.SetString("UserEmail", user.Email);

            return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }


        public IActionResult Index()
        {
            var musicList = _context.Music
                .OrderByDescending(m => m.createdat)
                .ToList();

            return View(musicList);
        }


        public IActionResult ListenTag()
        {
            return View();
        }

        public IActionResult Leaderboard()
        {
            return View();
        }

        public IActionResult ListeningHistory()
        {
            return View();
        }

        public IActionResult Playlist()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

    

        public IActionResult Favorites()
        {
            return View();
        }
    }
}
