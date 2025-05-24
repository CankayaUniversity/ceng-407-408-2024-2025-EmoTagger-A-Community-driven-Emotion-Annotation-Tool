using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmoTagger.Models
{
    [Table("friends")]
    public class Friend
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("friend_id")]
        public int FriendId { get; set; }

        [Column("status")]
        public FriendStatus Status { get; set; } = FriendStatus.Pending;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum FriendStatus
    {
        Pending,
        Accepted,
        Rejected,
        Blocked
    }
}