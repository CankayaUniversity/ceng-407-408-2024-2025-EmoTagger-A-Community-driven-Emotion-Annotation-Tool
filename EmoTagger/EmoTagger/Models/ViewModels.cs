namespace EmoTagger.ViewModels
{
    public class HomeViewModel
    {
        public List<EmotionCategoryViewModel> EmotionCategories { get; set; } = new List<EmotionCategoryViewModel>();
        public List<TrendingSongViewModel> TrendingSongs { get; set; } = new List<TrendingSongViewModel>();
        public List<MostTaggedSongViewModel> MostTaggedSongs { get; set; } = new List<MostTaggedSongViewModel>();
        public List<TagDistributionViewModel> TagDistribution { get; set; } = new List<TagDistributionViewModel>();
        public List<RecommendedSongViewModel> RecommendedSongs { get; set; } = new List<RecommendedSongViewModel>();
    }

    public class EmotionCategoryViewModel
    {
        public string Tag { get; set; }
        public int SongCount { get; set; }
        public int PopularityScore { get; set; }
    }

    public class TrendingSongViewModel
    {
        public int MusicId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Filename { get; set; }
        public string MostCommonTag { get; set; }
        public int PlayCount { get; set; }
    }

    public class MostTaggedSongViewModel
    {
        public int MusicId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Filename { get; set; }
        public int TagCount { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class TagDistributionViewModel
    {
        public string Tag { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class RecommendedSongViewModel
    {
        public int MusicId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Filename { get; set; }
        public string Tag { get; set; }
    }

    public class LeaderboardUserViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string ProfileImageUrl { get; set; }
        public int Count { get; set; } // Dinleme veya tag sayısı
    }

    public class LeaderboardViewModel
    {
        public List<LeaderboardUserViewModel> TopListeners { get; set; } = new();
        public List<LeaderboardUserViewModel> TopTaggers { get; set; } = new();
    }
}