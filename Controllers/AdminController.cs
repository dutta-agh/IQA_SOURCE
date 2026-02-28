using Microsoft.AspNetCore.Mvc;
using IQA_SOURCE.Data;
using IQA_SOURCE.Models.Admin;

namespace IQA_SOURCE.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IAssessmentTypeRepository _assessmentTypeRepository;

        public AdminController(IAdminRepository adminRepository, IAssessmentTypeRepository assessmentTypeRepository)
        {
            _adminRepository = adminRepository;
            _assessmentTypeRepository = assessmentTypeRepository;
        }

        // Login Page
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid input" });
            }

            var result = await _adminRepository.AuthenticateUser(model.Username, model.Password);

            if (result.Success)
            {
                HttpContext.Session.SetString("UserId", result.UserId);
                HttpContext.Session.SetString("UserName", result.UserName);
            }

            return Json(new
            {
                success = result.Success,
                message = result.Message
            });
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Dashboard
        [HttpGet]
        public IActionResult Dashboard()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // Content Master
        [HttpGet]
        public IActionResult ContentMaster()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllContents()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _adminRepository.GetAllContents(userId);
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg,
                data = result.Data
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetContentByCode(string code)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _adminRepository.GetContentByCode(code, userId);
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg,
                data = result.Data.FirstOrDefault()
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveContent([FromBody] ContentMaster model)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            ContentMasterResponse result;
            
            // Check if it's an update or insert
            var existing = await _adminRepository.GetContentByCode(model.CmCode, userId);
            
            if (existing.Data.Any())
            {
                result = await _adminRepository.UpdateContent(model, userId);
            }
            else
            {
                result = await _adminRepository.InsertContent(model, userId);
            }

            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg
            });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteContent([FromBody] string code)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _adminRepository.DeleteContent(code, userId);
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg
            });
        }

        // Assessment Type Master
        [HttpGet]
        public IActionResult AssessmentTypeMaster()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAssessmentTypes()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _assessmentTypeRepository.GetAllAssessmentTypes(userId);
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg,
                data = result.Data
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAssessmentTypeByCode(string code)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _assessmentTypeRepository.GetAssessmentTypeByCode(code, userId);
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg,
                data = result.Data.FirstOrDefault()
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveAssessmentType([FromBody] AssessmentTypeMaster model)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            AssessmentTypeMasterResponse result;
            
            // Check if it's an update or insert
            var existing = await _assessmentTypeRepository.GetAssessmentTypeByCode(model.AtmCode, userId);
            
            if (existing.Data.Any())
            {
                result = await _assessmentTypeRepository.UpdateAssessmentType(model, userId);
            }
            else
            {
                result = await _assessmentTypeRepository.InsertAssessmentType(model, userId);
            }

            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg
            });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAssessmentType([FromBody] string code)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _assessmentTypeRepository.DeleteAssessmentType(code, userId);
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg
            });
        }
    }
}