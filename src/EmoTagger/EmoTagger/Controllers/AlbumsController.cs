using Microsoft.AspNetCore.Mvc;

public class AlbumsController : Controller
{
    public IActionResult Index()
    {
        return View(); // Views/Albums/Index.cshtml'i döndürür
    }
}
