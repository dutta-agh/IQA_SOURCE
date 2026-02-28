namespace IQA_SOURCE.Models.Admin
{
    public class SpeedTestLog
    {
        public int Id { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public int? ScreenWidth { get; set; }
        public int? ScreenHeight { get; set; }
        public decimal? DownloadSpeedMbps { get; set; }
        public decimal? ResolutionPassed { get; set; }
        public bool SpeedPassed { get; set; }
        public bool OverallPassed { get; set; }
        public DateTime? TestDateTime { get; set; }
        public string? SessionId { get; set; }
        public string? ReferrerUrl { get; set; }
        public string? BrowserInfo { get; set; }
        public string? AssessmentCode { get; set; }
        public bool PrivateModeDetected { get; set; }
        public string? PrivateModeBrowser { get; set; }
        public string? DeviceType { get; set; }
        public decimal? UploadSpeedMbps { get; set; }
        public int? Latency { get; set; }
    }

    public class SpeedTestLogResponse
    {
        public int OutputCode { get; set; }
        public string? OutputMsg { get; set; }
        public List<SpeedTestLog>? Data { get; set; }
    }

    public class SpeedTestLogSaveRequest
    {
        public string? SessionId { get; set; }
        public string? AssessmentCode { get; set; }
        public bool PrivateModeDetected { get; set; }
        public string? PrivateModeBrowser { get; set; }
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public bool ResolutionPassed { get; set; }
        public string? DeviceType { get; set; }
        public bool SpeedPassed { get; set; }
        public decimal? DownloadSpeedMbps { get; set; }
        public decimal? UploadSpeedMbps { get; set; }
        public int? Latency { get; set; }
        public bool OverallPassed { get; set; }
    }
}