using IQA_SOURCE.Models.Colorblindness;

namespace IQA_SOURCE.Data
{
    public interface IColorblindnessRepository
    {
        // Admin - Image Management
        Task<ColorblindnessImageResponse> GetAllColorblindnessImages(string assessmentType, string userId);
        Task<ColorblindnessImageResponse> GetColorblindnessImageById(int imageId, string userId);
        Task<ColorblindnessImageResponse> SaveColorblindnessImage(ColorblindnessImage model, string userId);
        Task<ColorblindnessImageResponse> DeleteColorblindnessImage(int imageId, string userId);

        // User - Test
        Task<ColorblindnessTestResponse> GetColorblindnessImagesForTest(string assessmentType);
        Task<ColorblindnessTestResultResponse> SaveUserResponses(List<UserColorblindnessResponse> responses, string userId);

        // Reports
        Task<ColorblindnessTestResultResponse> GetColorblindnessResultsBySession(string sessionId, string userId);
        Task<ColorblindnessTestResultResponse> GetColorblindnessResultsBySessionId(string sessionId, string userId);
        Task<ColorblindnessTestResultResponse> GetAllColorblindnessResults(string assessmentType, string userId);
        Task<ColorblindnessTestResultResponse> GetColorblindnessResultsByDateRange(string assessmentType, DateTime startDate, DateTime endDate, string userId);
    }
}