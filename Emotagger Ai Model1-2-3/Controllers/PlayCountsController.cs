using Microsoft.AspNetCore.Mvc;
using EmoTagger.Models;
using EmoTagger.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace EmoTagger.Controllers
{
    public class PlayCountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlayCountsController> _logger;

        public PlayCountsController(ApplicationDbContext context, ILogger<PlayCountsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult CheckLogin()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            return Json(new { isLoggedIn = userId != null });
        }

        [HttpGet]
        public IActionResult GetCounts(int musicId)
        {
            try
            {
                if (musicId <= 0)
                {
                    _logger.LogWarning($"Geçersiz müzik ID: {musicId}");
                    return Json(new { success = false, message = "Geçersiz müzik ID" });
                }

                // Tüm dinlenme sayısını hesapla
                var totalPlayCount = _context.PlayCounts
                    .Where(p => p.MusicId == musicId)
                    .Sum(p => p.Count);

                // Kullanıcının dinleme sayısını al
                int userPlayCount = 0;
                var userId = HttpContext.Session.GetInt32("UserId");

                if (userId != null)
                {
                    userPlayCount = _context.PlayCounts
                        .Where(p => p.MusicId == musicId && p.UserId == userId)
                        .Sum(p => p.Count);
                }

                _logger.LogInformation($"GetCounts - Müzik ID: {musicId}, Toplam: {totalPlayCount}, Kullanıcı: {userPlayCount}");

                return Json(new
                {
                    success = true,
                    totalPlayCount,
                    userPlayCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetCounts - Hata - Müzik ID: {musicId}");
                return Json(new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateCounts(int musicId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");

                _logger.LogInformation($"⭐ UpdateCounts çağrıldı - musicId: {musicId}, userId: {userId}");

                try
                {
                    // Musics tablosunu güncelle
                    var music = _context.Musics.FirstOrDefault(m => m.musicid == musicId);
                    if (music != null)
                    {
                        music.playcount += 1;
                        _context.SaveChanges();
                        _logger.LogInformation($"🎵 Müzik playcount: {music.playcount}");
                    }
                    else
                    {
                        _logger.LogWarning($"❌ Müzik bulunamadı: MusicId={musicId}");
                        return Json(new { success = false, message = "Müzik bulunamadı" });
                    }

                    // Kullanıcı giriş yapmışsa PlayCounts tablosunu güncelle
                    int userPlayCount = 0;
                    if (userId.HasValue)
                    {
                        var existing = _context.PlayCounts.FirstOrDefault(p => p.MusicId == musicId && p.UserId == userId.Value);

                        if (existing != null)
                        {
                            existing.Count += 1;
                            existing.LastPlayed = DateTime.UtcNow;
                            userPlayCount = existing.Count;
                        }
                        else
                        {
                            var newEntry = new PlayCount
                            {
                                MusicId = musicId,
                                UserId = userId.Value,
                                Count = 1,
                                LastPlayed = DateTime.UtcNow
                            };
                            _context.PlayCounts.Add(newEntry);
                            userPlayCount = 1;
                        }

                        _context.SaveChanges();
                    }

                    // Toplam sayıyı direkt müzik tablosundan al
                    int totalPlayCount = music.playcount;

                    return Json(new
                    {
                        success = true,
                        totalPlayCount,
                        userPlayCount
                    });
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, $"💥 Veritabanı hatası: {dbEx.Message}");
                    return Json(new { success = false, message = dbEx.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"🔥 Genel hata: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}