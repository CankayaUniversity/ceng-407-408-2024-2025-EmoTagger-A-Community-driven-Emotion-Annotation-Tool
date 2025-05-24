using System;

namespace EmoTagger.Models
{
    public class FriendRequest
    {
        public int Id { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public bool IsAccepted { get; set; } = false;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
} 
 
 
 
 