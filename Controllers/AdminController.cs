using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using IQA_SOURCE.Data;
using IQA_SOURCE.Models.Admin;
using System.Text.Json;
using IQA_SOURCE.Services;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;
using MySqlConnector;
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
        private readonly IDbHelper _dbHelper;
        private readonly IImageQualityRepository _imageQualityRepository;
        private readonly ISystemCheckParamRepository _systemCheckParamRepository;
        private readonly IImageGroupRepository _imageGroupRepository;
        private readonly IBulkOperationsRepository _bulkOperationsRepository;
        private readonly IAdminUserRepository _adminUserRepository;
        private readonly IAdminMenuRepository _adminMenuRepository;

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
            IImageQualityRepository imageQualityRepository,
            ISystemCheckParamRepository systemCheckParamRepository,
            IImageGroupRepository imageGroupRepository,
            IBulkOperationsRepository bulkOperationsRepository,
            ILogger<AdminController> logger,
            IDbHelper dbHelper,
            IAdminUserRepository adminUserRepository,
            IAdminMenuRepository adminMenuRepository)
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
            _imageQualityRepository = imageQualityRepository;
            _systemCheckParamRepository = systemCheckParamRepository;
            _imageGroupRepository = imageGroupRepository;
            _bulkOperationsRepository = bulkOperationsRepository;
            _logger = logger;
            _dbHelper = dbHelper;
            _adminUserRepository = adminUserRepository;
            _adminMenuRepository = adminMenuRepository;
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
                HttpContext.Session.SetString("UserRole", result.UserRole);  // Add this line
            }

            return Json(new { success = result.Success, message = result.Message });
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
                return RedirectToAction("Login");

            return View();
        }

        // Content Master
        [HttpGet]
        public IActionResult ContentMaster()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllContents()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _adminRepository.GetAllContents(userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetContentByCode(string code)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _adminRepository.GetContentByCode(code, userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data.FirstOrDefault() });
        }

        [HttpPost]
        public async Task<IActionResult> SaveContent([FromBody] ContentMaster model)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            ContentMasterResponse result;
            var existing = await _adminRepository.GetContentByCode(model.CmCode, userId);
            result = existing.Data.Any()
                ? await _adminRepository.UpdateContent(model, userId)
                : await _adminRepository.InsertContent(model, userId);

            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteContent([FromBody] string code)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _adminRepository.DeleteContent(code, userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg });
        }

        // Assessment Type Master
        [HttpGet]
        public IActionResult AssessmentTypeMaster()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAssessmentTypes()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Un    authorized" });

            var result = await _assessmentTypeRepository.GetAllAssessmentTypes(userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetAssessmentTypeByCode(string code)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _assessmentTypeRepository.GetAssessmentTypeByCode(code, userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data.FirstOrDefault() });
        }

        [HttpPost]
        public async Task<IActionResult> SaveAssessmentType([FromBody] AssessmentTypeMaster model)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            AssessmentTypeMasterResponse result;
            var existing = await _assessmentTypeRepository.GetAssessmentTypeByCode(model.AtmCode, userId);
            result = existing.Data.Any()
                ? await _assessmentTypeRepository.UpdateAssessmentType(model, userId)
                : await _assessmentTypeRepository.InsertAssessmentType(model, userId);

            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAssessmentType([FromBody] string code)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _assessmentTypeRepository.DeleteAssessmentType(code, userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg });
        }

        // Question Master
        [HttpGet]
        public IActionResult QuestionMaster()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllQuestions()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _questionMasterRepository.GetAllQuestions(userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetQuestionWithOptions(int qId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _questionMasterRepository.GetQuestionWithOptions(qId, userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> SaveQuestion([FromBody] JsonElement model)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var question = new QuestionMaster
                {
                    QId             = model.TryGetProperty("qsId",            out var qId)            ? qId.GetInt32()                                         : 0,
                    QsCode          = model.TryGetProperty("qsCode",          out var qsCode)          ? qsCode.GetString()                                     : null,
                    QsText          = model.TryGetProperty("qsText",          out var qsText)          ? qsText.GetString()                                     : null,
                    QsType          = model.TryGetProperty("qsType",          out var qsType)          ? qsType.GetString()                                     : null,
                    QsCategory      = model.TryGetProperty("qsCategory",      out var qsCategory)      ? qsCategory.GetString()                                 : null,
                    QsMaxSelections = model.TryGetProperty("qsMaxSelections", out var qsMaxSelections) && qsMaxSelections.ValueKind != JsonValueKind.Null ? qsMaxSelections.GetInt32()  : (int?)null,
                    QsOrderNo       = model.TryGetProperty("qsOrderNo",       out var qsOrderNo)       && qsOrderNo.ValueKind       != JsonValueKind.Null ? qsOrderNo.GetInt32()        : (int?)null,
                    QsActive        = model.TryGetProperty("qsActive",        out var qsActive)        ? ConvertToIntActive(qsActive)                          : 1
                };

                QuestionMasterResponse result = question.QId > 0
                    ? await _questionMasterRepository.UpdateQuestion(question, userId)
                    : await _questionMasterRepository.InsertQuestion(question, userId);

                return Json(new { success = result.OutputCode == 1, message = result.OutputMsg });
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
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _questionMasterRepository.DeleteQuestion(qId, userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg });
        }

        [HttpPost]
        public async Task<IActionResult> SaveQuestionOption([FromBody] JsonElement model)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var option = new QuestionOption
                {
                    QoId      = model.TryGetProperty("qoId",      out var qoId)      ? qoId.GetInt32()                                     : 0,
                    QoQId     = model.TryGetProperty("qoQId",     out var qoQId)     ? qoQId.GetInt32()                                    : 0,
                    QoText    = model.TryGetProperty("qoText",    out var qoText)    ? qoText.GetString()                                  : null,
                    QoValue   = model.TryGetProperty("qoValue",   out var qoValue)   ? qoValue.GetString()                                 : null,
                    QoOrderNo = model.TryGetProperty("qoOrderNo", out var qoOrderNo) && qoOrderNo.ValueKind != JsonValueKind.Null ? qoOrderNo.GetInt32() : (int?)null,
                    QoActive  = model.TryGetProperty("qoActive",  out var qoActive)  ? ConvertToIntActive(qoActive)                       : 1
                };

                QuestionMasterResponse result = option.QoId > 0
                    ? await _questionMasterRepository.UpdateQuestionOption(option, userId)
                    : await _questionMasterRepository.InsertQuestionOption(option, userId);

                return Json(new { success = result.OutputCode == 1, message = result.OutputMsg });
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
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _questionMasterRepository.DeleteQuestionOption(qoId, userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg });
        }

        [HttpPost]
        [RequestSizeLimit(2147483648)]
        [RequestFormLimits(MultipartBodyLengthLimit = 2147483648, ValueCountLimit = 100000)]
            public async Task<IActionResult> UploadImages([FromForm] string assessmentType, [FromForm] string groupCode, [FromForm] IFormFile[] files)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            if (files == null || files.Length == 0)
                return Json(new { success = false, message = "No files uploaded" });

            // Validate group code
            if (string.IsNullOrWhiteSpace(groupCode))
                return Json(new { success = false, message = "Image group code is required" });

            try
            {
                var uploadBatch = Guid.NewGuid().ToString();
                var uploadPath  = Path.Combine(_imageSettings.BasePath, assessmentType);

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var masterImages   = new List<ImageMaster>();
                var linkedImages   = new List<ImageLinked>();
                var skippedMasters = new List<string>();
                var errors         = new List<string>();
                var fileGroups     = files.GroupBy(f => GetBaseName(f.FileName)).ToList();

                foreach (var group in fileGroups)
                {
                    var masterFile = group.FirstOrDefault(f => IsMasterImage(f.FileName));
                    if (masterFile == null) continue;

                    var masterFileName = Path.GetFileName(masterFile.FileName);
                    var masterExists   = await _imageRepository.CheckDuplicateFileName(assessmentType, masterFileName);

                    if (masterExists)
                    {
                        skippedMasters.Add(masterFileName);
                        var existingMaster = await _imageRepository.GetMasterImageByFileName(assessmentType, masterFileName, userId);
                        if (existingMaster == null) continue;

                        foreach (var linkedFile in group.Where(f => !IsMasterImage(f.FileName)))
                        {
                            var linkedFileName = Path.GetFileName(linkedFile.FileName);
                            var linkedExists   = await _imageRepository.CheckLinkedImageExists(existingMaster.ImId, linkedFileName);
                            if (linkedExists) continue;

                            try
                            {
                                var linkedFilePath = Path.Combine(uploadPath, linkedFileName);
                                using (var stream = new FileStream(linkedFilePath, FileMode.Create))
                                    await linkedFile.CopyToAsync(stream);

                                var linkedMetadata               = await _metadataService.ExtractMetadata(linkedFilePath);
                                var (qualityLevel, qualityType)  = ParseQualityInfo(linkedFileName);
                                var webPath                      = $"{_imageSettings.WebBasePath}/{assessmentType}/{linkedFileName}";

                                linkedImages.Add(BuildLinkedImage(existingMaster.ImId, linkedFileName, webPath, linkedFile.Length, linkedMetadata, qualityLevel, qualityType, uploadBatch, groupCode));
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Error processing linked image {linkedFileName}: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            var masterFilePath = Path.Combine(uploadPath, masterFileName);
                            using (var stream = new FileStream(masterFilePath, FileMode.Create))
                                await masterFile.CopyToAsync(stream);

                            var metadata = await _metadataService.ExtractMetadata(masterFilePath);
                            var webPath  = $"{_imageSettings.WebBasePath}/{assessmentType}/{masterFileName}";
                            masterImages.Add(BuildMasterImage(assessmentType, masterFileName, webPath, masterFile.Length, metadata, uploadBatch, groupCode));

                            foreach (var linkedFile in group.Where(f => !IsMasterImage(f.FileName)))
                            {
                                try
                                {
                                    var linkedFileName = Path.GetFileName(linkedFile.FileName);
                                    var linkedFilePath = Path.Combine(uploadPath, linkedFileName);
                                    using (var stream = new FileStream(linkedFilePath, FileMode.Create))
                                        await linkedFile.CopyToAsync(stream);

                                    var linkedMetadata              = await _metadataService.ExtractMetadata(linkedFilePath);
                                    var (qualityLevel, qualityType) = ParseQualityInfo(linkedFileName);
                                    var webPath1                    = $"{_imageSettings.WebBasePath}/{assessmentType}/{linkedFileName}";

                                    linkedImages.Add(BuildLinkedImage(0, linkedFileName, webPath1, linkedFile.Length, linkedMetadata, qualityLevel, qualityType, uploadBatch, groupCode));
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

                ImageUploadResponse result;
                if (masterImages.Count > 0 || linkedImages.Count > 0)
                {
                    result = await _imageRepository.SaveImages(masterImages, linkedImages, userId);
                    result.Data.SkippedMasterImages   = skippedMasters.Count;
                    result.Data.SkippedMasterFileNames = skippedMasters;
                    if (errors.Count > 0)
                    {
                        result.Data.ErrorMessages ??= new List<string>();
                        result.Data.ErrorMessages.AddRange(errors);
                    }
                }
                else
                {
                    result = new ImageUploadResponse
                    {
                        OutputCode = 1,
                        OutputMsg  = "No new images to upload. All master images already exist.",
                        Data       = new ImageUploadResult
                        {
                            TotalFiles             = files.Length,
                            MasterImagesUploaded   = 0,
                            LinkedImagesUploaded   = 0,
                            SkippedMasterImages    = skippedMasters.Count,
                            SkippedMasterFileNames = skippedMasters,
                            ErrorMessages          = errors
                        }
                    };
                }

                var msg = new System.Text.StringBuilder();
                if (result.Data.MasterImagesUploaded > 0) msg.Append($"{result.Data.MasterImagesUploaded} master image(s) uploaded. ");
                if (result.Data.LinkedImagesUploaded > 0) msg.Append($"{result.Data.LinkedImagesUploaded} linked image(s) uploaded. ");
                if (result.Data.SkippedMasterImages  > 0) msg.Append($"{result.Data.SkippedMasterImages} master image(s) skipped (already exist). ");
                if (errors.Count                     > 0) msg.Append($"{errors.Count} error(s) occurred. ");
                result.OutputMsg = msg.ToString().Trim();

                return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
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
                return RedirectToAction("Login");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetImagesByAssessmentType(string assessmentType)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _imageRepository.GetImagesByAssessmentType(assessmentType, userId);
            
            // ✅ FIX: Enrich images with group names
            if (result.OutputCode == 1 && result.Data != null && result.Data.Count > 0)
            {
                // Get all image groups for mapping
                var groupsResult = await _imageGroupRepository.GetAllImageGroups(userId);
                var groupsMap = groupsResult.Data?
                    .ToDictionary(g => g.IgCode, g => g.IgName)
                    ?? new Dictionary<string, string>();

                // Enrich each image with its group name
                foreach (var image in result.Data)
                {
                    if (!string.IsNullOrEmpty(image.ImGroupCode) && string.IsNullOrEmpty(image.ImGroupName))
                    {
                        if (groupsMap.TryGetValue(image.ImGroupCode, out var groupName))
                        {
                            image.ImGroupName = groupName;
                        }
                    }
                }
            }
            
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetImagesByAssessmentTypeAndGroup(string assessmentType, string? groupCode)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var result = await _imageRepository.GetImagesByAssessmentTypeAndGroup(assessmentType, groupCode, userId);
                
                // ✅ FIX: Enrich images with group names (same logic as dashboard)
                if (result.OutputCode == 1 && result.Data != null && result.Data.Count > 0)
                {
                    // Get all image groups for mapping
                    var groupsResult = await _imageGroupRepository.GetAllImageGroups(userId);
                    var groupsMap = groupsResult.Data?
                        .ToDictionary(g => g.IgCode, g => g.IgName)
                        ?? new Dictionary<string, string>();

                    // Enrich each image with its group name
                    foreach (var image in result.Data)
                    {
                        // If groupCode is set but groupName is empty, look it up
                        if (!string.IsNullOrEmpty(image.ImGroupCode) && string.IsNullOrEmpty(image.ImGroupName))
                        {
                            if (groupsMap.TryGetValue(image.ImGroupCode, out var groupName))
                            {
                                image.ImGroupName = groupName;
                            }
                        }
                    }
                }
                
                return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage([FromBody] int imageId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _imageRepository.DeleteImage(imageId, userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg });
        }

        // Folder Processing
        [HttpPost]
        public async Task<IActionResult> ProcessFolderImages([FromBody] FolderProcessRequest request)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            if (string.IsNullOrEmpty(request.FolderPath))
                return Json(new { success = false, message = "Folder path is required" });

            if (string.IsNullOrEmpty(request.AssessmentType))
                return Json(new { success = false, message = "Assessment type is required" });

            try
            {
                var result = await _imageRepository.ProcessFolderImages(request.FolderPath, request.AssessmentType, userId);

                if (result.OutputCode == 1 && result.Data != null)
                {
                    var imagesToProcess = await _imageRepository.GetImagesByAssessmentType(request.AssessmentType, userId);
                    foreach (var image in imagesToProcess.Data)
                    {
                        if (!System.IO.File.Exists(image.ImFilePath)) continue;
                        try
                        {
                            var metadata = await _metadataService.ExtractMetadata(image.ImFilePath);
                            image.ImWidth              = metadata.Width;
                            image.ImHeight             = metadata.Height;
                            image.ImFormat             = metadata.Format;
                            image.ImColorSpace         = metadata.ColorSpace;
                            image.ImBitDepth           = metadata.BitDepth;
                            image.ImDpiX               = metadata.DpiX;
                            image.ImDpiY               = metadata.DpiY;
                            image.ImExifData           = JsonSerializer.Serialize(metadata.ExifData);
                            image.ImCameraMake         = metadata.CameraMake;
                            image.ImCameraModel        = metadata.CameraModel;
                            image.ImLensModel          = metadata.LensModel;
                            image.ImFocalLength        = metadata.FocalLength;
                            image.ImAperture           = metadata.Aperture;
                            image.ImShutterSpeed       = metadata.ShutterSpeed;
                            image.ImIso                = metadata.Iso;
                            image.ImFlash              = metadata.Flash;
                            image.ImExposureMode       = metadata.ExposureMode;
                            image.ImWhiteBalance       = metadata.WhiteBalance;
                            image.ImDateTaken          = metadata.DateTaken;
                            image.ImOrientation        = metadata.Orientation;
                            image.ImCompressionQuality = metadata.CompressionQuality;
                        }
                        catch (Exception ex)
                        {
                            result.Data.ErrorMessages ??= new List<string>();
                            result.Data.ErrorMessages.Add($"Metadata extraction failed for {image.ImFileName}: {ex.Message}");
                        }
                    }
                }

                return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error processing folder: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _dashboardRepository.GetDashboardStats(userId);
            
            // ✅ FIX: Enrich dashboard stats with group names
            if (result.OutputCode == 1 && result.Data != null)
            {
                // Get all image groups for mapping
                var groupsResult = await _imageGroupRepository.GetAllImageGroups(userId);
                var groupsMap = groupsResult.Data?
                    .ToDictionary(g => g.IgCode, g => g.IgName) 
                    ?? new Dictionary<string, string>();

                // Enrich assessment image summaries with group names
                if (result.Data.AssessmentImageSummaries != null && result.Data.AssessmentImageSummaries.Count > 0)
                {
                    foreach (var summary in result.Data.AssessmentImageSummaries)
                    {
                        // If groupCode is set but groupName is empty, look it up
                        if (!string.IsNullOrEmpty(summary.GroupCode) && string.IsNullOrEmpty(summary.GroupName))
                        {
                            if (groupsMap.TryGetValue(summary.GroupCode, out var groupName))
                            {
                                summary.GroupName = groupName;
                            }
                        }
                    }
                }
            }

            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
        }

        [HttpGet]
        public IActionResult AssessmentImages()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetImagesWithAuditTrail(string assessmentType)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _imageRepository.GetImagesWithAuditTrail(assessmentType, userId);
            
            // Enrich result data with group names
            if (result.OutputCode == 1 && result.Data != null)
            {
                // Get all image groups
                var groupsResult = await _imageGroupRepository.GetAllImageGroups(userId);
                var groupsMap = groupsResult.Data?
                    .ToDictionary(g => g.IgCode, g => g.IgName) 
                    ?? new Dictionary<string, string>();

                // Add group names to each image
                foreach (var image in result.Data)
                {
                    if (!string.IsNullOrEmpty(image.ImGroupCode) && groupsMap.TryGetValue(image.ImGroupCode, out var groupName))
                    {
                        image.ImGroupName = groupName;
                    }
                    
                    if (image.LinkedImages != null)
                    {
                        foreach (var linked in image.LinkedImages)
                        {
                            if (!string.IsNullOrEmpty(linked.IlGroupCode) && groupsMap.TryGetValue(linked.IlGroupCode, out var linkedGroupName))
                            {
                                linked.IlGroupName = linkedGroupName;
                            }
                        }
                    }
                }
            }

            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllImageRatings(string? assessmentCode)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var (outputCode, outputMsg, data) = await _imageQualityRepository.GetImageRatingsForAdmin(assessmentCode, userId);
            
            // Enrich data with group names if available
            if (outputCode == 1 && data != null && data.Count > 0)
            {
                // Get all image groups
                var groupsResult = await _imageGroupRepository.GetAllImageGroups(userId);
                var groupsMap = groupsResult.Data?
                    .ToDictionary(g => g.IgCode, g => g.IgName) 
                    ?? new Dictionary<string, string>();

                // Add group names to each rating - using master group code
                foreach (var rating in data)
                {
                    if (string.IsNullOrEmpty(rating.GroupName))
                    {
                        if (!string.IsNullOrEmpty(rating.MasterGroupCode) && groupsMap.TryGetValue(rating.MasterGroupCode, out var groupName))
                        {
                            rating.GroupName = groupName;
                        }
                        else
                        {
                            rating.GroupName = "N/A";
                        }
                    }
                }
            }

            return Json(new { success = outputCode == 1, message = outputMsg, data });
        }

        // Speed Test Logs
        [HttpGet]
        public IActionResult SpeedTestLogs()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSpeedTestLogs()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _speedTestRepository.GetAllSpeedTestLogs(userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetSpeedTestLogsByAssessment(string assessmentCode)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _speedTestRepository.GetSpeedTestLogsByAssessment(assessmentCode, userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadSpeedTestLogsExcel(string? assessmentCode, DateTime? startDate, DateTime? endDate)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                SpeedTestLogResponse result;
                if (!string.IsNullOrEmpty(assessmentCode))
                    result = await _speedTestRepository.GetSpeedTestLogsByAssessment(assessmentCode, userId);
                else if (startDate.HasValue && endDate.HasValue)
                    result = await _speedTestRepository.GetSpeedTestLogsByDateRange(startDate.Value, endDate.Value, userId);
                else
                    result = await _speedTestRepository.GetAllSpeedTestLogs(userId);

                if (result.OutputCode != 1 || result.Data == null || !result.Data.Any())
                {
                    TempData["ErrorMessage"] = "No data found to export";
                    return RedirectToAction("SpeedTestLogs");
                }

                var workbook = new XSSFWorkbook();
                var sheet    = workbook.CreateSheet("Speed Test Logs");

                var headerRow   = sheet.CreateRow(0);
                var headerStyle = workbook.CreateCellStyle();
                var headerFont  = workbook.CreateFont();
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

                for (int i = 0; i < headers.Length; i++)
                    sheet.AutoSizeColumn(i);

                using var memoryStream = new MemoryStream();
                workbook.Write(memoryStream);
                return File(memoryStream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"SpeedTestLogs_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel file");
                TempData["ErrorMessage"] = "Error generating Excel file";
                return RedirectToAction("SpeedTestLogs");
            }
        }

        // System Check Parameters
        [HttpGet]
        public IActionResult SystemCheckParams()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSystemCheckParams()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _systemCheckParamRepository.GetAllParams(userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> UpsertSystemCheckParam([FromBody] SystemCheckParam model)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            if (string.IsNullOrWhiteSpace(model.ScpParamCode) || string.IsNullOrWhiteSpace(model.ScpParamValue))
                return Json(new { success = false, message = "Param code and value are required" });

            var result = await _systemCheckParamRepository.UpsertParam(model, userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg });
        }

        // Question Answers View
        [HttpGet]
        public IActionResult QuestionAnswers()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllQuestionAnswers()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _questionAnswerRepository.GetAllQuestionAnswers(userId);
            
            // Enrich with image group names for question answers
            if (result.OutputCode == 1 && result.Data != null && result.Data.Count > 0)
            {
                // Get all image groups
                var groupsResult = await _imageGroupRepository.GetAllImageGroups(userId);
                var groupsMap = groupsResult.Data?
                    .ToDictionary(g => g.IgCode, g => g.IgName)
                    ?? new Dictionary<string, string>();

                // Group by session and get first image group for each session
                var sessionGroupMap = new Dictionary<string, string>();
                var imageRatingsResult = await _imageQualityRepository.GetImageRatingsForAdmin(null, userId);
                var (_, _, irData) = imageRatingsResult;

                if (irData != null && irData.Count > 0)
                {
                    foreach (var rating in irData)
                    {
                        var sessionKey = rating.SessionId;
                        if (!sessionGroupMap.ContainsKey(sessionKey) && !string.IsNullOrEmpty(rating.GroupName))
                        {
                            sessionGroupMap[sessionKey] = rating.GroupName;
                        }
                    }
                }

                // Add group name to question answers
                foreach (var qa in result.Data)
                {
                    if (!string.IsNullOrEmpty(qa.SessionId) && sessionGroupMap.TryGetValue(qa.SessionId, out var groupName))
                    {
                        qa.GroupName = groupName;
                    }
                    else
                    {
                        qa.GroupName = "N/A";
                    }
                }
            }

            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetQuestionAnswersByCode(string assessmentCode)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _questionAnswerRepository.GetQuestionAnswersByCode(assessmentCode, userId);
            
            // Enrich with image group names for question answers
            if (result.OutputCode == 1 && result.Data != null && result.Data.Count > 0)
            {
                // Get image ratings to extract first group name per session
                var imageRatingsResult = await _imageQualityRepository.GetImageRatingsForAdmin(assessmentCode, userId);
                var (_, _, irData) = imageRatingsResult;

                var sessionGroupMap = new Dictionary<string, string>();
                if (irData != null && irData.Count > 0)
                {
                    foreach (var rating in irData)
                    {
                        var sessionKey = rating.SessionId;
                        if (!sessionGroupMap.ContainsKey(sessionKey) && !string.IsNullOrEmpty(rating.GroupName))
                        {
                            sessionGroupMap[sessionKey] = rating.GroupName;
                        }
                    }
                }

                // Add group name to question answers
                foreach (var qa in result.Data)
                {
                    if (!string.IsNullOrEmpty(qa.SessionId) && sessionGroupMap.TryGetValue(qa.SessionId, out var groupName))
                    {
                        qa.GroupName = groupName;
                    }
                    else
                    {
                        qa.GroupName = "N/A";
                    }
                }
            }

            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadQuestionAnswersExcel(string assessmentCode)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var responseData = await _questionAnswerRepository.GetQuestionAnswersForExcel(assessmentCode, userId);
                if (responseData.Count == 0)
                {
                    TempData["ErrorMessage"] = "No data found to export";
                    return RedirectToAction("QuestionAnswers");
                }

                var questionsQuery   = await _questionMasterRepository.GetAllQuestions(userId);
                var questions        = questionsQuery.Data.Where(q => q.QsActive == 1).OrderBy(q => q.QsOrderNo).ToList();
                var respondedCodes   = responseData.Select(r => r.QuestionCode).ToHashSet();
                var orderedQuestions = questions.Where(q => respondedCodes.Contains(q.QsCode)).ToList();

                var sessionGroups = responseData
                    .GroupBy(r => new { r.SessionId, r.IpAddress, r.SubmitTime })
                    .OrderByDescending(g => g.Key.SubmitTime)
                    .Select(g => new
                    {
                        g.Key.SessionId,
                        g.Key.IpAddress,
                        g.Key.SubmitTime,
                        AnswersByQuestion = g.ToDictionary(r => r.QuestionCode ?? "", r => r.SelectedOptions ?? "")
                    }).ToList();

                var workbook    = new XSSFWorkbook();
                var sheet       = workbook.CreateSheet($"QuestionAnswers_{assessmentCode}");
                var headerStyle = CreateHeaderStyle(workbook);
                var dataStyle   = CreateDataStyle(workbook);
                var altStyle    = CreateAltRowStyle(workbook);
                var headerRow   = sheet.CreateRow(0);

                string[] fixedHeaders = ["Session ID", "IP Address", "Submit Time"];
                for (int i = 0; i < fixedHeaders.Length; i++)
                {
                    var cell = headerRow.CreateCell(i);
                    cell.SetCellValue(fixedHeaders[i]);
                    cell.CellStyle = headerStyle;
                }
                for (int i = 0; i < orderedQuestions.Count; i++)
                {
                    var cell = headerRow.CreateCell(fixedHeaders.Length + i);
                    cell.SetCellValue(orderedQuestions[i].QsText ?? orderedQuestions[i].QsCode);
                    cell.CellStyle = headerStyle;
                }
                sheet.CreateFreezePane(0, 1);

                int rowIndex = 1;
                foreach (var session in sessionGroups)
                {
                    var row   = sheet.CreateRow(rowIndex);
                    var style = rowIndex % 2 == 0 ? altStyle : dataStyle;
                    row.CreateCell(0).SetCellValue(session.SessionId ?? ""); row.GetCell(0).CellStyle = style;
                    row.CreateCell(1).SetCellValue(session.IpAddress  ?? ""); row.GetCell(1).CellStyle = style;
                    row.CreateCell(2).SetCellValue(session.SubmitTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""); row.GetCell(2).CellStyle = style;

                    for (int i = 0; i < orderedQuestions.Count; i++)
                    {
                        var qCode     = orderedQuestions[i].QsCode ?? "";
                        var cellValue = session.AnswersByQuestion.TryGetValue(qCode, out var csv) ? csv : "";
                        var cell      = row.CreateCell(fixedHeaders.Length + i);
                        cell.SetCellValue(cellValue);
                        cell.CellStyle = style;
                    }
                    rowIndex++;
                }

                int totalCols = fixedHeaders.Length + orderedQuestions.Count;
                for (int i = 0; i < totalCols; i++)
                {
                    sheet.AutoSizeColumn(i);
                    if (sheet.GetColumnWidth(i) > 15000) sheet.SetColumnWidth(i, 15000);
                }

                // Image Ratings sheet — destructure the tuple return
                var (irCode, irMsg, irData) = await _imageQualityRepository.GetImageRatingsForAdmin(assessmentCode, userId);

                // Get all image groups for mapping
                var groupsResult = await _imageGroupRepository.GetAllImageGroups(userId);
                var groupsMap = groupsResult.Data?
                    .ToDictionary(g => g.IgCode, g => g.IgName) 
                    ?? new Dictionary<string, string>();

                // Enrich data with group names
                if (irData != null && irData.Count > 0)
                {
                    foreach (var rating in irData)
                    {
                        if (string.IsNullOrEmpty(rating.GroupName) && !string.IsNullOrEmpty(rating.MasterGroupCode))
                        {
                            if (groupsMap.TryGetValue(rating.MasterGroupCode, out var groupName))
                            {
                                rating.GroupName = groupName;
                            }
                        }
                    }
                }

                var irSheet     = workbook.CreateSheet("Image Ratings");
                var irHdrStyle  = CreateHeaderStyle(workbook);
                var irDataStyle = CreateDataStyle(workbook);
                var irAltStyle  = CreateAltRowStyle(workbook);

                string[] irHeaders = {
                    "Session ID", "Image Group", "Assessment", "IP Address", "Rated At",
                    "Ref Image", "Ref Resolution", "Ref DPI", "Ref Format",
                    "Main Image Rating", "Main Image Rating Label",
                    "Rated Image", "Rated Resolution", "Rated DPI", "Rated Format",
                    "Quality Level", "Quality Type", "Rating (1–5)", "Rating Label"
                };
                var irHeaderRow = irSheet.CreateRow(0);
                for (int i = 0; i < irHeaders.Length; i++)
                {
                    var cell = irHeaderRow.CreateCell(i);
                    cell.SetCellValue(irHeaders[i]);
                    cell.CellStyle = irHdrStyle;
                }
                irSheet.CreateFreezePane(0, 1);

                int irRowIdx = 1;
                foreach (var row in irData)
                {
                    var r     = irSheet.CreateRow(irRowIdx);
                    var style = irRowIdx % 2 == 0 ? irAltStyle : irDataStyle;
                    r.CreateCell(0).SetCellValue(row.SessionId ?? "");         r.GetCell(0).CellStyle  = style;
                    r.CreateCell(1).SetCellValue(row.GroupName ?? "N/A");      r.GetCell(1).CellStyle  = style;  // IMAGE GROUP NAME (NEW)
                    r.CreateCell(2).SetCellValue(row.AssessmentCode ?? "");    r.GetCell(2).CellStyle  = style;
                    r.CreateCell(3).SetCellValue(row.IpAddress ?? "");         r.GetCell(3).CellStyle  = style;
                    r.CreateCell(4).SetCellValue(row.RatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""); r.GetCell(4).CellStyle = style;
                    r.CreateCell(5).SetCellValue(row.MasterImageName ?? "");   r.GetCell(5).CellStyle  = style;
                    r.CreateCell(6).SetCellValue(row.MasterWidth.HasValue && row.MasterHeight.HasValue ? $"{row.MasterWidth} x {row.MasterHeight}" : ""); r.GetCell(6).CellStyle = style;
                    r.CreateCell(7).SetCellValue(row.MasterDpiX.HasValue ? $"{Math.Round(row.MasterDpiX.Value)} x {Math.Round(row.MasterDpiY ?? 0)}" : ""); r.GetCell(7).CellStyle = style;
                    r.CreateCell(8).SetCellValue(row.MasterFormat ?? "");      r.GetCell(8).CellStyle  = style;
                    // Main image (Sort) rating columns
                    r.CreateCell(9).SetCellValue(row.MasterImageRating.HasValue ? row.MasterImageRating.Value.ToString() : ""); r.GetCell(9).CellStyle = style;
                    r.CreateCell(10).SetCellValue(row.MasterImageRating.HasValue ? row.MasterImageRatingLabel : "");              r.GetCell(10).CellStyle = style;
                    // Linked image columns
                    r.CreateCell(11).SetCellValue(row.LinkedImageName ?? "");   r.GetCell(11).CellStyle = style;
                    r.CreateCell(12).SetCellValue(row.LinkedWidth.HasValue && row.LinkedHeight.HasValue ? $"{row.LinkedWidth} x {row.LinkedHeight}" : ""); r.GetCell(12).CellStyle = style;
                    r.CreateCell(13).SetCellValue(row.LinkedDpiX.HasValue ? $"{Math.Round(row.LinkedDpiX.Value)} x {Math.Round(row.LinkedDpiY ?? 0)}" : ""); r.GetCell(13).CellStyle = style;
                    r.CreateCell(14).SetCellValue(row.LinkedFormat ?? "");       r.GetCell(14).CellStyle = style;
                    r.CreateCell(15).SetCellValue(row.LinkedQualityLevel ?? ""); r.GetCell(15).CellStyle = style;
                    r.CreateCell(16).SetCellValue(row.LinkedQualityType  ?? ""); r.GetCell(16).CellStyle = style;
                    r.CreateCell(17).SetCellValue(row.QualityRating);            r.GetCell(17).CellStyle = style;
                    r.CreateCell(18).SetCellValue(row.QualityRatingLabel);       r.GetCell(18).CellStyle = style;
                    irRowIdx++;
                }

                for (int i = 0; i < irHeaders.Length; i++)
                {
                    irSheet.AutoSizeColumn(i);
                    if (irSheet.GetColumnWidth(i) > 15000) irSheet.SetColumnWidth(i, 15000);
                }

                using var memoryStream = new MemoryStream();
                workbook.Write(memoryStream);
                return File(memoryStream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Assessment_{assessmentCode}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating combined Excel file");
                TempData["ErrorMessage"] = $"Error generating Excel file: {ex.Message}";
                return RedirectToAction("QuestionAnswers");
            }
        }

        // ── Helper methods ──────────────────────────────────────────────────────

        private ImageMaster BuildMasterImage(string assessmentType, string fileName, string webPath, long fileSize, dynamic meta, string uploadBatch, string groupCode) =>
            new()
            {
                ImAssessmentType   = assessmentType,
                ImGroupCode        = groupCode,  // NEW: Add group code
                ImFileName         = fileName,
                ImFilePath         = webPath,
                ImFileSize         = fileSize,
                ImWidth            = meta.Width,
                ImHeight           = meta.Height,
                ImFormat           = meta.Format,
                ImColorSpace       = meta.ColorSpace,
                ImBitDepth         = meta.BitDepth,
                ImDpiX             = meta.DpiX,
                ImDpiY             = meta.DpiY,
                ImExifData         = JsonSerializer.Serialize(meta.ExifData),
                ImCameraMake       = meta.CameraMake,
                ImCameraModel      = meta.CameraModel,
                ImLensModel        = meta.LensModel,
                ImFocalLength      = meta.FocalLength,
                ImAperture         = meta.Aperture,
                ImShutterSpeed     = meta.ShutterSpeed,
                ImIso              = meta.Iso,
                ImFlash            = meta.Flash,
                ImExposureMode     = meta.ExposureMode,
                ImWhiteBalance     = meta.WhiteBalance,
                ImDateTaken        = meta.DateTaken,
                ImOrientation      = meta.Orientation,
                ImCompressionQuality = meta.CompressionQuality,
                ImUploadBatch      = uploadBatch
            };

        private ImageLinked BuildLinkedImage(int masterId, string fileName, string webPath, long fileSize, dynamic meta, string qualityLevel, string qualityType, string uploadBatch, string groupCode) =>
            new()
            {
                IlMasterId         = masterId,
                IlGroupCode        = groupCode,  // NEW: Add group code
                IlFileName         = fileName,
                IlFilePath         = webPath,
                IlFileSize         = fileSize,
                IlWidth            = meta.Width,
                IlHeight           = meta.Height,
                IlFormat           = meta.Format,
                IlColorSpace       = meta.ColorSpace,
                IlBitDepth         = meta.BitDepth,
                IlDpiX             = meta.DpiX,
                IlDpiY             = meta.DpiY,
                IlExifData         = JsonSerializer.Serialize(meta.ExifData),
                IlCameraMake       = meta.CameraMake,
                IlCameraModel      = meta.CameraModel,
                IlLensModel        = meta.LensModel,
                IlFocalLength      = meta.FocalLength,
                IlAperture         = meta.Aperture,
                IlShutterSpeed     = meta.ShutterSpeed,
                IlIso              = meta.Iso,
                IlFlash            = meta.Flash,
                IlExposureMode     = meta.ExposureMode,
                IlWhiteBalance     = meta.WhiteBalance,
                IlDateTaken        = meta.DateTaken,
                IlOrientation      = meta.Orientation,
                IlCompressionQuality = meta.CompressionQuality,
                IlQualityLevel     = qualityLevel,
                IlQualityType      = qualityType,
                IlUploadBatch      = uploadBatch
            };

        private string GetBaseName(string fileName)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            // Extract everything before the first + or -, e.g. "261_center" from "261_center+1" or "261_center-2"
            var idx = nameWithoutExt.IndexOfAny(['+', '-']);
            return idx >= 0 ? nameWithoutExt[..idx] : nameWithoutExt;
        }

        private bool IsMasterImage(string fileName)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            // Master: no + or - anywhere in the name, e.g. "261_center"
            return nameWithoutExt.IndexOfAny(['+', '-']) < 0;
        }

        private (string level, string type) ParseQualityInfo(string fileName)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            // Match sign and trailing digits at end, e.g. "261_center+1" → "+1" / "261_center-2" → "-2"
            var match = System.Text.RegularExpressions.Regex.Match(nameWithoutExt, @"([+\-])(\d+)$");
            if (match.Success)
                return ($"{match.Groups[1].Value}{match.Groups[2].Value}", match.Groups[1].Value == "+" ? "enhanced" : "degraded");
            return ("pair", "pair");
        }

        private int ConvertToIntActive(JsonElement activeElement) => activeElement.ValueKind switch
        {
            JsonValueKind.String => activeElement.GetString()?.ToUpper() is "Y" or "YES" or "TRUE" or "1" ? 1 : 0,
            JsonValueKind.Number => activeElement.GetInt32(),
            JsonValueKind.True   => 1,
            _                    => 1
        };

        private static ICellStyle CreateHeaderStyle(XSSFWorkbook workbook)
        {
            var style = workbook.CreateCellStyle();
            var font  = workbook.CreateFont();
            font.IsBold = true; font.Color = IndexedColors.White.Index; font.FontHeightInPoints = 11;
            style.SetFont(font);
            style.FillForegroundColor = IndexedColors.DarkBlue.Index;
            style.FillPattern         = FillPattern.SolidForeground;
            style.Alignment           = HorizontalAlignment.Center;
            style.VerticalAlignment   = VerticalAlignment.Center;
            style.BorderBottom        = BorderStyle.Medium; style.BorderTop = BorderStyle.Medium;
            style.BorderLeft          = BorderStyle.Thin;   style.BorderRight = BorderStyle.Thin;
            return style;
        }

        private static ICellStyle CreateDataStyle(XSSFWorkbook workbook)
        {
            var style = workbook.CreateCellStyle();
            var font  = workbook.CreateFont(); font.FontHeightInPoints = 10; style.SetFont(font);
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderBottom = BorderStyle.Thin; style.BorderTop = BorderStyle.Thin;
            style.BorderLeft   = BorderStyle.Thin; style.BorderRight = BorderStyle.Thin;
            return style;
        }

        private static ICellStyle CreateAltRowStyle(XSSFWorkbook workbook)
        {
            var style = workbook.CreateCellStyle();
            var font  = workbook.CreateFont(); font.FontHeightInPoints = 10; style.SetFont(font);
            style.FillForegroundColor = IndexedColors.LightCornflowerBlue.Index;
            style.FillPattern         = FillPattern.SolidForeground;
            style.VerticalAlignment   = VerticalAlignment.Center;
            style.BorderBottom = BorderStyle.Thin; style.BorderTop = BorderStyle.Thin;
            style.BorderLeft   = BorderStyle.Thin; style.BorderRight = BorderStyle.Thin;
            return style;
        }

        // Image Groups
        [HttpGet]
        public IActionResult ImageGroups()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllImageGroups()
        {
            try
            {
                // Try to get from session first, fallback to "Anonymous" if not available
                var userId = HttpContext.Session.GetString("UserId") ?? "Anonymous";
                
                _logger.LogInformation($"GetAllImageGroups called with userId: {userId}");
                
                var result = await _imageGroupRepository.GetAllImageGroups(userId);
                
                _logger.LogInformation($"GetAllImageGroups returned {result.Data?.Count ?? 0} groups. Success: {result.OutputCode == 1}");
                
                return Json(new
                {
                    success = result.OutputCode == 1,
                    message = result.OutputMsg,
                    data = result.Data ?? new List<ImageGroup>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading image groups");
                return Json(new 
                { 
                    success = false, 
                    message = $"Error: {ex.Message}", 
                    data = new List<ImageGroup>() 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveImageGroup([FromBody] ImageGroup model)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId") ?? "Anonymous";
                var result = await _imageGroupRepository.SaveImageGroup(model, userId);
                return Json(new
                {
                    success = result.OutputCode == 1,
                    message = result.OutputMsg
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetActiveImageGroup([FromBody] string groupCode)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId") ?? "Anonymous";
                var result = await _imageGroupRepository.SetActiveGroup(groupCode, userId);
                return Json(new
                {
                    success = result.OutputCode == 1,
                    message = result.OutputMsg
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImageGroup([FromBody] string groupCode)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId") ?? "Anonymous";
                var result = await _imageGroupRepository.DeleteImageGroup(groupCode, userId);
                return Json(new
                {
                    success = result.OutputCode == 1,
                    message = result.OutputMsg
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkDeleteImagesByGroup([FromBody] BulkDeleteByGroupRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId") ?? "Anonymous";
                var result = await _imageGroupRepository.BulkDeleteImagesByGroup(request.GroupCode, request.AssessmentType, userId);
                return Json(new
                {
                    success = result.OutputCode == 1,
                    message = result.OutputMsg,
                    data = result.Data
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Bulk Operations
        [HttpGet]
        public IActionResult BulkOperations()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAssessmentCodesWithData()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var (outputCode, outputMsg, data) = await _bulkOperationsRepository.GetAssessmentCodesWithData(userId);
            return Json(new
            {
                success = outputCode == 1,
                message = outputMsg,
                data = data
            });
        }

        [HttpPost]
        public async Task<IActionResult> BulkDeleteAssessmentData([FromBody] BulkDeleteAssessmentRequest request)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            if (string.IsNullOrWhiteSpace(request.AssessmentCode))
                return Json(new { success = false, message = "Assessment code is required" });

            try
            {
                var result = await _bulkOperationsRepository.BulkDeleteAssessmentData(request, userId);
                return Json(new
                {
                    success = result.OutputCode == 1,
                    message = result.OutputMsg,
                    data = result.Data
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAssessmentDataSummary(string assessmentCode)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                // Get counts for each data type
                var questionAnswersQuery = @"
                    SELECT COUNT(*) as count FROM user_responses WHERE ur_assessment_code = @assessmentCode";
                
                var imageRatingsQuery = @"
                    SELECT COUNT(*) as count FROM image_quality_ratings WHERE iqr_assessment_code = @assessmentCode";
                
                var speedTestQuery = @"
                    SELECT COUNT(*) as count FROM speed_test_logs WHERE stl_assessment_code = @assessmentCode";

                var parameters = new[] { new MySqlParameter("@assessmentCode", assessmentCode) };

                var qaResult = await Task.Run(() => _dbHelper.ExecuteQuery(questionAnswersQuery, parameters));
                var irResult = await Task.Run(() => _dbHelper.ExecuteQuery(imageRatingsQuery, parameters));
                var stResult = await Task.Run(() => _dbHelper.ExecuteQuery(speedTestQuery, parameters));

                var summary = new
                {
                    assessmentCode = assessmentCode,
                    questionAnswers = qaResult.Rows.Count > 0 ? Convert.ToInt32(qaResult.Rows[0]["count"]) : 0,
                    imageRatings = irResult.Rows.Count > 0 ? Convert.ToInt32(irResult.Rows[0]["count"]) : 0,
                    speedTestLogs = stResult.Rows.Count > 0 ? Convert.ToInt32(stResult.Rows[0]["count"]) : 0
                };

                return Json(new
                {
                    success = true,
                    message = "Summary retrieved successfully",
                    data = summary
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== ADMIN USER MANAGEMENT ==========
        [HttpGet]
        public IActionResult AdminUsers()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAdminUsers()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _adminUserRepository.GetAllAdminUsers(userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> SaveAdminUser([FromBody] AdminUser user)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                AdminUserResponse result;
                if (!string.IsNullOrEmpty(user.AuId))
                {
                    result = await _adminUserRepository.UpdateAdminUser(user, userId);
                }
                else
                {
                    result = await _adminUserRepository.InsertAdminUser(user, userId);
                }

                return Json(new { success = result.OutputCode == 1, message = result.OutputMsg });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAdminUser([FromBody] string auId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _adminUserRepository.DeleteAdminUser(auId, userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg });
        }

        // ========== ADMIN MENU MANAGEMENT ==========
        [HttpGet]
        public IActionResult AdminMenus()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAdminMenus()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _adminMenuRepository.GetAllAdminMenus(userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> SaveAdminMenu([FromBody] AdminMenu menu)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                AdminMenuResponse result;
                if (menu.AmId > 0)
                {
                    result = await _adminMenuRepository.UpdateAdminMenu(menu, userId);
                }
                else
                {
                    result = await _adminMenuRepository.InsertAdminMenu(menu, userId);
                }

                return Json(new { success = result.OutputCode == 1, message = result.OutputMsg });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAdminMenu([FromBody] int amId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _adminMenuRepository.DeleteAdminMenu(amId, userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg });
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuRoleAccess(int menuId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _adminMenuRepository.GetMenuRoleAccess(menuId, userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg, data = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> SaveMenuRoleAccess([FromBody] List<MenuRoleAccess> roleAccess)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _adminMenuRepository.SaveMenuRoleAccess(roleAccess, userId);
            return Json(new { success = result.OutputCode == 1, message = result.OutputMsg });
        }

        // Get menus for current user based on their role
        [HttpGet]
        public async Task<IActionResult> GetUserMenus()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole") ?? "Operator";
            
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized", data = new List<object>() });

            try
            {
                var menus = await _adminMenuRepository.GetMenusByUserRole(userRole);
                return Json(new 
                { 
                    success = true, 
                    message = $"Retrieved {menus.Count} menus for role {userRole}", 
                    data = menus 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}", data = new List<object>() });
            }
        }
    }

    public class FolderProcessRequest
    {
        public string FolderPath   { get; set; }
        public string AssessmentType { get; set; }
    }

    public class BulkDeleteByGroupRequest
    {
        public string GroupCode      { get; set; }
        public string AssessmentType { get; set; }
    }
}