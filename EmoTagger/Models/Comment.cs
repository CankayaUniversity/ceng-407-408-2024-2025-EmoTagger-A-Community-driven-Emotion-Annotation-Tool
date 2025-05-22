using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmoTagger.Models
{
    [Table("comments")]
    public class Comment
    {
        [Key]
        public int id { get; set; } // property adı küçük harfle

        [Required]
        public int music_id { get; set; } // property adı veritabanıyla aynı

        public int? user_id { get; set; }

        [Required]
        [StringLength(500)]
        public string comment_text { get; set; }

        public DateTime created_at { get; set; } = DateTime.UtcNow;

        [ForeignKey("music_id")]
        public virtual Music Music { get; set; }

        [ForeignKey("user_id")]
        public virtual User User { get; set; }
    }
}