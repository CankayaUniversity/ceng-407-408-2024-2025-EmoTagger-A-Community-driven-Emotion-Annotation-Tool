using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmoTagger.Models
{
    [Table("messages")]
   public class Message
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("sender_id")]
    public int SenderId { get; set; }

    [Column("receiver_id")]
    public int ReceiverId { get; set; }

    [Column("content")]
    public string Content { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
} 