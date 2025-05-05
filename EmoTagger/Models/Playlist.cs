using EmoTagger.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

[Table("playlists")]
public class Playlist
{
    [Key]
    [Column("playlist_id")]
    public int PlaylistId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }

    [Column("musicid")]
    public int MusicId { get; set; }

    [ForeignKey("MusicId")]
    public Music Music { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.Now;
}
