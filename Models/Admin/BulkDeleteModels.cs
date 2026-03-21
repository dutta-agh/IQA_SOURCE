using System.Collections.Generic;

namespace IQA_SOURCE.Models.Admin
{
    /// <summary>
    /// Base bulk delete request for assessment-level deletion
    /// </summary>
    public class BulkDeleteAssessmentRequest
    {
        /// <summary>
        /// The assessment code to delete data for
        /// </summary>
        public string AssessmentCode { get; set; }

        /// <summary>
        /// Optional: Delete only specific image group's data
        /// </summary>
        public string ImageGroupCode { get; set; }

        /// <summary>
        /// Flag to delete user question answers
        /// </summary>
        public bool DeleteQuestionAnswers { get; set; }

        /// <summary>
        /// Flag to delete image quality ratings
        /// </summary>
        public bool DeleteImageRatings { get; set; }

        /// <summary>
        /// Flag to delete speed test logs
        /// </summary>
        public bool DeleteSpeedTestLogs { get; set; }

        /// <summary>
        /// Flag to delete images associated with this assessment
        /// </summary>
        public bool DeleteImages { get; set; }
    }

    /// <summary>
    /// Bulk delete request for image group-level deletion
    /// </summary>
    public class BulkDeleteImageGroupRequest
    {
        /// <summary>
        /// The image group code to delete
        /// </summary>
        public string ImageGroupCode { get; set; }

        /// <summary>
        /// Optional: Filter by assessment type
        /// </summary>
        public string AssessmentType { get; set; }

        /// <summary>
        /// Flag to delete images in the group
        /// </summary>
        public bool DeleteImages { get; set; }

        /// <summary>
        /// Flag to delete ratings related to images in this group
        /// </summary>
        public bool DeleteRelatedRatings { get; set; }
    }

    /// <summary>
    /// Request for combined assessment + image group deletion
    /// </summary>
    public class BulkDeleteByAssessmentRequest : BulkDeleteAssessmentRequest
    {
        // Inherits all properties from BulkDeleteAssessmentRequest
        // Can add additional properties specific to this scenario if needed
    }

    /// <summary>
    /// Response model for bulk delete operations at assessment level
    /// </summary>
    public class BulkDeleteAssessmentResponse
    {
        /// <summary>
        /// Operation success code (1 = success, 0 = failure)
        /// </summary>
        public int OutputCode { get; set; }

        /// <summary>
        /// Operation result message
        /// </summary>
        public string OutputMsg { get; set; }

        /// <summary>
        /// Detailed result data
        /// </summary>
        public BulkDeleteAssessmentResult Data { get; set; }
    }

    /// <summary>
    /// Detailed result of bulk delete operation
    /// </summary>
    public class BulkDeleteAssessmentResult
    {
        /// <summary>
        /// Assessment code that was deleted
        /// </summary>
        public string AssessmentCode { get; set; }

        /// <summary>
        /// Number of user question answers deleted
        /// </summary>
        public int DeletedQuestionAnswers { get; set; }

        /// <summary>
        /// Number of image quality ratings deleted
        /// </summary>
        public int DeletedImageRatings { get; set; }

        /// <summary>
        /// Number of speed test logs deleted
        /// </summary>
        public int DeletedSpeedTestLogs { get; set; }

        /// <summary>
        /// Number of images deleted/deactivated
        /// </summary>
        public int DeletedImages { get; set; }

        /// <summary>
        /// Number of linked images deleted/deactivated
        /// </summary>
        public int DeletedLinkedImages { get; set; }

        /// <summary>
        /// Total records deleted across all types
        /// </summary>
        public int TotalRecordsDeleted { get; set; }

        /// <summary>
        /// List of error messages if any errors occurred
        /// </summary>
        public List<string> ErrorMessages { get; set; } = new();
    }

    /// <summary>
    /// Response model for bulk delete by image group
    /// </summary>
 

    /// <summary>
    /// Detailed result of bulk delete by image group operation
    /// </summary>
    public class BulkDeleteResult
    {
        /// <summary>
        /// Number of master images deleted/deactivated
        /// </summary>
        public int ImagesDeleted { get; set; }

        /// <summary>
        /// Number of linked images deleted/deactivated
        /// </summary>
        public int LinkedImagesDeleted { get; set; }

        /// <summary>
        /// Number of image quality ratings deleted
        /// </summary>
        public int ImageRatingsDeleted { get; set; }

        /// <summary>
        /// Number of user responses deleted
        /// </summary>
        public int UserResponsesDeleted { get; set; }

        /// <summary>
        /// Number of speed test logs deleted
        /// </summary>
        public int SpeedTestLogsDeleted { get; set; }

        /// <summary>
        /// Total records deleted
        /// </summary>
        public int TotalDeleted { get; set; }

        /// <summary>
        /// Master images count (for preview)
        /// </summary>
        public int DeletedMasterImages { get; set; }

        /// <summary>
        /// Linked images count (for preview)
        /// </summary>
        public int DeletedLinkedImages { get; set; }

        /// <summary>
        /// Summary message of deletion
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// List of error messages if any
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// List of error messages (alias)
        /// </summary>
        public List<string> ErrorMessages { get; set; } = new();
    }

    /// <summary>
    /// Preview data before bulk delete
    /// </summary>
    public class BulkDeletePreview
    {
        /// <summary>
        /// Assessment code being previewed
        /// </summary>
        public string AssessmentCode { get; set; }

        /// <summary>
        /// Image group code (if applicable)
        /// </summary>
        public string ImageGroupCode { get; set; }

        /// <summary>
        /// Count of user responses to be deleted
        /// </summary>
        public int UserResponseCount { get; set; }

        /// <summary>
        /// Count of image ratings to be deleted
        /// </summary>
        public int ImageRatingCount { get; set; }

        /// <summary>
        /// Count of speed test logs to be deleted
        /// </summary>
        public int SpeedTestLogCount { get; set; }

        /// <summary>
        /// Count of master images to be deleted/deactivated
        /// </summary>
        public int MasterImagesCount { get; set; }

        /// <summary>
        /// Count of linked images to be deleted/deactivated
        /// </summary>
        public int LinkedImagesCount { get; set; }

        /// <summary>
        /// List of related image groups
        /// </summary>
        public List<string> RelatedImageGroups { get; set; } = new();
    }
}