using Microsoft.EntityFrameworkCore;

namespace EmoTagger.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Music> Music { get; set; } // ← Bu satır önemli
        public DbSet<Playlist> Playlists { get; set; }
    }
}
