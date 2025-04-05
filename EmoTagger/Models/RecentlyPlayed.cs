using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmoTagger.Models
{
    [Table("recently_played")]
    public class RecentlyPlayed
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("music_id")]
        public int MusicId { get; set; }

        [Column("played_at")]
        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
    }
}
