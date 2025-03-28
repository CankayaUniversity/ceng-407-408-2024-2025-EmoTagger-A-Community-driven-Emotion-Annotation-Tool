using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("playlists")] 
public class Playlist
{
    [Key]
    public int playlist_id { get; set; }
    public int user_id { get; set; }
    public int music_id { get; set; }
    public DateTime added_at { get; set; }
}
