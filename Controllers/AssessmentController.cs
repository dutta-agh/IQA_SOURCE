using System.Diagnostics;
using IQA_SOURCE.Models;
using IQA_SOURCE.Models.Admin;
using Microsoft.AspNetCore.Mvc;
using YourApp.Data;
using IQA_SOURCE.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using IQA_SOURCE.Services;

namespace IQA_SOURCE.Controllers
{
    public class AssessmentController : Controller
    {
        private readonly ILogger<AssessmentController> _logger;
        private readonly IDbHelper _db;
        private readonly IAdminRepository _adminRepository;
        private readonly IAssessmentTypeRepository _assessmentTypeRepository;
        private readonly ISessionService _sessionService;
        private readonly IConfiguration _configuration;

        public AssessmentController(
            ILogger<AssessmentController> logger, 
            IDbHelper db, 
            IAdminRepository adminRepository, 
            IAssessmentTypeRepository assessmentTypeRepository,
            ISessionService sessionService,
            IConfiguration configuration)
        {
            _logger = logger;
            _db = db;
            _adminRepository = adminRepository;
            _assessmentTypeRepository = assessmentTypeRepository;
            _sessionService = sessionService;
            _configuration = configuration;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string assessmentType)
        {
            // Generate or retrieve unique session ID
            var sessionId = _sessionService.GetOrCreateSessionId();
            _logger.LogInformation($"Assessment started with SessionId: {sessionId}, AssessmentType: {assessmentType}");

            // Validate assessment type exists and is available
            if (!string.IsNullOrEmpty(assessmentType))
            {
                var result = await _assessmentTypeRepository.GetAssessmentTypeByCode(assessmentType, "Anonymous");
                if (result.OutputCode != 1 || !result.Data.Any())
                {
                    _logger.LogWarning($"Assessment type '{assessmentType}' not found.");
                    TempData["ErrorMessage"] = $"Assessment type '{assessmentType}' not found.";
                    return View("AssessmentNotFound");
                }

                var assessment = result.Data.First();
                
                // Check if assessment is available based on dates
                if (!assessment.IsAvailable)
                {
                    var now = DateTime.Now;
                    if (assessment.AtmActive != "Y")
                    {
                        TempData["ErrorMessage"] = $"The '{assessment.AtmName}' assessment is currently inactive.";
                    }
                    else if (assessment.AtmStartDate.HasValue && now < assessment.AtmStartDate.Value)
                    {
                        TempData["ErrorMessage"] = $"The '{assessment.AtmName}' assessment will be available from {assessment.AtmStartDate.Value:MMMM dd, yyyy HH:mm}.";
                    }
                    else if (assessment.AtmEndDate.HasValue && now > assessment.AtmEndDate.Value)
                    {
                        TempData["ErrorMessage"] = $"The '{assessment.AtmName}' assessment ended on {assessment.AtmEndDate.Value:MMMM dd, yyyy HH:mm}.";
                    }
                    
                    _logger.LogWarning($"Assessment '{assessmentType}' is not available.");
                    return View("AssessmentNotAvailable");
                }

                // Store assessment type in session
                _sessionService.SetAssessmentType(assessmentType);
                ViewBag.AssessmentType = assessmentType;
                ViewBag.AssessmentName = assessment.AtmName;
                ViewBag.AssessmentDuration = assessment.AtmDurationMinutes;
            }
            else
            {
                // Try to get from session as fallback
                var sessionAssessmentType = _sessionService.GetAssessmentType();
                if (!string.IsNullOrEmpty(sessionAssessmentType))
                {
                    ViewBag.AssessmentType = sessionAssessmentType;
                    assessmentType = sessionAssessmentType;
                }
                else
                {
                    TempData["ErrorMessage"] = "No assessment type specified.";
                    return View("AssessmentNotFound");
                }
            }

            // Pass session ID to view
            ViewBag.SessionId = sessionId;
            return View();
        }

        [AllowAnonymous]
        public IActionResult SpeedTest(string assessmentType)
        {
            // Ensure session is active
            var sessionId = _sessionService.GetOrCreateSessionId();
            _logger.LogInformation($"Speed test accessed with SessionId: {sessionId}, AssessmentType: {assessmentType}");

            if (!string.IsNullOrEmpty(assessmentType))
            {
                _sessionService.SetAssessmentType(assessmentType);
                ViewBag.AssessmentType = assessmentType;
            }
            else
            {
                // Try to get from session
                assessmentType = _sessionService.GetAssessmentType();
                if (!string.IsNullOrEmpty(assessmentType))
                {
                    ViewBag.AssessmentType = assessmentType;
                }
            }

            ViewBag.SessionId = sessionId;
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetIntroContent(string assessmentType = null)
        {
            try
            {
                var sessionId = _sessionService.GetOrCreateSessionId();

                // If no assessment type provided, try to get from session
                if (string.IsNullOrEmpty(assessmentType))
                {
                    assessmentType = _sessionService.GetAssessmentType();
                }

                _logger.LogInformation($"Loading intro content for SessionId: {sessionId}, AssessmentType: {assessmentType}");

                // Get intro content for this specific assessment type
                var contentCode = string.IsNullOrEmpty(assessmentType) ? "Intro" : $"Intro_{assessmentType}";
                
                var result = await _adminRepository.GetContentByCode(contentCode, "Anonymous");

                // If specific content not found, try generic intro
                if (result.OutputCode != 1 || !result.Data.Any())
                {
                    result = await _adminRepository.GetContentByCode("Intro", "Anonymous");
                }

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
                            cmActive = content.CmActive,
                            assessmentType = assessmentType,
                            sessionId = sessionId
                        }
                    });
                }

                return Json(new
                {
                    success = false,
                    message = "Intro content not found",
                    sessionId = sessionId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading intro content");
                return Json(new
                {
                    success = false,
                    message = "Error loading content",
                    error = ex.Message
                });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetSessionInfo()
        {
            var sessionData = _sessionService.GetSessionData();
            return Json(new
            {
                success = true,
                data = sessionData
            });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetAssessmentSettings()
        {
            try
            {
                var minWidth = _configuration.GetValue<int>("AssessmentSettings:MinScreenWidth", 1024);
                var minHeight = _configuration.GetValue<int>("AssessmentSettings:MinScreenHeight", 768);
                var allowedDevices = _configuration.GetSection("AssessmentSettings:AllowedDeviceTypes").Get<string[]>() 
                    ?? new[] { "Desktop", "Laptop" };

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        minScreenWidth = minWidth,
                        minScreenHeight = minHeight,
                        allowedDeviceTypes = allowedDevices
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading assessment settings");
                return Json(new
                {
                    success = false,
                    message = "Error loading settings"
                });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}