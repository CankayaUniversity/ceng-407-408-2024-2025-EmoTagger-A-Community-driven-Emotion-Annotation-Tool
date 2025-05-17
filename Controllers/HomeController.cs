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
            // Kategoriler
            var emotionTags = new[] { "Sad", "Happy", "Nostalgic", "Energetic", "Relaxing", "Romantic" };
            var categories = emotionTags.Select(tag => new EmoTagger.ViewModels.EmotionCategoryViewModel
            {
                Tag = tag,
                SongCount = _context.MusicTags.Where(mt => mt.Tag == tag).Select(mt => mt.MusicId).Distinct().Count(),
                PopularityScore = 0 // Gerekirse doldur
            }).ToList();

            var viewModel = new HomeViewModel
            {
                // Trend �ark�lar (en �ok dinlenen)
                TrendingSongs = _context.Musics
                    .OrderByDescending(m => m.playcount)
                    .Take(6)
                    .Select(m => new TrendingSongViewModel
                    {
                        MusicId = m.musicid,
                        Title = m.title,
                        Artist = m.artist,
                        Filename = m.filename,
                        MostCommonTag = "-", // Tag �zelli�i modelde yoksa "-" koy
                        PlayCount = m.playcount
                    }).ToList(),

                // �nerilenler (en yeni eklenen)
                RecommendedSongs = _context.Musics
                    .OrderByDescending(m => m.createdat)
                    .Take(4)
                    .Select(m => new RecommendedSongViewModel
                    {
                        MusicId = m.musicid,
                        Title = m.title,
                        Artist = m.artist,
                        Filename = m.filename,
                        Tag = "-" // Hen�z MostCommonTag gibi bir alan yoksa
                    }).ToList(),

                // Etiketli �ark�lar yoksa bo� liste d�n
                MostTaggedSongs = new List<MostTaggedSongViewModel>(),

                // Etiket da��l�m� bo� (JS ile g�ncelleniyor)
                TagDistribution = new List<TagDistributionViewModel>(),

                // Kategoriler
                EmotionCategories = categories
            };

            return View(viewModel);
        }
    }
}
