using EmoTagger.Data;
using EmoTagger.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmoTagger.Controllers
{
    public class MusicController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MusicController> _logger;

        public MusicController(ApplicationDbContext context, ILogger<MusicController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 🎧 Tüm Şarkıları Getir ve Player Görünümüne Gönder
        public IActionResult Player()
        {
            var songs = _context.Musics.ToList(); // Tüm şarkılar
            return View(songs);
        }
        public IActionResult MusicList()
        {
            var musicList = _context.Musics.ToList(); // Music tablon
            return View(musicList); // View'e model olarak gönder
        }

        [HttpGet]
        [Route("Music/Search")]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new List<object>());

            var musics = await _context.Musics
                .Where(m => m.title.ToLower().Contains(query.ToLower()) || m.artist.ToLower().Contains(query.ToLower()))
                .Select(m => new {
                    musicId = m.musicid,
                    title = m.title,
                    artist = m.artist
                })
                .Take(30)
                .ToListAsync();

            return Json(musics);
        }
    }
}