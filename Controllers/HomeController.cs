using System.Diagnostics;
using IQA_SOURCE.Models;
using Microsoft.AspNetCore.Mvc;
using YourApp.Data;
using System.Threading.Tasks;

namespace IQA_SOURCE.Controllers
{
    [Route("[controller]/[action]")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDbHelper _db;

        public HomeController(ILogger<HomeController> logger, IDbHelper db)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult Index(string? msg = null)
        {
            ViewBag.ConnectionMessage = msg;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestConnection()
        {
            var ok = await _db.TryOpenAsync();
            var msg = ok ? "Database connection successful." : "Database connection failed.";
            return RedirectToAction(nameof(Index), new { msg });
        }
    }
}
