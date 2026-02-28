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
        public int LiRisId { get; set; } // References RawImageSet
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
        public int IqrRisId { get; set; } // Raw image set ID
        public int IqrLiId { get; set; } // Selected linked image ID
        public int IqrQualityRating { get; set; } // 1-5 scale
        public string IqrIpAddress { get; set; } = string.Empty;
        public string IqrUserAgent { get; set; } = string.Empty;
        public DateTime? IqrCreatedDate { get; set; }
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

    public class ImageQualitySubmission
    {
        public string SessionId { get; set; } = string.Empty;
        public string AssessmentCode { get; set; } = string.Empty;
        public int RawImageSetId { get; set; }
        public int SelectedLinkedImageId { get; set; }
        public int QualityRating { get; set; } // 1-5
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