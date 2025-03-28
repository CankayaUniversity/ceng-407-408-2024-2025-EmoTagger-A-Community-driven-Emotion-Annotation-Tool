using EmoTagger.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EmoTagger.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
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

                return Json(new { success = true, redirectUrl = Url.Action("Login", "Dashboard") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
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
            var musicList = _context.Music.OrderByDescending(m => m.createdat).ToList();
            return View(musicList);
        }

        public IActionResult ListenTag() => View();
        public IActionResult Leaderboard() => View();
        public IActionResult ListeningHistory() => View();
        public IActionResult Profile() => View();
        public IActionResult Settings() => View();
        public IActionResult Favorites() => View();

        [HttpGet]
        public IActionResult Playlist(string searchTerm = "")
        {
            int userId = 1; // TODO: Use real user session ID later

            var model = new MusicViewModel
            {
                UserId = userId,
                SearchTerm = searchTerm ?? "",
                AvailableSongs = string.IsNullOrWhiteSpace(searchTerm) ? new List<MusicItem>() : GetSongs(searchTerm),
                Playlist = GetUserPlaylist(userId)
            };

            return View(model);
        }

        // inside DashboardController

        [HttpPost]
        public IActionResult AddToPlaylist(int userId, int musicId)
        {
            _context.Database.ExecuteSqlRaw(
                "INSERT INTO playlists (user_id, music_id, added_at) VALUES ({0}, {1}, NOW())", userId, musicId
            );

            return RedirectToAction("Playlist");
        }

        [HttpPost]
        public IActionResult RemoveFromPlaylist(int userId, int musicId)
        {
            _context.Database.ExecuteSqlRaw(
                "DELETE FROM playlists WHERE user_id = {0} AND music_id = {1}", userId, musicId
            );

            return RedirectToAction("Playlist");
        }


        private List<MusicItem> GetUserPlaylist(int userId)
        {
            return (from p in _context.Playlists
                    join m in _context.Music on p.music_id equals m.musicid
                    where p.user_id == userId
                    select new MusicItem
                    {
                        MusicId = m.musicid,
                        Title = m.title,
                        Artist = m.artist,
                        Album = m.filename,
                        Time = m.createdat.ToShortTimeString()
                    }).ToList();
        }

        private List<MusicItem> GetSongs(string searchTerm)
        {
            return _context.Music
                .Where(m => m.title.ToLower().Contains(searchTerm.ToLower()))
                .Select(m => new MusicItem
                {
                    MusicId = m.musicid,
                    Title = m.title,
                    Artist = m.artist,
                    Album = m.filename,
                    Time = m.createdat.ToShortTimeString()
                }).ToList();
        }
    }
}
