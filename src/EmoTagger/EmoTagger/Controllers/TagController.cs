using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmoTagger.Data;
using EmoTagger.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmoTagger.Controllers
{
    public class TagController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TagController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPopularTags(int musicId)
        {
            try
            {
                // Bir şarkı için tüm etiketlerin istatistiklerini getir
                var tagStats = await _context.MusicTags
                    .Where(mt => mt.MusicId == musicId)
                    .GroupBy(mt => mt.Tag)
                    .Select(g => new {
                        Tag = g.Key,
                        Count = g.Count(),
                        Percentage = 0.0 // Hesaplanacak
                    })
                    .OrderByDescending(t => t.Count)
                    .Take(10) // En popüler 10 etiket
                    .ToListAsync();

                if (tagStats.Count == 0)
                    return Json(new { success = true, tags = new List<object>() });

                // Toplam etiket sayısını hesapla
                int totalTags = tagStats.Sum(s => s.Count);

                // Yüzdeleri hesapla
                var result = tagStats.Select(s => new {
                    Tag = s.Tag,
                    Count = s.Count,
                    Percentage = Math.Round((double)s.Count / totalTags * 100, 1)
                }).ToList();

                return Json(new { success = true, tags = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Etiket istatistikleri alınırken hata oluştu: " + ex.Message });
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

                bool isUpdate = existingTag != null;

                if (isUpdate)
                {
                    existingTag.Tag = request.Tag;
                    existingTag.UpdatedAt = DateTime.UtcNow;
                    _context.SaveChanges();
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
                }

                // Şarkının ana kategorisini belirle
                var mainCategory = _context.MusicTags
                    .Where(mt => mt.MusicId == request.MusicId)
                    .GroupBy(mt => mt.Tag)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault() ?? "uncategorized";

                // Şarkı için istatistikleri hesapla
                var tagStats = _context.MusicTags
                    .Where(mt => mt.MusicId == request.MusicId)
                    .GroupBy(mt => mt.Tag)
                    .Select(g => new {
                        Tag = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(t => t.Count)
                    .Take(5)
                    .ToList();

                int totalVotes = tagStats.Sum(t => t.Count);

                // En popüler etiketlerin yüzdelerini hesapla
                var tagPercentages = tagStats.Select(t => new {
                    Tag = t.Tag,
                    Count = t.Count,
                    Percentage = Math.Round((double)t.Count / totalVotes * 100)
                }).ToList();

                return Json(new
                {
                    success = true,
                    message = isUpdate ? "Etiket güncellendi" : "Etiket eklendi",
                    isUpdate = isUpdate,
                    mainCategory = mainCategory,
                    tagStats = tagPercentages
                });
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
        public IActionResult GetMusicByCategory(string category, int count = 20)
        {
            try
            {
                if (string.IsNullOrEmpty(category))
                {
                    return Json(new { success = false, message = "Kategori belirtilmedi" });
                }

                // Veritabanından belirli kategorideki şarkıları getir
                var musicList = _context.MusicTags
                    .Where(mt => mt.Tag.ToLower() == category.ToLower())
                    .GroupBy(mt => mt.MusicId)
                    .Select(g => new {
                        MusicId = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(count)
                    .Join(_context.Musics,
                        tag => tag.MusicId,
                        music => music.musicid,
                        (tag, music) => new {
                            MusicId = music.musicid,
                            Title = music.title,
                            Artist = music.artist,
                            Filename = music.filename,
                            Category = category
                        })
                    .ToList();

                return Json(new { success = true, songs = musicList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Kategori müzikleri alınırken hata oluştu: " + ex.Message });
            }
        }

        // Şarkının ana kategorisini bulan metot
        [HttpGet]
        public IActionResult GetMainCategory(int musicId)
        {
            try
            {
                var mainCategory = _context.MusicTags
                    .Where(mt => mt.MusicId == musicId)
                    .GroupBy(mt => mt.Tag)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();

                if (mainCategory != null)
                {
                    return Json(new { success = true, category = mainCategory });
                }
                else
                {
                    return Json(new { success = false, message = "Bu şarkı için henüz bir kategori belirlenmemiş" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ana kategori belirlenirken hata oluştu: " + ex.Message });
            }
        }

        // Tüm kategorileri ve şarkı sayılarını getiren metot
        [HttpGet]
        public IActionResult GetAllCategories()
        {
            try
            {
                var categories = _context.MusicTags
                    .GroupBy(mt => mt.Tag)
                    .Select(g => new {
                        Tag = g.Key,
                        SongCount = g.Select(mt => mt.MusicId).Distinct().Count()
                    })
                    .OrderByDescending(c => c.SongCount)
                    .ToList();

                return Json(new { success = true, categories });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Kategoriler alınırken hata oluştu: " + ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetTagDistribution()
        {
            try
            {
                // Tüm etiketlerin dağılımını getir (grafikler için)
                var distribution = _context.MusicTags
                    .GroupBy(mt => mt.Tag)
                    .Select(g => new {
                        Tag = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                int totalTags = distribution.Sum(d => d.Count);

                var result = distribution.Select(d => new {
                    Tag = d.Tag,
                    Count = d.Count,
                    Percentage = Math.Round((double)d.Count / totalTags * 100, 1)
                }).ToList();

                return Json(new { success = true, distribution = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Etiket dağılımı alınırken hata oluştu: " + ex.Message });
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

        [HttpGet]
        public IActionResult GetMostTaggedSongs(int count = 10)
        {
            try
            {
                // En çok etiketlenen şarkıları getir
                var mostTagged = _context.MusicTags
                    .GroupBy(mt => mt.MusicId)
                    .Select(g => new {
                        MusicId = g.Key,
                        TagCount = g.Count()
                    })
                    .OrderByDescending(x => x.TagCount)
                    .Take(count)
                    .Join(_context.Musics, // Müzik bilgilerini al
                        stat => stat.MusicId,
                        music => music.musicid,
                        (stat, music) => new {
                            MusicId = music.musicid,
                            Title = music.title,
                            Artist = music.artist,
                            TotalTags = stat.TagCount
                        })
                    .ToList();

                return Json(new { success = true, songs = mostTagged });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "En çok etiketlenen şarkılar alınırken hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetRecommendedByTag(string tag, int count = 10)
        {
            try
            {
                // Belirli bir etikete göre şarkı önerileri
                var recommendations = _context.MusicTags
                    .Where(mt => mt.Tag.ToLower() == tag.ToLower())
                    .GroupBy(mt => mt.MusicId)
                    .Select(g => new {
                        MusicId = g.Key,
                        TagCount = g.Count() // Kaç kişi bu etiketi vermiş
                    })
                    .OrderByDescending(x => x.TagCount)
                    .Take(count)
                    .Join(_context.Musics,
                        stat => stat.MusicId,
                        music => music.musicid,
                        (stat, music) => new {
                            MusicId = music.musicid,
                            Title = music.title,
                            Artist = music.artist,
                            TagCount = stat.TagCount,
                            Filename = music.filename
                        })
                    .ToList();

                return Json(new { success = true, recommendations });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Öneriler alınırken hata oluştu: " + ex.Message });
            }
        }

        // Class for tag request (DashboardController'dan kopyalandı)
        public class TagRequest
        {
            public int MusicId { get; set; }
            public string Tag { get; set; }
        }
    }
}