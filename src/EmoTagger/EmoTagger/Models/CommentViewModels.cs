using System;

namespace EmoTagger.Models
{
    public class CommentViewModel
    {
        public int Id { get; set; }
        public string Comment { get; set; }
        public string UserName { get; set; }
        public int? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsCurrentUser { get; set; }
    }

    public class AddCommentModel
    {
        public int MusicId { get; set; }
        public string Comment { get; set; }
        public bool IsAnonymous { get; set; }
    }
}