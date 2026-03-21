using IQA_SOURCE.Models.Admin;

namespace IQA_SOURCE.Data
{
    public interface IBulkOperationsRepository
    {
        Task<BulkDeleteAssessmentResponse> BulkDeleteAssessmentData(BulkDeleteAssessmentRequest request, string userId);
        Task<(int OutputCode, string OutputMsg, List<string> Data)> GetAssessmentCodesWithData(string userId);
        Task<BulkDeleteResponse> BulkDeleteByImageGroup(BulkDeleteImageGroupRequest request, string userId);
        Task<BulkDeleteResponse> BulkDeleteByAssessment(BulkDeleteByAssessmentRequest request, string userId);
        Task<(int OutputCode, string OutputMsg, object Data)> GetBulkDeletePreview(string assessmentCode, string? groupCode, string userId);
    }
}