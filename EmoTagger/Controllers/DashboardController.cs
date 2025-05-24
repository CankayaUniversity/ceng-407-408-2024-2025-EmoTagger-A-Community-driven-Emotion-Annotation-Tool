using EmoTagger.Data;
using EmoTagger.Models;
using EmoTagger.Services;
using EmoTagger.ViewComponents;
using EmoTagger.ViewModels;
using EmoTagger.Views.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.IO;
using System.Linq;

namespace EmoTagger.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<DashboardController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DashboardController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            EmailService emailService,
            ILogger<DashboardController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeMusic([FromBody] MusicAnalysisRequest request)
        {
            try
            {
                // Müzik dosyasının yolunu oluştur
                var musicPath = Path.Combine(_webHostEnvironment.WebRootPath, "music", request.Filename);

                if (!System.IO.File.Exists(musicPath))
                {
                    return Json(new { error = "Müzik dosyası bulunamadı." });
                }

                // Burada gerçek analiz işlemini yapabilirsin
                // Örnek olarak sahte bir analiz sonucu dönüyorum:
                var analysis = new MusicAnalysis
                {
                    EmotionAnalysis = "Neşeli, Enerjik",
                    MusicalFeatures = "Hızlı tempo, Enstrümantal ağırlıklı",
                    SuggestedTags = new[] { "happy", "energetic", "fast" }
                };

                // Gerçek analiz fonksiyonunu burada çağırabilirsin:
                // var analysis = await AnalyzeMusicWithMusicNN(musicPath);

                return Json(new
                {
                    emotionAnalysis = analysis.EmotionAnalysis,
                    musicalFeatures = analysis.MusicalFeatures,
                    suggestedTags = analysis.SuggestedTags
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müzik analizi sırasında hata oluştu");
                return Json(new { error = "Analiz sırasında bir hata oluştu." });
            }
        }

        private async Task<MusicAnalysis> AnalyzeMusicWithMusicNN(string musicPath)
        {
            try
            {
                // MusicNN modelini yükle
                var model = musicnn.load_model();
                
                // Müzik dosyasını yükle ve analiz et
                var features = musicnn.extract_features(musicPath, model);
                
                // Duygu analizi yap
                var emotions = AnalyzeEmotions(features);
                
                // Müzikal özellikleri analiz et
                var musicalFeatures = AnalyzeMusicalFeatures(features);
                
                // Etiketleri öner
                var suggestedTags = GenerateSuggestedTags(emotions, musicalFeatures);

                return new MusicAnalysis
                {
                    EmotionAnalysis = emotions,
                    MusicalFeatures = musicalFeatures,
                    SuggestedTags = suggestedTags
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MusicNN analizi sırasında hata oluştu");
                throw;
            }
        }

        private string AnalyzeEmotions(dynamic features)
        {
            // MusicNN'den gelen özellikleri kullanarak duygu analizi yap
            var emotions = new List<string>();
            
            // Örnek duygu analizi (gerçek implementasyon MusicNN'in çıktılarına göre değişecek)
            if (features.valence > 0.7) emotions.Add("neşeli");
            if (features.valence < 0.3) emotions.Add("hüzünlü");
            if (features.arousal > 0.7) emotions.Add("enerjik");
            if (features.arousal < 0.3) emotions.Add("sakin");
            
            return string.Join(", ", emotions);
        }

        private string AnalyzeMusicalFeatures(dynamic features)
        {
            // MusicNN'den gelen özellikleri kullanarak müzikal özellikleri analiz et
            var musicalFeatures = new List<string>();
            
            // Örnek müzikal özellik analizi (gerçek implementasyon MusicNN'in çıktılarına göre değişecek)
            if (features.tempo > 120) musicalFeatures.Add("hızlı tempo");
            if (features.tempo < 80) musicalFeatures.Add("yavaş tempo");
            if (features.instrumentalness > 0.7) musicalFeatures.Add("enstrümantal ağırlıklı");
            if (features.danceability > 0.7) musicalFeatures.Add("dans edilebilir");
            
            return string.Join(", ", musicalFeatures);
        }

        private string[] GenerateSuggestedTags(string emotions, string musicalFeatures)
        {
            // Duygu ve müzikal özelliklere göre etiketler öner
            var tags = new HashSet<string>();
            
            // Duygulardan etiketler oluştur
            foreach (var emotion in emotions.Split(','))
            {
                tags.Add(emotion.Trim());
            }
            
            // Müzikal özelliklerden etiketler oluştur
            foreach (var feature in musicalFeatures.Split(','))
            {
                tags.Add(feature.Trim());
            }
            
            return tags.ToArray();
        }

        public class MusicAnalysisRequest
        {
            public string Filename { get; set; }
            public string Title { get; set; }
            public string Artist { get; set; }
        }

        public class MusicAnalysis
        {
            public string EmotionAnalysis { get; set; }
            public string MusicalFeatures { get; set; }
            public string[] SuggestedTags { get; set; }
        }

        [HttpPost]
        [Route("Dashboard/PredictEmotion")]
        public async Task<IActionResult> PredictEmotion()
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null)
                return BadRequest("No file uploaded");

            using var httpClient = new HttpClient();
            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            content.Add(new StreamContent(fileStream), "file", file.FileName);

            var response = await httpClient.PostAsync("http://localhost:8000/predict", content);
            var responseString = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("FastAPI yanıtı: " + responseString);

            // FastAPI her zaman JSON dönerse:
            return Content(responseString, "application/json");
        }
    }
} 