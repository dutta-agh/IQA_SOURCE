using System.Text.Json;

namespace IQA_SOURCE.Models.Admin
{
    public class ImageMaster
    {
        public int ImId { get; set; }
        public string ImAssessmentType { get; set; }
        public string ImFileName { get; set; }
        public string ImFilePath { get; set; }
        public long? ImFileSize { get; set; }
        public int? ImWidth { get; set; }
        public int? ImHeight { get; set; }
        public string ImFormat { get; set; }
        public string ImColorSpace { get; set; }
        public int? ImBitDepth { get; set; }
        public decimal? ImDpiX { get; set; }
        public decimal? ImDpiY { get; set; }
        public string ImExifData { get; set; } // JSON string
        public string ImCameraMake { get; set; }
        public string ImCameraModel { get; set; }
        public string ImLensModel { get; set; }
        public decimal? ImFocalLength { get; set; }
        public string ImAperture { get; set; }
        public string ImShutterSpeed { get; set; }
        public int? ImIso { get; set; }
        public string ImFlash { get; set; }
        public string ImExposureMode { get; set; }
        public string ImWhiteBalance { get; set; }
        public DateTime? ImDateTaken { get; set; }
        public int? ImOrientation { get; set; }
        public int? ImCompressionQuality { get; set; }
        public string ImUploadBatch { get; set; }
        public string ImCreatedUser { get; set; }
        public DateTime? ImCreatedDate { get; set; }
        public string ImModifiedUser { get; set; }
        public DateTime? ImModifiedDate { get; set; }
        public int ImActive { get; set; }
        
        // Navigation property
        public List<ImageLinked> LinkedImages { get; set; }
    }

    public class ImageLinked
    {
        public int IlId { get; set; }
        public int IlMasterId { get; set; }
        public string IlFileName { get; set; }
        public string IlFilePath { get; set; }
        public long? IlFileSize { get; set; }
        public int? IlWidth { get; set; }
        public int? IlHeight { get; set; }
        public string IlFormat { get; set; }
        public string IlColorSpace { get; set; }
        public int? IlBitDepth { get; set; }
        public decimal? IlDpiX { get; set; }
        public decimal? IlDpiY { get; set; }
        public string IlExifData { get; set; } // JSON string
        public string IlCameraMake { get; set; }
        public string IlCameraModel { get; set; }
        public string IlLensModel { get; set; }
        public decimal? IlFocalLength { get; set; }
        public string IlAperture { get; set; }
        public string IlShutterSpeed { get; set; }
        public int? IlIso { get; set; }
        public string IlFlash { get; set; }
        public string IlExposureMode { get; set; }
        public string IlWhiteBalance { get; set; }
        public DateTime? IlDateTaken { get; set; }
        public int? IlOrientation { get; set; }
        public int? IlCompressionQuality { get; set; }
        public string IlQualityLevel { get; set; }
        public string IlQualityType { get; set; }
        public string IlUploadBatch { get; set; }
        public string IlCreatedUser { get; set; }
        public DateTime? IlCreatedDate { get; set; }
        public int ImActive { get; set; }
    }

    public class ImageMetadata
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; }
        public string ColorSpace { get; set; }
        public int? BitDepth { get; set; }
        public decimal? DpiX { get; set; }
        public decimal? DpiY { get; set; }
        public Dictionary<string, object> ExifData { get; set; }
        public string CameraMake { get; set; }
        public string CameraModel { get; set; }
        public string LensModel { get; set; }
        public decimal? FocalLength { get; set; }
        public string Aperture { get; set; }
        public string ShutterSpeed { get; set; }
        public int? Iso { get; set; }
        public string Flash { get; set; }
        public string ExposureMode { get; set; }
        public string WhiteBalance { get; set; }
        public DateTime? DateTaken { get; set; }
        public int? Orientation { get; set; }
        public int? CompressionQuality { get; set; }
    }

    public class ImageUploadRequest
    {
        public string AssessmentType { get; set; }
        public IFormFile[] Files { get; set; }
    }

    public class ImageUploadResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; }
        public ImageUploadResult Data { get; set; }
    }

    public class ImageUploadResult
    {
        public int TotalFiles { get; set; }
        public int MasterImagesUploaded { get; set; }
        public int LinkedImagesUploaded { get; set; }
        public int FailedUploads { get; set; }
        public int SkippedMasterImages { get; set; }
        public List<string> SkippedMasterFileNames { get; set; }
        public List<string> ErrorMessages { get; set; }
        public string UploadBatch { get; set; }
    }

    public class ImageMasterResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; }
        public List<ImageMaster> Data { get; set; }
    }
}