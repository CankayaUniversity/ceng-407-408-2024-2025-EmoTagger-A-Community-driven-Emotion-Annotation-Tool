using Microsoft.AspNetCore.Mvc;
using EmoTagger.Data;
using EmoTagger.ViewModels;
using System.Linq;

namespace EmoTagger.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var viewModel = new HomeViewModel
            {
                // Trend þarkýlar (en çok dinlenen)
                TrendingSongs = _context.Musics
                    .OrderByDescending(m => m.playcount)
                    .Take(6)
                    .Select(m => new TrendingSongViewModel
                    {
                        MusicId = m.musicid,
                        Title = m.title,
                        Artist = m.artist,
                        Filename = m.filename,
                        MostCommonTag = "-", // Tag özelliði modelde yoksa "-" koy
                        PlayCount = m.playcount
                    }).ToList(),

                // Önerilenler (en yeni eklenen)
                RecommendedSongs = _context.Musics
                    .OrderByDescending(m => m.createdat)
                    .Take(4)
                    .Select(m => new RecommendedSongViewModel
                    {
                        MusicId = m.musicid,
                        Title = m.title,
                        Artist = m.artist,
                        Filename = m.filename,
                        Tag = "-" // Henüz MostCommonTag gibi bir alan yoksa
                    }).ToList(),

                // Etiketli þarkýlar yoksa boþ liste dön
                MostTaggedSongs = new List<MostTaggedSongViewModel>(),

                // Etiket daðýlýmý boþ (JS ile güncelleniyor)
                TagDistribution = new List<TagDistributionViewModel>(),

                // Kategoriler boþ (JS ile güncelleniyor)
                EmotionCategories = new List<EmotionCategoryViewModel>()
            };

            return View(viewModel);
        }
    }
}
