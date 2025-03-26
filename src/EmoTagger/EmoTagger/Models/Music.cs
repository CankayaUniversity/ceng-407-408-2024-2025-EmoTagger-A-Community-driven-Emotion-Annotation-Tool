using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmoTagger.Models
{
    [Table("music")]
    public class Music
    {
        
        public int musicid { get; set; }
        public string title { get; set; }
        public string artist { get; set; }
        public string filename { get; set; }
        public DateTime createdat { get; set; } = DateTime.Now;
    }

}
