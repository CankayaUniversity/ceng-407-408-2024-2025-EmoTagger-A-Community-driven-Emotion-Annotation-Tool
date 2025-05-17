using Microsoft.AspNetCore.Mvc;

public class ReleaseController : Controller
{
    public IActionResult Index()
    {
        return View(); // Views/Release/Index.cshtml'i döndürür
    }
}
