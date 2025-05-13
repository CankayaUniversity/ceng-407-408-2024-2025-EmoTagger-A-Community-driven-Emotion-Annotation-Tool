using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EmoTagger.Models
{
    public class MusicTag
    {
        [Key]
        public int Id { get; set; }

        public int MusicId { get; set; }

        public int UserId { get; set; }

        [StringLength(20)]
        public string Tag { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("MusicId")]
        public virtual Music Music { get; set; } // ✅ Bunu yorumdan çıkar ve aktif et

    }

}
