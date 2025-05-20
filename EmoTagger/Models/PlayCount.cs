
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmoTagger.Models
{
    [Table("play_counts")]
    public class PlayCount
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("music_id")]
        public int MusicId { get; set; }

        [Column("count")]
        public int Count { get; set; } = 1;

        [Column("last_played")]
        public DateTime LastPlayed { get; set; } = DateTime.UtcNow;

        [ForeignKey("MusicId")]
        public virtual Music Music { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}