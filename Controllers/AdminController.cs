using EmoTagger.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System;
using EmoTagger.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace EmoTagger.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Constructor - Dependency Injection ile context alınıyor
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Admin sayfasına erişimi kontrol eden middleware
        private async Task<bool> IsAdminAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return false;

            var user = await _context.Users.FindAsync(userId);
            return user != null && user.IsAdmin;
        }

        public async Task<IActionResult> Index()
        {
            if (!await IsAdminAsync())
                return RedirectToAction("Login", "Dashboard");

            // Run all 3 model scripts
            string model1Output = RunPythonScript("Scripts/model1.py");
            string model2Output = RunPythonScript("Scripts/model2.py,train_model2.py,audio_feature_extractor.py");
            string model3Output = RunPythonScript("Scripts/model3.py,model3_lyrics.py");

            // Define paths for HTML results
            string model1HtmlPath = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "predicted_emotions.html");
            string model2HtmlPath = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "model2_predictions.html");
            string model3HtmlPath = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "model3_lyrics_emotions.html");

            // Set ViewBags for each model's HTML
            ViewBag.EmotionHtml1 = System.IO.File.Exists(model1HtmlPath)
                ? System.IO.File.ReadAllText(model1HtmlPath)
                : $"<p style='color:red;'>Model 1 HTML not found!</p><pre>{model1Output}</pre>";
            ViewBag.EmotionHtml2 = System.IO.File.Exists(model2HtmlPath)
                ? System.IO.File.ReadAllText(model2HtmlPath)
                : $"<p style='color:red;'>Model 2 HTML not found!</p><pre>{model2Output}</pre>";
            ViewBag.EmotionHtml3 = System.IO.File.Exists(model3HtmlPath)
                ? System.IO.File.ReadAllText(model3HtmlPath)
                : $"<p style='color:red;'>Model 3 HTML not found!</p><pre>{model3Output}</pre>";

            // Original statistics from first implementation
            ViewBag.UserCount = await _context.Users.CountAsync();
            ViewBag.MusicCount = await _context.Musics.CountAsync();
            ViewBag.TotalTagCount = await _context.MusicTags.CountAsync();

            // En çok ve en az dinlenen müzik(ler)
            var musicPlayCounts = await _context.RecentlyPlayed
                .GroupBy(rp => rp.MusicId)
                .Select(g => new { MusicId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            // En çok dinlenen ilk 3 ve eşit olanlar
            var mostCounts = musicPlayCounts.Take(3).Select(x => x.Count).ToList();
            int minTopCount = mostCounts.Count > 0 ? mostCounts.Min() : 0;
            var mostPlayedMusics = musicPlayCounts.Where(x => x.Count >= minTopCount).ToList();
            var mostPlayedIds = mostPlayedMusics.Select(mp => mp.MusicId).ToList();
            var mostPlayedMusicEntities = await _context.Musics
                .Where(m => mostPlayedIds.Contains(m.musicid))
                .ToListAsync();
            ViewBag.MostPlayedList = mostPlayedMusicEntities
                .Select(m => new {
                    title = m.title,
                    artist = m.artist,
                    Count = mostPlayedMusics.First(mp => mp.MusicId == m.musicid).Count
                })
                .OrderByDescending(m => m.Count)
                .ThenBy(m => m.title)
                .ToList();

            // En az dinlenen müzik(ler) - birden fazla varsa hepsi
            if (musicPlayCounts.Count > 0)
            {
                var minCount = musicPlayCounts.Min(x => x.Count);
                var leastPlayedMusics = musicPlayCounts.Where(x => x.Count == minCount).ToList();
                var leastPlayedIds = leastPlayedMusics.Select(lp => lp.MusicId).ToList();
                var leastPlayedMusicEntities = await _context.Musics
                    .Where(m => leastPlayedIds.Contains(m.musicid))
                    .ToListAsync();
                ViewBag.LeastPlayedList = leastPlayedMusicEntities
                    .Select(m => new {
                        title = m.title,
                        artist = m.artist,
                        Count = minCount
                    })
                    .OrderBy(m => m.title)
                    .ToList();
            }
            else
            {
                ViewBag.LeastPlayedList = new List<object>();
            }

            // Ortalama dinlenme
            ViewBag.AvgPlayCount = musicPlayCounts.Count > 0 ? (int)Math.Round(musicPlayCounts.Average(x => x.Count)) : 0;

            return View("~/Views/Dashboard/AdminPanel.cshtml");
        }

        private string RunPythonScript(string scriptPath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = scriptPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            try
            {
                var process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                return string.IsNullOrWhiteSpace(error) ? output : $"Python Error:\n{error}";
            }
            catch (Exception ex)
            {
                return $"Script failed: {ex.Message}";
            }
        }

        // Mevcut Users() metodu - HTML view döndürür
        public IActionResult Users()
        {
            return View();
        }

        public async Task<IActionResult> Musics(int page = 1, int pageSize = 20)
        {
            if (!await IsAdminAsync())
                return RedirectToAction("Login", "Dashboard");

            var query = _context.Musics.OrderBy(m => m.musicid);
            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var musics = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View(musics);
        }

        // JSON veri endpoint'i - Kullanıcı verilerini JSON olarak döndürür
        [HttpGet]
        public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 10)
        {
            if (!await IsAdminAsync())
                return Unauthorized();

            var query = _context.Users.AsQueryable();

            int totalUsers = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.Country,
                    u.PhoneNumber,
                    u.CreatedAt,
                    PlayCount = _context.RecentlyPlayed
                        .Where(rp => rp.UserId == u.Id)
                        .Select(rp => rp.MusicId)
                        .Distinct()
                        .Count(),
                    TagCount = _context.MusicTags
                        .Where(mt => mt.UserId == u.Id)
                        .Count()
                })
                .ToListAsync();

            return Json(new
            {
                users,
                page,
                pageSize,
                totalUsers
            });
        }

        // Kullanıcı silme API endpoint'i
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteUserRequest request)
        {
            if (!await IsAdminAsync())
                return Json(new { success = false, message = "Yetkisiz erişim" });

            var user = await _context.Users.FindAsync(request.Id);
            if (user == null)
                return Json(new { success = false, message = "Kullanıcı bulunamadı" });

            try
            {
                var userId = user.Id;
                _context.RecentlyPlayed.RemoveRange(_context.RecentlyPlayed.Where(r => r.UserId == userId));
                _context.MusicTags.RemoveRange(_context.MusicTags.Where(t => t.UserId == userId));
                _context.PlayCounts.RemoveRange(_context.PlayCounts.Where(p => p.UserId == userId));
                _context.FriendRequests.RemoveRange(_context.FriendRequests.Where(f => f.FromUserId == userId || f.ToUserId == userId));
                _context.Favorites.RemoveRange(_context.Favorites.Where(f => f.user_id == userId));
                // ... başka ilişkili tablolar varsa ekle

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Silme işlemi sırasında bir hata oluştu", error = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        // Kullanıcı silme isteği için model sınıfı
        public class DeleteUserRequest
        {
            public int Id { get; set; }
        }

        // Müzik düzenleme sayfası
        public async Task<IActionResult> EditMusic(int id)
        {
            if (!await IsAdminAsync())
                return RedirectToAction("Login", "Dashboard");

            var music = await _context.Musics.FindAsync(id);
            if (music == null)
                return NotFound();

            return View(music);
        }

        // Müzik güncelleme (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMusic(Music music)
        {
            if (!await IsAdminAsync())
                return RedirectToAction("Login", "Dashboard");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingMusic = await _context.Musics.FindAsync(music.musicid);
                    if (existingMusic == null)
                        return NotFound();

                    existingMusic.title = music.title;
                    existingMusic.artist = music.artist;

                    _context.Update(existingMusic);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Musics));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Güncelleme sırasında bir hata oluştu.");
                }
            }
            return View(music);
        }

        // Müzik silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMusic(int id)
        {
            if (!await IsAdminAsync())
                return RedirectToAction("Login", "Dashboard");

            var music = await _context.Musics.FindAsync(id);
            if (music == null)
                return NotFound();

            _context.Musics.Remove(music);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Musics));
        }

        // Inline edit için JSON endpoint
        [HttpPost]
        [Route("Admin/EditMusicJson")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMusicJson([FromBody] MusicEditRequest request)
        {
            if (!await IsAdminAsync())
                return Json(new { success = false, message = "Yetkisiz erişim" });

            if (request == null || request.musicid <= 0 || string.IsNullOrWhiteSpace(request.title) || string.IsNullOrWhiteSpace(request.artist))
                return Json(new { success = false, message = "Geçersiz veri" });

            try
            {
                var music = await _context.Musics.FindAsync(request.musicid);
                if (music == null)
                    return Json(new { success = false, message = "Müzik bulunamadı" });

                music.title = request.title;
                music.artist = request.artist;
                // createdat alanının Kind'ı Unspecified ise UTC olarak ayarla
                if (music.createdat.Kind == DateTimeKind.Unspecified)
                    music.createdat = DateTime.SpecifyKind(music.createdat, DateTimeKind.Utc);
                _context.Update(music);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Güncelleme sırasında hata oluştu",
                    error = ex.Message,
                    inner = ex.InnerException?.Message,
                    stack = ex.StackTrace,
                    innerStack = ex.InnerException?.StackTrace,
                    innerInner = ex.InnerException?.InnerException?.Message,
                    innerInnerStack = ex.InnerException?.InnerException?.StackTrace
                });
            }
        }

        public class MusicEditRequest
        {
            public int musicid { get; set; }
            public string title { get; set; }
            public string artist { get; set; }
        }
    }
}