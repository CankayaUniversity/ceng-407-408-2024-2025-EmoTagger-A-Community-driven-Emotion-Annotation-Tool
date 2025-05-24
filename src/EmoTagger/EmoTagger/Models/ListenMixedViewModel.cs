namespace EmoTagger.Models
{
    public class ListenMixedTrackViewModel
    {
        public int MusicId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Filename { get; set; }
        public int PlayCount { get; set; }
    }

    public class ListenMixedViewModel
    {
        public List<ListenMixedTrackViewModel> Tracks { get; set; } = new List<ListenMixedTrackViewModel>();
        public List<ListenMixedTrackViewModel> AllTracks { get; set; } = new List<ListenMixedTrackViewModel>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public Music CurrentTrack { get; set; }
    }
}