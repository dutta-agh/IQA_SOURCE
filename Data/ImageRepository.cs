using System.Data;
using System.Text.Json;
using MySqlConnector;
using IQA_SOURCE.Models.Admin;
using YourApp.Data;
using IQA_SOURCE.Helpers;

namespace IQA_SOURCE.Data
{
    public class ImageRepository : IImageRepository
    {
        private readonly IDbHelper _dbHelper;
        private readonly ILogger<ImageRepository> _logger; // ✅ ADD LOGGER

        public ImageRepository(IDbHelper dbHelper, ILogger<ImageRepository> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        public async Task<ImageUploadResponse> ProcessFolderImages(string folderPath, string assessmentType, string userId)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    return new ImageUploadResponse
                    {
                        OutputCode = 0,
                        OutputMsg = "Folder path does not exist",
                        Data = new ImageUploadResult
                        {
                            ErrorMessages = new List<string> { "Invalid folder path" }
                        }
                    };
                }

                var uploadBatch = Guid.NewGuid().ToString();
                var masterImages = new List<ImageMaster>();
                var linkedImages = new List<ImageLinked>();
                var skippedMasters = new List<string>();
                var errors = new List<string>();

                // Get all image files from folder
                var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".gif" };
                var imageFiles = Directory.GetFiles(folderPath)
                    .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .ToList();

                if (imageFiles.Count == 0)
                {
                    return new ImageUploadResponse
                    {
                        OutputCode = 0,
                        OutputMsg = "No supported image files found in the folder",
                        Data = new ImageUploadResult
                        {
                            ErrorMessages = new List<string> { "No image files found" }
                        }
                    };
                }

                // Group files by base name
                var fileGroups = imageFiles.GroupBy(f => GetBaseName(Path.GetFileName(f))).ToList();

