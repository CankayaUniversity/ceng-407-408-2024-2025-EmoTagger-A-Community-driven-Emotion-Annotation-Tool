using EmoTagger.Data;
using EmoTagger.Models;
using EmoTagger.Services;
using EmoTagger.ViewComponents;
using EmoTagger.Views.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DashboardController(
     ApplicationDbContext context,
     IWebHostEnvironment webHostEnvironment,
     EmailService emailService,
     ILogger<DashboardController> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        // Add this method to the DashboardController class
        [HttpPost]
        public IActionResult ToggleFavorite([FromBody] FavoriteModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Bu işlem için giriş yapmalısınız." });
            }

            try
            {
                // Favori var mı kontrol et
                var existingFavorite = _context.Favorites
                    .FirstOrDefault(f => f.user_id == userId.Value && f.music_id == model.MusicId);

                if (existingFavorite != null && model.Remove)
                {
                    // Favoriden çıkar
                    _context.Favorites.Remove(existingFavorite);
                    _context.SaveChanges();
                    return Json(new { success = true, isFavorite = false });
                }
                else if (existingFavorite == null && !model.Remove)
                {
                    // Favoriye ekle
                    var favorite = new Favorite
                    {
                        user_id = userId.Value,
                        music_id = model.MusicId,
                        added_at = DateTime.UtcNow
                    };

                    _context.Favorites.Add(favorite);
                    _context.SaveChanges();
                    return Json(new { success = true, isFavorite = true });
                }

                return Json(new { success = true, isFavorite = existingFavorite != null });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }


        [HttpGet]
        public IActionResult CheckSession()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            return Json(new { isLoggedIn = (userId != null) });
        }
        // Favori durumunu kontrol et
        [HttpGet]
        public async Task<IActionResult> CheckFavorite(int musicId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { isFavorite = false });
            }

            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdStr, out int userId))
                {
                    return Json(new { isFavorite = false });
                }

                var isFavorite = await _context.Favorites
                    .AnyAsync(f => f.user_id == userId && f.music_id == musicId);

                return Json(new { isFavorite });
            }
            catch
            {
                return Json(new { isFavorite = false });
            }
        }


        // Favorilerden kaldır
        [HttpPost]
        public async Task<IActionResult> RemoveFavorite([FromBody] FavoriteModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Bu işlem için giriş yapmalısınız." });
            }

            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdStr, out int userId))
                {
                    return Json(new { success = false, message = "Geçersiz kullanıcı ID." });
                }

                var favorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.user_id == userId && f.music_id == model.MusicId);

                if (favorite != null)
                {
                    _context.Favorites.Remove(favorite);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Favori bulunamadı." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult Favorites()
        {
            // Session kontrolü
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                // Eğer AJAX isteği ise JSON dön
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Bu işlem için giriş yapmalısınız." });
                }
                // Normal sayfa isteği ise Login sayfasına yönlendir
                return RedirectToAction("Login", "Dashboard");
            }

            // Kullanıcı giriş yapmışsa normal sayfayı göster
            return View();
        }

        [HttpGet]
        public IActionResult GetFavorites()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Bu işlem için giriş yapmalısınız." });
            }

            try
            {
                // Veritabanındaki gerçek sütun adlarını kullan
                var favorites = _context.Favorites
                    .Where(f => f.user_id == userId.Value) // integer ile integer karşılaştırması
                    .Join(_context.Musics,
                          f => f.music_id,  // Favorites tablosundaki sütun adı
                          m => m.musicid,   // Music tablosundaki sütun adı
                          (f, m) => new {
                              id = m.musicid,
                              title = m.title,
                              artist = m.artist,
                              addedAt = f.added_at
                          })
                    .OrderByDescending(f => f.addedAt)
                    .ToList();

                return Json(new { success = true, favorites });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetSongTag(int musicId)
        {
            try
            {
                // Check if user is logged in
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return StatusCode(401, new { success = false, message = "Please login to view tags" });
                }

                // Get the user's tag for this song
                var tag = _context.MusicTags
                    .Where(mt => mt.MusicId == musicId && mt.UserId == userId)
                    .Select(mt => mt.Tag)
                    .FirstOrDefault();

                if (tag != null)
                {
                    return Json(new { success = true, tag });
                }
                else
                {
                    return Json(new { success = false, message = "No tag found" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }
        [HttpPost]
        public IActionResult SaveTag([FromBody] TagRequest request)
        {
            try
            {
                // Check if user is logged in
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Please login to tag songs" });
                }

                // Validate request
                if (request == null || request.MusicId <= 0 || string.IsNullOrEmpty(request.Tag))
                {
                    return Json(new { success = false, message = "Invalid request" });
                }

                // Check if the tag already exists for this user and music
                var existingTag = _context.MusicTags
                    .FirstOrDefault(mt => mt.MusicId == request.MusicId && mt.UserId == userId);

                if (existingTag != null)
                {
                    existingTag.Tag = request.Tag;
                    existingTag.UpdatedAt = DateTime.UtcNow;
                    _context.SaveChanges();
                    return Json(new { success = true, message = "Etiket güncellendi", isUpdate = true });
                }
                else
                {
                    var newTag = new MusicTag
                    {
                        MusicId = request.MusicId,
                        UserId = userId.Value,
                        Tag = request.Tag,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.MusicTags.Add(newTag);
                    _context.SaveChanges();
                    return Json(new { success = true, message = "Etiket eklendi", isUpdate = false });
                }

            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "Inner exception yok";
                System.Diagnostics.Debug.WriteLine($"Hata: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Inner Exception: {innerMessage}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                return Json(new { success = false, message = $"Bir hata oluştu: {innerMessage}" });
            }

        }

        [HttpGet]
        public IActionResult GetRecentlyTagged(int count = 5)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Please login to view your tags" });
                }

                // Get the user's recent tags
                var recentTags = _context.MusicTags
      .Where(mt => mt.UserId == userId)
      .Include(mt => mt.Music) // Müzik bilgilerini dahil et
      .OrderByDescending(mt => mt.UpdatedAt ?? mt.CreatedAt)
      .Take(count)
      .Select(mt => new
      {
          mt.MusicId,
          mt.Tag,
          Title = mt.Music.title,
          Artist = mt.Music.artist,
          mt.CreatedAt,
          mt.UpdatedAt,
          TaggedAt = mt.UpdatedAt ?? mt.CreatedAt
      })
      .ToList();


                return Json(new { success = true, tags = recentTags });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // Class for tag request
        public class TagRequest
        {
            public int MusicId { get; set; }
            public string Tag { get; set; }
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
            HttpContext.Session.SetString("UserProfileImage", user.ProfileImageUrl ?? "/assets/images/default-profile.png");

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
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Property = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToList() })
                    .ToList();

                Console.WriteLine("Model doğrulama hataları: " + string.Join(", ", errors));
                return Json(new { success = false, message = "Lütfen tüm alanları eksiksiz doldurun!", errors = errors });
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
        // Controller içinde
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // Oturumdaki kullanıcı e-postasını al
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Dashboard");
            }

            // Veritabanından kullanıcıyı sorgula
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return NotFound();
            }

            // ProfileViewModel oluştur
            var profileViewModel = new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Country = user.Country,
                // Profil resmini veritabanından al, yoksa varsayılan resmi kullan
                ProfileImageUrl = user.ProfileImageUrl ?? "/assets/images/default-profile.png"
            };

            return View(profileViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Geçersiz giriş!";
                return View("Profile", model);
            }

            // Gelen verileri kontrol için logla
            System.Diagnostics.Debug.WriteLine($"GELEN VERİ: {model.FirstName}, {model.LastName}, {model.Email}, {model.PhoneNumber}, {model.Country}");

            // Kullanıcıyı e-posta ile bul
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı!";
                return RedirectToAction("Login", "Dashboard");
            }

            // Güncelle
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Country = model.Country;

            // Profil resmi güncellemesi
            if (!string.IsNullOrEmpty(model.ProfileImageUrl))
            {
                user.ProfileImageUrl = model.ProfileImageUrl;
            }

            // Kaydet
            var result = await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine("SaveChanges sonucu: " + result);

            if (result > 0)
            {
                TempData["SuccessMessage"] = "Profil başarıyla güncellendi!";
            }
            else
            {
                TempData["WarningMessage"] = "Değişiklik yapılmadı.";
            }

            return RedirectToAction("Profile");
        }


        [HttpPost]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profileImage)
        {
            if (profileImage == null || profileImage.Length == 0)
            {
                TempData["ErrorMessage"] = "Dosya seçilmedi.";
                return RedirectToAction("Profile");
            }

            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Dashboard");
            }

            // Dosya türünü kontrol et
            var extension = Path.GetExtension(profileImage.FileName).ToLower();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };

            if (!allowedExtensions.Contains(extension))
            {
                TempData["ErrorMessage"] = "Sadece resim dosyaları kabul edilmektedir (.jpg, .jpeg, .png, .gif).";
                return RedirectToAction("Profile");
            }

            try
            {
                // Veritabanından kullanıcıyı bul
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                    return RedirectToAction("Profile");
                }

                // Önceki resmi hatırla
                var oldImageUrl = user.ProfileImageUrl;
                System.Diagnostics.Debug.WriteLine($"Önceki profil resmi: {oldImageUrl}");

                // Benzersiz dosya adı oluştur
                var fileName = $"{Guid.NewGuid()}{extension}";

                // Yükleme klasörü yolunu al
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");

                // Klasör yoksa oluştur
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, fileName);

                // Dosyayı kaydet
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(fileStream);
                }

                // Dosya yolunu oluştur
                var fileUrl = $"/uploads/profiles/{fileName}";

                // Kullanıcının profil resmini veritabanında güncelle
                user.ProfileImageUrl = fileUrl;

                // Değişiklikleri kaydet ve etkilenen satır sayısını al
                var result = await _context.SaveChangesAsync();

                // Debug için
                System.Diagnostics.Debug.WriteLine($"Etkilenen satır sayısı: {result}");
                System.Diagnostics.Debug.WriteLine($"Yeni profil resmi: {fileUrl}");

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Profil resmi başarıyla güncellendi!";

                    // Eğer önceki resim varsayılan değilse ve mevcut disk üzerinde ise, silebiliriz
            
                }
                else
                {
                    TempData["WarningMessage"] = "Profil resmi güncellenemedi. Hiçbir değişiklik kaydedilmedi.";
                }
            }
            catch (Exception ex)
            {
                var innerErrorMsg = ex.InnerException?.Message ?? "İç hata yok";
                TempData["ErrorMessage"] = $"Resim yüklenirken hata oluştu: {ex.Message} - {innerErrorMsg}";

                // Debug için
                System.Diagnostics.Debug.WriteLine($"UploadProfilePicture hatası: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"İç hata: {innerErrorMsg}");
            }

            return RedirectToAction("Profile");
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
            try
            {
                // Kullanıcı kimliğini kontrol et
                var userId = HttpContext.Session.GetInt32("UserId");
                _logger.LogInformation($"GetHistory çağrıldı. Session UserId: {userId}");

                // Oturum açılmamışsa, Claims'den de kontrol edelim
                if (userId == null && User.Identity.IsAuthenticated)
                {
                    var userIdClaim = User.FindFirst("UserId")?.Value;
                    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int id))
                    {
                        userId = id;
                        _logger.LogInformation($"Session'da UserId bulunamadı, Claims'den alındı: {userId}");
                        // Session'a UserId'yi kaydedelim ki daha sonra sorun yaşanmasın
                        HttpContext.Session.SetInt32("UserId", id);
                    }
                }

                if (userId == null)
                {
                    _logger.LogWarning("Oturum açmamış kullanıcı geçmişe erişmeye çalıştı");
                    return Unauthorized(new { message = "Oturum açmanız gerekiyor" });
                }

                // Kullanıcının dinleme kayıtlarını getir
                _logger.LogInformation($"RecentlyPlayed tablosundan kayıtlar alınıyor, UserId: {userId}");
                var recentlyPlayed = _context.RecentlyPlayed
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.PlayedAt)
                    .ToList();

                _logger.LogInformation($"Kullanıcı {userId} için {recentlyPlayed.Count} adet dinleme kaydı bulundu");

                if (recentlyPlayed.Count == 0)
                {
                    return Json(new { played = new List<object>() });
                }

                // Şarkı ID'lerini alalım
                var musicIds = recentlyPlayed.Select(p => p.MusicId).Distinct().ToList();
                _logger.LogInformation($"Toplam {musicIds.Count} benzersiz şarkı ID'si bulundu");

                try
                {
                    // Tüm müzik bilgilerini tek sorguda alalım
                    var musicDetails = _context.Musics
                        .Where(m => musicIds.Contains(m.musicid))
                        .ToDictionary(m => m.musicid);

                    _logger.LogInformation($"{musicIds.Count} ID için {musicDetails.Count} adet müzik bilgisi bulundu");

                    // Şarkı bilgilerini birleştir
                    var result = recentlyPlayed.Select(p => {
                        musicDetails.TryGetValue(p.MusicId, out var music);

                        // Null kontrolü yap
                        string title = "Bulunamadı";
                        string artist = "Bulunamadı";

                        if (music != null)
                        {
                            title = string.IsNullOrEmpty(music.title) ? "Başlık Yok" : music.title;
                            artist = string.IsNullOrEmpty(music.artist) ? "Sanatçı Yok" : music.artist;
                        }

                        return new
                        {
                            Title = title,
                            Artist = artist,
                            PlayedAt = p.PlayedAt.ToString("g"),
                            MusicId = p.MusicId
                        };
                    }).ToList();

                    var response = new { played = result };
                    _logger.LogInformation($"Başarılı yanıt: {result.Count} kayıt gönderiliyor");
                    return Json(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Müzik detayları alınırken hata oluştu");

                    // Hata durumunda en azından temel dinleme verilerini dönelim
                    var fallbackResult = recentlyPlayed.Select(p => new
                    {
                        Title = "Veri alınamadı",
                        Artist = "Veri alınamadı",
                        PlayedAt = p.PlayedAt.ToString("g"),
                        MusicId = p.MusicId
                    }).ToList();

                    return Json(new { played = fallbackResult, error = "Müzik detayları alınamadı" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetHistory metodu hatası: {Message}", ex.Message);
                _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
                return Json(new { error = "Sunucu hatası: " + ex.Message, played = new List<object>() });
            }
        }
    }
}
 

