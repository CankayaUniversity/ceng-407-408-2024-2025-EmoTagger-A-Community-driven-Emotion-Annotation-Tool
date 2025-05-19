using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;

public class AdminController : Controller
{
    public IActionResult Index()
    {
        // Run all 3 model scripts
        string model1Output = RunPythonScript("Scripts/model1.py");
        string model2Output = RunPythonScript("Scripts/model2.py,train_model2.py,audio_feature_extractor.py");
        string model3Output = RunPythonScript("Scripts/model3.py,model3_lyrics.py");

        // Define paths for HTML results
        string model1HtmlPath = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "predicted_emotions.html");
        string model2HtmlPath = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "model2_predictions.html");
        string model3HtmlPath = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "model3_lyrics_emotions.html");

        // Set ViewBags for each model's HTML
        ViewBag.EmotionHtml1 = System.IO.File.Exists(model1HtmlPath)
            ? System.IO.File.ReadAllText(model1HtmlPath)
            : $"<p style='color:red;'>Model 1 HTML not found!</p><pre>{model1Output}</pre>";

        ViewBag.EmotionHtml2 = System.IO.File.Exists(model2HtmlPath)
            ? System.IO.File.ReadAllText(model2HtmlPath)
            : $"<p style='color:red;'>Model 2 HTML not found!</p><pre>{model2Output}</pre>";

        ViewBag.EmotionHtml3 = System.IO.File.Exists(model3HtmlPath)
            ? System.IO.File.ReadAllText(model3HtmlPath)
            : $"<p style='color:red;'>Model 3 HTML not found!</p><pre>{model3Output}</pre>";

        return View("~/Views/Dashboard/AdminPanel.cshtml");
    }

    private string RunPythonScript(string scriptPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = scriptPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            var process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return string.IsNullOrWhiteSpace(error) ? output : $"Python Error:\n{error}";
        }
        catch (Exception ex)
        {
            return $"Script failed: {ex.Message}";
        }
    }
}
