using Microsoft.EntityFrameworkCore;
using EmoTagger.Models;

namespace EmoTagger.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<MusicTag> MusicTags { get; set; }
        public DbSet<Music> Musics { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistMusic> PlaylistMusics { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<RecentlyPlayed> RecentlyPlayed { get; set; }
        public DbSet<UserMusicPlay> UserMusicPlays { get; set; }
        public DbSet<PlayCount> PlayCounts { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<Comment> Comments { get; set; }



    }
}