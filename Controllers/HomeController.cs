using System.Diagnostics;
using IQA_SOURCE.Models;
using IQA_SOURCE.Models.Admin;
using Microsoft.AspNetCore.Mvc;
using YourApp.Data;
using IQA_SOURCE.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace IQA_SOURCE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDbHelper _db;
        private readonly IAdminRepository _adminRepository;

        public HomeController(ILogger<HomeController> logger, IDbHelper db, IAdminRepository adminRepository)
        {
            _logger = logger;
            _db = db;
            _adminRepository = adminRepository;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult SpeedTest()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetIntroContent()
        {
            try
            {
                var result = await _adminRepository.GetContentByCode("Intro", "Anonymous");

                if (result.OutputCode == 1 && result.Data != null && result.Data.Any())
                {
                    var content = result.Data.First();
                    return Json(new
                    {
                        success = true,
                        data = new
                        {
                            cmCode = content.CmCode,
                            cmContent = content.CmContent,
                            cmActive = content.CmActive
                        }
                    });
                }

                return Json(new
                {
                    success = false,
                    message = "Intro content not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading intro content");
                return Json(new
                {
                    success = false,
                    message = "Error loading content"
                });
            }
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
    }
}