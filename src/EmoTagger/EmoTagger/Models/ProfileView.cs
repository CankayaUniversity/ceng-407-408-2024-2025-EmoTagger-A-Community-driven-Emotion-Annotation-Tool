using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmoTagger.Models
{
    [Table("profile_views")]
    public class ProfileView
    {
        [Key]
        public int Id { get; set; }

        [Column("viewer_id")]
        public int ViewerId { get; set; }

        [Column("viewed_id")]
        public int ViewedId { get; set; }

        [Column("viewed_at")]
        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ViewerId")]
        public virtual User Viewer { get; set; }

        [ForeignKey("ViewedId")]
        public virtual User Viewed { get; set; }
    }
} 