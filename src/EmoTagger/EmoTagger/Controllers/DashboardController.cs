using EmoTagger.Data;
using EmoTagger.Models;
using EmoTagger.Services;
using EmoTagger.ViewComponents;
using EmoTagger.ViewModels;
using EmoTagger.Views.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
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

        [HttpGet]
        public async Task<IActionResult> GetComments(int musicId)
        {
            try
            {
                // Debug bilgisi
                Console.WriteLine($"GetComments çağrıldı, müzik ID: {musicId}");

                // Bu müzik ID için yorumları getir - lowercase property adlarıyla
                var comments = await _context.Comments
                    .Where(c => c.music_id == musicId)
                    .OrderByDescending(c => c.created_at)
// GetComments metodundaki Select içerisinde
.Select(c => new
{
    Id = c.id,                 // 'Id' yerine 'id' kullanın
    Comment = c.comment_text,
    UserId = c.user_id,
    UserName = c.user_id.HasValue ?
        (_context.Users.FirstOrDefault(u => u.Id == c.user_id) != null ?
            _context.Users.FirstOrDefault(u => u.Id == c.user_id).FirstName + " " +
            _context.Users.FirstOrDefault(u => u.Id == c.user_id).LastName :
            "Kullanıcı Bulunamadı") :
        "Anonim",
    CreatedAt = c.created_at,
    IsCurrentUser = c.user_id.HasValue && HttpContext.Session.GetInt32("UserId") == c.user_id
})
                    .ToListAsync();

                Console.WriteLine($"Bulunan yorum sayısı: {comments.Count}");
                return Json(new { success = true, comments });
            }
            catch (Exception ex)
            {
                // Hata detaylarını logla
                Console.WriteLine($"GetComments hatası: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"İç hata: {ex.InnerException.Message}");
                }
                // Stack trace da ekle
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Yorumlar alınırken bir hata oluştu", error = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] AddCommentModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Comment))
                {
                    return Json(new { success = false, message = "Yorum boş olamaz" });
                }

                int? userId = HttpContext.Session.GetInt32("UserId");

                // Eğer anonim gönderiliyorsa userId null olmalı
                if (model.IsAnonymous || !userId.HasValue)
                {
                    userId = null;
                }

                var comment = new Comment
                {
                    music_id = model.MusicId,      // music_id kullanın
                    user_id = userId,              // user_id kullanın 
                    comment_text = model.Comment,  // comment_text kullanın
                    created_at = DateTime.UtcNow   // created_at kullanın
                };
                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                var innerError = ex.InnerException != null ? ex.InnerException.Message : "Yok";
                return Json(new
                {
                    success = false,
                    message = "Yorum eklenirken bir hata oluştu",
                    error = ex.Message,
                    innerError = innerError
                });
            }
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
            return Json(new { isLoggedIn = (userId != null), userId });
        }
        // Favori durumunu kontrol et
        [HttpGet]
        public async Task<IActionResult> CheckFavorite(int musicId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { isFavorite = false });
            }

            try
            {
                var isFavorite = await _context.Favorites
                    .AnyAsync(f => f.user_id == userId.Value && f.music_id == musicId);

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
                          (f, m) => new
                          {
                              id = m.musicid,
                              title = m.title,
                              artist = m.artist,
                              filename = m.filename, // filename alanı eklendi
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
            if (loginUser.Email == "emotagger@admin.com")
            {
                Console.WriteLine($"Gelen şifre: {loginUser.Password}");
                Console.WriteLine($"Hash: {user.Password}");
            }

            // ✅ Session ayarları
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.FirstName);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserProfileImage", user.ProfileImageUrl ?? "/assets/images/default-profile.png");

            // ✅ Admin kontrolü
            HttpContext.Session.SetString("UserRole", user.IsAdmin ? "Admin" : "User");

            return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });

        }




        public IActionResult ListenMixed(int page = 1)
        {
            int pageSize = 15;
            var sortedMusics = _context.Musics.OrderBy(m => m.musicid).ToList();

            int totalItems = sortedMusics.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            int skip = (page - 1) * pageSize;
            var pagedTracks = sortedMusics.Skip(skip).Take(pageSize).ToList();

            // Gerçek dinlenme sayılarını PlayCounts tablosundan toplayarak ekle
            var playCountsDict = _context.PlayCounts
                .GroupBy(pc => pc.MusicId)
                .Select(g => new { MusicId = g.Key, Total = g.Sum(x => x.Count) })
                .ToDictionary(x => x.MusicId, x => x.Total);

            var tracksWithCounts = pagedTracks.Select(m => new EmoTagger.Models.ListenMixedTrackViewModel
            {
                MusicId = m.musicid,
                Title = m.title,
                Artist = m.artist,
                Filename = m.filename,
                PlayCount = playCountsDict.ContainsKey(m.musicid) ? playCountsDict[m.musicid] : 0
            }).ToList();

            // Tüm müzikleri sıralı şekilde al (footer player için)
            var allTracks = sortedMusics.Select(m => new EmoTagger.Models.ListenMixedTrackViewModel
            {
                MusicId = m.musicid,
                Title = m.title,
                Artist = m.artist,
                Filename = m.filename,
                PlayCount = playCountsDict.ContainsKey(m.musicid) ? playCountsDict[m.musicid] : 0
            }).ToList();

            // CurrentTrack işlemi (isteğe bağlı)
            Music currentTrack = null;
            if (HttpContext.Session.TryGetValue("CurrentMusicId", out byte[] trackIdBytes))
            {
                string trackIdStr = System.Text.Encoding.UTF8.GetString(trackIdBytes);
                if (int.TryParse(trackIdStr, out int currentTrackId))
                {
                    currentTrack = _context.Musics.FirstOrDefault(m => m.musicid == currentTrackId);
                }
            }

            var viewModel = new ListenMixedViewModel
            {
                Tracks = tracksWithCounts,
                AllTracks = allTracks, // Tüm müzikleri ekle
                CurrentPage = page,
                TotalPages = totalPages,
                CurrentTrack = currentTrack
            };

            return View(viewModel);
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
        [HttpGet]
        public async Task<IActionResult> Messages(int friendsPage = 1, int requestsPage = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Dashboard");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login", "Dashboard");

            // Get user's music statistics
            var mostTaggedGenre = await _context.MusicTags
                .Where(t => t.UserId == userId.Value)
                .GroupBy(t => t.Tag)
                .Select(g => new { Tag = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Select(x => x.Tag)
                .FirstOrDefaultAsync();

            var mostTaggedCount = 0;
            if (!string.IsNullOrEmpty(mostTaggedGenre))
            {
                mostTaggedCount = await _context.MusicTags
                    .Where(t => t.UserId == userId.Value && t.Tag == mostTaggedGenre)
                    .CountAsync();
            }

            var totalTagCount = await _context.MusicTags
                .Where(t => t.UserId == userId.Value)
                .CountAsync();

            var totalPlayedMusic = await _context.RecentlyPlayed
                .Where(p => p.UserId == userId.Value)
                .Select(p => p.MusicId)
                .Distinct()
                .CountAsync();

            var favoriteCount = await _context.Favorites
                .Where(f => f.user_id == userId.Value)
                .CountAsync();

            // Get friends with pagination
            var pageSize = 5;
            var friends = await _context.FriendRequests
                .Where(f => (f.FromUserId == userId.Value || f.ToUserId == userId.Value) && f.IsAccepted)
                .Select(f => f.FromUserId == userId.Value ? f.ToUserId : f.FromUserId)
                .ToListAsync();

            var friendsListRaw = await _context.Users
                .Where(u => friends.Contains(u.Id))
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            var uniqueFriends = friendsListRaw
                .GroupBy(f => f.Id)
                .Select(g => g.First())
                .ToList();

            var totalFriends = uniqueFriends.Count;
            var friendsTotalPages = (int)Math.Ceiling(totalFriends / (double)pageSize);

            var friendsList = uniqueFriends
                .Skip((friendsPage - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new FriendViewModel
                {
                    Id = u.Id,
                    Name = u.FirstName + " " + u.LastName,
                    ProfileImageUrl = u.ProfileImageUrl,
                    IsOnline = u.IsOnline
                }).ToList();

            // Friend requests
            var allRequests = await _context.FriendRequests
                .Where(f => f.ToUserId == userId.Value && !f.IsAccepted)
                .OrderByDescending(f => f.RequestedAt)
                .ToListAsync();

            var requestsList = allRequests
                .GroupBy(f => f.FromUserId)
                .Select(g => g.First())
                .Select(f => new FriendRequestViewModel
                {
                    RequestId = f.Id,
                    FromUserId = f.FromUserId,
                    FromUserName = _context.Users.Where(u => u.Id == f.FromUserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                    ProfileImageUrl = _context.Users.Where(u => u.Id == f.FromUserId).Select(u => u.ProfileImageUrl).FirstOrDefault()
                }).ToList();

            var totalRequests = requestsList.Count;
            var requestsTotalPages = (int)Math.Ceiling(totalRequests / (double)pageSize);

            // Create ProfileViewModel
            var profileViewModel = new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Country = user.Country,
                ProfileImageUrl = user.ProfileImageUrl ?? "/assets/images/default-profile.png",
                IncomingRequestsCount = totalRequests,
                MostTaggedGenre = mostTaggedGenre,
                MostTaggedCount = mostTaggedCount,
                TotalTagCount = totalTagCount,
                TotalPlayedMusic = totalPlayedMusic,
                FavoriteCount = favoriteCount,
                Friends = friendsList,
                IncomingRequests = requestsList,
                FriendsPage = friendsPage,
                RequestsPage = requestsPage,
                FriendsTotalPages = friendsTotalPages,
                RequestsTotalPages = requestsTotalPages,
                PageSize = pageSize
            };

            return View(profileViewModel);
        }
        // 🛡️ Kullanıcı Kayıt İşlemi
        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            // Form verilerini konsola yazdır
            Console.WriteLine($"Form verileri: FirstName={user.FirstName}, LastName={user.LastName}, Email={user.Email}");
            Console.WriteLine($"Şifre kontrolü: Password uzunluğu={user.Password?.Length ?? 0}, ConfirmPassword uzunluğu={user.ConfirmPassword?.Length ?? 0}");

            // Email kontrolü
            if (string.IsNullOrEmpty(user.Email))
            {
                return Json(new { success = false, message = "E-posta adresi girilmedi!" });
            }

            // Kullanıcı adı soyadı kontrolü
            if (string.IsNullOrEmpty(user.FirstName) || string.IsNullOrEmpty(user.LastName))
            {
                return Json(new { success = false, message = "Ad ve soyad alanları doldurulmalıdır!" });
            }

            // Email formatı kontrolü
            if (!user.Email.Contains("@") || !user.Email.Contains("."))
            {
                return Json(new { success = false, message = "Geçerli bir e-posta adresi girin!" });
            }

            // Şifre boş mu kontrolü
            if (string.IsNullOrEmpty(user.Password))
            {
                return Json(new { success = false, message = "Şifre girilmedi!" });
            }

            // Şifre uzunluğu kontrolü
            if (user.Password.Length < 6)
            {
                return Json(new { success = false, message = "Şifreniz en az 6 karakter olmalıdır." });
            }

            // Email kayıtlı mı kontrolü
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                return Json(new { success = false, message = "Bu e-posta zaten kayıtlı!" });
            }

            // Şifre eşleşme kontrolü
            if (user.Password != user.ConfirmPassword)
            {
                return Json(new { success = false, message = "Şifreler uyuşmuyor!" });
            }

            try
            {
                // Şifreyi hashle ve kayıt tarihini ekle
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                user.CreatedAt = DateTime.UtcNow;

                // Kullanıcıyı veritabanına ekle
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Başarılı yanıt
                return Json(new
                {
                    success = true,
                    message = "Kayıt işlemi başarılı!",
                    redirectUrl = Url.Action("Login", "Dashboard")
                });
            }
            catch (Exception ex)
            {
                // Hata durumunda detayları konsola yazdır
                Console.WriteLine($"Kayıt hatası: {ex.Message}");
                Console.WriteLine($"İç hata: {ex.InnerException?.Message}");

                return Json(new
                {
                    success = false,
                    message = "Kayıt sırasında bir hata oluştu: " + (ex.InnerException?.Message ?? ex.Message)
                });
            }
        }

        // 🎧 Dinleme Geçmişi
        public async Task<IActionResult> ListeningHistory(int page = 1, int pageSize = 10)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var historyQuery = _context.RecentlyPlayed
                .Include(h => h.Music)
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.PlayedAt);

            var totalCount = await historyQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var history = await historyQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new EmoTagger.Models.ListeningHistoryItem
                {
                    Title = h.Music.title,
                    Artist = h.Music.artist,
                    PlayedAt = h.PlayedAt.ToString("dd.MM.yyyy HH:mm")
                })
                .ToListAsync();

            var model = new EmoTagger.Models.ListeningHistoryViewModel
            {
                History = history,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(model);
        }


        // 🏆 Liderlik Tablosu
        public IActionResult Leaderboard()
        {
            // En çok müzik dinleyenler (PlayCounts tablosu üzerinden)
            var topListeners = _context.PlayCounts
                .GroupBy(pc => pc.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Sum(x => x.Count)
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList()
                .Join(_context.Users,
                    stat => stat.UserId,
                    user => user.Id,
                    (stat, user) => new EmoTagger.ViewModels.LeaderboardUserViewModel
                    {
                        UserId = user.Id,
                        UserName = user.FirstName + " " + user.LastName,
                        ProfileImageUrl = string.IsNullOrEmpty(user.ProfileImageUrl) ? "/assets/images/default-profile.png" : user.ProfileImageUrl,
                        Count = stat.Count
                    })
                .ToList();

            // En çok tag atanlar (MusicTags tablosu üzerinden)
            var topTaggers = _context.MusicTags
                .GroupBy(mt => mt.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList()
                .Join(_context.Users,
                    stat => stat.UserId,
                    user => user.Id,
                    (stat, user) => new EmoTagger.ViewModels.LeaderboardUserViewModel
                    {
                        UserId = user.Id,
                        UserName = user.FirstName + " " + user.LastName,
                        ProfileImageUrl = string.IsNullOrEmpty(user.ProfileImageUrl) ? "/assets/images/default-profile.png" : user.ProfileImageUrl,
                        Count = stat.Count
                    })
                .ToList();

            var model = new EmoTagger.ViewModels.LeaderboardViewModel
            {
                TopListeners = topListeners,
                TopTaggers = topTaggers
            };

            return View(model);
        }

        // 🎧 Playlist Sayfası
        public IActionResult Playlist()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Dashboard");

            var model = new MusicViewModel
            {
                UserId = userId.Value
            };
            return View(model);
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
        public async Task<IActionResult> Profile(int friendsPage = 1, int requestsPage = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Dashboard");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login", "Dashboard");

            // Get user's music statistics
            var mostTaggedGenre = await _context.MusicTags
                .Where(t => t.UserId == userId.Value)
                .GroupBy(t => t.Tag)
                .Select(g => new { Tag = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Select(x => x.Tag)
                .FirstOrDefaultAsync();

            var mostTaggedCount = 0;
            if (!string.IsNullOrEmpty(mostTaggedGenre))
            {
                mostTaggedCount = await _context.MusicTags
                    .Where(t => t.UserId == userId.Value && t.Tag == mostTaggedGenre)
                    .CountAsync();
            }

            var totalTagCount = await _context.MusicTags
                .Where(t => t.UserId == userId.Value)
                .CountAsync();

            var totalPlayedMusic = await _context.RecentlyPlayed
                .Where(p => p.UserId == userId.Value)
                .Select(p => p.MusicId)
                .Distinct()
                .CountAsync();

            var favoriteCount = await _context.Favorites
                .Where(f => f.user_id == userId.Value)
                .CountAsync();

            // Get friends with pagination
            var pageSize = 5;
            var friends = await _context.FriendRequests
                .Where(f => (f.FromUserId == userId.Value || f.ToUserId == userId.Value) && f.IsAccepted)
                .Select(f => f.FromUserId == userId.Value ? f.ToUserId : f.FromUserId)
                .ToListAsync();

            var friendsListRaw = await _context.Users
                .Where(u => friends.Contains(u.Id))
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            var uniqueFriends = friendsListRaw
                .GroupBy(f => f.Id)
                .Select(g => g.First())
                .ToList();

            var totalFriends = uniqueFriends.Count;
            var friendsTotalPages = (int)Math.Ceiling(totalFriends / (double)pageSize);

            var friendsList = uniqueFriends
                .Skip((friendsPage - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new FriendViewModel
                {
                    Id = u.Id,
                    Name = u.FirstName + " " + u.LastName,
                    ProfileImageUrl = u.ProfileImageUrl,
                    IsOnline = u.IsOnline
                }).ToList();

            // 1. Adım: Sorguyu veritabanından çek
            var allRequests = await _context.FriendRequests
                .Where(f => f.ToUserId == userId.Value && !f.IsAccepted)
                .OrderByDescending(f => f.RequestedAt)
                .ToListAsync();

            // 2. Adım: C# tarafında tekilleştir
            var requestsList = allRequests
                .GroupBy(f => f.FromUserId)
                .Select(g => g.First())
                .Select(f => new FriendRequestViewModel
                {
                    RequestId = f.Id,
                    FromUserId = f.FromUserId,
                    FromUserName = _context.Users.Where(u => u.Id == f.FromUserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                    ProfileImageUrl = _context.Users.Where(u => u.Id == f.FromUserId).Select(u => u.ProfileImageUrl).FirstOrDefault()
                }).ToList();

            var totalRequests = requestsList.Count;
            var requestsTotalPages = (int)Math.Ceiling(totalRequests / (double)pageSize);

            // Create ProfileViewModel
            var profileViewModel = new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Country = user.Country,
                ProfileImageUrl = user.ProfileImageUrl ?? "/assets/images/default-profile.png",
                IncomingRequestsCount = totalRequests,
                MostTaggedGenre = mostTaggedGenre,
                MostTaggedCount = mostTaggedCount,
                TotalTagCount = totalTagCount,
                TotalPlayedMusic = totalPlayedMusic,
                FavoriteCount = favoriteCount,
                Friends = friendsList,
                IncomingRequests = requestsList,
                FriendsPage = friendsPage,
                RequestsPage = requestsPage,
                FriendsTotalPages = friendsTotalPages,
                RequestsTotalPages = requestsTotalPages,
                PageSize = pageSize
            };

            return View(profileViewModel);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            // Kullanıcı kimlik doğrulama kontrolü
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Dashboard");

            // Model doğrulama kontrolü
            if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
            {
                TempData["ErrorMessage"] = "Ad ve soyad alanları boş bırakılamaz.";
                return RedirectToAction("Profile", new { friendsPage = model.FriendsPage, requestsPage = model.RequestsPage });
            }

            try
            {
                // Kullanıcıyı veritabanından al
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.Id == userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                    return RedirectToAction("Login");
                }

                // Kullanıcı bilgilerini güncelle
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.Country = model.Country;

                // UpdatedAt alanını güncelleme
                try
                {
                    user.UpdatedAt = DateTime.UtcNow; // UTC kullanın
                }
                catch (Exception updateEx)
                {
                    TempData["ErrorMessage"] = $"UpdatedAt güncellemesi başarısız: {updateEx.Message}";
                    return RedirectToAction("Profile", new { friendsPage = model.FriendsPage, requestsPage = model.RequestsPage });
                }

                // Değişiklikleri veritabanına kaydet
                try
                {
                    _context.Entry(user).State = EntityState.Modified; // Entity'nin durumunu açıkça belirt
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
                }
                catch (DbUpdateException dbEx)
                {
                    var innerMsg = dbEx.InnerException != null ? dbEx.InnerException.Message : "İç hata yok";
                    TempData["ErrorMessage"] = $"Veritabanı güncellemesi başarısız: {dbEx.Message} | İç hata: {innerMsg}";
                    return RedirectToAction("Profile", new { friendsPage = model.FriendsPage, requestsPage = model.RequestsPage });
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : "İç hata yok";
                TempData["ErrorMessage"] = $"Profil güncellenirken bir hata oluştu: {ex.Message} | İç hata: {innerMsg}";
            }

            return RedirectToAction("Profile", new { friendsPage = model.FriendsPage, requestsPage = model.RequestsPage });
        }

        [HttpGet]
        public IActionResult GetProfileImageUrl()
        {
            // Session'dan profil resmi URL'ini al
            var profileImageUrl = HttpContext.Session.GetString("UserProfileImage") ?? "/assets/images/default-profile.png";

            // JSON olarak dön
            return Json(new { profileImageUrl });
        }
        [HttpPost]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profileImage)
        {
            try
            {
                // 1. Kullanıcı kimliğini al
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["ErrorMessage"] = "Oturum bulunamadı. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Dashboard");
                }

                // 2. Dosya kontrolleri
                if (profileImage == null || profileImage.Length == 0)
                {
                    TempData["ErrorMessage"] = "Dosya seçilmedi.";
                    return RedirectToAction("Profile");
                }

                // 3. Dosya türünü kontrol et
                var extension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] = "Sadece resim dosyaları kabul edilmektedir (.jpg, .jpeg, .png, .gif).";
                    return RedirectToAction("Profile");
                }

                // 4. Kullanıcıyı veritabanından bul
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                    return RedirectToAction("Login", "Dashboard");
                }

                // 5. Önceki resmi hatırla
                var oldImageUrl = user.ProfileImageUrl;
                System.Diagnostics.Debug.WriteLine($"Önceki profil resmi: {oldImageUrl}");

                // 6. Yükleme klasörü ve dosya adı
                var timestamp = DateTime.Now.Ticks; // Önbellek sorunu için timestamp
                var fileName = $"user_{userId}_{timestamp}{extension}";
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, fileName);

                // 7. Dosyayı kaydet
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(fileStream);
                }

                // 8. Dosya yolu
                var fileUrl = $"/uploads/profiles/{fileName}";

                // 9. Kullanıcı bilgilerini güncelle
                user.ProfileImageUrl = fileUrl;

                // 10. UpdatedAt alanını güncelle (eğer varsa)
                if (user.GetType().GetProperty("UpdatedAt") != null)
                {
                    // Reflection kullanarak dinamik olarak UpdatedAt özelliğini güncelle
                    var updatedAtProperty = user.GetType().GetProperty("UpdatedAt");
                    updatedAtProperty.SetValue(user, DateTime.UtcNow);
                }

                // 11. Değişiklikleri kaydet
                _context.Entry(user).State = EntityState.Modified;
                var result = await _context.SaveChangesAsync();

                // 12. Debug için bilgi
                System.Diagnostics.Debug.WriteLine($"Etkilenen satır sayısı: {result}");
                System.Diagnostics.Debug.WriteLine($"Yeni profil resmi: {fileUrl}");

                if (result > 0)
                {
                    // 13. Session'daki profil resmini güncelle
                    HttpContext.Session.SetString("UserProfileImage", fileUrl);

                    // 14. Başarı mesajı
                    TempData["SuccessMessage"] = "Profil resmi başarıyla güncellendi!";

                    // 15. JavaScript ile sayfanın yenilenmesini sağla
                    TempData["RefreshScript"] = "window.location.href = window.location.pathname + '?refresh=" + timestamp + "';";

                    // 16. Eski resmi temizle
                    if (!string.IsNullOrEmpty(oldImageUrl) &&
                        !oldImageUrl.Contains("default-profile.png") &&
                        oldImageUrl.StartsWith("/uploads/profiles/"))
                    {
                        try
                        {
                            // Eski dosyayı bul ve sil
                            var oldFileName = Path.GetFileName(oldImageUrl.Split('?')[0]);
                            var oldFilePath = Path.Combine(uploadsFolder, oldFileName);

                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                                System.Diagnostics.Debug.WriteLine($"Eski profil resmi silindi: {oldFilePath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Eski resim silinirken hata: {ex.Message}");
                        }
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Profil resmi güncellenemedi. Hiçbir değişiklik kaydedilmedi.";
                }
            }
            catch (Exception ex)
            {
                var innerExMessage = ex.InnerException != null ? ex.InnerException.Message : "İç hata yok";
                TempData["ErrorMessage"] = $"Profil resmi yüklenirken bir hata oluştu: {ex.Message} | {innerExMessage}";

                // Debug için
                System.Diagnostics.Debug.WriteLine($"UploadProfilePicture hatası: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"İç hata: {innerExMessage}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            // Sayfayı refresh parametresi ile yenile
            return RedirectToAction("Profile", new { refresh = DateTime.Now.Ticks });
        }

        [HttpGet]
        public IActionResult Logout()
        {
            // Tüm session verilerini temizle
            HttpContext.Session.Clear();

            // Çerezleri temizle (varsa)
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            // Tarayıcının önbelleğe almasını engellemek için timestamp ekle
            return RedirectToAction("Index", "Home", new { t = DateTime.Now.Ticks });
        }
        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel();

            viewModel.EmotionCategories = new List<EmotionCategoryViewModel>
    {
        new EmotionCategoryViewModel { Tag = "Sad" },
        new EmotionCategoryViewModel { Tag = "Happy" },
        new EmotionCategoryViewModel { Tag = "Nostalgic" },
        new EmotionCategoryViewModel { Tag = "Energetic" },
        new EmotionCategoryViewModel { Tag = "Relaxing" },
        new EmotionCategoryViewModel { Tag = "Romantic" }
    };

            // Diğer özellikleri de boş listelerle başlat
            viewModel.TrendingSongs = new List<TrendingSongViewModel>();
            viewModel.MostTaggedSongs = new List<MostTaggedSongViewModel>();
            viewModel.TagDistribution = new List<TagDistributionViewModel>();
            viewModel.RecommendedSongs = new List<RecommendedSongViewModel>();

            return View(viewModel);
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
        public IActionResult GetRecentlyPlayed()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized();
            }

            // PlayCounts tablosu yerine RecentlyPlayed tablosunu sorgulayın
            var lastPlays = _context.RecentlyPlayed
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PlayedAt)
                .Take(10)
                .Select(p => new
                {
                    Title = p.Music.title,
                    Artist = p.Music.artist,
                    PlayedAt = p.PlayedAt
                }).ToList();

            return Json(new { success = true, played = lastPlays });
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
                    var result = recentlyPlayed.Select(p =>
                    {
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

        [HttpPost]
        public async Task<IActionResult> UpdateOnlineStatus(bool isOnline)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Oturum açmanız gerekiyor." });
                }

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı." });
                }

                user.IsOnline = isOnline;
                user.LastSeen = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOnlineUsers()
        {
            try
            {
                var onlineUsers = await _context.Users
                    .Where(u => u.IsOnline)
                    .Select(u => new
                    {
                        id = u.Id,
                        firstName = u.FirstName,
                        lastName = u.LastName,
                        profileImageUrl = u.ProfileImageUrl,
                        lastSeen = u.LastSeen
                    })
                    .ToListAsync();

                return Json(new { success = true, onlineUsers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult SearchUsers(string query)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || string.IsNullOrWhiteSpace(query))
                return Json(new { success = false, users = new List<object>() });

            var friends = _context.FriendRequests
                .Where(f => (f.FromUserId == userId || f.ToUserId == userId) && f.IsAccepted)
                .Select(f => f.FromUserId == userId ? f.ToUserId : f.FromUserId)
                .ToList();

            var requests = _context.FriendRequests
                .Where(f => (f.FromUserId == userId || f.ToUserId == userId) && !f.IsAccepted)
                .ToList();

            var users = _context.Users
                .Where(u => (u.FirstName + " " + u.LastName).ToLower().Contains(query.ToLower())
                            && u.Id != userId)
                .Take(10)
                .ToList()
                .Select(u => new
                {
                    id = u.Id,
                    firstName = u.FirstName,
                    lastName = u.LastName,
                    profileImageUrl = u.ProfileImageUrl,
                    isOnline = u.IsOnline,
                    requested = requests.Any(r => (r.FromUserId == userId && r.ToUserId == u.Id) || (r.FromUserId == u.Id && r.ToUserId == userId)),
                    isFriend = friends.Contains(u.Id)
                })
                .ToList();

            return Json(new { success = true, users });
        }

        [HttpPost]
        public IActionResult AddFriend([FromBody] System.Text.Json.JsonElement data)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            int friendId = data.GetProperty("friendId").GetInt32();

            if (userId == null || friendId == userId)
                return Json(new { success = false });

            // Zaten istek var mı?
            var existing = _context.FriendRequests
                .FirstOrDefault(f =>
                    (f.FromUserId == userId && f.ToUserId == friendId) ||
                    (f.FromUserId == friendId && f.ToUserId == userId));

            if (existing != null)
                return Json(new { success = false, message = "Zaten istek var." });

            var request = new FriendRequest
            {
                FromUserId = userId.Value,
                ToUserId = friendId,
                IsAccepted = false
            };
            _context.FriendRequests.Add(request);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Dashboard");

            var request = await _context.FriendRequests.FirstOrDefaultAsync(f => f.Id == id && f.ToUserId == userId.Value && !f.IsAccepted);
            if (request == null)
            {
                TempData["ErrorMessage"] = "İstek bulunamadı veya zaten kabul edilmiş.";
                return RedirectToAction("Profile");
            }
            request.IsAccepted = true;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Arkadaş isteği kabul edildi.";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult GetFriends()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false, friends = new List<object>() });

            var friends = _context.FriendRequests
                .Where(f => (f.FromUserId == userId || f.ToUserId == userId) && f.IsAccepted)
                .Select(f => f.FromUserId == userId ? f.ToUserId : f.FromUserId)
                .ToList();

            var users = _context.Users
                .Where(u => friends.Contains(u.Id))
                .Select(u => new
                {
                    id = u.Id,
                    name = u.FirstName + " " + u.LastName,
                    profileImageUrl = u.ProfileImageUrl,
                    isOnline = u.IsOnline
                })
                .ToList();

            return Json(new { success = true, friends = users });
        }

        [HttpPost]
        public IActionResult CancelFriendRequest([FromBody] System.Text.Json.JsonElement data)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            int friendId = data.GetProperty("friendId").GetInt32();

            var request = _context.FriendRequests.FirstOrDefault(f =>
                !f.IsAccepted &&
                ((f.FromUserId == userId && f.ToUserId == friendId) || (f.FromUserId == friendId && f.ToUserId == userId)));
            if (request == null)
                return Json(new { success = false });

            _context.FriendRequests.Remove(request);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetIncomingFriendRequests()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false, requests = new List<object>() });

            var requests = _context.FriendRequests
                .Where(f => f.ToUserId == userId && !f.IsAccepted)
                .Select(f => new
                {
                    requestId = f.Id,
                    fromUserId = f.FromUserId,
                    fromUserName = _context.Users.Where(u => u.Id == f.FromUserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                    profileImageUrl = _context.Users.Where(u => u.Id == f.FromUserId).Select(u => u.ProfileImageUrl).FirstOrDefault()
                })
                .ToList();

            return Json(new { success = true, requests });
        }

        [HttpPost]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Dashboard");

            var request = await _context.FriendRequests.FirstOrDefaultAsync(f => f.Id == id && f.ToUserId == userId.Value && !f.IsAccepted);
            if (request == null)
            {
                TempData["ErrorMessage"] = "İstek bulunamadı veya zaten reddedilmiş.";
                return RedirectToAction("Profile");
            }

            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Arkadaş isteği reddedildi.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public IActionResult RemoveFriend([FromBody] System.Text.Json.JsonElement data)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            int friendId = data.GetProperty("friendId").GetInt32();

            var request = _context.FriendRequests.FirstOrDefault(f =>
                f.IsAccepted &&
                ((f.FromUserId == userId && f.ToUserId == friendId) || (f.FromUserId == friendId && f.ToUserId == userId)));
            if (request == null)
                return Json(new { success = false });

            _context.FriendRequests.Remove(request);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult BlockFriend([FromBody] System.Text.Json.JsonElement data)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            int friendId = data.GetProperty("friendId").GetInt32();

            // Engelleme için yeni bir tablo önerilir, burada sadece arkadaşlıktan çıkarıyoruz
            var request = _context.FriendRequests.FirstOrDefault(f =>
                ((f.FromUserId == userId && f.ToUserId == friendId) || (f.FromUserId == friendId && f.ToUserId == userId)));
            if (request != null)
            {
                _context.FriendRequests.Remove(request);
                _context.SaveChanges();
            }
            // Burada ayrıca bir BlockedUsers tablosuna ekleme yapılabilir
            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetUserMusicStats()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false });

            // En çok tag verilen tür ve tag sayısı
            var tagStats = _context.MusicTags
                .Where(mt => mt.UserId == userId)
                .GroupBy(mt => mt.Tag)
                .Select(g => new { Tag = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            var mostTaggedGenre = tagStats.FirstOrDefault()?.Tag ?? "Yok";
            var mostTaggedCount = tagStats.FirstOrDefault()?.Count ?? 0;
            var totalTagCount = tagStats.Sum(x => x.Count);

            // Toplam dinlenen müzik (recently_played tablosundan, farklı music_id sayısı)
            var totalPlayedMusic = _context.RecentlyPlayed
                .Where(rp => rp.UserId == userId)
                .Select(rp => rp.MusicId)
                .Distinct()
                .Count();

            // Favoriler (Favorites tablosu)
            int favoriteCount = 0;
            if (_context.GetType().GetProperty("Favorites") != null)
            {
                favoriteCount = _context.Favorites
                    .Where(f => f.user_id == userId)
                    .Count();
            }

            return Json(new
            {
                success = true,
                mostTaggedGenre,
                mostTaggedCount,
                totalTagCount,
                totalPlayedMusic,
                favoriteCount
            });
        }

        [HttpGet]
        public IActionResult GetPlayCounts(int musicId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { total = 0, user = 0 });

            var total = _context.PlayCounts.Where(x => x.MusicId == musicId).Sum(x => x.Count);
            var user = _context.PlayCounts.Where(x => x.MusicId == musicId && x.UserId == userId).Sum(x => x.Count);

            return Json(new { total, user });
        }

        [HttpPost]
        public IActionResult UzatLogPlayed([FromBody] LogPlayedRequest request)
        {
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("UzatLogPlayed: Geçersiz istek - null");
                    return BadRequest(new { success = false, message = "Geçersiz istek" });
                }
                if (request.MusicId <= 0)
                {
                    _logger.LogWarning($"UzatLogPlayed: Geçersiz MusicId - {request.MusicId}");
                    return BadRequest(new { success = false, message = "Geçersiz müzik ID" });
                }
                _logger.LogInformation($"UzatLogPlayed: MusicId = {request.MusicId}");
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("UzatLogPlayed: Oturum bulunamadı");
                    return Json(new { success = false, message = "Oturum açmanız gerekiyor" });
                }
                var music = _context.Musics.FirstOrDefault(m => m.musicid == request.MusicId);
                if (music == null)
                {
                    _logger.LogWarning($"UzatLogPlayed: Müzik bulunamadı - ID: {request.MusicId}");
                    return Json(new { success = false, message = "Müzik bulunamadı" });
                }
                try
                {
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
                        _logger.LogInformation($"UzatLogPlayed: Başarılı - UserId: {userId}, MusicId: {request.MusicId}");
                    }
                    else
                    {
                        _logger.LogInformation($"UzatLogPlayed: Zaten var - UserId: {userId}, MusicId: {request.MusicId}");
                    }
                    return Json(new { success = true });
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, $"UzatLogPlayed: DB hatası - UserId: {userId}, MusicId: {request.MusicId}");
                    return Json(new { success = false, message = "Veritabanı hatası" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UzatLogPlayed: Genel hata");
                return Json(new { success = false, message = "Sunucu hatası" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchMusic(string query, int page = 1, int pageSize = 15)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new { musics = new List<object>(), totalPages = 0, currentPage = 1 });

            var musicsQuery = _context.Musics
                .Where(m => m.title.ToLower().Contains(query.ToLower()) || m.artist.ToLower().Contains(query.ToLower()));

            int totalCount = await musicsQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var musics = await musicsQuery
                .OrderBy(m => m.title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    MusicId = m.musicid,
                    Title = m.title,
                    Artist = m.artist,
                    Filename = m.filename,
                    PlayCount = m.playcount
                })
                .ToListAsync();

            return Json(new { musics, totalPages, currentPage = page });
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeMusic([FromBody] AnalyzeMusicRequest request)
        {
            try
            {
                if (request.MusicId <= 0)
                {
                    return Json(new { success = false, message = "Invalid music ID" });
                }

                // Get music file path from database
                var music = await _context.Musics.FindAsync(request.MusicId);
                if (music == null)
                {
                    return Json(new { success = false, message = "Music not found" });
                }

                var musicPath = Path.Combine(_webHostEnvironment.WebRootPath, "music", music.filename);
                if (!System.IO.File.Exists(musicPath))
                {
                    return Json(new { success = false, message = "Music file not found" });
                }

                // --- PYTHON API'YE İSTEK AT ---
                using (var httpClient = new HttpClient())
                {
                    var payload = new { music_path = musicPath };
                    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync("http://localhost:5005/analyze", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("FastAPI hata döndürdü: " + response.StatusCode);
                        return StatusCode((int)response.StatusCode, new { success = false, message = "AI service error", detail = response.ReasonPhrase });
                    }

                    var resultString = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AnalyzeMusicResult>(resultString);

                    if (result == null || !result.success)
                    {
                        return Json(new { success = false, message = "Failed to analyze music" });
                    }

                    // Frontend ile uyumlu dönüş
                    _logger.LogInformation("FastAPI yanıtı: " + resultString);
                    return Json(new
                    {
                        success = true,
                        mood = result.mood,
                        tempo = result.tempo,
                        energy = result.energy,
                        rhythm = result.rhythm
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing music");
                return Json(new { success = false, message = "An error occurred while analyzing the music" });
            }
        }

        public class AnalyzeMusicRequest
        {
            public int MusicId { get; set; }
        }

        public class AnalyzeMusicResult
        {
            public bool success { get; set; }
            public string mood { get; set; }
            public string tempo { get; set; }
            public string energy { get; set; }
            public string rhythm { get; set; }
        }

        [HttpPost]
        [Route("Dashboard/PredictEmotion")]
        public async Task<IActionResult> PredictEmotion()
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null)
                return BadRequest(new { success = false, message = "No file uploaded" });

            using var httpClient = new HttpClient();
            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            content.Add(new StreamContent(fileStream), "file", file.FileName);

            var response = await httpClient.PostAsync("http://localhost:8000/predict", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("FastAPI hata döndürdü: " + response.StatusCode + " - " + responseString);
                return Json(new { success = false, message = "AI service error", detail = responseString });
            }

            try
            {
                var jsonDoc = System.Text.Json.JsonDocument.Parse(responseString);
                var root = jsonDoc.RootElement;

                // tag ve confidence varsa dön
                if (root.TryGetProperty("tag", out var tagProp) && root.TryGetProperty("confidence", out var confProp))
                {
                    return Json(new
                    {
                        success = true,
                        tag = tagProp.GetString(),
                        confidence = confProp.GetDouble()
                    });
                }
                else
                {
                    return Json(new { success = false, message = "AI response missing tag or confidence", raw = responseString });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("JSON parse error: " + ex.Message);
                return Json(new { success = false, message = "AI response is not valid JSON", raw = responseString });
            }
        }


        [HttpPost]
        [Route("Dashboard/SaveAIFeedback")]
        public IActionResult SaveAIFeedback([FromBody] AIFeedbackModel model)
        {
            // AI tag yoksa veya '-' ise feedback kaydını engelle
            if (model == null || model.MusicId <= 0 || string.IsNullOrEmpty(model.Tag) || model.Tag == "-" || string.IsNullOrEmpty(model.Feedback))
                return Json(new { success = false, message = "Eksik veri veya AI etiketi oluşmadan oy kullanılamaz!" });

            try
            {
                // UserId'yi sessiondan al
                var userId = HttpContext.Session.GetInt32("UserId");
                model.UserId = userId;

                // AI feedback tablosuna ekle
                var feedback = new AIFeedback
                {
                    music_id = model.MusicId,
                    ai_tag = model.Tag,
                    user_feedback = model.Feedback,
                    created_at = DateTime.UtcNow,
                    UserId = model.UserId // <-- EKLENDİ
                };
                _context.AIFeedbacks.Add(feedback);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Veritabanı hatası: " + ex.Message });
            }
        }
        // Model
        public class AIFeedbackModel
        {
            public int MusicId { get; set; }
            public string Tag { get; set; }
            public string Feedback { get; set; }
            public int? UserId { get; set; }
        }

        // Entity (örnek, senin modeline göre değiştir)
        [Table("ai_feedback")]
        public class AIFeedback
        {
            public int id { get; set; }
            public int music_id { get; set; }
            public string ai_tag { get; set; }
            public string user_feedback { get; set; }
            public DateTime created_at { get; set; }
            [Column("user_id")]
            public int? UserId { get; set; }
        }

        public class SendMessageModel
        {
            public int FriendId { get; set; }
            public string Content { get; set; }
        }

        [HttpPost]
        public IActionResult SendMessage([FromBody] SendMessageModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false, message = "Not logged in" });

            if (model == null || string.IsNullOrWhiteSpace(model.Content))
                return Json(new { success = false, message = "Invalid data" });

            try
            {
                // Mesajı veritabanına kaydet
                var message = new Message
                {
                    SenderId = userId.Value,
                    ReceiverId = model.FriendId,
                    Content = model.Content,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Messages.Add(message);
                _context.SaveChanges();

                // Frontend için uygun formatta dön
                var messageView = new
                {
                    id = message.Id,
                    content = message.Content,
                    isFromCurrentUser = true,
                    timestamp = message.CreatedAt,
                    isRead = message.IsRead
                };

                return Json(new { success = true, message = messageView });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error sending message: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetMessages(int friendId, int page = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false, messages = new List<object>(), message = "Not logged in" });

            try
            {
                int pageSize = 10;
                var messages = _context.Messages
                    .Where(m =>
                        (m.SenderId == userId && m.ReceiverId == friendId) ||
                        (m.SenderId == friendId && m.ReceiverId == userId))
                    .OrderBy(m => m.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new
                    {
                        id = m.Id,
                        content = m.Content,
                        isFromCurrentUser = m.SenderId == userId,
                        timestamp = m.CreatedAt,
                        isRead = m.IsRead
                    })
                    .ToList();

                return Json(new { success = true, messages = messages });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, messages = new List<object>(), message = "Error loading messages: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult MarkMessagesAsRead([FromBody] System.Text.Json.JsonElement data)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false, message = "Not logged in" });

            int friendId = data.GetProperty("friendId").GetInt32();

            // Bu kullanıcıya gelen ve okunmamış mesajları bul
            var unreadMessages = _context.Messages
                .Where(m => m.SenderId == friendId && m.ReceiverId == userId && !m.IsRead)
                .ToList();

            foreach (var msg in unreadMessages)
                msg.IsRead = true;

            _context.SaveChanges();

            return Json(new { success = true });
        }
    }
}




