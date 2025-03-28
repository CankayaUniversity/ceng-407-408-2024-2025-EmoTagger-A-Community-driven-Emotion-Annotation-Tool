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

        [HttpGet]
    public async Task<IActionResult> SearchMusic(string searchTerm)
    {
        var musics = await _context.Music
            .Where(m => m.title.Contains(searchTerm) || m.artist.Contains(searchTerm))
            .ToListAsync();

        return Json(musics);
    }

        public IActionResult Player()
        {
            var songs = _context.Music.ToList(); // tüm şarkılar
            return View(songs);
        }
    }

}
