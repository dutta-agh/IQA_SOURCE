using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using IQA_SOURCE.Data;
using IQA_SOURCE.Models.Admin;
using System.Text.Json;
using IQA_SOURCE.Services;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;
using MySqlConnector; // Change from MySql.Data.MySqlClient
using System.Data;
using YourApp.Data;

namespace IQA_SOURCE.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IAssessmentTypeRepository _assessmentTypeRepository;   
        private readonly IQuestionMasterRepository _questionMasterRepository;
        private readonly IImageMetadataService _metadataService;
        private readonly IImageRepository _imageRepository;
        private readonly ImageStorageSettings _imageSettings;
        private readonly IDashboardRepository _dashboardRepository;
        private readonly ISpeedTestRepository _speedTestRepository;
        private readonly ILogger<AdminController> _logger;
        private readonly IQuestionAnswerRepository _questionAnswerRepository;
        private readonly IDbHelper _dbHelper; // Add this instead of IDbConnection

        public AdminController(
            IAdminRepository adminRepository, 
            IAssessmentTypeRepository assessmentTypeRepository, 
            IQuestionMasterRepository questionMasterRepository,
            IImageMetadataService metadataService,
            IImageRepository imageRepository,
            IOptions<ImageStorageSettings> imageSettings,
            IDashboardRepository dashboardRepository,
            ISpeedTestRepository speedTestRepository,
            IQuestionAnswerRepository questionAnswerRepository,
            ILogger<AdminController> logger,
            IDbHelper dbHelper) // Change from IDbConnection to IDbHelper
        {
            _adminRepository = adminRepository;
            _assessmentTypeRepository = assessmentTypeRepository;
            _questionMasterRepository = questionMasterRepository;
            _metadataService = metadataService;
            _imageRepository = imageRepository;
            _imageSettings = imageSettings.Value;
            _dashboardRepository = dashboardRepository;
            _speedTestRepository = speedTestRepository;
            _questionAnswerRepository = questionAnswerRepository;
            _logger = logger;
            _dbHelper = dbHelper; // Change from _dbConnection
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

        // Question Master
        [HttpGet]
        public IActionResult QuestionMaster()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllQuestions()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _questionMasterRepository.GetAllQuestions(userId);
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg,
                data = result.Data
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetQuestionWithOptions(int qId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _questionMasterRepository.GetQuestionWithOptions(qId, userId);
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg,
                data = result.Data
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveQuestion([FromBody] JsonElement model)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                // Parse the JSON and convert qsActive from string to int
                var question = new QuestionMaster
                {
                    QId = model.TryGetProperty("qsId", out var qId) ? qId.GetInt32() : 0,
                    QsCode = model.TryGetProperty("qsCode", out var qsCode) ? qsCode.GetString() : null,
                    QsText = model.TryGetProperty("qsText", out var qsText) ? qsText.GetString() : null,
                    QsType = model.TryGetProperty("qsType", out var qsType) ? qsType.GetString() : null,
                    QsCategory = model.TryGetProperty("qsCategory", out var qsCategory) ? qsCategory.GetString() : null,
                    QsMaxSelections = model.TryGetProperty("qsMaxSelections", out var qsMaxSelections) && qsMaxSelections.ValueKind != JsonValueKind.Null 
                        ? qsMaxSelections.GetInt32() 
                        : (int?)null,
                    QsOrderNo = model.TryGetProperty("qsOrderNo", out var qsOrderNo) && qsOrderNo.ValueKind != JsonValueKind.Null 
                        ? qsOrderNo.GetInt32() 
                        : (int?)null,
                    QsActive = model.TryGetProperty("qsActive", out var qsActive) 
                        ? ConvertToIntActive(qsActive) 
                        : 1
                };

                QuestionMasterResponse result;

                if (question.QId > 0)
                {
                    result = await _questionMasterRepository.UpdateQuestion(question, userId);
                }
                else
                {
                    result = await _questionMasterRepository.InsertQuestion(question, userId);
                }

                return Json(new
                {
                    success = result.OutputCode == 1,
                    message = result.OutputMsg
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQuestion([FromBody] int qId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _questionMasterRepository.DeleteQuestion(qId, userId);
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveQuestionOption([FromBody] JsonElement model)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var option = new QuestionOption
                {
                    QoId = model.TryGetProperty("qoId", out var qoId) ? qoId.GetInt32() : 0,
                    QoQId = model.TryGetProperty("qoQId", out var qoQId) ? qoQId.GetInt32() : 0,
                    QoText = model.TryGetProperty("qoText", out var qoText) ? qoText.GetString() : null,
                    QoValue = model.TryGetProperty("qoValue", out var qoValue) ? qoValue.GetString() : null,
                    QoOrderNo = model.TryGetProperty("qoOrderNo", out var qoOrderNo) && qoOrderNo.ValueKind != JsonValueKind.Null 
                        ? qoOrderNo.GetInt32() 
                        : (int?)null,
                    QoActive = model.TryGetProperty("qoActive", out var qoActive) 
                        ? ConvertToIntActive(qoActive) 
                        : 1
                };

                QuestionMasterResponse result;

                if (option.QoId > 0)
                {
                    result = await _questionMasterRepository.UpdateQuestionOption(option, userId);
                }
                else
                {
                    result = await _questionMasterRepository.InsertQuestionOption(option, userId);
                }

                return Json(new
                {
                    success = result.OutputCode == 1,
                    message = result.OutputMsg
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQuestionOption([FromBody] int qoId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _questionMasterRepository.DeleteQuestionOption(qoId, userId);
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg
            });
        }

        [HttpPost]
        [RequestSizeLimit(524288000)] // 500MB
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
        public async Task<IActionResult> UploadImages([FromForm] string assessmentType, [FromForm] IFormFile[] files)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            if (files == null || files.Length == 0)
            {
                return Json(new { success = false, message = "No files uploaded" });
            }

            try
            {
                var uploadBatch = Guid.NewGuid().ToString();
                
                // Use Linux server path from configuration
                var uploadPath = Path.Combine(_imageSettings.BasePath, assessmentType);
                
                // Ensure directory exists (works on Linux)
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var masterImages = new List<ImageMaster>();
                var linkedImages = new List<ImageLinked>();
                var skippedMasters = new List<string>();
                var errors = new List<string>();

                // Group files by base name
                var fileGroups = files.GroupBy(f => GetBaseName(f.FileName)).ToList();

                foreach (var group in fileGroups)
                {
                    var masterFile = group.FirstOrDefault(f => IsMasterImage(f.FileName));
                    
                    if (masterFile != null)
                    {
                        var masterFileName = Path.GetFileName(masterFile.FileName);
                        
                        // Check if master image already exists
                        var masterExists = await _imageRepository.CheckDuplicateFileName(assessmentType, masterFileName);
                        
                        if (masterExists)
                        {
                            // Skip master but process linked images
                            skippedMasters.Add(masterFileName);
                            
                            // Get the existing master image ID
                            var existingMaster = await _imageRepository.GetMasterImageByFileName(assessmentType, masterFileName, userId);
                            
                            if (existingMaster != null)
                            {
                                // Process only new linked images for this master
                                foreach (var linkedFile in group.Where(f => !IsMasterImage(f.FileName)))
                                {
                                    var linkedFileName = Path.GetFileName(linkedFile.FileName);
                                    
                                    // Check if this linked image already exists
                                    var linkedExists = await _imageRepository.CheckLinkedImageExists(existingMaster.ImId, linkedFileName);
                                    
                                    if (!linkedExists)
                                    {
                                        try
                                        {
                                            // Save to Linux server path
                                            var linkedFilePath = Path.Combine(uploadPath, linkedFileName);
                                           
                                            using (var stream = new FileStream(linkedFilePath, FileMode.Create))
                                            {
                                                await linkedFile.CopyToAsync(stream);
                                            }

                                            // Extract metadata
                                            var linkedMetadata = await _metadataService.ExtractMetadata(linkedFilePath);
                                            var (qualityLevel, qualityType) = ParseQualityInfo(linkedFileName);

                                            // Store web-accessible path
                                            var webPath = $"{_imageSettings.WebBasePath}/{assessmentType}/{linkedFileName}";

                                            linkedImages.Add(new ImageLinked
                                            {
                                                IlMasterId = existingMaster.ImId,
                                                IlFileName = linkedFileName,
                                                IlFilePath = webPath, // Web-accessible path
                                                IlFileSize = linkedFile.Length,
                                                IlWidth = linkedMetadata.Width,
                                                IlHeight = linkedMetadata.Height,
                                                IlFormat = linkedMetadata.Format,
                                                IlColorSpace = linkedMetadata.ColorSpace,
                                                IlBitDepth = linkedMetadata.BitDepth,
                                                IlDpiX = linkedMetadata.DpiX,
                                                IlDpiY = linkedMetadata.DpiY,
                                                IlExifData = JsonSerializer.Serialize(linkedMetadata.ExifData),
                                                IlCameraMake = linkedMetadata.CameraMake,
                                                IlCameraModel = linkedMetadata.CameraModel,
                                                IlLensModel = linkedMetadata.LensModel,
                                                IlFocalLength = linkedMetadata.FocalLength,
                                                IlAperture = linkedMetadata.Aperture,
                                                IlShutterSpeed = linkedMetadata.ShutterSpeed,
                                                IlIso = linkedMetadata.Iso,
                                                IlFlash = linkedMetadata.Flash,
                                                IlExposureMode = linkedMetadata.ExposureMode,
                                                IlWhiteBalance = linkedMetadata.WhiteBalance,
                                                IlDateTaken = linkedMetadata.DateTaken,
                                                IlOrientation = linkedMetadata.Orientation,
                                                IlCompressionQuality = linkedMetadata.CompressionQuality,
                                                IlQualityLevel = qualityLevel,
                                                IlQualityType = qualityType,
                                                IlUploadBatch = uploadBatch
                                            });
                                        }
                                        catch (Exception ex)
                                        {
                                            errors.Add($"Error processing linked image {linkedFileName}: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Master doesn't exist, proceed with normal upload
                            try
                            {
                                var masterFilePath = Path.Combine(uploadPath, masterFileName);
                                
                                using (var stream = new FileStream(masterFilePath, FileMode.Create))
                                {
                                    await masterFile.CopyToAsync(stream);
                                }

                                // Extract metadata and EXIF
                                var metadata = await _metadataService.ExtractMetadata(masterFilePath);

                                // Store web-accessible path
                                var webPath = $"{_imageSettings.WebBasePath}/{assessmentType}/{masterFileName}";

                                masterImages.Add(new ImageMaster
                                {
                                    ImAssessmentType = assessmentType,
                                    ImFileName = masterFileName,
                                    ImFilePath = webPath, // Web-accessible path
                                    ImFileSize = masterFile.Length,
                                    ImWidth = metadata.Width,
                                    ImHeight = metadata.Height,
                                    ImFormat = metadata.Format,
                                    ImColorSpace = metadata.ColorSpace,
                                    ImBitDepth = metadata.BitDepth,
                                    ImDpiX = metadata.DpiX,
                                    ImDpiY = metadata.DpiY,
                                    ImExifData = JsonSerializer.Serialize(metadata.ExifData),
                                    ImCameraMake = metadata.CameraMake,
                                    ImCameraModel = metadata.CameraModel,
                                    ImLensModel = metadata.LensModel,
                                    ImFocalLength = metadata.FocalLength,
                                    ImAperture = metadata.Aperture,
                                    ImShutterSpeed = metadata.ShutterSpeed,
                                    ImIso = metadata.Iso,
                                    ImFlash = metadata.Flash,
                                    ImExposureMode = metadata.ExposureMode,
                                    ImWhiteBalance = metadata.WhiteBalance,
                                    ImDateTaken = metadata.DateTaken,
                                    ImOrientation = metadata.Orientation,
                                    ImCompressionQuality = metadata.CompressionQuality,
                                    ImUploadBatch = uploadBatch
                                });

                                // Process linked images for NEW master
                                foreach (var linkedFile in group.Where(f => !IsMasterImage(f.FileName)))
                                {
                                    try
                                    {
                                        var linkedFileName = Path.GetFileName(linkedFile.FileName);
                                        var linkedFilePath = Path.Combine(uploadPath, linkedFileName);
                                        
                                        using (var stream = new FileStream(linkedFilePath, FileMode.Create))
                                        {
                                            await linkedFile.CopyToAsync(stream);
                                        }

                                        // Extract metadata for linked image
                                        var linkedMetadata = await _metadataService.ExtractMetadata(linkedFilePath);
                                        var (qualityLevel, qualityType) = ParseQualityInfo(linkedFileName);

                                        var webPath1 = $"{_imageSettings.WebBasePath}/{assessmentType}/{linkedFileName}";

                                        linkedImages.Add(new ImageLinked
                                        {
                                            IlFileName = linkedFileName,
                                            IlFilePath = webPath1,
                                            IlFileSize = linkedFile.Length,
                                            IlWidth = linkedMetadata.Width,
                                            IlHeight = linkedMetadata.Height,
                                            IlFormat = linkedMetadata.Format,
                                            IlColorSpace = linkedMetadata.ColorSpace,
                                            IlBitDepth = linkedMetadata.BitDepth,
                                            IlDpiX = linkedMetadata.DpiX,
                                            IlDpiY = linkedMetadata.DpiY,
                                            IlExifData = JsonSerializer.Serialize(linkedMetadata.ExifData),
                                            IlCameraMake = linkedMetadata.CameraMake,
                                            IlCameraModel = linkedMetadata.CameraModel,
                                            IlLensModel = linkedMetadata.LensModel,
                                            IlFocalLength = linkedMetadata.FocalLength,
                                            IlAperture = linkedMetadata.Aperture,
                                            IlShutterSpeed = linkedMetadata.ShutterSpeed,
                                            IlIso = linkedMetadata.Iso,
                                            IlFlash = linkedMetadata.Flash,
                                            IlExposureMode = linkedMetadata.ExposureMode,
                                            IlWhiteBalance = linkedMetadata.WhiteBalance,
                                            IlDateTaken = linkedMetadata.DateTaken,
                                            IlOrientation = linkedMetadata.Orientation,
                                            IlCompressionQuality = linkedMetadata.CompressionQuality,
                                            IlQualityLevel = qualityLevel,
                                            IlQualityType = qualityType,
                                            IlUploadBatch = uploadBatch
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        errors.Add($"Error processing linked image: {ex.Message}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Error processing master image {masterFileName}: {ex.Message}");
                            }
                        }
                    }
                }

                // Save to database
                ImageUploadResponse result;
                
                if (masterImages.Count > 0 || linkedImages.Count > 0)
                {
                    result = await _imageRepository.SaveImages(masterImages, linkedImages, userId);
                    
                    // Add skip information to result
                    result.Data.SkippedMasterImages = skippedMasters.Count;
                    result.Data.SkippedMasterFileNames = skippedMasters;
                    if (errors.Count > 0)
                    {
                        result.Data.ErrorMessages = result.Data.ErrorMessages ?? new List<string>();
                        result.Data.ErrorMessages.AddRange(errors);
                    }
                }
                else
                {
                    result = new ImageUploadResponse
                    {
                        OutputCode = 1,
                        OutputMsg = "No new images to upload. All master images already exist.",
                        Data = new ImageUploadResult
                        {
                            TotalFiles = files.Length,
                            MasterImagesUploaded = 0,
                            LinkedImagesUploaded = 0,
                            SkippedMasterImages = skippedMasters.Count,
                            SkippedMasterFileNames = skippedMasters,
                            ErrorMessages = errors
                        }
                    };
                }

                // Build detailed message
                var messageBuilder = new System.Text.StringBuilder();
                if (result.Data.MasterImagesUploaded > 0)
                    messageBuilder.Append($"{result.Data.MasterImagesUploaded} master image(s) uploaded. ");
                if (result.Data.LinkedImagesUploaded > 0)
                    messageBuilder.Append($"{result.Data.LinkedImagesUploaded} linked image(s) uploaded. ");
                if (result.Data.SkippedMasterImages > 0)
                    messageBuilder.Append($"{result.Data.SkippedMasterImages} master image(s) skipped (already exist). ");
                if (errors.Count > 0)
                    messageBuilder.Append($"{errors.Count} error(s) occurred. ");

                result.OutputMsg = messageBuilder.ToString().Trim();

                return Json(new
                {
                    success = result.OutputCode == 1,
                    message = result.OutputMsg,
                    data = result.Data
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Upload error: {ex.Message}" });
            }
        }

        // Images Master
        [HttpGet]
        public IActionResult ImagesMaster()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetImagesByAssessmentType(string assessmentType)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _imageRepository.GetImagesByAssessmentType(assessmentType, userId);
            
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg,
                data = result.Data
            });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage([FromBody] int imageId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _imageRepository.DeleteImage(imageId, userId);
            
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg
            });
        }

        // Folder Processing
        [HttpPost]
        public async Task<IActionResult> ProcessFolderImages([FromBody] FolderProcessRequest request)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            if (string.IsNullOrEmpty(request.FolderPath))
            {
                return Json(new { success = false, message = "Folder path is required" });
            }

            if (string.IsNullOrEmpty(request.AssessmentType))
            {
                return Json(new { success = false, message = "Assessment type is required" });
            }

            try
            {
                // Extract metadata for all images in folder
                var result = await _imageRepository.ProcessFolderImages(request.FolderPath, request.AssessmentType, userId);

                // Now we need to extract metadata for the images
                if (result.OutputCode == 1 && result.Data != null)
                {
                    // Get all newly added images and extract metadata
                    var imagesToProcess = await _imageRepository.GetImagesByAssessmentType(request.AssessmentType, userId);
                    
                    foreach (var image in imagesToProcess.Data)
                    {
                        if (System.IO.File.Exists(image.ImFilePath))
                        {
                            try
                            {
                                var metadata = await _metadataService.ExtractMetadata(image.ImFilePath);
                                
                                // Update the image with metadata (you'll need to add an update method)
                                image.ImWidth = metadata.Width;
                                image.ImHeight = metadata.Height;
                                image.ImFormat = metadata.Format;
                                image.ImColorSpace = metadata.ColorSpace;
                                image.ImBitDepth = metadata.BitDepth;
                                image.ImDpiX = metadata.DpiX;
                                image.ImDpiY = metadata.DpiY;
                                image.ImExifData = JsonSerializer.Serialize(metadata.ExifData);
                                image.ImCameraMake = metadata.CameraMake;
                                image.ImCameraModel = metadata.CameraModel;
                                image.ImLensModel = metadata.LensModel;
                                image.ImFocalLength = metadata.FocalLength;
                                image.ImAperture = metadata.Aperture;
                                image.ImShutterSpeed = metadata.ShutterSpeed;
                                image.ImIso = metadata.Iso;
                                image.ImFlash = metadata.Flash;
                                image.ImExposureMode = metadata.ExposureMode;
                                image.ImWhiteBalance = metadata.WhiteBalance;
                                image.ImDateTaken = metadata.DateTaken;
                                image.ImOrientation = metadata.Orientation;
                                image.ImCompressionQuality = metadata.CompressionQuality;
                                
                                // Update in database (you'll need to implement UpdateImageMetadata in repository)
                                // await _imageRepository.UpdateImageMetadata(image, userId);
                            }
                            catch (Exception ex)
                            {
                                // Log metadata extraction error but continue
                                result.Data.ErrorMessages = result.Data.ErrorMessages ?? new List<string>();
                                result.Data.ErrorMessages.Add($"Metadata extraction failed for {image.ImFileName}: {ex.Message}");
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = result.OutputCode == 1,
                    message = result.OutputMsg,
                    data = result.Data
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error processing folder: {ex.Message}" });
            }
        }

        // Helper methods for image processing
        private string GetBaseName(string fileName)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            // Extract base name (e.g., "1" from "1-2.jpg" or "1+3.png")
            var match = System.Text.RegularExpressions.Regex.Match(nameWithoutExt, @"^(\d+)");
            return match.Success ? match.Groups[1].Value : nameWithoutExt;
        }

        private bool IsMasterImage(string fileName)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            // Master images are pure numbers (e.g., "1.jpg", "2.png")
            return System.Text.RegularExpressions.Regex.IsMatch(nameWithoutExt, @"^\d+$");
        }

        private (string level, string type) ParseQualityInfo(string fileName)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var match = System.Text.RegularExpressions.Regex.Match(nameWithoutExt, @"^(\d+)([-+])(\d+)");
            
            if (match.Success)
            {
                var level = match.Groups[3].Value;
                var type = match.Groups[2].Value == "+" ? "enhanced" : "degraded";
                return ($"{match.Groups[2].Value}{level}", type);
            }
            
            return ("pair", "pair");
        }

        // Helper method to convert various active formats to int
        private int ConvertToIntActive(JsonElement activeElement)
        {
            if (activeElement.ValueKind == JsonValueKind.String)
            {
                var str = activeElement.GetString()?.ToUpper();
                return str == "Y" || str == "YES" || str == "TRUE" || str == "1" ? 1 : 0;
            }
            else if (activeElement.ValueKind == JsonValueKind.Number)
            {
                return activeElement.GetInt32();
            }
            else if (activeElement.ValueKind == JsonValueKind.True)
            {
                return 1;
            }
            else if (activeElement.ValueKind == JsonValueKind.False)
            {
                return 0;
            }
            return 1; // Default to active
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _dashboardRepository.GetDashboardStats(userId);
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg,
                data = result.Data
            });
        }

        [HttpGet]
        public IActionResult AssessmentImages()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetImagesWithAuditTrail(string assessmentType)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _imageRepository.GetImagesWithAuditTrail(assessmentType, userId);
            
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg,
                data = result.Data
            });
        }

        // Speed Test Logs
        [HttpGet]
        public IActionResult SpeedTestLogs()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSpeedTestLogs()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _speedTestRepository.GetAllSpeedTestLogs(userId);
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg,
                data = result.Data
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetSpeedTestLogsByAssessment(string assessmentCode)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _speedTestRepository.GetSpeedTestLogsByAssessment(assessmentCode, userId);
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg,
                data = result.Data
            });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadSpeedTestLogsExcel(string? assessmentCode, DateTime? startDate, DateTime? endDate)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                SpeedTestLogResponse result;
                
                if (!string.IsNullOrEmpty(assessmentCode))
                {
                    result = await _speedTestRepository.GetSpeedTestLogsByAssessment(assessmentCode, userId);
                }
                else if (startDate.HasValue && endDate.HasValue)
                {
                    result = await _speedTestRepository.GetSpeedTestLogsByDateRange(startDate.Value, endDate.Value, userId);
                }
                else
                {
                    result = await _speedTestRepository.GetAllSpeedTestLogs(userId);
                }

                if (result.OutputCode != 1 || result.Data == null || !result.Data.Any())
                {
                    TempData["ErrorMessage"] = "No data found to export";
                    return RedirectToAction("SpeedTestLogs");
                }

                // Create Excel file using NPOI
                var workbook = new XSSFWorkbook();
                var sheet = workbook.CreateSheet("Speed Test Logs");

                // Create header row
                var headerRow = sheet.CreateRow(0);
                var headerStyle = workbook.CreateCellStyle();
                var headerFont = workbook.CreateFont();
                headerFont.IsBold = true;
                headerStyle.SetFont(headerFont);

                string[] headers = {
                    "ID", "Session ID", "Assessment Code", "Test Date/Time", 
                    "IP Address", "Private Mode", "Browser", "Device Type",
                    "Screen Width", "Screen Height", "Resolution Passed",
                    "Download Speed (Mbps)", "Upload Speed (Mbps)", "Latency (ms)",
                    "Speed Passed", "Overall Passed", "User Agent", "Referrer URL"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = headerStyle;
                }

                // Fill data rows
                int rowIndex = 1;
                foreach (var log in result.Data)
                {
                    var row = sheet.CreateRow(rowIndex++);
                    row.CreateCell(0).SetCellValue(log.Id);
                    row.CreateCell(1).SetCellValue(log.SessionId ?? "");
                    row.CreateCell(2).SetCellValue(log.AssessmentCode ?? "");
                    row.CreateCell(3).SetCellValue(log.TestDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "");
                    row.CreateCell(4).SetCellValue(log.IpAddress ?? "");
                    row.CreateCell(5).SetCellValue(log.PrivateModeDetected ? "Yes" : "No");
                    row.CreateCell(6).SetCellValue(log.PrivateModeBrowser ?? "");
                    row.CreateCell(7).SetCellValue(log.DeviceType ?? "");
                    row.CreateCell(8).SetCellValue(log.ScreenWidth?.ToString() ?? "");
                    row.CreateCell(9).SetCellValue(log.ScreenHeight?.ToString() ?? "");
                    row.CreateCell(10).SetCellValue(log.ResolutionPassed > 0 ? "Yes" : "No");
                    row.CreateCell(11).SetCellValue(log.DownloadSpeedMbps?.ToString("F2") ?? "");
                    row.CreateCell(12).SetCellValue(log.UploadSpeedMbps?.ToString("F2") ?? "");
                    row.CreateCell(13).SetCellValue(log.Latency?.ToString() ?? "");
                    row.CreateCell(14).SetCellValue(log.SpeedPassed ? "Yes" : "No");
                    row.CreateCell(15).SetCellValue(log.OverallPassed ? "Yes" : "No");
                    row.CreateCell(16).SetCellValue(log.UserAgent ?? "");
                    row.CreateCell(17).SetCellValue(log.ReferrerUrl ?? "");
                }

                // Auto-size columns
                for (int i = 0; i < headers.Length; i++)
                {
                    sheet.AutoSizeColumn(i);
                }

                // Write to memory stream
                using var memoryStream = new MemoryStream();
                workbook.Write(memoryStream);
                var fileName = $"SpeedTestLogs_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                
                return File(memoryStream.ToArray(), 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel file");
                TempData["ErrorMessage"] = "Error generating Excel file";
                return RedirectToAction("SpeedTestLogs");
            }
        }

        // Question Answers View
        [HttpGet]
        public IActionResult QuestionAnswers()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllQuestionAnswers()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _questionAnswerRepository.GetAllQuestionAnswers(userId);
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg,
                data = result.Data
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetQuestionAnswersByCode(string assessmentCode)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _questionAnswerRepository.GetQuestionAnswersByCode(assessmentCode, userId);
            return Json(new
            {
                success = result.OutputCode == 1,
                message = result.OutputMsg,
                data = result.Data
            });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadQuestionAnswersExcel(string assessmentCode)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                // Get all questions for this assessment to create column headers
                var questionsQuery = await _questionMasterRepository.GetAllQuestions(userId);
                var questions = questionsQuery.Data.Where(q => q.QsActive == 1).OrderBy(q => q.QsOrderNo).ToList();

                // Get all responses data
                var responseData = await _questionAnswerRepository.GetQuestionAnswersForExcel(assessmentCode, userId);

                if (!responseData.Any())
                {
                    TempData["ErrorMessage"] = "No data found to export";
                    return RedirectToAction("QuestionAnswers");
                }

                // Create Excel workbook
                var workbook = new XSSFWorkbook();
                var sheet = workbook.CreateSheet($"QuestionAnswers_{assessmentCode}");

                // Create header style
                var headerStyle = workbook.CreateCellStyle();
                var headerFont = workbook.CreateFont();
                headerFont.IsBold = true;
                headerFont.Color = IndexedColors.White.Index;
                headerStyle.SetFont(headerFont);
                headerStyle.FillForegroundColor = IndexedColors.DarkBlue.Index;
                headerStyle.FillPattern = FillPattern.SolidForeground;
                headerStyle.Alignment = HorizontalAlignment.Center;
                headerStyle.VerticalAlignment = VerticalAlignment.Center;

                // Get all unique option names for all questions
                var allOptionNames = new HashSet<string>();
                foreach (var question in questions)
                {
                    var optionsResult = await _questionMasterRepository.GetQuestionWithOptions(question.QId, userId);
                    if (optionsResult.Data != null)
                    {
                        var questionWithOptions = optionsResult.Data;
                        if (questionWithOptions != null)
                        {
                            foreach (var option in questionWithOptions.Options.Where(o => o.QoActive == 1))
                            {
                                allOptionNames.Add(option.QoText);
                            }
                        }
                    }
                }

                var optionNamesList = allOptionNames.OrderBy(o => o).ToList();

                // Create header row
                var headerRow = sheet.CreateRow(0);
                var columnHeaders = new List<string> 
                { 
                    "Session ID", 
                    "IP Address", 
                    "Submit Time", 
                    "Question Code", 
                    "Question Text", 
                    "Question Type"
                };
                columnHeaders.AddRange(optionNamesList);

                for (int i = 0; i < columnHeaders.Count; i++)
                {
                    var cell = headerRow.CreateCell(i);
                    cell.SetCellValue(columnHeaders[i]);
                    cell.CellStyle = headerStyle;
                }

                // Fill data rows
                int rowIndex = 1;
                
                // Group by session to process each response
                var groupedBySession = responseData.GroupBy(r => new { r.SessionId, r.SubmitTime });

                foreach (var sessionGroup in groupedBySession)
                {
                    foreach (var dataRow in sessionGroup)
                    {
                        var row = sheet.CreateRow(rowIndex++);
                        
                        int colIndex = 0;
                        row.CreateCell(colIndex++).SetCellValue(dataRow.SessionId ?? "");
                        row.CreateCell(colIndex++).SetCellValue(dataRow.IpAddress ?? "");
                        row.CreateCell(colIndex++).SetCellValue(dataRow.SubmitTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "");
                        row.CreateCell(colIndex++).SetCellValue(dataRow.QuestionCode ?? "");
                        row.CreateCell(colIndex++).SetCellValue(dataRow.QuestionText ?? "");
                        row.CreateCell(colIndex++).SetCellValue(dataRow.QuestionType ?? "");

                        // Get selected options for this question/response
                        var questionId = questions.FirstOrDefault(q => q.QsCode == dataRow.QuestionCode)?.QId ?? 0;
                        if (questionId > 0)
                        {
                            var optionsResult = await _questionMasterRepository.GetQuestionWithOptions(questionId, userId);
                            if (optionsResult.Data != null)
                            {
                                var questionWithOptions = optionsResult.Data;
                                if (questionWithOptions != null)
                                {
                                    // FIXED: Correct column names for all tables
                                    var selectedOptionsQuery = @"
                                        SELECT GROUP_CONCAT(qo.QoText ORDER BY qo.QoOrderNo SEPARATOR ', ') as SelectedOptions
                                        FROM tbl_question_answer_details urd
                                        INNER JOIN tbl_question_answers ur ON urd.UrdUrid = ur.Urid
                                        LEFT JOIN tbl_question_options qo ON urd.UrdQoId = qo.QoId
                                        WHERE ur.UrSessionid = @sessionId 
                                        AND urd.UrdQoid = @questionId
                                        AND ur.UrAssessmentCode = @assessmentCode";

                                    var selectedParams = new[]
                                    {
                                        new MySqlParameter("@sessionId", dataRow.SessionId),
                                        new MySqlParameter("@questionId", questionId),
                                        new MySqlParameter("@assessmentCode", assessmentCode)
                                    };

                                    var result = await Task.Run(() => _dbHelper.ExecuteQuery(selectedOptionsQuery, selectedParams));
                                    
                                    string selectedOptionsText = "";
                                    if (result.Rows.Count > 0)
                                    {
                                        selectedOptionsText = result.Rows[0]["SelectedOptions"]?.ToString() ?? "";
                                    }

                                    var selectedOptionsList = selectedOptionsText.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList();

                                    // Mark selected options in their respective columns
                                    foreach (var optionName in optionNamesList)
                                    {
                                        var cellValue = selectedOptionsList.Contains(optionName) ? "✓" : "";
                                        row.CreateCell(colIndex++).SetCellValue(cellValue);
                                    }
                                }
                            }
                        }
                    }
                }

                // Auto-size columns
                for (int i = 0; i < columnHeaders.Count; i++)
                {
                    sheet.AutoSizeColumn(i);
                    // Set maximum width to prevent extremely wide columns
                    if (sheet.GetColumnWidth(i) > 15000)
                    {
                        sheet.SetColumnWidth(i, 15000);
                    }
                }

                // Write to memory stream
                using var memoryStream = new MemoryStream();
                workbook.Write(memoryStream);
                var fileName = $"QuestionAnswers_{assessmentCode}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                
                return File(memoryStream.ToArray(), 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating question answers Excel file");
                TempData["ErrorMessage"] = $"Error generating Excel file: {ex.Message}";
                return RedirectToAction("QuestionAnswers");
            }
        }
    }

    public class FolderProcessRequest
    {
        public string FolderPath { get; set; }
        public string AssessmentType { get; set; }
    }
}