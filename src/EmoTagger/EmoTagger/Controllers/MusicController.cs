using EmoTagger.Models;
using Microsoft.AspNetCore.Mvc;

namespace EmoTagger.Controllers
{
    public class MusicController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MusicController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Player()
        {
            var songs = _context.Music.ToList(); // tüm şarkılar
            return View(songs);
        }
    }

}
