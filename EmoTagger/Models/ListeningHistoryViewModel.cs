using System.Collections.Generic;

namespace EmoTagger.Models
{
    public class ListeningHistoryViewModel
    {
        public List<ListeningHistoryItem> History { get; set; } = new List<ListeningHistoryItem>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class ListeningHistoryItem
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string PlayedAt { get; set; } // string for formatted date
    }
} 
 
 
 
 
 
 