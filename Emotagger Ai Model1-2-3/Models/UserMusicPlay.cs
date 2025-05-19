using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EmoTagger.Models
{
    [Table("user_music_plays")]
    public class UserMusicPlay
    {
        [Key]
        public int id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("music_id")]
        public int MusicId { get; set; }

        [Column("play_count")]
        public int PlayCount { get; set; } = 0;

        [Column("last_played")]
        public DateTime LastPlayed { get; set; } = DateTime.UtcNow;

        // İlişkiler
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("MusicId")]
        public virtual Music Music { get; set; }
    }
}