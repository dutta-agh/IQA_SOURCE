using IQA_SOURCE.Data;
using IQA_SOURCE.Models;
using IQA_SOURCE.Models.Admin;
using IQA_SOURCE.Models.Colorblindness;
using IQA_SOURCE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Diagnostics;
using System.Threading.Tasks;
using YourApp.Data;

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
        private readonly ISystemCheckParamRepository _systemCheckParamRepository;
        private readonly IImageGroupRepository _imageGroupRepository;
        private readonly IImageRepository _imageRepository;
        private readonly IColorblindnessRepository _colorblindnessRepository;

        public AssessmentController(
            ILogger<AssessmentController> logger,
            IDbHelper db,
            IAdminRepository adminRepository,
            IAssessmentTypeRepository assessmentTypeRepository,
            ISessionService sessionService,
            IConfiguration configuration,
            ISpeedTestRepository speedTestRepository,
            IUserResponseRepository responseRepository,
            IImageQualityRepository imageQualityRepository,
            ISystemCheckParamRepository systemCheckParamRepository,
            IImageGroupRepository imageGroupRepository,
            IImageRepository imageRepository,
            IColorblindnessRepository colorblindnessRepository)
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
            _systemCheckParamRepository = systemCheckParamRepository;
            _imageGroupRepository = imageGroupRepository;
            _imageRepository = imageRepository;
            _colorblindnessRepository = colorblindnessRepository;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string assessmentType)
        {
            // Generate or retrieve unique session ID
            var sessionId = _sessionService.GetOrCreateSessionId();
            _logger.LogInformation($"Assessment started with SessionId: {sessionId}, AssessmentType: {assessmentType}");

            // Set flag indicating user accessed index page
            _sessionService.SetSessionData("HasIndexAccess", true);

            // ✅ CRITICAL: Initialize assessment type early, even if empty
            // This ensures it's available for subsequent requests
            if (string.IsNullOrEmpty(assessmentType))
            {
                assessmentType = _sessionService.GetAssessmentType();
            }

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

                // ✅ CRITICAL: Store assessment type in session IMMEDIATELY after validation
                // This ensures it persists across all subsequent requests
                _sessionService.SetAssessmentType(assessmentType);
                
                _logger.LogInformation($"Assessment type '{assessmentType}' set in session for SessionId: {sessionId}");

                ViewBag.AssessmentType = assessmentType;
                ViewBag.AssessmentName = assessment.AtmName;
                ViewBag.AssessmentDuration = assessment.AtmDurationMinutes;
            }
            else
            {
                // ✅ No assessment type provided - this is still a valid scenario
                // User might be accessing the index page without a specific assessment
                _logger.LogInformation($"Index page accessed without assessment type for SessionId: {sessionId}");
                TempData["ErrorMessage"] = "No assessment type specified.";
                return View("AssessmentNotFound");
            }

            // ✅ Pass both sessionId and assessmentCode to view so JavaScript can use them
            ViewBag.SessionId = sessionId;
            ViewBag.AssessmentCode = assessmentType;
            
            // ✅ Log what we're passing to the view
            _logger.LogInformation($"Index view rendered - SessionId: {sessionId}, AssessmentCode: {assessmentType}");
            
            return View();
        }

        [AllowAnonymous]
        public IActionResult SpeedTest(string assessmentType)
        {
            // Ensure session is active
            var sessionId = _sessionService.GetOrCreateSessionId();
            _logger.LogInformation($"Speed test accessed with SessionId: {sessionId}, AssessmentType: {assessmentType}");

            // No restriction - colorblindness test is optional
            // Users can skip colorblindness and go directly to speed test

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
                assessmentType = _sessionService.GetAssessmentType();
                if (!string.IsNullOrEmpty(assessmentType))
                {
                    ViewBag.AssessmentType = assessmentType;
                }
            }

            ViewBag.SessionId = sessionId;
            return View();
        }

        //[AllowAnonymous]
        //public IActionResult SpeedTest(string assessmentType)
        //{
        //    // Ensure session is active
        //    var sessionId = _sessionService.GetOrCreateSessionId();
        //    _logger.LogInformation($"Speed test accessed with SessionId: {sessionId}, AssessmentType: {assessmentType}");

        //    // Check if referrer is from the index page or if session flag is set
        //    var referrer = Request.Headers["Referer"].ToString();
        //    var hasIndexAccessObj = _sessionService.GetSessionData<object>("HasIndexAccess") as bool?;
        //    bool hasIndexAccess = hasIndexAccessObj is bool b && b;

        //    if (!hasIndexAccess)
        //    {
        //        // Check if referrer is from index page
        //        if (string.IsNullOrEmpty(referrer) || 
        //            (!referrer.Contains("/Assessment/" + assessmentType, StringComparison.OrdinalIgnoreCase) &&
        //             !referrer.Contains("/" + assessmentType, StringComparison.OrdinalIgnoreCase)))
        //        {
        //            _logger.LogWarning($"Direct access attempt to SpeedTest blocked for SessionId: {sessionId}");
        //            TempData["ErrorMessage"] = "Please start the assessment from the beginning.";
        //            return RedirectToAction("Index", new { assessmentType });
        //        }
        //    }

        //    // Set flag for this session
        //    _sessionService.SetSessionData("HasIndexAccess", true);
        //    _sessionService.SetSessionData("SpeedTestAccess", true);

        //    if (!string.IsNullOrEmpty(assessmentType))
        //    {
        //        _sessionService.SetAssessmentType(assessmentType);
        //        ViewBag.AssessmentType = assessmentType;
        //    }
        //    else
        //    {
        //        // Try to get from session
        //        assessmentType = _sessionService.GetAssessmentType();
        //        if (!string.IsNullOrEmpty(assessmentType))
        //        {
        //            ViewBag.AssessmentType = assessmentType;
        //        }
        //    }

        //    ViewBag.SessionId = sessionId;
        //    return View();
        //}

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
        public async Task<IActionResult> GetAssessmentSettings()
        {
            try
            {
                var settings = await _systemCheckParamRepository.GetResolvedSettings();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        minScreenWidth             = settings.MinScreenWidth,
                        minScreenHeight            = settings.MinScreenHeight,
                        allowedDeviceTypes         = settings.AllowedDevices,
                        minDownloadSpeedMbps       = settings.MinDownloadMbps,
                        incognitoRequired          = settings.IncognitoRequired,
                        isColorblindnessEnabled    = settings.IsColorblindnessEnabled
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading assessment settings");
                return Json(new { success = false, message = "Error loading settings" });
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
                
                var ipAddress = !string.IsNullOrWhiteSpace(request.IpAddress)
                    ? request.IpAddress
                    : HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
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
            if (string.IsNullOrEmpty(assessmentCode))
                assessmentCode = RouteData.Values["assessmentType"]?.ToString();

            if (string.IsNullOrEmpty(assessmentCode))
                return BadRequest("Assessment code is required");

            try
            {
                var sessionId = _sessionService.GetOrCreateSessionId();
                var viewModel = await _responseRepository.GetQuestionsForAssessment(assessmentCode, sessionId);

                if (viewModel == null)
                    return NotFound($"Assessment '{assessmentCode}' not found or inactive");

                // Switch on the assessment code to determine which view to navigate to
                // after the questions form is submitted.
                ViewBag.PostQuestionAction = assessmentCode.ToUpperInvariant() switch
                {
                    "SORT" => "SortAssessment",
                    "IQA"  => "ImageAssessment",
                    _      => assessmentCode.Contains("SORT", StringComparison.OrdinalIgnoreCase)
                                  ? "SortAssessment"
                                  : "ImageAssessment"
                };

                ViewBag.SessionId      = sessionId;
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
            if (submission == null)
            {
                return Json(new { success = false, message = "Invalid submission data" });
            }

            if (string.IsNullOrWhiteSpace(submission.AssessmentCode))
            {
                return Json(new { success = false, message = "Assessment code is required" });
            }

            if (submission.Answers == null || submission.Answers.Count == 0)
            {
                return Json(new { success = false, message = "At least one answer is required" });
            }

            // Validate each answer has at least one selected option
            var invalidAnswers = submission.Answers
                .Where(a => a.SelectedOptionIds == null || a.SelectedOptionIds.Count == 0)
                .ToList();

            if (invalidAnswers.Count > 0)
            {
                return Json(new
                {
                    success = false,
                    message = $"{invalidAnswers.Count} answer(s) have no option selected"
                });
            }

            try
            {
                var sessionId = _sessionService.GetOrCreateSessionId();
                submission.SessionId = sessionId;

                _logger.LogInformation(
                    "Submitting {Count} answers for session {SessionId}, assessment {AssessmentCode}",
                    submission.Answers.Count, sessionId, submission.AssessmentCode);

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
        public async Task<IActionResult> GetAssessmentImagesByGroup(string assessmentCode, string? groupCode)
        {
            var userId = HttpContext.Session.GetString("UserId") ?? "Anonymous";

            try
            {
                var result = await _imageRepository.GetImagesByAssessmentTypeAndGroup(assessmentCode, groupCode, userId);
                return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetImageGroups()
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId") ?? "Anonymous";
                var result = await _imageGroupRepository.GetAllImageGroups(userId);
                return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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

                if (imageSetResult.OutputCode == 0 || imageSetResult.Data == null) // All completed or none available
                {
                    _logger.LogInformation($"All image sets completed for session: {sessionId}, assessment: {assessmentCode}");
                    TempData["SuccessMessage"] = "You have completed all image sets. Thank you!";
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
                    LinkedImages = linkedImagesResult.Data.OrderBy(_ => Random.Shared.Next()).ToList(),
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

                // Use client-supplied IP from ipify; fall back to connection IP
                var ipAddress = !string.IsNullOrWhiteSpace(submission.IpAddress)
                    ? submission.IpAddress
                    : HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                var userAgent = Request.Headers["User-Agent"].ToString();

                _logger.LogInformation($"Submitting image quality rating - SessionId: {sessionId}, RawImageSetId: {submission.RawImageSetId}, LinkedImageId: {submission.SelectedLinkedImageId}, Rating: {submission.QualityRating}");

                var result = await _imageQualityRepository.SaveImageQualityRating(submission, ipAddress, userAgent, "Anonymous");

                if (result.OutputCode == 1)
                {
                    // Get progress after submission
                    var progressResult = await _imageQualityRepository.GetAssessmentProgress(sessionId, submission.AssessmentCode, "Anonymous");

                    // ✅ FIXED: Check if ALL sets are completed (CompletedSets >= TotalSets)
                    var isCompleted = progressResult.Data.CompletedSets >= progressResult.Data.TotalSets;

                    _logger.LogInformation($"Image quality rating submitted - SessionId: {sessionId}, CompletedSets: {progressResult.Data.CompletedSets}, TotalSets: {progressResult.Data.TotalSets}, IsCompleted: {isCompleted}");

                    return Json(new
                    {
                        success = true,
                        message = result.OutputMsg,
                        data = new
                        {
                            completedSets = progressResult.Data.CompletedSets,
                            totalSets = progressResult.Data.TotalSets,
                            isCompleted = isCompleted
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
        public async Task<IActionResult> GetImageAssessmentProgress(string sessionId = null, string assessmentCode = null)
        {
            try
            {
                // ✅ FIX: Accept parameters as explicit fallback for session
                var actualSessionId = !string.IsNullOrEmpty(sessionId) 
                    ? sessionId 
                    : _sessionService.GetOrCreateSessionId();
                    
                var actualAssessmentCode = !string.IsNullOrEmpty(assessmentCode) 
                    ? assessmentCode 
                    : _sessionService.GetAssessmentType();

                // ✅ Log what we received and what we're using
                _logger.LogInformation(
                    $"GetImageAssessmentProgress called - Params: sessionId={sessionId}, assessmentCode={assessmentCode} | Using: sessionId={actualSessionId}, assessmentCode={actualAssessmentCode}");

                // ✅ Validate we have required values
                if (string.IsNullOrEmpty(actualSessionId))
                {
                    _logger.LogWarning("GetImageAssessmentProgress: No session ID found");
                    return Json(new { success = false, message = "Session ID not found" });
                }

                if (string.IsNullOrEmpty(actualAssessmentCode))
                {
                    _logger.LogWarning($"GetImageAssessmentProgress: No assessment code found for session {actualSessionId}");
                    return Json(new { success = false, message = "Assessment code not found. Please start the assessment from the beginning." });
                }

                var result = await _imageQualityRepository.GetAssessmentProgress(actualSessionId, actualAssessmentCode, "Anonymous");

                _logger.LogInformation(
                    $"GetImageAssessmentProgress success - SessionId: {actualSessionId}, AssessmentCode: {actualAssessmentCode}, CompletedSets: {result.Data?.CompletedSets}, TotalSets: {result.Data?.TotalSets}");

                return Json(new
                {
                    success = result.OutputCode == 1,
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
                            cmCode         = content.CmCode,
                            cmContent      = content.CmContent,
                            cmActive       = content.CmActive,
                            assessmentType = assessmentType,
                            sessionId      = sessionId
                        }
                    });
                }

                // Return default content if none found
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        cmCode         = "IQA_Intro",
                        cmContent      = "<h3>IQA (Image Quality Assessment)</h3><p>You will be shown a series of image sets. For each set, select the image you believe has the best quality and rate it.</p>",
                        cmActive       = "Y",
                        assessmentType = assessmentType,
                        sessionId      = sessionId
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

        // ── Sort Assessment ────────────────────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SortAssessment(string assessmentCode)
        {
            try
            {
                var sessionId = _sessionService.GetOrCreateSessionId();

                if (string.IsNullOrEmpty(assessmentCode))
                {
                    assessmentCode = _sessionService.GetAssessmentType() ?? string.Empty;
                }

                if (string.IsNullOrEmpty(assessmentCode))
                {
                    TempData["ErrorMessage"] = "No assessment code specified.";
                    return RedirectToAction("Index");
                }

                // ✅ CRITICAL: Always set in session, even if already set
                _sessionService.SetAssessmentType(assessmentCode);

                _logger.LogInformation("Sort assessment started - SessionId: {SessionId}, AssessmentCode: {Code}", sessionId, assessmentCode);

                var assessmentTypeResult = await _assessmentTypeRepository.GetAssessmentTypeByCode(assessmentCode, "Anonymous");
                if (assessmentTypeResult.OutputCode != 1 || !assessmentTypeResult.Data.Any())
                {
                    TempData["ErrorMessage"] = $"Assessment '{assessmentCode}' not found.";
                    return RedirectToAction("Index");
                }

                var assessmentName = assessmentTypeResult.Data.First().AtmName;

                var imageSetResult = await _imageQualityRepository.GetNextRandomRawImageSet(sessionId, assessmentCode, "Anonymous");

                if (imageSetResult.OutputCode == 0 || imageSetResult.Data == null)
                {
                    _logger.LogInformation("All sort sets completed for session: {SessionId}, assessment: {Code}", sessionId, assessmentCode);
                    TempData["SuccessMessage"] = "You have completed all image sets. Thank you!";
                    return RedirectToAction("Index", new { assessmentType = assessmentCode });
                }

                var linkedImagesResult = await _imageQualityRepository.GetLinkedImagesByRawImageSetId(imageSetResult.Data.RisId, "Anonymous");

                if (linkedImagesResult.OutputCode != 1 || !linkedImagesResult.Data.Any())
                {
                    _logger.LogWarning("No linked images found for RawImageSetId: {RisId}", imageSetResult.Data.RisId);
                    TempData["ErrorMessage"] = "No linked images available for this image set.";
                    return RedirectToAction("Index", new { assessmentType = assessmentCode });
                }

                var progressResult = await _imageQualityRepository.GetAssessmentProgress(sessionId, assessmentCode, "Anonymous");

                var rawItem = new SortImageItem
                {
                    ImageId    = 0,
                    IsRawImage = true,
                    ImagePath  = imageSetResult.Data.RisRawImagePath,
                    ImageLabel = imageSetResult.Data.RisName
                };

                var linkedItems = linkedImagesResult.Data.Select(li => new SortImageItem
                {
                    ImageId    = li.LiId,
                    IsRawImage = false,
                    ImagePath  = li.LiImagePath,
                    ImageLabel = li.LiImageLabel
                }).ToList();

                var allImages = linkedItems.Append(rawItem)
                                           .OrderBy(_ => Random.Shared.Next())
                                           .ToList();

                var viewModel = new SortAssessmentViewModel
                {
                    SessionId        = sessionId,
                    AssessmentCode   = assessmentCode,
                    AssessmentName   = assessmentName,
                    RawImage         = imageSetResult.Data,
                    AllImages        = allImages,
                    CurrentSetNumber = progressResult.Data.CompletedSets + 1,
                    TotalSets        = progressResult.Data.TotalSets,
                    IsCompleted      = progressResult.Data.IsCompleted
                };

                // ✅ Log what we're passing to view
                _logger.LogInformation($"SortAssessment view - SessionId: {sessionId}, AssessmentCode: {assessmentCode}");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sort assessment");
                TempData["ErrorMessage"] = "An error occurred while loading the assessment.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitSortRatings([FromBody] SortRatingSubmission submission)
        {
            try
            {
                // ✅ MILLISECOND-PRECISION TIMING SUPPORT
                var sessionId = _sessionService.GetOrCreateSessionId();
                submission.SessionId = sessionId;

                // Validate submission
                if (submission.RawImageSetId <= 0)
                    return Json(new { success = false, message = "Invalid image set" });

                if (submission.Ratings == null || submission.Ratings.Count == 0)
                    return Json(new { success = false, message = "No ratings provided" });

                // ✅ Validate millisecond timing data
                if (submission.TotalSessionTimeMilliseconds < 0)
                    return Json(new { success = false, message = "Invalid session time" });

                if (submission.Ratings.Any(r => r.Rating < -3 || r.Rating > 3))
                    return Json(new { success = false, message = "All ratings must be between -3 and +3" });

                if (submission.Ratings.Any(r => r.TimeTakenMilliseconds < 0))
                    return Json(new { success = false, message = "Invalid timing data" });

                var ipAddress = !string.IsNullOrWhiteSpace(submission.IpAddress)
                    ? submission.IpAddress
                    : HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                var userAgent = Request.Headers["User-Agent"].ToString();

                _logger.LogInformation(
                    "Submitting {Count} sort ratings - SessionId: {SessionId}, RawImageSetId: {RisId}, TotalTime: {Time}ms",
                    submission.Ratings.Count, sessionId, submission.RawImageSetId, submission.TotalSessionTimeMilliseconds);

                // ✅ Log timing details for analytics
                foreach (var rating in submission.Ratings)
                {
                    _logger.LogInformation(
                        "Image {ImageId} rated {Rating} in {TimeTaken}ms (display: {DisplayDuration}ms)",
                        rating.ImageId, rating.Rating, rating.TimeTakenMilliseconds, rating.DisplayDurationMilliseconds);
                }

                // Pass full submission with millisecond timing to repository
                var result = await _imageQualityRepository.SaveSortRatings(submission, ipAddress, userAgent, "Anonymous");

                if (result.OutputCode == 1)
                {
                    var progressResult = await _imageQualityRepository.GetAssessmentProgress(sessionId, submission.AssessmentCode, "Anonymous");

                    var hasMoreImages = progressResult.Data.CompletedSets < progressResult.Data.TotalSets;

                    _logger.LogInformation(
                        "Sort ratings submitted successfully - SessionId: {SessionId}, RawImageSetId: {RisId}, CompletedSets: {Completed}/{Total}, HasMoreImages: {HasMore}, TotalTime: {Time}ms",
                        sessionId, submission.RawImageSetId, progressResult.Data.CompletedSets, progressResult.Data.TotalSets, hasMoreImages, submission.TotalSessionTimeMilliseconds);

                    return Json(new
                    {
                        success = true,
                        message = result.OutputMsg,
                        data = new
                        {
                            completedSets = progressResult.Data.CompletedSets,
                            totalSets = progressResult.Data.TotalSets,
                            hasMoreImages = hasMoreImages,
                            isCompleted = hasMoreImages == false,
                            percentComplete = 0,
                            timingMetrics = new
                            {
                                totalSessionTimeMilliseconds = submission.TotalSessionTimeMilliseconds,
                                averageTimePerImageMs = submission.AverageTimePerImageMilliseconds,
                                fastestRatingMs = submission.FastestRatingMilliseconds,
                                slowestRatingMs = submission.SlowestRatingMilliseconds
                            }
                        }
                    });
                }

                _logger.LogWarning(
                    "Sort ratings submission failed - SessionId: {SessionId}, OutputCode: {Code}, Message: {Message}",
                    sessionId, result.OutputCode, result.OutputMsg);

                return Json(new { success = false, message = result.OutputMsg });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting sort ratings");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ColorblindnessTest(string assessmentType)
        {
            try
            {
                var sessionId = _sessionService.GetOrCreateSessionId();
                _logger.LogInformation($"Colorblindness test accessed - SessionId: {sessionId}, AssessmentType: {assessmentType}");

                if (string.IsNullOrEmpty(assessmentType))
                {
                    assessmentType = _sessionService.GetAssessmentType();
                    if (string.IsNullOrEmpty(assessmentType))
                    {
                        TempData["ErrorMessage"] = "No assessment type specified.";
                        return RedirectToAction("Index");
                    }
                }

                // Store in session for reference
                _sessionService.SetAssessmentType(assessmentType);

                // Get test time limit from system parameters (default 5 seconds)
                var settingsResult = await _systemCheckParamRepository.GetResolvedSettings();
                var timeLimit = 5; // Default

                timeLimit = settingsResult.ColorblindnessTimeLimit;

                ViewBag.SessionId = sessionId;
                ViewBag.AssessmentType = assessmentType;
                ViewBag.TestTimeLimit = timeLimit;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading colorblindness test");
                TempData["ErrorMessage"] = "An error occurred while loading the test.";
                return RedirectToAction("Index");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetColorblindnessImagesForTest(string assessmentType)
        {
            try
            {
                var result = await _colorblindnessRepository.GetColorblindnessImagesForTest(assessmentType);

                if (result.OutputCode == 1 && result.Data != null && result.Data.Count > 0)
                {
                    // Shuffle images on server side for better randomization
                    var shuffled = result.Data.OrderBy(_ => Random.Shared.Next()).ToList();

                    return Json(new
                    {
                        success = true,
                        data = shuffled.Select(img => new
                        {
                            imageId = img.CbImageId,
                            imageUrl = img.ImageUrl,
                            options = img.Options,
                            correctAnswer = img.CorrectAnswer // The first item is the correct answer before shuffling
                        }).ToList()
                    });
                }

                return Json(new
                {
                    success = false,
                    message = "No colorblindness test images available"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading colorblindness test images");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SubmitColorblindnessResponses([FromBody] ColorblindnessTestSubmission submission)
        {
            try
            {
                if (submission?.Responses == null || submission.Responses.Count == 0)
                    return Json(new { success = false, message = "No responses provided" });

                // Get client IP address
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                
                // Get user agent
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString() ?? "Unknown";

                // Enrich responses with metadata
                var enrichedResponses = new List<UserColorblindnessResponse>();
                int sequence = 1;
                foreach (var response in submission.Responses)
                {
                    enrichedResponses.Add(new UserColorblindnessResponse
                    {
                        SessionId = submission.SessionId,
                        AssessmentType = submission.AssessmentType,
                        ImageId = response.ImageId,
                        ImageSequence = sequence++, // Assign sequence number based on order in list
                        SelectedAnswer = response.SelectedAnswer,
                        CorrectAnswer = response.CorrectAnswer,
                        IsCorrect = response.IsCorrect,
                        TimeTakenSeconds = response.TimeTakenSeconds,
                        IpAddress = ipAddress,
                        UserAgent = userAgent
                    }); 
                }

                // Save all responses per image
                var result = await _colorblindnessRepository.SaveUserResponses(enrichedResponses, "system");

                if (result.OutputCode == 1)
                {
                    return Json(new
                    {
                        success = true,
                        message = result.OutputMsg,
                        successCount = result.SuccessCount,
                        failureCount = result.FailureCount
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = result.OutputMsg,
                        successCount = result.SuccessCount,
                        failureCount = result.FailureCount
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting colorblindness responses");
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}"
                });
            }
        }
    }
}