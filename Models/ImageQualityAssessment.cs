namespace IQA_SOURCE.Models
{
    // Raw image set (parent image) - linked to assessment
    public class RawImageSet
    {
        public int RisId { get; set; }
        public string RisAssessmentCode { get; set; } = string.Empty;
        public string RisCode { get; set; } = string.Empty;
        public string RisName { get; set; } = string.Empty;
        public string RisRawImagePath { get; set; } = string.Empty;
        public string RisActive { get; set; } = "Y";
        public int RisDisplayOrder { get; set; }
        public DateTime? RisCreatedDate { get; set; }
        public string RisCreatedUser { get; set; } = string.Empty;
        public DateTime? RisModifiedDate { get; set; }
        public string RisModifiedUser { get; set; } = string.Empty;
    }

    // Linked/processed images for each raw image
    public class LinkedImage
    {
        public int LiId { get; set; }
        public int LiRisId { get; set; }
        public string LiImagePath { get; set; } = string.Empty;
        public string LiImageLabel { get; set; } = string.Empty;
        public string LiActive { get; set; } = "Y";
        public int LiDisplayOrder { get; set; }
        public DateTime? LiCreatedDate { get; set; }
        public string LiCreatedUser { get; set; } = string.Empty;
        public DateTime? LiModifiedDate { get; set; }
        public string LiModifiedUser { get; set; } = string.Empty;
    }

    // User quality rating submission
    public class ImageQualityRating
    {
        public int IqrId { get; set; }
        public string IqrSessionId { get; set; } = string.Empty;
        public string IqrAssessmentCode { get; set; } = string.Empty;
        public int IqrRisId { get; set; }
        public int IqrLiId { get; set; }
        public int IqrQualityRating { get; set; }
        public string IqrIpAddress { get; set; } = string.Empty;
        public string IqrUserAgent { get; set; } = string.Empty;
        public DateTime? IqrCreatedDate { get; set; }
        public string MasterGroupCode { get; set; }
        public string GroupName { get; set; }  // NEW: Group display name
    }

    // Admin grid + Excel row for image ratings
    public class ImageRatingAdminRow
    {
        public int RatingId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string AssessmentCode { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime? RatedAt { get; set; }

        // Reference (master) image
        public int MasterImageId { get; set; }
        public string MasterImageName { get; set; } = string.Empty;
        public string MasterImageUrl { get; set; } = string.Empty;
        public int? MasterWidth { get; set; }
        public int? MasterHeight { get; set; }
        public string? MasterFormat { get; set; }
        public double? MasterDpiX { get; set; }
        public double? MasterDpiY { get; set; }
        public string? MasterExifData { get; set; }
        public string MasterGroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;

        /// <summary>Rating given to the main (master) image in Sort assessments. Null for non-Sort assessments.</summary>
        public int? MasterImageRating { get; set; }
        public string MasterImageRatingLabel => MasterImageRating switch
        {
            1 => "Bad",
            2 => "Poor",
            3 => "Fair",
            4 => "Good",
            5 => "Excellent",
            _ => "N/A"
        };

        // Rated (linked) image
        public int LinkedImageId { get; set; }
        public string LinkedImageName { get; set; } = string.Empty;
        public string LinkedImageUrl { get; set; } = string.Empty;
        public int? LinkedWidth { get; set; }
        public int? LinkedHeight { get; set; }
        public string? LinkedFormat { get; set; }
        public double? LinkedDpiX { get; set; }
        public double? LinkedDpiY { get; set; }
        public string? LinkedExifData { get; set; }
        public string? LinkedQualityLevel { get; set; }
        public string? LinkedQualityType { get; set; }

        public int TimeTakenMilliseconds { get; set; }
        public int DisplayDurationMilliseconds { get; set; }
        public DateTime? RatingTimestamp { get; set; }
        public int SessionTotalTimeMs { get; set; }

        // Rating
        public int QualityRating { get; set; }
        public string QualityRatingLabel => QualityRating switch
        {
            1 => "Bad",
            2 => "Poor",
            3 => "Fair",
            4 => "Good",
            5 => "Excellent",
            _ => "N/A"
        };

        public int Id { get; internal set; }
    }

    // View models
    public class ImageAssessmentViewModel
    {
        public string SessionId { get; set; } = string.Empty;
        public string AssessmentCode { get; set; } = string.Empty;
        public string AssessmentName { get; set; } = string.Empty;
        public RawImageSet RawImage { get; set; } = new();
        public List<LinkedImage> LinkedImages { get; set; } = new();
        public int CurrentSetNumber { get; set; }
        public int TotalSets { get; set; }
        public bool IsCompleted { get; set; }
    }

    // Sort assessment: all images (raw + linked) shown together, each rated 1-5 uniquely
    public class SortAssessmentViewModel
    {
        public string SessionId { get; set; } = string.Empty;
        public string AssessmentCode { get; set; } = string.Empty;
        public string AssessmentName { get; set; } = string.Empty;
        public RawImageSet RawImage { get; set; } = new();

        /// <summary>All images shuffled together (raw image is embedded anonymously among these).</summary>
        public List<SortImageItem> AllImages { get; set; } = new();

        public int CurrentSetNumber { get; set; }
        public int TotalSets { get; set; }
        public bool IsCompleted { get; set; }
    }

    /// <summary>Represents one image card in the Sort assessment grid (raw or linked).</summary>
    public class SortImageItem
    {
        /// <summary>For linked images: il_id. For the raw image: 0.</summary>
        public int ImageId { get; set; }
        public bool IsRawImage { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string ImageLabel { get; set; } = string.Empty;
    }

    /// <summary>Per-image rating sent from the Sort page with millisecond precision timing.</summary>
    public class SortImageRatingEntry
    {
        /// <summary>il_id for a linked image, 0 for the raw/reference image.</summary>
        public int ImageId { get; set; }
        
        public bool IsRawImage { get; set; }
        
        /// <summary>Quality rating on -3 to +3 scale where -3=Much Worse, 0=Same, +3=Much Better</summary>
        public int Rating { get; set; }

        // ✅ TIME TRACKING IN MILLISECONDS FOR PRECISION

        /// <summary>Time taken to rate this specific image in milliseconds (from display to rating submission).</summary>
        public long TimeTakenMilliseconds { get; set; } = 0;

        /// <summary>Duration the image was displayed before being rated (in milliseconds).</summary>
        public long DisplayDurationMilliseconds { get; set; } = 0;

        /// <summary>Exact timestamp when this image was rated.</summary>
        public DateTime RatingTimestamp { get; set; } = DateTime.UtcNow;

        // ✅ CONVENIENCE PROPERTIES FOR SECONDS (READ-ONLY)

        /// <summary>Time taken to rate this image in seconds (derived from milliseconds).</summary>
        public decimal TimeTakenSeconds => TimeTakenMilliseconds / 1000m;

        /// <summary>Display duration in seconds (derived from milliseconds).</summary>
        public decimal DisplayDurationSeconds => DisplayDurationMilliseconds / 1000m;
    }

    /// <summary>Full Sort assessment submission payload with millisecond-precision time tracking.</summary>
    public class SortRatingSubmission
    {
        /// <summary>Unique session identifier for this assessment session.</summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Assessment code/type identifier (e.g., "SORT", "IQA").</summary>
        public string AssessmentCode { get; set; } = string.Empty;

        /// <summary>Raw image set ID being rated in this submission.</summary>
        public int RawImageSetId { get; set; }

        /// <summary>List of individual image ratings with per-image time tracking in milliseconds.</summary>
        public List<SortImageRatingEntry> Ratings { get; set; } = new();

        /// <summary>Client's IP address from ipify or fallback to connection IP.</summary>
        public string? IpAddress { get; set; }

        // ✅ SESSION-LEVEL TIME TRACKING IN MILLISECONDS

        /// <summary>Total time spent rating this entire image set in milliseconds.</summary>
        public long TotalSessionTimeMilliseconds { get; set; } = 0;

        /// <summary>Timestamp when the entire set submission was completed.</summary>
        public DateTime SetCompletionTime { get; set; } = DateTime.UtcNow;

        // ✅ CONVENIENCE PROPERTIES FOR SECONDS (READ-ONLY)

        /// <summary>Total session time in seconds (derived from milliseconds).</summary>
        public decimal TotalSessionTimeSeconds => TotalSessionTimeMilliseconds / 1000m;

        /// <summary>Average time per image rating in milliseconds.</summary>
        public long AverageTimePerImageMilliseconds => Ratings.Count > 0
            ? TotalSessionTimeMilliseconds / Ratings.Count
            : 0;

        /// <summary>Average time per image rating in seconds (derived from milliseconds).</summary>
        public decimal AverageTimePerImageSeconds => Ratings.Count > 0
            ? AverageTimePerImageMilliseconds / 1000m
            : 0;

        /// <summary>Fastest rating time among all images in this set (in milliseconds).</summary>
        public long FastestRatingMilliseconds => Ratings.Count > 0
            ? Ratings.Min(r => r.TimeTakenMilliseconds)
            : 0;

        /// <summary>Fastest rating time in seconds (derived from milliseconds).</summary>
        public decimal FastestRatingSeconds => FastestRatingMilliseconds / 1000m;

        /// <summary>Slowest rating time among all images in this set (in milliseconds).</summary>
        public long SlowestRatingMilliseconds => Ratings.Count > 0
            ? Ratings.Max(r => r.TimeTakenMilliseconds)
            : 0;

        /// <summary>Slowest rating time in seconds (derived from milliseconds).</summary>
        public decimal SlowestRatingSeconds => SlowestRatingMilliseconds / 1000m;
    }

    public class ImageQualitySubmission
    {
        public string SessionId { get; set; } = string.Empty;
        public string AssessmentCode { get; set; } = string.Empty;
        public int RawImageSetId { get; set; }
        public int SelectedLinkedImageId { get; set; }
        public int QualityRating { get; set; }
        public string? IpAddress { get; set; }
    }

    public class ImageAssessmentProgress
    {
        public string SessionId { get; set; } = string.Empty;
        public string AssessmentCode { get; set; } = string.Empty;
        public List<int> CompletedRawImageSetIds { get; set; } = new();
        public int TotalSets { get; set; }
        public int CompletedSets { get; set; }
        public bool IsCompleted { get; set; }
    }
}