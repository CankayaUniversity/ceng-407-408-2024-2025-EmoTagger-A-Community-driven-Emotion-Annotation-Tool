using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EmoTagger.Controllers
{
    [ApiController]
    [Route("SpotifyAnalysis")]
    public class SpotifyAnalysisController : ControllerBase
    {
        private readonly string clientId = "025128e760fc4643bc24c28f5b4a5528";
        private readonly string clientSecret = "6a090b1e7ce74c30a74cb37742e7523a";

        [HttpGet("Analyze")] // /SpotifyAnalysis/Analyze?songName=...&artist=...
        public async Task<IActionResult> Analyze(string songName, string artist)
        {
            var debug = new List<string>();
            debug.Add($"Aranan şarkı: {songName} | Sanatçı: {artist}");

            var token = await GetAccessTokenAsync();
            debug.Add($"Token: {(token != null ? "Alındı" : "Alınamadı")}");
            if (string.IsNullOrEmpty(token))
                return Ok(new { success = false, message = "Spotify token alınamadı", debug });

            var trackId = await SearchSpotifyTrackId(songName, artist, token);
            debug.Add($"TrackId: {trackId}");
            if (string.IsNullOrEmpty(trackId))
                return Ok(new { success = false, message = "Şarkı bulunamadı", debug });

            var features = await GetAudioFeaturesAsync(trackId, token);
            debug.Add($"Audio features: {(features != null ? features.ToString() : "Yok")}");
            if (features == null)
                return Ok(new { success = false, message = "Audio features bulunamadı", debug });

            // Kategori tahmini öncesi null kontrolü
            if (features == null ||
                features["valence"] == null ||
                features["energy"] == null ||
                features["danceability"] == null ||
                features["tempo"] == null ||
                features["mode"] == null)
            {
                debug.Add("Audio features eksik veya null!");
                return Ok(new { success = false, message = "Audio features eksik veya null!", features, debug });
            }

            string category = PredictCategoryFromAudioFeatures(
                (double)features["valence"],
                (double)features["energy"],
                (double)features["danceability"],
                (double)features["tempo"],
                (int)features["mode"]
            );

            return Ok(new { success = true, category, features, debug });
        }

        private async Task<string> GetAccessTokenAsync()
        {
            using var client = new HttpClient();
            var auth = System.Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await client.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[SpotifyAnalysis] Token yanıtı: {json}");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[SpotifyAnalysis] Token alınamadı! Status: {response.StatusCode}");
                return null;
            }
            return JObject.Parse(json)["access_token"]?.ToString();
        }

        private async Task<string> SearchSpotifyTrackId(string songName, string artist, string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var searchQuery = $"{songName} {artist}".Trim();
            Console.WriteLine($"[SpotifyAnalysis] Arama sorgusu: {searchQuery}");
            var searchUrl = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(searchQuery)}&type=track&limit=1";
            Console.WriteLine($"[SpotifyAnalysis] Spotify search URL: {searchUrl}");
            var searchResponse = await client.GetAsync(searchUrl);
            var searchContent = await searchResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"[SpotifyAnalysis] Spotify search yanıtı: {searchContent}");
            if (!searchResponse.IsSuccessStatusCode || !searchContent.Contains("tracks"))
            {
                Console.WriteLine($"[SpotifyAnalysis] Şarkı bulunamadı veya Spotify'dan hata döndü.");
                return null;
            }
            var obj = JObject.Parse(searchContent);
            var trackId = obj["tracks"]?["items"]?.First?["id"]?.ToString();
            Console.WriteLine($"[SpotifyAnalysis] Bulunan trackId: {trackId}");
            return trackId;
        }

        private async Task<JObject> GetAudioFeaturesAsync(string trackId, string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var url = $"https://api.spotify.com/v1/audio-features/{trackId}";
            Console.WriteLine($"[SpotifyAnalysis] Audio features URL: {url}");
            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[SpotifyAnalysis] Audio features yanıtı: {json}");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[SpotifyAnalysis] Audio features alınamadı! Status: {response.StatusCode}");
                // Hata durumunda dönen yanıtı da debug için döndür
                return JObject.Parse($"{{\"error_response\":{json}}}");
            }
            return JObject.Parse(json);
        }

        private string PredictCategoryFromAudioFeatures(double valence, double energy, double danceability, double tempo, int mode)
        {
            if (valence > 0.7 && energy > 0.6)
                return "Happy";
            if (valence < 0.3 && energy < 0.5)
                return "Sad";
            if (energy > 0.7 && tempo > 120)
                return "Energetic";
            if (valence > 0.5 && danceability > 0.7)
                return "Romantic";
            if (energy < 0.4 && tempo < 90)
                return "Relaxing";
            if (valence < 0.5 && mode == 0)
                return "Nostalgic";
            return "Mixed";
        }
    }
} 