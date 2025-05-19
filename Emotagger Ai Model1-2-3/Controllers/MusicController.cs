using EmoTagger.Data;
using EmoTagger.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmoTagger.Controllers
{
    public class MusicController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MusicController(ApplicationDbContext context)
        {
            _context = context;
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


    }
}