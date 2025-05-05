
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;

namespace EmoTagger.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PlaylistController : ControllerBase
    {
        private readonly IConfiguration _config;

        public PlaylistController(IConfiguration config)
        {
            _config = config;
        }

        private string GetConnectionString()
        {
            return _config.GetConnectionString("DefaultConnection");
        }

        // Model class for request
        public class PlaylistRequest
        {
            public int UserId { get; set; }
            public int MusicId { get; set; }
        }

        // Model class for response
        public class MusicModel
        {
            public int MusicId { get; set; }
            public string Title { get; set; }
            public string Artist { get; set; }
            public string Album { get; set; }
            public string Time { get; set; }
        }

        // POST: /Playlist/Add
        [HttpPost("Add")]
        public IActionResult AddToPlaylist([FromBody] PlaylistRequest request)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            var cmd = new NpgsqlCommand("INSERT INTO playlists (user_id, music_id, added_at) VALUES (@uid, @mid, NOW())", conn);
            cmd.Parameters.AddWithValue("uid", request.UserId);
            cmd.Parameters.AddWithValue("mid", request.MusicId);
            cmd.ExecuteNonQuery();

            return Ok(new { message = "Music added to playlist." });
        }

        // DELETE: /Playlist/Remove
        [HttpDelete("Remove")]
        public IActionResult RemoveFromPlaylist([FromBody] PlaylistRequest request)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            var cmd = new NpgsqlCommand("DELETE FROM playlists WHERE user_id = @uid AND music_id = @mid", conn);
            cmd.Parameters.AddWithValue("uid", request.UserId);
            cmd.Parameters.AddWithValue("mid", request.MusicId);
            cmd.ExecuteNonQuery();

            return Ok(new { message = "Music removed from playlist." });
        }

        // GET: /Playlist/User/1
        [HttpGet("User/{userId}")]
        public IActionResult GetPlaylistByUser(int userId)
        {
            List<MusicModel> playlist = new();

            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            var cmd = new NpgsqlCommand(@"
                SELECT m.musicid, m.title, m.artist, m.filename, m.createdat
                FROM playlists p
                JOIN music m ON p.music_id = m.musicid
                WHERE p.user_id = @uid
                ORDER BY p.added_at DESC", conn);
            cmd.Parameters.AddWithValue("uid", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                playlist.Add(new MusicModel
                {
                    MusicId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Artist = reader.GetString(2),
                    Album = reader.GetString(3),
                    Time = reader.GetDateTime(4).ToShortTimeString()
                });
            }

            return Ok(playlist);
        }
    }
}
