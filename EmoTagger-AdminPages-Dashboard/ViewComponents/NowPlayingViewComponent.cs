using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmoTagger.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmoTagger.Data;

namespace EmoTagger.ViewComponents
{
    public class NowPlayingViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public NowPlayingViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int? musicId = null)
        {
            // Tüm müzik parçalarını musicid'ye göre sırala
            var allTracks = await _context.Musics
                .OrderBy(m => m.musicid)
                .ToListAsync();

            // Eğer belirli bir musicId verilmişse o parçayı, verilmemişse son eklenen parçayı kullan
            Music currentTrack = null;

            if (musicId.HasValue)
            {
                currentTrack = allTracks.FirstOrDefault(t => t.musicid == musicId.Value);
            }

            // Eğer musicId bulunamadıysa veya verilmediyse en son ekleneni kullan
            if (currentTrack == null)
            {
                currentTrack = await _context.Musics
                    .OrderByDescending(m => m.createdat)
                    .FirstOrDefaultAsync();
            }

            // Eğer veritabanı boşsa null kontrolü
            if (currentTrack == null && allTracks.Any())
            {
                currentTrack = allTracks.First();
            }

            // View model oluştur
            var viewModel = new NowPlayingViewModel
            {
                AllTracks = allTracks,
                CurrentTrack = currentTrack
            };

            return View(viewModel);
        }
    }

    // View model to hold both current track and playlist data
   public class NowPlayingViewModel
{
    public List<Music> AllTracks { get; set; } // Track yerine Music

    public Music CurrentTrack { get; set; } // Track yerine Music

    }
}