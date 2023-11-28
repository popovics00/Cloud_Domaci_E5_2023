using Microsoft.AspNetCore.Mvc;

namespace Web1.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
