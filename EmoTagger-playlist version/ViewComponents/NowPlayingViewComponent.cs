using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmoTagger.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmoTagger.ViewComponents
{
    public class NowPlayingViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public NowPlayingViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Get the current/latest music track
            var latestMusic = await _context.Music
                .OrderByDescending(m => m.createdat)
                .FirstOrDefaultAsync();

            // Get all music tracks ordered by creation date
            var allTracks = await _context.Music
                .OrderByDescending(m => m.createdat)
                .ToListAsync();

            // Create a view model that includes both the current track and all tracks
            var viewModel = new NowPlayingViewModel
            {
                CurrentTrack = latestMusic,
                AllTracks = allTracks
            };

            return View(viewModel);
        }
    }

    // View model to hold both current track and playlist data
    public class NowPlayingViewModel
    {
        public Music CurrentTrack { get; set; }
        public List<Music> AllTracks { get; set; }
    }
}