using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmoTagger.Models
{
    [Table("music")]
    public class Music
    {
        [Key]
        public int musicid { get; set; }

        public string title { get; set; }
        public string artist { get; set; }
        public string filename { get; set; }
        public DateTime createdat { get; set; } = DateTime.Now;

        public int? UserId { get; set; }  // 💖 Nullable yaparsan login olmayan da ekler
        public int playcount { get; set; } = 0; // Varsayılan olarak 0'dan başlar
    }
}
