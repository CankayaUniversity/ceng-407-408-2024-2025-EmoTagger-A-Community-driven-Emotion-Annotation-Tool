using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmoTagger.Data;
using EmoTagger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmoTagger.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PlaylistController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlaylistController> _logger;

        public PlaylistController(ApplicationDbContext context, ILogger<PlaylistController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Playlist/User/{userId}
        [HttpGet("User/{userId}")]
        public async Task<IActionResult> GetUserPlaylists(int userId)
        {
            try
            {
                var playlists = await _context.Playlists
                    .Where(p => p.UserId == userId)
                    .Select(p => new
                    {
                        p.PlaylistId,
                        p.Name,
                        p.Description,
                        p.CreatedAt,
                        SongCount = p.PlaylistMusics.Count
                    })
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                return Ok(playlists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user playlists");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: /Playlist/{playlistId}
        [HttpGet("{playlistId}")]
        public async Task<IActionResult> GetPlaylist(int playlistId)
        {
            try
            {
                var playlist = await _context.Playlists
                    .Include(p => p.PlaylistMusics)
                        .ThenInclude(pm => pm.Music)
                    .FirstOrDefaultAsync(p => p.PlaylistId == playlistId);

                if (playlist == null)
                    return NotFound("Playlist not found");

                var songs = playlist.PlaylistMusics
                    .OrderBy(pm => pm.Order)
                    .Select(pm => new
                    {
                        pm.Music.musicid,
                        pm.Music.title,
                        pm.Music.artist,
                        pm.Music.filename,
                        pm.AddedAt
                    });

                return Ok(new
                {
                    playlist.PlaylistId,
                    playlist.Name,
                    playlist.Description,
                    playlist.CreatedAt,
                    Songs = songs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting playlist");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: /Playlist/Create
        [HttpPost("Create")]
        public async Task<IActionResult> CreatePlaylist([FromBody] CreatePlaylistRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized("User not logged in");

                var playlist = new Playlist
                {
                    UserId = userId.Value,
                    Name = request.Name,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Playlists.Add(playlist);
                await _context.SaveChangesAsync();

                return Ok(new { playlist.PlaylistId, message = "Playlist created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating playlist");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: /Playlist/AddMusic
        [HttpPost("AddMusic")]
        public async Task<IActionResult> AddMusicToPlaylist([FromBody] AddMusicRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized("User not logged in");

                var playlist = await _context.Playlists
                    .FirstOrDefaultAsync(p => p.PlaylistId == request.PlaylistId && p.UserId == userId.Value);

                if (playlist == null)
                    return NotFound("Playlist not found");

                var music = await _context.Musics.FindAsync(request.MusicId);
                if (music == null)
                    return NotFound("Music not found");

                var maxOrder = await _context.PlaylistMusics
                    .Where(pm => pm.PlaylistId == request.PlaylistId)
                    .MaxAsync(pm => (int?)pm.Order) ?? 0;

                var playlistMusic = new PlaylistMusic
                {
                    PlaylistId = request.PlaylistId,
                    MusicId = request.MusicId,
                    AddedAt = DateTime.UtcNow,
                    Order = maxOrder + 1
                };

                _context.PlaylistMusics.Add(playlistMusic);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Music added to playlist" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding music to playlist");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: /Playlist/RemoveMusic
        [HttpDelete("RemoveMusic")]
        public async Task<IActionResult> RemoveMusicFromPlaylist([FromBody] RemoveMusicRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized("User not logged in");

                var playlist = await _context.Playlists
                    .FirstOrDefaultAsync(p => p.PlaylistId == request.PlaylistId && p.UserId == userId.Value);

                if (playlist == null)
                    return NotFound("Playlist not found");

                var playlistMusic = await _context.PlaylistMusics
                    .FirstOrDefaultAsync(pm => pm.PlaylistId == request.PlaylistId && pm.MusicId == request.MusicId);

                if (playlistMusic == null)
                    return NotFound("Music not found in playlist");

                _context.PlaylistMusics.Remove(playlistMusic);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Music removed from playlist" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing music from playlist");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: /Playlist/{playlistId}
        [HttpDelete("{playlistId}")]
        public async Task<IActionResult> DeletePlaylist(int playlistId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized("User not logged in");

                var playlist = await _context.Playlists
                    .FirstOrDefaultAsync(p => p.PlaylistId == playlistId && p.UserId == userId.Value);

                if (playlist == null)
                    return NotFound("Playlist not found");

                _context.Playlists.Remove(playlist);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Playlist deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting playlist");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: /Playlist/UpdateName
        [HttpPost("UpdateName")]
        public async Task<IActionResult> UpdatePlaylistName([FromBody] UpdatePlaylistNameRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized("User not logged in");

                var playlist = await _context.Playlists.FirstOrDefaultAsync(p => p.PlaylistId == request.PlaylistId && p.UserId == userId.Value);
                if (playlist == null)
                    return NotFound("Playlist not found");

                playlist.Name = request.NewName;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Playlist name updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating playlist name");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: /Playlist/UpdateDescription
        [HttpPost("UpdateDescription")]
        public async Task<IActionResult> UpdatePlaylistDescription([FromBody] UpdatePlaylistDescriptionRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return Unauthorized("User not logged in");

                var playlist = await _context.Playlists.FirstOrDefaultAsync(p => p.PlaylistId == request.PlaylistId && p.UserId == userId.Value);
                if (playlist == null)
                    return NotFound("Playlist not found");

                playlist.Description = request.NewDescription;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Playlist description updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating playlist description");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class CreatePlaylistRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class AddMusicRequest
    {
        public int PlaylistId { get; set; }
        public int MusicId { get; set; }
    }

    public class RemoveMusicRequest
    {
        public int PlaylistId { get; set; }
        public int MusicId { get; set; }
    }

    public class UpdatePlaylistNameRequest
    {
        public int PlaylistId { get; set; }
        public string NewName { get; set; }
    }

    public class UpdatePlaylistDescriptionRequest
    {
        public int PlaylistId { get; set; }
        public string NewDescription { get; set; }
    }
}
