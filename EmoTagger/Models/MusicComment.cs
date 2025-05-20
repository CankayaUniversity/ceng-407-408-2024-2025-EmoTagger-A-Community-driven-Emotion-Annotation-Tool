using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmoTagger.Models
{
    public class MusicComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MusicId { get; set; }

        public int? UserId { get; set; }  // Anonim yorumlar için nullable

        [StringLength(100)]
        public string UserName { get; set; } = "Anonim";

        [Required]
        [StringLength(500)]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("MusicId")]
        public virtual Music Music { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
} 