                foreach (var group in fileGroups)
                {
                    var masterFile = group.FirstOrDefault(f => IsMasterImage(Path.GetFileName(f)));
                    
                    if (masterFile != null)
                    {
                        var masterFileName = Path.GetFileName(masterFile);
                        
                        // Check if master image already exists
                        var masterExists = await CheckDuplicateFileName(assessmentType, masterFileName);
                        
                        if (masterExists)
                        {
                            skippedMasters.Add(masterFileName);
                            
                            // Get the existing master image ID
                            var existingMaster = await GetMasterImageByFileName(assessmentType, masterFileName, userId);
                            
                            if (existingMaster != null)
                            {
                                // Process only new linked images for this master
                                foreach (var linkedFile in group.Where(f => !IsMasterImage(Path.GetFileName(f))))
                                {
                                    var linkedFileName = Path.GetFileName(linkedFile);
                                    
                                    // Check if this linked image already exists
                                    var linkedExists = await CheckLinkedImageExists(existingMaster.ImId, linkedFileName);
                                    
                                    if (!linkedExists)
                                    {
                                        try
                                        {
                                            var linkedImage = await CreateImageLinkedFromFile(linkedFile, existingMaster.ImId, assessmentType, uploadBatch);
                                            linkedImages.Add(linkedImage);
                                        }
                                        catch (Exception ex)
                                        {
                                            errors.Add($"Error processing {linkedFileName}: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Master doesn't exist, create new master
                            try
                            {
                                var masterImage = await CreateImageMasterFromFile(masterFile, assessmentType, uploadBatch);
                                masterImages.Add(masterImage);

                                // Process linked images for NEW master
                                foreach (var linkedFile in group.Where(f => !IsMasterImage(Path.GetFileName(f))))
                                {
                                    try
                                    {
                                        var linkedImage = await CreateImageLinkedFromFile(linkedFile, 0, assessmentType, uploadBatch);
                                        linkedImages.Add(linkedImage);
                                    }
                                    catch (Exception ex)
                                    {
                                        errors.Add($"Error processing {Path.GetFileName(linkedFile)}: {ex.Message}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Error processing master {masterFileName}: {ex.Message}");
                            }
                        }
                    }
                }

                // Save to database
                ImageUploadResponse result;
                
                if (masterImages.Count > 0 || linkedImages.Count > 0)
                {
                    result = await SaveImages(masterImages, linkedImages, userId);
                    
                    // Add skip information to result
                    result.Data.SkippedMasterImages = skippedMasters.Count;
                    result.Data.SkippedMasterFileNames = skippedMasters;
                    result.Data.ErrorMessages = errors;
                }
                else
                {
                    result = new ImageUploadResponse
                    {
                        OutputCode = 1,
                        OutputMsg = "No new images to upload. All master images already exist.",
                        Data = new ImageUploadResult
                        {
                            TotalFiles = imageFiles.Count,
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

                return result;
            }
            catch (Exception ex)
            {
                return new ImageUploadResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error processing folder: {ex.Message}",
                    Data = new ImageUploadResult
                    {
                        ErrorMessages = new List<string> { ex.Message }
                    }
                };
            }
        }

        private async Task<ImageMaster> CreateImageMasterFromFile(string filePath, string assessmentType, string uploadBatch)
        {
            var fileName = Path.GetFileName(filePath);
            var fileInfo = new FileInfo(filePath);
            
            // You'll need to inject IImageMetadataService or pass it as parameter
            // For now, returning basic structure - metadata extraction should be done in controller
            return new ImageMaster
            {
                ImAssessmentType = assessmentType,
                ImFileName = fileName,
                ImFilePath = filePath, // Store original path or copy to wwwroot
                ImFileSize = fileInfo.Length,
                ImUploadBatch = uploadBatch
            };
        }

        private async Task<ImageLinked> CreateImageLinkedFromFile(string filePath, int masterId, string assessmentType, string uploadBatch)
        {
            var fileName = Path.GetFileName(filePath);
            var fileInfo = new FileInfo(filePath);
            var (qualityLevel, qualityType) = ParseQualityInfo(fileName);
            
            return new ImageLinked
            {
                IlMasterId = masterId,
                IlFileName = fileName,
                IlFilePath = filePath, // Store original path or copy to wwwroot
                IlFileSize = fileInfo.Length,
                IlQualityLevel = qualityLevel,
                IlQualityType = qualityType,
                IlUploadBatch = uploadBatch
            };
        }

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

        public async Task<ImageUploadResponse> SaveImages(List<ImageMaster> masterImages, List<ImageLinked> linkedImages, string userId)
        {
            try
            {
                int masterCount = 0;
                int linkedCount = 0;
                var processedLinkedImages = new HashSet<string>();

                foreach (var master in masterImages)
                {
                    var insertMasterQuery = @"
                        INSERT INTO image_master (
                            im_assessment_type, im_group_code, im_file_name, im_file_path, 
                            im_file_size, im_width, im_height, im_format,
                            im_color_space, im_bit_depth, im_dpi_x, im_dpi_y,
                            im_exif_data, im_camera_make, im_camera_model,
                            im_lens_model, im_focal_length, im_aperture,
                            im_shutter_speed, im_iso, im_flash,
                            im_exposure_mode, im_white_balance, im_date_taken,
                            im_orientation, im_compression_quality,
                            im_upload_batch, im_created_user, im_created_date, im_active
                        ) VALUES (
                            @assessmentType, @groupCode, @fileName, @filePath, 
                            @fileSize, @width, @height, @format,
                            @colorSpace, @bitDepth, @dpiX, @dpiY,
                            @exifData, @cameraMake, @cameraModel,
                            @lensModel, @focalLength, @aperture,
                            @shutterSpeed, @iso, @flash,
                            @exposureMode, @whiteBalance, @dateTaken,
                            @orientation, @compressionQuality,
                            @uploadBatch, @userId, NOW(), 1
                        );
                        SELECT LAST_INSERT_ID();";

                    var masterParams = new[]
                    {
                        new MySqlParameter("@assessmentType", master.ImAssessmentType),
                        new MySqlParameter("@groupCode", (object?)master.ImGroupCode ?? DBNull.Value),
                        new MySqlParameter("@fileName", master.ImFileName),
                        new MySqlParameter("@filePath", master.ImFilePath),
                        new MySqlParameter("@fileSize", (object)master.ImFileSize ?? DBNull.Value),
                        new MySqlParameter("@width", (object)master.ImWidth ?? DBNull.Value),
                        new MySqlParameter("@height", (object)master.ImHeight ?? DBNull.Value),
                        new MySqlParameter("@format", (object)master.ImFormat ?? DBNull.Value),
                        new MySqlParameter("@colorSpace", (object)master.ImColorSpace ?? DBNull.Value),
                        new MySqlParameter("@bitDepth", (object)master.ImBitDepth ?? DBNull.Value),
                        new MySqlParameter("@dpiX", (object)master.ImDpiX ?? DBNull.Value),
                        new MySqlParameter("@dpiY", (object)master.ImDpiY ?? DBNull.Value),
                        new MySqlParameter("@exifData", (object)master.ImExifData ?? DBNull.Value),
                        new MySqlParameter("@cameraMake", (object)master.ImCameraMake ?? DBNull.Value),
                        new MySqlParameter("@cameraModel", (object)master.ImCameraModel ?? DBNull.Value),
                        new MySqlParameter("@lensModel", (object)master.ImLensModel ?? DBNull.Value),
                        new MySqlParameter("@focalLength", (object)master.ImFocalLength ?? DBNull.Value),
                        new MySqlParameter("@aperture", (object)master.ImAperture ?? DBNull.Value),
                        new MySqlParameter("@shutterSpeed", (object)master.ImShutterSpeed ?? DBNull.Value),
                        new MySqlParameter("@iso", (object)master.ImIso ?? DBNull.Value),
                        new MySqlParameter("@flash", (object)master.ImFlash ?? DBNull.Value),
                        new MySqlParameter("@exposureMode", (object)master.ImExposureMode ?? DBNull.Value),
                        new MySqlParameter("@whiteBalance", (object)master.ImWhiteBalance ?? DBNull.Value),
                        new MySqlParameter("@dateTaken", (object)master.ImDateTaken ?? DBNull.Value),
                        new MySqlParameter("@orientation", (object)master.ImOrientation ?? DBNull.Value),
                        new MySqlParameter("@compressionQuality", (object)master.ImCompressionQuality ?? DBNull.Value),
                        new MySqlParameter("@uploadBatch", master.ImUploadBatch),
                        new MySqlParameter("@userId", userId)
                    };

                    var masterIdResult = await Task.Run(() => _dbHelper.ExecuteQuery(insertMasterQuery, masterParams));
                    if (masterIdResult.Rows.Count > 0)
                    {
                        int masterId = Convert.ToInt32(masterIdResult.Rows[0][0]);
                        masterCount++;

                        var relatedLinked = linkedImages.Where(l =>
                            l.IlMasterId == 0 &&
                            l.IlFileName.StartsWith(master.ImFileName.Split('.')[0])
                        ).ToList();
                        
                        foreach (var linked in relatedLinked)
                        {
                            linked.IlMasterId = masterId;

                            // ✅ FIXED: Removed il_group_code from column list and VALUES
                            var insertLinkedQuery = @"
                                INSERT INTO image_linked (
                                    il_master_id, il_file_name, il_file_path, 
                                    il_file_size, il_width, il_height, il_format,
                                    il_color_space, il_bit_depth, il_dpi_x, il_dpi_y,
                                    il_exif_data, il_camera_make, il_camera_model,
                                    il_lens_model, il_focal_length, il_aperture,
                                    il_shutter_speed, il_iso, il_flash,
                                    il_exposure_mode, il_white_balance, il_date_taken,
                                    il_orientation, il_compression_quality,
                                    il_quality_level, il_quality_type, il_upload_batch,
                                    il_created_user, il_created_date, im_active
                                ) VALUES (
                                    @masterId, @fileName, @filePath, 
                                    @fileSize, @width, @height, @format,
                                    @colorSpace, @bitDepth, @dpiX, @dpiY,
                                    @exifData, @cameraMake, @cameraModel,
                                    @lensModel, @focalLength, @aperture,
                                    @shutterSpeed, @iso, @flash,
                                    @exposureMode, @whiteBalance, @dateTaken,
                                    @orientation, @compressionQuality,
                                    @uploadBatch, @userId, NOW(), 1
                                )";

                            var linkedParams = new[]
                            {
                                new MySqlParameter("@masterId", linked.IlMasterId),
                                new MySqlParameter("@fileName", linked.IlFileName),
                                new MySqlParameter("@filePath", linked.IlFilePath),
                                new MySqlParameter("@fileSize", (object)linked.IlFileSize ?? DBNull.Value),
                                new MySqlParameter("@width", (object)linked.IlWidth ?? DBNull.Value),
                                new MySqlParameter("@height", (object)linked.IlHeight ?? DBNull.Value),
                                new MySqlParameter("@format", (object)linked.IlFormat ?? DBNull.Value),
                                new MySqlParameter("@colorSpace", (object)linked.IlColorSpace ?? DBNull.Value),
                                new MySqlParameter("@bitDepth", (object)linked.IlBitDepth ?? DBNull.Value),
                                new MySqlParameter("@dpiX", (object)linked.IlDpiX ?? DBNull.Value),
                                new MySqlParameter("@dpiY", (object)linked.IlDpiY ?? DBNull.Value),
                                new MySqlParameter("@exifData", (object)linked.IlExifData ?? DBNull.Value),
                                new MySqlParameter("@cameraMake", (object)linked.IlCameraMake ?? DBNull.Value),
                                new MySqlParameter("@cameraModel", (object)linked.IlCameraModel ?? DBNull.Value),
                                new MySqlParameter("@lensModel", (object)linked.IlLensModel ?? DBNull.Value),
                                new MySqlParameter("@focalLength", (object)linked.IlFocalLength ?? DBNull.Value),
                                new MySqlParameter("@aperture", (object)linked.IlAperture ?? DBNull.Value),
                                new MySqlParameter("@shutterSpeed", (object)linked.IlShutterSpeed ?? DBNull.Value),
                                new MySqlParameter("@iso", (object)linked.IlIso ?? DBNull.Value),
                                new MySqlParameter("@flash", (object)linked.IlFlash ?? DBNull.Value),
                                new MySqlParameter("@exposureMode", (object)linked.IlExposureMode ?? DBNull.Value),
                                new MySqlParameter("@whiteBalance", (object)linked.IlWhiteBalance ?? DBNull.Value),
                                new MySqlParameter("@dateTaken", (object)linked.IlDateTaken ?? DBNull.Value),
                                new MySqlParameter("@orientation", (object)linked.IlOrientation ?? DBNull.Value),
                                new MySqlParameter("@compressionQuality", (object)linked.IlCompressionQuality ?? DBNull.Value),
                                new MySqlParameter("@qualityLevel", linked.IlQualityLevel),
                                new MySqlParameter("@qualityType", linked.IlQualityType),
                                new MySqlParameter("@uploadBatch", linked.IlUploadBatch),
                                new MySqlParameter("@userId", userId)
                            };

                            var linkedRows = await Task.Run(() => _dbHelper.ExecuteNonQuery(insertLinkedQuery, linkedParams));
                            if (linkedRows > 0)
                            {
                                linkedCount++;
                                processedLinkedImages.Add(linked.IlFileName);
                            }
                        }
                    }
                }

                // Insert linked images for existing masters (IlMasterId > 0)
                var linkedImagesForExistingMasters = linkedImages.Where(l =>
                    l.IlMasterId > 0 &&
                    !processedLinkedImages.Contains(l.IlFileName)
                ).ToList();

                foreach (var linked in linkedImagesForExistingMasters)
                {
                    // ✅ FIXED: Removed il_group_code from column list and VALUES
                    var insertLinkedQuery = @"
                        INSERT INTO image_linked (
                            il_master_id, il_file_name, il_file_path, 
                            il_file_size, il_width, il_height, il_format,
                            il_color_space, il_bit_depth, il_dpi_x, il_dpi_y,
                            il_exif_data, il_camera_make, il_camera_model,
                            il_lens_model, il_focal_length, il_aperture,
                            il_shutter_speed, il_iso, il_flash,
                            il_exposure_mode, il_white_balance, il_date_taken,
                            il_orientation, il_compression_quality,
                            il_quality_level, il_quality_type, il_upload_batch,
                            il_created_user, il_created_date, im_active
                        ) VALUES (
                            @masterId, @fileName, @filePath, 
                            @fileSize, @width, @height, @format,
                            @colorSpace, @bitDepth, @dpiX, @dpiY,
                            @exifData, @cameraMake, @cameraModel,
                            @lensModel, @focalLength, @aperture,
                            @shutterSpeed, @iso, @flash,
                            @exposureMode, @whiteBalance, @dateTaken,
                            @orientation, @compressionQuality,
                            @uploadBatch, @userId, NOW(), 1
                        )";

                    var linkedParams = new[]
                    {
                        new MySqlParameter("@masterId", linked.IlMasterId),
                        new MySqlParameter("@fileName", linked.IlFileName),
                        new MySqlParameter("@filePath", linked.IlFilePath),
                        new MySqlParameter("@fileSize", (object)linked.IlFileSize ?? DBNull.Value),
                        new MySqlParameter("@width", (object)linked.IlWidth ?? DBNull.Value),
                        new MySqlParameter("@height", (object)linked.IlHeight ?? DBNull.Value),
                        new MySqlParameter("@format", (object)linked.IlFormat ?? DBNull.Value),
                        new MySqlParameter("@colorSpace", (object)linked.IlColorSpace ?? DBNull.Value),
                        new MySqlParameter("@bitDepth", (object)linked.IlBitDepth ?? DBNull.Value),
                        new MySqlParameter("@dpiX", (object)linked.IlDpiX ?? DBNull.Value),
                        new MySqlParameter("@dpiY", (object)linked.IlDpiY ?? DBNull.Value),
                        new MySqlParameter("@exifData", (object)linked.IlExifData ?? DBNull.Value),
                        new MySqlParameter("@cameraMake", (object)linked.IlCameraMake ?? DBNull.Value),
                        new MySqlParameter("@cameraModel", (object)linked.IlCameraModel ?? DBNull.Value),
                        new MySqlParameter("@lensModel", (object)linked.IlLensModel ?? DBNull.Value),
                        new MySqlParameter("@focalLength", (object)linked.IlFocalLength ?? DBNull.Value),
                        new MySqlParameter("@aperture", (object)linked.IlAperture ?? DBNull.Value),
                        new MySqlParameter("@shutterSpeed", (object)linked.IlShutterSpeed ?? DBNull.Value),
                        new MySqlParameter("@iso", (object)linked.IlIso ?? DBNull.Value),
                        new MySqlParameter("@flash", (object)linked.IlFlash ?? DBNull.Value),
                        new MySqlParameter("@exposureMode", (object)linked.IlExposureMode ?? DBNull.Value),
                        new MySqlParameter("@whiteBalance", (object)linked.IlWhiteBalance ?? DBNull.Value),
                        new MySqlParameter("@dateTaken", (object)linked.IlDateTaken ?? DBNull.Value),
                        new MySqlParameter("@orientation", (object)linked.IlOrientation ?? DBNull.Value),
                        new MySqlParameter("@compressionQuality", (object)linked.IlCompressionQuality ?? DBNull.Value),
                        new MySqlParameter("@qualityLevel", linked.IlQualityLevel),
                        new MySqlParameter("@qualityType", linked.IlQualityType),
                        new MySqlParameter("@uploadBatch", linked.IlUploadBatch),
                        new MySqlParameter("@userId", userId)
                    };

                    var linkedRows = await Task.Run(() => _dbHelper.ExecuteNonQuery(insertLinkedQuery, linkedParams));
                    if (linkedRows > 0) linkedCount++;
                }

                return new ImageUploadResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Images uploaded successfully",
                    Data = new ImageUploadResult
                    {
                        TotalFiles           = masterImages.Count + linkedImages.Count,
                        MasterImagesUploaded = masterCount,
                        LinkedImagesUploaded = linkedCount,
                        FailedUploads        = 0,
                        ErrorMessages        = new List<string>()
                    }
                };
            }
            catch (Exception ex)
            {
                return new ImageUploadResponse
                {
                    OutputCode = 0,
                    OutputMsg  = $"Error: {ex.Message}",
                    Data       = new ImageUploadResult
                    {
                        ErrorMessages = new List<string> { ex.Message }
                    }
                };
            }
        }

        public async Task<ImageMasterResponse> GetImagesByAssessmentType(string assessmentType, string userId)
        {
            try
            {
                // ✅ FIXED: Return ALL active images regardless of group (including untagged)
                var query = @"
                    SELECT * FROM image_master 
                    WHERE im_assessment_type = @assessmentType 
                    AND im_active = 1
                    ORDER BY CASE WHEN im_group_code IS NULL THEN 1 ELSE 0 END, im_group_code, im_created_date DESC";

                var parameters = new[]
                {
                    new MySqlParameter("@assessmentType", assessmentType)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var images = new List<ImageMaster>();
                foreach (DataRow row in result.Rows)
                {
                    images.Add(MapToImageMaster(row));
                }

                return new ImageMasterResponse
                {
                    OutputCode = 1,
                    OutputMsg = $"Retrieved {images.Count} images successfully",
                    Data = images
                };
            }
            catch (Exception ex)
            {
                return new ImageMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving images: {ex.Message}",
                    Data = new List<ImageMaster>()
                };
            }
        }

        public async Task<ImageMasterResponse> GetImagesByAssessmentTypeAndGroup(string assessmentType, string? groupCode, string userId)
        {
            try
            {
                string query;
                MySqlParameter[] parameters;

                if (string.IsNullOrEmpty(groupCode))
                {
                    query = @"
                        SELECT * FROM image_master 
                        WHERE im_assessment_type = @assessmentType 
                        AND im_active = 1
                        AND im_group_code IS NOT NULL
                        ORDER BY im_created_date DESC";

                    parameters = new[]
                    {
                        new MySqlParameter("@assessmentType", assessmentType)
                    };
                }
                else
                {
                    query = @"
                        SELECT * FROM image_master 
                        WHERE im_assessment_type = @assessmentType 
                        AND im_group_code = @groupCode
                        AND im_active = 1
                        ORDER BY im_created_date DESC";

                    parameters = new[]
                    {
                        new MySqlParameter("@assessmentType", assessmentType),
                        new MySqlParameter("@groupCode", groupCode)
                    };
                }

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var images = new List<ImageMaster>();
                foreach (DataRow row in result.Rows)
                {
                    var master = MapToImageMaster(row);

                    // ✅ FIXED: Changed to use im_active (which exists in image_linked)
                    var linkedQuery = @"
                        SELECT * FROM image_linked 
                        WHERE il_master_id = @masterId 
                        AND im_active = 1
                        ORDER BY il_quality_level";

                    var linkedParams = new[]
                    {
                        new MySqlParameter("@masterId", master.ImId)
                    };

                    var linkedResult = await Task.Run(() => _dbHelper.ExecuteQuery(linkedQuery, linkedParams));

                    master.LinkedImages = new List<ImageLinked>();
                    foreach (DataRow linkedRow in linkedResult.Rows)
                    {
                        master.LinkedImages.Add(MapToImageLinked(linkedRow));
                    }

                    images.Add(master);
                }

                return new ImageMasterResponse
                {
                    OutputCode = 1,
                    OutputMsg = $"Retrieved {images.Count} images successfully",
                    Data = images
                };
            }
            catch (Exception ex)
            {
                return new ImageMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving images: {ex.Message}",
                    Data = new List<ImageMaster>()
                };
            }
        }

        public async Task<ImageMasterResponse> GetImageById(int imageId, string userId)
        {
            try
            {
                var query = @"
                    SELECT * FROM image_master 
                    WHERE im_id = @imageId 
                    AND im_active = 1
                    LIMIT 1";

                var parameters = new[]
                {
                    new MySqlParameter("@imageId", imageId)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var images = new List<ImageMaster>();
                if (result.Rows.Count > 0)
                {
                    var master = MapToImageMaster(result.Rows[0]);

                    // ✅ FIXED: Changed to use im_active (which exists in image_linked)
                    var linkedQuery = @"
                        SELECT * FROM image_linked 
                        WHERE il_master_id = @masterId 
                        AND im_active = 1
                        ORDER BY il_quality_level";

                    var linkedParams = new[]
                    {
                        new MySqlParameter("@masterId", imageId)
                    };

                    var linkedResult = await Task.Run(() => _dbHelper.ExecuteQuery(linkedQuery, linkedParams));

                    master.LinkedImages = new List<ImageLinked>();
                    foreach (DataRow linkedRow in linkedResult.Rows)
                    {
                        master.LinkedImages.Add(MapToImageLinked(linkedRow));
                    }

                    images.Add(master);
                }

                return new ImageMasterResponse
                {
                    OutputCode = 1,
                    OutputMsg = result.Rows.Count > 0 ? "Image retrieved successfully" : "Image not found",
                    Data = images
                };
            }
            catch (Exception ex)
            {
                return new ImageMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving image: {ex.Message}",
                    Data = new List<ImageMaster>()
                };
            }
        }

        public async Task<ImageMaster> GetMasterImageByFileName(string assessmentType, string fileName, string userId)
        {
            try
            {
                var query = @"
                    SELECT * FROM image_master 
                    WHERE im_assessment_type = @assessmentType 
                    AND im_file_name = @fileName 
                    AND im_active = 1
                    LIMIT 1";

                var parameters = new[]
                {
                    new MySqlParameter("@assessmentType", assessmentType),
                    new MySqlParameter("@fileName", fileName)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                if (result.Rows.Count > 0)
                {
                    return MapToImageMaster(result.Rows[0]);
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log error
                return null;
            }
        }

        public async Task<bool> CheckDuplicateFileName(string assessmentType, string fileName)
        {
            try
            {
                var query = @"
                    SELECT COUNT(*) as cnt 
                    FROM image_master 
                    WHERE im_assessment_type = @assessmentType 
                    AND im_file_name = @fileName 
                    AND im_active = 1";

                var parameters = new[]
                {
                    new MySqlParameter("@assessmentType", assessmentType),
                    new MySqlParameter("@fileName", fileName)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));
                
                if (result.Rows.Count > 0)
                {
                    return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CheckLinkedImageExists(int masterId, string fileName)
        {
            try
            {
                var query = @"
                    SELECT COUNT(*) as cnt 
                    FROM image_linked 
                    WHERE il_master_id = @masterId 
                    AND il_file_name = @fileName 
                    AND im_active = 1";  // ✅ This is correct since image_linked has im_active column

                var parameters = new[]
                {
                    new MySqlParameter("@masterId", masterId),
                    new MySqlParameter("@fileName", fileName)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));
                
                if (result.Rows.Count > 0)
                {
                    return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ImageUploadResponse> SaveLinkedImagesOnly(List<ImageLinked> linkedImages, string userId)
        {
            try
            {
                int linkedCount = 0;

                foreach (var linked in linkedImages)
                {
                    var insertLinkedQuery = @"
                        INSERT INTO image_linked (
                            il_master_id, il_file_name, il_file_path, 
                            il_file_size, il_width, il_height, il_format,
                            il_color_space, il_bit_depth, il_dpi_x, il_dpi_y,
                            il_exif_data, il_camera_make, il_camera_model,
                            il_lens_model, il_focal_length, il_aperture,
                            il_shutter_speed, il_iso, il_flash,
                            il_exposure_mode, il_white_balance, il_date_taken,
                            il_orientation, il_compression_quality,
                            il_quality_level, il_quality_type, il_upload_batch,
                            il_created_user, il_created_date, im_active
                        ) VALUES (
                            @masterId, @fileName, @filePath, 
                            @fileSize, @width, @height, @format,
                            @colorSpace, @bitDepth, @dpiX, @dpiY,
                            @exifData, @cameraMake, @cameraModel,
                            @lensModel, @focalLength, @aperture,
                            @shutterSpeed, @iso, @flash,
                            @exposureMode, @whiteBalance, @dateTaken,
                            @orientation, @compressionQuality,
                            @qualityLevel, @qualityType, @uploadBatch,
                            @userId, NOW(), 1
                        )";

                    var linkedParams = new[]
                    {
                        new MySqlParameter("@masterId", linked.IlMasterId),
                        new MySqlParameter("@fileName", linked.IlFileName),
                        new MySqlParameter("@filePath", linked.IlFilePath),
                        new MySqlParameter("@fileSize", (object)linked.IlFileSize ?? DBNull.Value),
                        new MySqlParameter("@width", (object)linked.IlWidth ?? DBNull.Value),
                        new MySqlParameter("@height", (object)linked.IlHeight ?? DBNull.Value),
                        new MySqlParameter("@format", (object)linked.IlFormat ?? DBNull.Value),
                        new MySqlParameter("@colorSpace", (object)linked.IlColorSpace ?? DBNull.Value),
                        new MySqlParameter("@bitDepth", (object)linked.IlBitDepth ?? DBNull.Value),
                        new MySqlParameter("@dpiX", (object)linked.IlDpiX ?? DBNull.Value),
                        new MySqlParameter("@dpiY", (object)linked.IlDpiY ?? DBNull.Value),
                        new MySqlParameter("@exifData", (object)linked.IlExifData ?? DBNull.Value),
                        new MySqlParameter("@cameraMake", (object)linked.IlCameraMake ?? DBNull.Value),
                        new MySqlParameter("@cameraModel", (object)linked.IlCameraModel ?? DBNull.Value),
                        new MySqlParameter("@lensModel", (object)linked.IlLensModel ?? DBNull.Value),
                        new MySqlParameter("@focalLength", (object)linked.IlFocalLength ?? DBNull.Value),
                        new MySqlParameter("@aperture", (object)linked.IlAperture ?? DBNull.Value),
                        new MySqlParameter("@shutterSpeed", (object)linked.IlShutterSpeed ?? DBNull.Value),
                        new MySqlParameter("@iso", (object)linked.IlIso ?? DBNull.Value),
                        new MySqlParameter("@flash", (object)linked.IlFlash ?? DBNull.Value),
                        new MySqlParameter("@exposureMode", (object)linked.IlExposureMode ?? DBNull.Value),
                        new MySqlParameter("@whiteBalance", (object)linked.IlWhiteBalance ?? DBNull.Value),
                        new MySqlParameter("@dateTaken", (object)linked.IlDateTaken ?? DBNull.Value),
                        new MySqlParameter("@orientation", (object)linked.IlOrientation ?? DBNull.Value),
                        new MySqlParameter("@compressionQuality", (object)linked.IlCompressionQuality ?? DBNull.Value),
                        new MySqlParameter("@qualityLevel", linked.IlQualityLevel),
                        new MySqlParameter("@qualityType", linked.IlQualityType),
                        new MySqlParameter("@uploadBatch", linked.IlUploadBatch),
                        new MySqlParameter("@userId", userId)
                    };

                    var linkedRows = await Task.Run(() => _dbHelper.ExecuteNonQuery(insertLinkedQuery, linkedParams));
                    if (linkedRows > 0) linkedCount++;
                }

                return new ImageUploadResponse
                {
                    OutputCode = 1,
                    OutputMsg = $"{linkedCount} linked images uploaded successfully",
                    Data = new ImageUploadResult
                    {
                        TotalFiles = linkedImages.Count,
                        MasterImagesUploaded = 0,
                        LinkedImagesUploaded = linkedCount,
                        FailedUploads = 0,
                        ErrorMessages = new List<string>()
                    }
                };
            }
            catch (Exception ex)
            {
                return new ImageUploadResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error: {ex.Message}",
                    Data = new ImageUploadResult
                    {
                        ErrorMessages = new List<string> { ex.Message }
                    }
                };
            }
        }

        public async Task<ImageMasterResponse> DeleteImage(int imageId, string userId)
        {
            try
            {
                // Soft delete - set active to 0
                var query = @"
                    UPDATE image_master
                    SET 
                        im_active = 0,
                        im_modified_user = @userId,
                        im_modified_date = NOW()
                    WHERE im_id = @imageId";

                var parameters = new[]
                {
                    new MySqlParameter("@imageId", imageId),
                    new MySqlParameter("@userId", userId)
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));

                if (rowsAffected > 0)
                {
                    // Also soft delete linked images (no modified_user/modified_date columns)
                    var linkedQuery = @"
                        UPDATE image_linked
                        SET im_active = 0
                        WHERE il_master_id = @imageId";

                    await Task.Run(() => _dbHelper.ExecuteNonQuery(linkedQuery, parameters));
                }

                return new ImageMasterResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Image deleted successfully" : "Image not found",
                    Data = new List<ImageMaster>()
                };
            }
            catch (Exception ex)
            {
                return new ImageMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error deleting image: {ex.Message}",
                    Data = new List<ImageMaster>()
                };
            }
        }

        private ImageMaster MapToImageMaster(DataRow row)
        {
            return new ImageMaster
            {
                ImId                 = row["im_id"] != DBNull.Value ? Convert.ToInt32(row["im_id"]) : 0,
                ImAssessmentType     = row["im_assessment_type"]?.ToString(),
                ImGroupCode          = row["im_group_code"]?.ToString(),  // ✅ ADD THIS LINE
                ImFileName           = row["im_file_name"]?.ToString(),
                ImFilePath           = ImageUrlHelper.BuildImageUrl(row["im_file_path"]?.ToString()),
                ImFileSize           = row["im_file_size"] != DBNull.Value ? Convert.ToInt64(row["im_file_size"]) : null,
                ImWidth              = row["im_width"] != DBNull.Value ? Convert.ToInt32(row["im_width"]) : null,
                ImHeight             = row["im_height"] != DBNull.Value ? Convert.ToInt32(row["im_height"]) : null,
                ImFormat             = row["im_format"]?.ToString(),
                ImColorSpace         = row["im_color_space"]?.ToString(),
                ImBitDepth           = row["im_bit_depth"] != DBNull.Value ? Convert.ToInt32(row["im_bit_depth"]) : null,
                ImDpiX               = row["im_dpi_x"] != DBNull.Value ? Convert.ToDecimal(row["im_dpi_x"]) : null,
                ImDpiY               = row["im_dpi_y"] != DBNull.Value ? Convert.ToDecimal(row["im_dpi_y"]) : null,
                ImExifData           = row["im_exif_data"]?.ToString(),
                ImCameraMake         = row["im_camera_make"]?.ToString(),
                ImCameraModel        = row["im_camera_model"]?.ToString(),
                ImLensModel          = row["im_lens_model"]?.ToString(),
                ImFocalLength        = row["im_focal_length"] != DBNull.Value ? Convert.ToDecimal(row["im_focal_length"]) : null,
                ImAperture           = row["im_aperture"]?.ToString(),
                ImShutterSpeed       = row["im_shutter_speed"]?.ToString(),
                ImIso                = row["im_iso"] != DBNull.Value ? Convert.ToInt32(row["im_iso"]) : null,
                ImFlash              = row["im_flash"]?.ToString(),
                ImExposureMode       = row["im_exposure_mode"]?.ToString(),
                ImWhiteBalance       = row["im_white_balance"]?.ToString(),
                ImDateTaken          = row["im_date_taken"] != DBNull.Value ? (DateTime?)row["im_date_taken"] : null,
                ImOrientation        = row["im_orientation"] != DBNull.Value ? Convert.ToInt32(row["im_orientation"]) : null,
                ImCompressionQuality = row["im_compression_quality"] != DBNull.Value ? Convert.ToInt32(row["im_compression_quality"]) : null,
                ImUploadBatch        = row["im_upload_batch"]?.ToString(),
                ImCreatedUser        = row["im_created_user"]?.ToString(),
                ImCreatedDate        = row["im_created_date"] != DBNull.Value ? (DateTime?)row["im_created_date"] : null,
                ImModifiedUser       = row["im_modified_user"]?.ToString(),
                ImModifiedDate       = row["im_modified_date"] != DBNull.Value ? (DateTime?)row["im_modified_date"] : null,
                ImActive             = row["im_active"] != DBNull.Value ? Convert.ToInt32(row["im_active"]) : 1
            };
        }

        private ImageLinked MapToImageLinked(DataRow row)
        {
            return new ImageLinked
            {
                IlId                 = row["il_id"] != DBNull.Value ? Convert.ToInt32(row["il_id"]) : 0,
                IlMasterId           = row["il_master_id"] != DBNull.Value ? Convert.ToInt32(row["il_master_id"]) : 0,
                IlFileName           = row["il_file_name"]?.ToString(),
                IlFilePath           = ImageUrlHelper.BuildImageUrl(row["il_file_path"]?.ToString()),
                IlFileSize           = row["il_file_size"] != DBNull.Value ? Convert.ToInt64(row["il_file_size"]) : null,
                IlWidth              = row["il_width"] != DBNull.Value ? Convert.ToInt32(row["il_width"]) : null,
                IlHeight             = row["il_height"] != DBNull.Value ? Convert.ToInt32(row["il_height"]) : null,
                IlFormat             = row["il_format"]?.ToString(),
                IlColorSpace         = row["il_color_space"]?.ToString(),
                IlBitDepth           = row["il_bit_depth"] != DBNull.Value ? Convert.ToInt32(row["il_bit_depth"]) : null,
                IlDpiX               = row["il_dpi_x"] != DBNull.Value ? Convert.ToDecimal(row["il_dpi_x"]) : null,
                IlDpiY               = row["il_dpi_y"] != DBNull.Value ? Convert.ToDecimal(row["il_dpi_y"]) : null,
                IlExifData           = row["il_exif_data"]?.ToString(),
                IlCameraMake         = row["il_camera_make"]?.ToString(),
                IlCameraModel        = row["il_camera_model"]?.ToString(),
                IlLensModel          = row["il_lens_model"]?.ToString(),
                IlFocalLength        = row["il_focal_length"] != DBNull.Value ? Convert.ToDecimal(row["il_focal_length"]) : null,
                IlAperture           = row["il_aperture"]?.ToString(),
                IlShutterSpeed       = row["il_shutter_speed"]?.ToString(),
                IlIso                = row["il_iso"] != DBNull.Value ? Convert.ToInt32(row["il_iso"]) : null,
                IlFlash              = row["il_flash"]?.ToString(),
                IlExposureMode       = row["il_exposure_mode"]?.ToString(),
                IlWhiteBalance       = row["il_white_balance"]?.ToString(),
                IlDateTaken          = row["il_date_taken"] != DBNull.Value ? (DateTime?)row["il_date_taken"] : null,
                IlOrientation        = row["il_orientation"] != DBNull.Value ? Convert.ToInt32(row["il_orientation"]) : null,
                IlCompressionQuality = row["il_compression_quality"] != DBNull.Value ? Convert.ToInt32(row["il_compression_quality"]) : null,
                IlQualityLevel       = row["il_quality_level"]?.ToString(),
                IlQualityType        = row["il_quality_type"]?.ToString(),
                IlUploadBatch        = row["il_upload_batch"]?.ToString(),
                IlCreatedUser        = row["il_created_user"]?.ToString(),
                IlCreatedDate        = row["il_created_date"] != DBNull.Value ? (DateTime?)row["il_created_date"] : null,
               ImActive             = row["im_active"] != DBNull.Value ? Convert.ToInt32(row["im_active"]) : 1
            };
        }

        public async Task<ImageAuditTrailResponse> GetImagesWithAuditTrail(string assessmentType, string userId)
        {
            try
            {
                // ✅ FIXED: Removed the CASE WHEN filter from GROUP BY to ensure we count all linked images
                var query = @"
                    SELECT 
                        im.im_id,
                        im.im_file_name,
                        im.im_file_path,
                        im.im_file_size,
                        im.im_width,
                        im.im_height,
                        im.im_format,
                        im.im_upload_batch,
                        im.im_created_date,
                        im.im_created_user,
                        im.im_modified_date,
                        im.im_modified_user,
                        COUNT(il.il_id) as linked_count
                    FROM image_master im
                    LEFT JOIN image_linked il ON im.im_id = il.il_master_id AND il.im_active = 1
                    WHERE im.im_assessment_type = @assessmentType
                    AND im.im_active = 1
                    GROUP BY im.im_id, im.im_file_name, im.im_file_path, im.im_file_size, 
                             im.im_width, im.im_height, im.im_format, im.im_upload_batch,
                             im.im_created_date, im.im_created_user, im.im_modified_date, im.im_modified_user
                    ORDER BY im.im_created_date DESC";

                var parameters = new[]
                {
                    new MySqlParameter("@assessmentType", assessmentType)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var images = new List<ImageAuditTrail>();
                foreach (DataRow row in result.Rows)
                {
                    var imageAudit = new ImageAuditTrail
                    {
                        ImId = row["im_id"] != DBNull.Value ? Convert.ToInt32(row["im_id"]) : 0,
                        ImFileName = row["im_file_name"]?.ToString(),
                        ImFilePath = ImageUrlHelper.BuildImageUrl(row["im_file_path"]?.ToString()),
                        ImFileSize = row["im_file_size"] != DBNull.Value ? Convert.ToInt64(row["im_file_size"]) : null,
                        ImWidth = row["im_width"] != DBNull.Value ? Convert.ToInt32(row["im_width"]) : null,
                        ImHeight = row["im_height"] != DBNull.Value ? Convert.ToInt32(row["im_height"]) : null,
                        ImFormat = row["im_format"]?.ToString(),
                        ImUploadBatch = row["im_upload_batch"]?.ToString(),
                        ImCreatedDate = row["im_created_date"] != DBNull.Value ? (DateTime?)row["im_created_date"] : null,
                        ImCreatedUser = row["im_created_user"]?.ToString(),
                        ImModifiedDate = row["im_modified_date"] != DBNull.Value ? (DateTime?)row["im_modified_date"] : null,
                        ImModifiedUser = row["im_modified_user"]?.ToString(),
                        LinkedImagesCount = row["linked_count"] != DBNull.Value ? Convert.ToInt32(row["linked_count"]) : 0
                    };

                    // Get linked images for this master (only active ones)
                    if (imageAudit.LinkedImagesCount > 0)
                    {
                        var linkedQuery = @"
                            SELECT 
                                il_id,
                                il_file_name,
                                il_file_path,
                                il_file_size,
                                il_width,
                                il_height,
                                il_format,
                                il_color_space,
                                il_bit_depth,
                                il_dpi_x,
                                il_dpi_y,
                                il_orientation,
                                il_compression_quality,
                                il_exif_data,
                                il_camera_make,
                                il_camera_model,
                                il_lens_model,
                                il_focal_length,
                                il_aperture,
                                il_shutter_speed,
                                il_iso,
                                il_flash,
                                il_exposure_mode,
                                il_white_balance,
                                il_date_taken,
                                il_upload_batch,
                                il_quality_level,
                                il_quality_type,
                                il_created_date,
                                il_created_user
                            FROM image_linked
                            WHERE il_master_id = @masterId
                            AND im_active = 1
                            ORDER BY CAST(il_quality_level AS UNSIGNED) ASC";

                        var linkedParams = new[]
                        {
                            new MySqlParameter("@masterId", imageAudit.ImId)
                        };

                        var linkedResult = await Task.Run(() => _dbHelper.ExecuteQuery(linkedQuery, linkedParams));

                        foreach (DataRow linkedRow in linkedResult.Rows)
                        {
                            imageAudit.LinkedImages.Add(new LinkedImageAuditTrail
                            {
                                IlId = linkedRow["il_id"] != DBNull.Value ? Convert.ToInt32(linkedRow["il_id"]) : 0,
                                IlFileName = linkedRow["il_file_name"]?.ToString(),
                                IlFilePath = ImageUrlHelper.BuildImageUrl(linkedRow["il_file_path"]?.ToString()),
                                IlFileSize = linkedRow["il_file_size"] != DBNull.Value ? Convert.ToInt64(linkedRow["il_file_size"]) : null,
                                IlWidth = linkedRow["il_width"] != DBNull.Value ? Convert.ToInt32(linkedRow["il_width"]) : null,
                                IlHeight = linkedRow["il_height"] != DBNull.Value ? Convert.ToInt32(linkedRow["il_height"]) : null,
                                IlFormat = linkedRow["il_format"]?.ToString(),
                                IlColorSpace = linkedRow["il_color_space"]?.ToString(),
                                IlBitDepth = linkedRow["il_bit_depth"] != DBNull.Value ? Convert.ToInt32(linkedRow["il_bit_depth"]) : null,
                                IlDpiX = linkedRow["il_dpi_x"] != DBNull.Value ? Convert.ToDecimal(linkedRow["il_dpi_x"]) : null,
                                IlDpiY = linkedRow["il_dpi_y"] != DBNull.Value ? Convert.ToDecimal(linkedRow["il_dpi_y"]) : null,
                                IlOrientation = linkedRow["il_orientation"] != DBNull.Value ? Convert.ToInt32(linkedRow["il_orientation"]) : null,
                                IlCompressionQuality = linkedRow["il_compression_quality"] != DBNull.Value ? Convert.ToInt32(linkedRow["il_compression_quality"]) : null,
                                IlExifData = linkedRow["il_exif_data"]?.ToString(),
                                IlCameraMake = linkedRow["il_camera_make"]?.ToString(),
                                IlCameraModel = linkedRow["il_camera_model"]?.ToString(),
                                IlLensModel = linkedRow["il_lens_model"]?.ToString(),
                                IlFocalLength = linkedRow["il_focal_length"] != DBNull.Value ? Convert.ToDecimal(linkedRow["il_focal_length"]) : null,
                                IlAperture = linkedRow["il_aperture"]?.ToString(),
                                IlShutterSpeed = linkedRow["il_shutter_speed"]?.ToString(),
                                IlIso = linkedRow["il_iso"] != DBNull.Value ? Convert.ToInt32(linkedRow["il_iso"]) : null,
                                IlFlash = linkedRow["il_flash"]?.ToString(),
                                IlExposureMode = linkedRow["il_exposure_mode"]?.ToString(),
                                IlWhiteBalance = linkedRow["il_white_balance"]?.ToString(),
                                IlDateTaken = linkedRow["il_date_taken"] != DBNull.Value ? (DateTime?)linkedRow["il_date_taken"] : null,
                                IlUploadBatch = linkedRow["il_upload_batch"]?.ToString(),
                                IlQualityLevel = linkedRow["il_quality_level"]?.ToString(),
                                IlQualityType = linkedRow["il_quality_type"]?.ToString(),
                                IlCreatedDate = linkedRow["il_created_date"] != DBNull.Value ? (DateTime?)linkedRow["il_created_date"] : null,
                                IlCreatedUser = linkedRow["il_created_user"]?.ToString()
                            });
                        }
                    }

                    images.Add(imageAudit);
                }

                return new ImageAuditTrailResponse
                {
                    OutputCode = 1,
                    OutputMsg = $"Retrieved {images.Count} images with audit trail",
                    Data = images
                };
            }
            catch (Exception ex)
            {
                return new ImageAuditTrailResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving images with audit trail: {ex.Message}",
                    Data = new List<ImageAuditTrail>()
                };
            }
        }

        // Add this method to the ImageRepository class
        public async Task<int> GetImageCountByGroup(string groupCode, string userId)
        {
            try
            {
                var query = @"
                    SELECT COUNT(*) as cnt 
                    FROM image_master 
                    WHERE im_group_code = @groupCode 
                    AND im_active = 1";

                var parameters = new[]
                {
                    new MySqlParameter("@groupCode", groupCode)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));
                
                if (result.Rows.Count > 0)
                {
                    return Convert.ToInt32(result.Rows[0]["cnt"]);
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error getting image count for group {groupCode}");
                return 0;
            }
        }
    }
}