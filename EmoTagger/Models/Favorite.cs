using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EmoTagger.Models
{
    [Table("Favorites")]
    public class Favorite
    {
        [Key]
        [Column("f.id")]
        public int id { get; set; }

        [Column("user_id")]
        public int user_id { get; set; }

        [Column("music_id")]
        public int music_id { get; set; }

        [Column("added_at")]
        public DateTime added_at { get; set; }
    }
}
