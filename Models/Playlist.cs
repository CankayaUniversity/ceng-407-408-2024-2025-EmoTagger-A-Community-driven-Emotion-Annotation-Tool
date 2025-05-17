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

    [Column("name")]
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Column("description")]
    [StringLength(500)]
    public string Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<PlaylistMusic> PlaylistMusics { get; set; }
}

[Table("playlist_musics")]
public class PlaylistMusic
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("playlist_id")]
    public int PlaylistId { get; set; }

    [ForeignKey("PlaylistId")]
    public Playlist Playlist { get; set; }

    [Column("music_id")]
    public int MusicId { get; set; }

    [ForeignKey("MusicId")]
    public Music Music { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [Column("order")]
    public int Order { get; set; }
}