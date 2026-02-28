using Microsoft.AspNetCore.Mvc;

namespace IQA_SOURCE.Controllers
{
    public class ConfigController : Controller
    {
        [HttpGet]
        public IActionResult GetConstants()
        {
            return Json(new
            {
                subApplicationPath = Constants.SubApplicationPathJS
            });
        }
    }
}