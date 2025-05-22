using System.Collections.Generic;

namespace EmoTagger.ViewModels
{
    public class HomeViewModel
    {
        public List<EmotionCategoryViewModel> EmotionCategories { get; set; }
        public List<TrendingSongViewModel> TrendingSongs { get; set; }
        public List<MostTaggedSongViewModel> MostTaggedSongs { get; set; }
        public List<TagDistributionViewModel> TagDistribution { get; set; }
        public List<RecommendedSongViewModel> RecommendedSongs { get; set; }
    }

    public class EmotionCategoryViewModel
    {
        public string Tag { get; set; }
        public int SongCount { get; set; }
    }

    public class TrendingSongViewModel
    {
        public int MusicId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Filename { get; set; }
        // Diğer özellikler...
    }

    public class MostTaggedSongViewModel
    {
        public int MusicId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Filename { get; set; }
        public int TagCount { get; set; }
        // Diğer özellikler...
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
        // Diğer özellikler...
    }
}