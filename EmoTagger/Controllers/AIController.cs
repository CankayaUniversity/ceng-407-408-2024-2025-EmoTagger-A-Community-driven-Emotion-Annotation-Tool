using Microsoft.AspNetCore.Mvc;
using EmoTagger.Models;
using System.Collections.Generic;

namespace EmoTagger.Controllers
{
    [ApiController]
    [Route("AI")]
    public class AIController : ControllerBase
    {
        private readonly AIAnalysisService _aiService;
        public AIController()
        {
            _aiService = new AIAnalysisService();
        }

        [HttpGet("AnalyzeMusic")]
        public IActionResult AnalyzeMusic(int musicId)
        {
            var rhythmResult = _aiService.AnalyzeByRhythm(musicId);
            var lyricsResult = _aiService.AnalyzeByLyrics(musicId);
            var titleResult = _aiService.AnalyzeByTitle(musicId);
            var userTags = _aiService.GetUserTags(musicId);

            return Ok(new {
                success = true,
                aiResults = new {
                    byRhythm = rhythmResult,
                    byLyrics = lyricsResult,
                    byTitle = titleResult
                },
                userTags = userTags
            });
        }
    }

    public class AIAnalysisService
    {
        public AnalysisResult AnalyzeByRhythm(int musicId)
        {
            return new AnalysisResult
            {
                Dominant = "Energetic",
                Confidence = 0.72,
                Distribution = new Dictionary<string, double>
                {
                    { "Happy", 30 }, { "Sad", 10 }, { "Energetic", 50 }, { "Nostalgic", 10 }
                }
            };
        }
        public AnalysisResult AnalyzeByLyrics(int musicId)
        {
            return new AnalysisResult
            {
                Dominant = "Sad",
                Confidence = 0.65,
                Distribution = new Dictionary<string, double>
                {
                    { "Happy", 15 }, { "Sad", 60 }, { "Energetic", 10 }, { "Nostalgic", 15 }
                }
            };
        }
        public AnalysisResult AnalyzeByTitle(int musicId)
        {
            return new AnalysisResult
            {
                Dominant = "Happy",
                Confidence = 0.55,
                Distribution = new Dictionary<string, double>
                {
                    { "Happy", 55 }, { "Sad", 10 }, { "Energetic", 20 }, { "Nostalgic", 15 }
                }
            };
        }
        public Dictionary<string, int> GetUserTags(int musicId)
        {
            return new Dictionary<string, int>
            {
                { "Happy", 12 }, { "Sad", 5 }, { "Energetic", 7 }, { "Nostalgic", 3 }
            };
        }
    }
} 