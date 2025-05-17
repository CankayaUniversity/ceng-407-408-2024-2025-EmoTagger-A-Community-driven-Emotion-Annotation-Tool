using System.Collections.Generic;

namespace EmoTagger.Models
{
    public class AnalysisResult
    {
        public string Dominant { get; set; }
        public double Confidence { get; set; }
        public Dictionary<string, double> Distribution { get; set; }
    }
} 