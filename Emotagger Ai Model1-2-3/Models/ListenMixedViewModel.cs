using System.Collections.Generic;

namespace EmoTagger.Models
{
    public class ListenMixedViewModel
    {
        public List<Music> Tracks { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public Music CurrentTrack { get; set; }  // Bu da ekli olmalı
    }
}
