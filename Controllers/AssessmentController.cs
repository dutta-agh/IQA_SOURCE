using System.Diagnostics;
using IQA_SOURCE.Models;
using IQA_SOURCE.Models.Admin;
using Microsoft.AspNetCore.Mvc;
using YourApp.Data;
using IQA_SOURCE.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using IQA_SOURCE.Services;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace IQA_SOURCE.Controllers
{
    public class AssessmentController : Controller
    {
        private readonly IDbHelper _db;
        private readonly IAdminRepository _adminRepository;
        private readonly IAssessmentTypeRepository _assessmentTypeRepository;
        private readonly ISessionService _sessionService;   
        private readonly IConfiguration _configuration;
        private readonly ISpeedTestRepository _speedTestRepository;
        private readonly ILogger<AssessmentController> _logger;
        private readonly IUserResponseRepository _responseRepository;
        private readonly IImageQualityRepository _imageQualityRepository;

        public AssessmentController(
            ILogger<AssessmentController> logger, 
            IDbHelper db, 
            IAdminRepository adminRepository, 
            IAssessmentTypeRepository assessmentTypeRepository,
            ISessionService sessionService,
            IConfiguration configuration,
            ISpeedTestRepository speedTestRepository,
            IUserResponseRepository responseRepository,
            IImageQualityRepository imageQualityRepository)
        {
            _logger = logger;
            _db = db;
            _adminRepository = adminRepository;
            _assessmentTypeRepository = assessmentTypeRepository;
            _sessionService = sessionService;
            _configuration = configuration;
            _speedTestRepository = speedTestRepository;
            _responseRepository = responseRepository;
            _imageQualityRepository = imageQualityRepository;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string assessmentType)
        {
            // Generate or retrieve unique session ID
            var sessionId = _sessionService.GetOrCreateSessionId();
            _logger.LogInformation($"Assessment started with SessionId: {sessionId}, AssessmentType: {assessmentType}");

            // Set flag indicating user accessed index page
            _sessionService.SetSessionData("HasIndexAccess", true);

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

            // Check if referrer is from the index page or if session flag is set
            var referrer = Request.Headers["Referer"].ToString();
            var hasIndexAccessObj = _sessionService.GetSessionData<object>("HasIndexAccess") as bool?;
            bool hasIndexAccess = hasIndexAccessObj is bool b && b;
            
            if (!hasIndexAccess)
            {
                // Check if referrer is from index page
                if (string.IsNullOrEmpty(referrer) || 
                    (!referrer.Contains("/Assessment/" + assessmentType, StringComparison.OrdinalIgnoreCase) &&
                     !referrer.Contains("/" + assessmentType, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning($"Direct access attempt to SpeedTest blocked for SessionId: {sessionId}");
                    TempData["ErrorMessage"] = "Please start the assessment from the beginning.";
                    return RedirectToAction("Index", new { assessmentType });
                }
            }

            // Set flag for this session
            _sessionService.SetSessionData("HasIndexAccess", true);
            _sessionService.SetSessionData("SpeedTestAccess", true);

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

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SaveSpeedTestLog([FromBody] SpeedTestLogSaveRequest request)
        {
            try
            {
                // Log the incoming request for debugging
                _logger.LogInformation($"SaveSpeedTestLog called - SessionId: {request?.SessionId}, AssessmentCode: {request?.AssessmentCode}");
                
                if (request == null)
                {
                    _logger.LogError("Request is null");
                    return Json(new
                    {
                        success = false,
                        message = "Invalid request data"
                    });
                }
                
                var sessionId = _sessionService.GetOrCreateSessionId();
                var assessmentType = _sessionService.GetAssessmentType();
                
                request.SessionId = sessionId;
                request.AssessmentCode = assessmentType ?? request.AssessmentCode;
                
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var userAgent = Request.Headers["User-Agent"].ToString();
                var referrerUrl = Request.Headers["Referer"].ToString();
                
                _logger.LogInformation($"Calling repository - SessionId: {request.SessionId}, AssessmentCode: {request.AssessmentCode}");
                
                var result = await _speedTestRepository.SaveSpeedTestLog(request, ipAddress, userAgent, referrerUrl);
                
                _logger.LogInformation($"Repository result - OutputCode: {result.OutputCode}, OutputMsg: {result.OutputMsg}");
                
                return Json(new
                {
                    success = result.OutputCode == 1,
                    message = result.OutputMsg,
                    data = new
                    {
                        sessionId = request.SessionId,
                        assessmentCode = request.AssessmentCode,
                        timestamp = DateTime.Now
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving speed test log");
                return Json(new
                {
                    success = false,
                    message = "Error saving speed test log",
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Questions(string assessmentCode)
        {
            // Handle both route parameter names
            if (string.IsNullOrEmpty(assessmentCode))
            {
                // Try to get from route value with alternate name
                assessmentCode = RouteData.Values["assessmentType"]?.ToString();
            }

            if (string.IsNullOrEmpty(assessmentCode))
            {
                return BadRequest("Assessment code is required");
            }

            try
            {
                var sessionId = _sessionService.GetOrCreateSessionId();
                var viewModel = await _responseRepository.GetQuestionsForAssessment(assessmentCode, sessionId);

                if (viewModel == null)
                {
                    return NotFound($"Assessment '{assessmentCode}' not found or inactive");
                }

                ViewBag.SessionId = sessionId;
                ViewBag.AssessmentCode = assessmentCode;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading questions for assessment {AssessmentCode}", assessmentCode);
                return StatusCode(500, "Error loading questions");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitResponses([FromBody] QuestionSubmissionModel submission)
        {
            // Log validation errors
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new 
                    { 
                        Field = x.Key, 
                        Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToList() 
                    })
                    .ToList();
        
                _logger.LogError($"Model validation failed: {string.Join("; ", errors.Select(e => $"{e.Field}: {string.Join(", ", e.Errors)}"))}");
        
                return Json(new { 
                    success = false, 
                    message = "Invalid submission data", 
                    errors = errors,
                    modelState = errors.ToDictionary(e => e.Field, e => e.Errors)
                });
            }

            try
            {
                var sessionId = _sessionService.GetOrCreateSessionId();
                submission.SessionId = sessionId;

                _logger.LogInformation($"Submitting {submission.Answers?.Count ?? 0} answers for session {sessionId}, assessment {submission.AssessmentCode}");

                var result = await _responseRepository.SaveUserResponses(submission, sessionId);

                return Json(new
                {
                    success = result.OutputCode == 1,
                    message = result.OutputMsg,
                    data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting responses");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ImageAssessment(string assessmentCode)
        {
            try
            {
                var sessionId = _sessionService.GetOrCreateSessionId();
                
                // Check if assessment code matches session
                var sessionAssessmentCode = _sessionService.GetAssessmentType();
                if (string.IsNullOrEmpty(assessmentCode))
                {
                    assessmentCode = sessionAssessmentCode ?? string.Empty;
                }

                if (string.IsNullOrEmpty(assessmentCode))
                {
                    TempData["ErrorMessage"] = "No assessment code specified.";
                    return RedirectToAction("Index");
                }

                _logger.LogInformation($"Image assessment started - SessionId: {sessionId}, AssessmentCode: {assessmentCode}");

                // Get assessment details
                var assessmentTypeResult = await _assessmentTypeRepository.GetAssessmentTypeByCode(assessmentCode, "Anonymous");
                if (assessmentTypeResult.OutputCode != 1 || !assessmentTypeResult.Data.Any())
                {
                    TempData["ErrorMessage"] = $"Assessment '{assessmentCode}' not found.";
                    return RedirectToAction("Index");
                }

                var assessmentName = assessmentTypeResult.Data.First().AtmName;

                // Get next random image set
                var imageSetResult = await _imageQualityRepository.GetNextRandomRawImageSet(sessionId, assessmentCode, "Anonymous");
                
                if (imageSetResult.OutputCode == 0) // All completed
                {
                    ViewBag.AssessmentName = assessmentName;
                    return View("ImageAssessmentComplete");
                }

                if (imageSetResult.OutputCode != 1 || imageSetResult.Data == null)
                {
                    _logger.LogWarning($"No image sets found for assessment: {assessmentCode}");
                    TempData["ErrorMessage"] = "No image sets available for this assessment.";
                    return RedirectToAction("Index", new { assessmentType = assessmentCode });
                }

                // Get linked images for this raw image set
                var linkedImagesResult = await _imageQualityRepository.GetLinkedImagesByRawImageSetId(imageSetResult.Data.RisId, "Anonymous");
                
                if (linkedImagesResult.OutputCode != 1 || !linkedImagesResult.Data.Any())
                {
                    _logger.LogWarning($"No linked images found for RawImageSetId: {imageSetResult.Data.RisId}");
                    TempData["ErrorMessage"] = "No linked images available for this image set.";
                    return RedirectToAction("Index", new { assessmentType = assessmentCode });
                }

                // Get progress
                var progressResult = await _imageQualityRepository.GetAssessmentProgress(sessionId, assessmentCode, "Anonymous");

                var viewModel = new ImageAssessmentViewModel
                {
                    SessionId = sessionId,
                    AssessmentCode = assessmentCode,
                    AssessmentName = assessmentName,
                    RawImage = imageSetResult.Data,
                    LinkedImages = linkedImagesResult.Data,
                    CurrentSetNumber = progressResult.Data.CompletedSets + 1,
                    TotalSets = progressResult.Data.TotalSets,
                    IsCompleted = progressResult.Data.IsCompleted
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading image assessment");
                TempData["ErrorMessage"] = "An error occurred while loading the image assessment.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitImageQualityRating([FromBody] ImageQualitySubmission submission)
        {
            try
            {
                var sessionId = _sessionService.GetOrCreateSessionId();
                submission.SessionId = sessionId;

                // Validate submission
                if (submission.RawImageSetId <= 0 || submission.SelectedLinkedImageId <= 0)
                {
                    return Json(new { success = false, message = "Invalid image selection" });
                }

                if (submission.QualityRating < 1 || submission.QualityRating > 5)
                {
                    return Json(new { success = false, message = "Quality rating must be between 1 and 5" });
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var userAgent = Request.Headers["User-Agent"].ToString();

                _logger.LogInformation($"Submitting image quality rating - SessionId: {sessionId}, RawImageSetId: {submission.RawImageSetId}, LinkedImageId: {submission.SelectedLinkedImageId}, Rating: {submission.QualityRating}");

                var result = await _imageQualityRepository.SaveImageQualityRating(submission, ipAddress, userAgent, "Anonymous");

                if (result.OutputCode == 1)
                {
                    // Get progress after submission
                    var progressResult = await _imageQualityRepository.GetAssessmentProgress(sessionId, submission.AssessmentCode, "Anonymous");

                    return Json(new
                    {
                        success = true,
                        message = result.OutputMsg,
                        data = new
                        {
                            completedSets = progressResult.Data.CompletedSets,
                            totalSets = progressResult.Data.TotalSets,
                            isCompleted = progressResult.Data.IsCompleted
                        }
                    });
                }

                return Json(new { success = false, message = result.OutputMsg });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting image quality rating");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetImageAssessmentProgress()
        {
            try
            {
                var sessionId = _sessionService.GetOrCreateSessionId();
                var assessmentCode = _sessionService.GetAssessmentType();

                if (string.IsNullOrEmpty(assessmentCode))
                {
                    return Json(new { success = false, message = "No assessment code found in session" });
                }

                var result = await _imageQualityRepository.GetAssessmentProgress(sessionId, assessmentCode, "Anonymous");

                return Json(new
                {
                    success = true,
                    data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assessment progress");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetImageAssessmentIntro(string assessmentType = null)
        {
            try
            {
                var sessionId = _sessionService.GetOrCreateSessionId();

                // If no assessment type provided, try to get from session
                if (string.IsNullOrEmpty(assessmentType))
                {
                    assessmentType = _sessionService.GetAssessmentType();
                }

                _logger.LogInformation($"Loading image assessment intro for SessionId: {sessionId}, AssessmentType: {assessmentType}");

                // Get IQA intro content for this specific assessment type
                var contentCode = string.IsNullOrEmpty(assessmentType) ? "IQA_Intro" : $"IQA_Intro_{assessmentType}";
                
                var result = await _adminRepository.GetContentByCode(contentCode, "Anonymous");

                // If specific content not found, try generic IQA intro
                if (result.OutputCode != 1 || !result.Data.Any())
                {
                    result = await _adminRepository.GetContentByCode("IQA_Intro", "Anonymous");
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

                // Return default content if none found
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        cmCode = "IQA_Intro",
                        cmContent = "<h3>IQA (Image Quality Assessment)</h3><p>You will be shown a series of image sets. For each set, select the image you believe has the best quality and rate it.</p>",
                        cmActive = "Y",
                        assessmentType = assessmentType,
                        sessionId = sessionId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading image assessment intro content");
                return Json(new
                {
                    success = false,
                    message = "Error loading content",
                    error = ex.Message
                });
            }
        }
    }
}