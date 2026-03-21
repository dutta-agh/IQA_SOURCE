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

                _logger.LogInformation("Sort assessment started - SessionId: {SessionId}, AssessmentCode: {Code}", sessionId, assessmentCode);

                var assessmentTypeResult = await _assessmentTypeRepository.GetAssessmentTypeByCode(assessmentCode, "Anonymous");
                if (assessmentTypeResult.OutputCode != 1 || !assessmentTypeResult.Data.Any())
                {
                    TempData["ErrorMessage"] = $"Assessment '{assessmentCode}' not found.";
                    return RedirectToAction("Index");
                }

                var assessmentName = assessmentTypeResult.Data.First().AtmName;

                // Reuse existing method — picks a random master image not yet rated in this session
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

                // Build the shuffled pool: raw image + all linked images, identity hidden
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

                // Combine then shuffle — user cannot tell which is raw
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
                var sessionId = _sessionService.GetOrCreateSessionId();
                submission.SessionId = sessionId;

                if (submission.RawImageSetId <= 0)
                    return Json(new { success = false, message = "Invalid image set" });

                if (submission.Ratings == null || submission.Ratings.Count == 0)
                    return Json(new { success = false, message = "No ratings provided" });

                if (submission.Ratings.Any(r => r.Rating < 1 || r.Rating > 5))
                    return Json(new { success = false, message = "All ratings must be between 1 and 5" });

                // Enforce uniqueness — no two images may share the same rating
                var duplicates = submission.Ratings
                    .GroupBy(r => r.Rating)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicates.Count > 0)
                    return Json(new { success = false, message = $"Rating value(s) {string.Join(", ", duplicates)} assigned to more than one image. Each rating must be unique." });

                var ipAddress = !string.IsNullOrWhiteSpace(submission.IpAddress)
                    ? submission.IpAddress
                    : HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                var userAgent = Request.Headers["User-Agent"].ToString();

                _logger.LogInformation(
                    "Submitting {Count} sort ratings - SessionId: {SessionId}, RawImageSetId: {RisId}",
                    submission.Ratings.Count, sessionId, submission.RawImageSetId);

                var result = await _imageQualityRepository.SaveSortRatings(submission, ipAddress, userAgent, "Anonymous");

                if (result.OutputCode == 1)
                {
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
                            correctAnswer = img.Options.First() // The first item is the correct answer before shuffling
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

        // ── Colorblindness Test Results Download ────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DownloadColorblindnessResultsExcel(string assessmentType, DateTime? startDate = null, DateTime? endDate = null)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                // Validate assessmentType parameter
                if (string.IsNullOrWhiteSpace(assessmentType))
                {
                    TempData["ErrorMessage"] = "Assessment type is required. Please select an assessment type and try again.";
                    return RedirectToAction("ColorblindnessResults", "Admin");
                }

                _logger.LogInformation($"DownloadColorblindnessResultsExcel called with assessmentType: {assessmentType}, startDate: {startDate}, endDate: {endDate}");

                ColorblindnessTestResultResponse result;
                if (startDate.HasValue && endDate.HasValue)
                {
                    _logger.LogInformation($"Fetching results by date range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                    result = await _colorblindnessRepository.GetColorblindnessResultsByDateRange(assessmentType, startDate.Value, endDate.Value, userId);
                }
                else
                {
                    _logger.LogInformation($"Fetching all results for assessment type: {assessmentType}");
                    result = await _colorblindnessRepository.GetAllColorblindnessResults(assessmentType, userId);
                }

                _logger.LogInformation($"Repository returned: OutputCode={result.OutputCode}, DataCount={result.Data?.Count ?? 0}, Message={result.OutputMsg}");

                if (result.OutputCode != 1)
                {
                    TempData["ErrorMessage"] = $"Error retrieving data: {result.OutputMsg}";
                    return RedirectToAction("ColorblindnessResults", "Admin");
                }

                if (result.Data == null || !result.Data.Any())
                {
                    TempData["ErrorMessage"] = $"No colorblindness test data found for assessment type '{assessmentType}'. Please check your filters and try again.";
                    return RedirectToAction("ColorblindnessResults", "Admin");
                }

                var workbook = new XSSFWorkbook();

                // Create Summary Sheet
                CreateSummarySheet(workbook, result.Data);

                // Create Image-Wise Details Sheet
                CreateImageWiseSheet(workbook, result.Data);

                // Write to memory stream
                using var memoryStream = new MemoryStream();
                workbook.Write(memoryStream);   
                
                _logger.LogInformation($"Excel file generated successfully for {result.Data.Count} records");
                
                return File(memoryStream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ColorblindnessResults_{assessmentType}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating colorblindness results Excel file");
                TempData["ErrorMessage"] = $"Error generating Excel file: {ex.Message}";
                return RedirectToAction("ColorblindnessResults", "Admin");
            }
        }

        private void CreateSummarySheet(XSSFWorkbook workbook, List<ColorblindnessTestResult> results)
        {
            var sheet = workbook.CreateSheet("Summary");

            // Create styles
            var headerStyle = CreateHeaderStyle(workbook);
            var dataStyle = CreateDataStyle(workbook);
            var altStyle = CreateAlternateRowStyle(workbook);

            // Create header row
            var headerRow = sheet.CreateRow(0);
            string[] headers = {
                "Session ID", "Assessment Type", "Total Questions Attempted",
                "Correct Answers", "Incorrect Answers", "Can't Read Answers",
                "Accuracy (%)", "Average Time Per Question (sec)", "Test Start Time",
                "Test End Time", "IP Address", "Test Status"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
            }

            // Add data rows
            int rowIndex = 1;
            foreach (var testResult in results)
            {
                var row = sheet.CreateRow(rowIndex);
                var style = rowIndex % 2 == 0 ? altStyle : dataStyle;

                row.CreateCell(0).SetCellValue(testResult.SessionId ?? "");
                row.CreateCell(0).CellStyle = style;

                row.CreateCell(1).SetCellValue(testResult.AssessmentType ?? "");
                row.CreateCell(1).CellStyle = style;

                row.CreateCell(2).SetCellValue(testResult.TotalQuestionsAttempted);
                row.CreateCell(2).CellStyle = style;

                row.CreateCell(3).SetCellValue(testResult.CorrectAnswers);
                row.CreateCell(3).CellStyle = style;

                row.CreateCell(4).SetCellValue(testResult.IncorrectAnswers);
                row.CreateCell(4).CellStyle = style;

                row.CreateCell(5).SetCellValue(testResult.CantReadAnswers);
                row.CreateCell(5).CellStyle = style;

                row.CreateCell(6).SetCellValue($"{testResult.AccuracyPercentage:F2}");
                row.CreateCell(6).CellStyle = style;

                row.CreateCell(7).SetCellValue($"{testResult.AverageTimePerQuestion:F2}");
                row.CreateCell(7).CellStyle = style;

                row.CreateCell(8).SetCellValue(testResult.TestStartTime.ToString("yyyy-MM-dd HH:mm:ss"));
                row.CreateCell(8).CellStyle = style;

                row.CreateCell(9).SetCellValue(testResult.TestEndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "");
                row.CreateCell(9).CellStyle = style;

                row.CreateCell(10).SetCellValue(testResult.IpAddress ?? "");
                row.CreateCell(10).CellStyle = style;

                row.CreateCell(11).SetCellValue(testResult.TestStatus ?? "");
                row.CreateCell(11).CellStyle = style;

                rowIndex++;
            }

            // Auto-size columns
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
                if (sheet.GetColumnWidth(i) > 15000)
                    sheet.SetColumnWidth(i, 15000);
            }
        }

        private void CreateImageWiseSheet(XSSFWorkbook workbook, List<ColorblindnessTestResult> results)
        {
            var sheet = workbook.CreateSheet("Image-Wise Details");

            var headerStyle = CreateHeaderStyle(workbook);
            var dataStyle = CreateDataStyle(workbook);
            var altStyle = CreateAlternateRowStyle(workbook);

            // Create header row
            var headerRow = sheet.CreateRow(0);
            string[] headers = {
                "Session ID", "Image Sequence", "Image ID",
                "Correct Answer", "User Answer", "Result Status",
                "Time Taken (sec)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
            }

            // Add image-wise data
            int rowIndex = 1;
            foreach (var testResult in results)
            {
                if (testResult.ImageWiseResults != null && testResult.ImageWiseResults.Any())
                {
                    foreach (var imageResult in testResult.ImageWiseResults)
                    {
                        var row = sheet.CreateRow(rowIndex);
                        var style = rowIndex % 2 == 0 ? altStyle : dataStyle;

                        row.CreateCell(0).SetCellValue(testResult.SessionId ?? "");
                        row.CreateCell(0).CellStyle = style;

                        row.CreateCell(1).SetCellValue(imageResult.ImageSequence);
                        row.CreateCell(1).CellStyle = style;

                        row.CreateCell(2).SetCellValue(imageResult.ImageId);
                        row.CreateCell(2).CellStyle = style;

                        row.CreateCell(3).SetCellValue(imageResult.CorrectAnswer ?? "");
                        row.CreateCell(3).CellStyle = style;

                        row.CreateCell(4).SetCellValue(imageResult.SelectedAnswer ?? "Skipped");
                        row.CreateCell(4).CellStyle = style;

                        row.CreateCell(5).SetCellValue(imageResult.ResultStatus ?? "");
                        row.CreateCell(5).CellStyle = style;

                        row.CreateCell(6).SetCellValue(imageResult.TimeTakenSeconds);
                        row.CreateCell(6).CellStyle = style;

                        rowIndex++;
                    }
                }
            }

            // Auto-size columns
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
                if (sheet.GetColumnWidth(i) > 15000)
                    sheet.SetColumnWidth(i, 15000);
            }
        }

        private ICellStyle CreateHeaderStyle(XSSFWorkbook workbook)
        {
            var style = workbook.CreateCellStyle();
            var font = workbook.CreateFont();
            font.IsBold = true;
            font.Color = IndexedColors.White.Index;
            font.FontHeightInPoints = 11;
            style.SetFont(font);
            style.FillForegroundColor = IndexedColors.DarkBlue.Index;
            style.FillPattern = FillPattern.SolidForeground;
            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderBottom = BorderStyle.Medium;
            style.BorderTop = BorderStyle.Medium;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            return style;
        }

        private ICellStyle CreateDataStyle(XSSFWorkbook workbook)
        {
            var style = workbook.CreateCellStyle();
            var font = workbook.CreateFont();
            font.FontHeightInPoints = 10;
            style.SetFont(font);
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            return style;
        }

        private ICellStyle CreateAlternateRowStyle(XSSFWorkbook workbook)
        {
            var style = workbook.CreateCellStyle();
            var font = workbook.CreateFont();
            font.FontHeightInPoints = 10;
            style.SetFont(font);
            style.FillForegroundColor = IndexedColors.LightCornflowerBlue.Index;
            style.FillPattern = FillPattern.SolidForeground;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            return style;
        }
    }
}