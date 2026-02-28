namespace IQA_SOURCE.Models.Admin
{
    public class DashboardStats
    {
        public int TotalAssessmentTypes { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalImages { get; set; }
        public int TotalLinkedImages { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public List<AssessmentImageSummary> AssessmentImageSummaries { get; set; } = new();
    }

    public class AssessmentImageSummary
    {
        public string AssessmentType { get; set; }
        public string AssessmentName { get; set; }
        public int TotalMasterImages { get; set; }
        public int TotalLinkedImages { get; set; }
        public int ImagesWithLinked { get; set; }
        public DateTime? LastUploadDate { get; set; }
        public string LastUploadBy { get; set; }
        public decimal AverageFileSize { get; set; }
        public long TotalStorageUsed { get; set; }
    }

    public class DashboardStatsResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; }
        public DashboardStats Data { get; set; }
    }

    public class AssessmentImageSummaryResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; }
        public List<AssessmentImageSummary> Data { get; set; }
    }

    public class ImageAuditTrail
    {
        public int ImId { get; set; }
        public string ImFileName { get; set; }
        public string ImFilePath { get; set; }
        public long? ImFileSize { get; set; }
        public int? ImWidth { get; set; }
        public int? ImHeight { get; set; }
        public string ImFormat { get; set; }
        public string ImUploadBatch { get; set; }
        public DateTime? ImCreatedDate { get; set; }
        public string ImCreatedUser { get; set; }
        public DateTime? ImModifiedDate { get; set; }
        public string ImModifiedUser { get; set; }
        public int LinkedImagesCount { get; set; }
        public List<LinkedImageAuditTrail> LinkedImages { get; set; } = new();
    }

    public class LinkedImageAuditTrail
    {
        public int IlId { get; set; }
        public string IlFileName { get; set; }
        public string IlFilePath { get; set; }
        public long? IlFileSize { get; set; }
        public string IlQualityLevel { get; set; }
        public string IlQualityType { get; set; }
        public DateTime? IlCreatedDate { get; set; }
        public string IlCreatedUser { get; set; }
    }

    public class ImageAuditTrailResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; }
        public List<ImageAuditTrail> Data { get; set; }
    }
}