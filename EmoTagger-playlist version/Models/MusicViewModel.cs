namespace EmoTagger.Models
{
    public class MusicViewModel
    {
        public int UserId { get; set; }
        public List<MusicItem> AvailableSongs { get; set; }
        public List<MusicItem> Playlist { get; set; }
        public string SearchTerm { get; set; }
    }

    public class MusicItem
    {
        public int MusicId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Time { get; set; }
    }
}
