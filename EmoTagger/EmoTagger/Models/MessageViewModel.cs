namespace EmoTagger.Models
{
    public class MessageViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public bool IsFromCurrentUser { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }
} 