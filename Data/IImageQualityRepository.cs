using IQA_SOURCE.Models;

namespace IQA_SOURCE.Data
{
    public interface IImageQualityRepository
    {
        Task<(int OutputCode, string OutputMsg, List<RawImageSet> Data)> GetRawImageSetsByAssessment(string assessmentCode, string userCode);
        Task<(int OutputCode, string OutputMsg, RawImageSet? Data)> GetNextRandomRawImageSet(string sessionId, string assessmentCode, string userCode);
        Task<(int OutputCode, string OutputMsg, List<LinkedImage> Data)> GetLinkedImagesByRawImageSetId(int rawImageSetId, string userCode);
        Task<(int OutputCode, string OutputMsg)> SaveImageQualityRating(ImageQualitySubmission submission, string ipAddress, string userAgent, string userCode);

        /// <summary>Save multiple per-image ratings for the Sort assessment type in a single call.</summary>
        Task<(int OutputCode, string OutputMsg)> SaveSortRatings(SortRatingSubmission submission, string ipAddress, string userAgent, string userCode);

        Task<(int OutputCode, string OutputMsg, ImageAssessmentProgress Data)> GetAssessmentProgress(string sessionId, string assessmentCode, string userCode);
        Task<(int OutputCode, string OutputMsg, List<int> Data)> GetCompletedRawImageSetIds(string sessionId, string assessmentCode, string userCode);
        Task<(int OutputCode, string OutputMsg, List<ImageRatingAdminRow> Data)> GetImageRatingsForAdmin(string? assessmentCode, string userCode);
    }
}