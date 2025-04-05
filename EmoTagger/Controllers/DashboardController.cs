using EmoTagger.Data;
using EmoTagger.Models;
using EmoTagger.Services;
using EmoTagger.ViewComponents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EmoTagger.Controllers
{

    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
     ApplicationDbContext context,
     EmailService emailService,
     ILogger<DashboardController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public IActionResult GetTrackDetails(int trackId)
        {
            var track = _context.Musics
                .FirstOrDefault(m => m.musicid == trackId);

            if (track == null)
            {
                return NotFound();
            }

            return Json(new
            {
                track.title,
                track.artist,
                track.filename
            });
        }



        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        // 🛡️ Kullanıcı Girişi
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

            // Doğru session ekleme
            HttpContext.Session.SetInt32("UserId", user.Id);

            HttpContext.Session.SetString("UserName", user.FirstName); // FirstName kullan
            HttpContext.Session.SetString("UserEmail", user.Email);

            return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
        }




        public IActionResult ListenMixed()
        {
            var musics = _context.Musics.ToList();  // Postgres verisini çek
            return View(musics);                   // View'e gönder
        }
        // 🎧 `Player` Sayfası
        public IActionResult Player()
        {
            var songs = _context.Musics.ToList(); // Tüm şarkılar
            return View(songs);
        }

        // 🛡️ Kullanıcı Kayıt Sayfası
        [HttpGet]
        public IActionResult Register()
        {
            return View(); // Views/Dashboard/Register.cshtml
        }

        // 🛡️ Kullanıcı Kayıt İşlemi
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

        // 🎧 Dinleme Geçmişi
        public async Task<IActionResult> ListeningHistory()
        {


            return View(); // ListeningHistory.cshtml
        }


        // 🏆 Liderlik Tablosu
        public IActionResult Leaderboard()
        {
            return View();
        }

        // 🎧 Playlist Sayfası
        public IActionResult Playlist()
        {
            return View();
        }

        // ❤️ Favoriler
        public IActionResult Favorites()
        {
            return View();
        }

        // 🛡️ Profil Sayfası
        public IActionResult Profile()
        {
            return View();
        }

        // ⚙️ Ayarlar
        public IActionResult Settings()
        {
            return View();
        }

        // 🏠 Ana Sayfa

        public IActionResult MusicList()
        {
            var musicList = _context.Musics.ToList(); // Music tablon
            return View(musicList); // View'e model olarak gönder
        }



        // 🛑 Çıkış Yap
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
        public async Task<IActionResult> Index()
        {


            return View();
        }
        [HttpGet]
        public IActionResult ListenTag()
        {


            return View();
        }
        public class LogPlayedRequest
        {
            public int MusicId { get; set; }
        }

        [HttpPost]
        public IActionResult LogPlayed([FromBody] LogPlayedRequest request)
        {
            try
            {
                // Request null kontrolü
                if (request == null)
                {
                    _logger.LogWarning("LogPlayed: Geçersiz istek - null");
                    return BadRequest(new { success = false, message = "Geçersiz istek" });
                }

                // Music ID kontrolü
                if (request.MusicId <= 0)
                {
                    _logger.LogWarning($"LogPlayed: Geçersiz MusicId - {request.MusicId}");
                    return BadRequest(new { success = false, message = "Geçersiz müzik ID" });
                }

                _logger.LogInformation($"LogPlayed: MusicId = {request.MusicId}");

                // Oturum kontrolü
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("LogPlayed: Oturum bulunamadı");
                    return Json(new { success = false, message = "Oturum açmanız gerekiyor" });
                }

                // Müzik varlık kontrolü
                var music = _context.Musics.FirstOrDefault(m => m.musicid == request.MusicId);
                if (music == null)
                {
                    _logger.LogWarning($"LogPlayed: Müzik bulunamadı - ID: {request.MusicId}");
                    return Json(new { success = false, message = "Müzik bulunamadı" });
                }

                try
                {
                    // 1 dakika içinde aynı şarkı tekrar eklenmesin
                    var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
                    var exists = _context.RecentlyPlayed
                        .Any(r => r.UserId == userId && r.MusicId == request.MusicId && r.PlayedAt >= oneMinuteAgo);

                    if (!exists)
                    {
                        var recentlyPlayed = new RecentlyPlayed
                        {
                            UserId = userId.Value,
                            MusicId = request.MusicId,
                            PlayedAt = DateTime.UtcNow
                        };

                        _context.RecentlyPlayed.Add(recentlyPlayed);
                        _context.SaveChanges();
                        _logger.LogInformation($"LogPlayed: Başarılı - UserId: {userId}, MusicId: {request.MusicId}");
                    }
                    else
                    {
                        _logger.LogInformation($"LogPlayed: Zaten var - UserId: {userId}, MusicId: {request.MusicId}");
                    }

                    return Json(new { success = true });
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, $"LogPlayed: DB hatası - UserId: {userId}, MusicId: {request.MusicId}");
                    return Json(new { success = false, message = "Veritabanı hatası" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LogPlayed: Genel hata");
                return Json(new { success = false, message = "Sunucu hatası" });
            }
        }

        [HttpGet]
        public IActionResult CheckLogin()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            return Json(new { isLoggedIn = userId != null });
        }

        [HttpGet]
        public IActionResult GetHistory()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                _logger.LogWarning("Oturum açmamış kullanıcı geçmişe erişmeye çalıştı");
                return Unauthorized(new { message = "Oturum açmanız gerekiyor" });
            }

            try
            {
                // Önce RecentlyPlayed kayıtlarını kontrol edelim
                var recentlyPlayed = _context.RecentlyPlayed
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.PlayedAt)
                    .Take(10)
                    .ToList();

                _logger.LogInformation($"Kullanıcı {userId} için {recentlyPlayed.Count} adet dinleme kaydı bulundu");

                if (recentlyPlayed.Count == 0)
                {
                    return Json(new { played = new List<object>() });
                }

                // Şarkı ID'lerini alalım
                var musicIds = recentlyPlayed.Select(p => p.MusicId).ToList();

                // Tüm müzik bilgilerini tek sorguda alalım
                var musicDetails = _context.Musics
                    .Where(m => musicIds.Contains(m.musicid))
                    .ToDictionary(m => m.musicid);

                _logger.LogInformation($"{musicIds.Count} ID için {musicDetails.Count} adet müzik bilgisi bulundu");

                // Şarkı bilgilerini birleştir
                var result = recentlyPlayed.Select(p => {
                    musicDetails.TryGetValue(p.MusicId, out var music);

                    return new
                    {
                        Title = music?.title ?? "Bulunamadı",
                        Artist = music?.artist ?? "Bulunamadı",
                        PlayedAt = p.PlayedAt.ToString("g"),
                        MusicId = p.MusicId // DEBUG için ID'yi de ekleyin
                    };
                }).ToList();

                return Json(new { played = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetHistory hatası");
                return Json(new { error = ex.Message, played = new List<object>() });
            }
        }

    }
}
 

