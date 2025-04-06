using Microsoft.EntityFrameworkCore;
using EmoTagger.Models;

namespace EmoTagger.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<MusicTag> MusicTags { get; set; }
        public DbSet<Music> Musics { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<RecentlyPlayed> RecentlyPlayed { get; set; }
    }
}