using IQA_SOURCE.Models.Admin;

namespace IQA_SOURCE.Data
{
    public interface IBulkOperationsRepository
    {
        Task<BulkDeleteAssessmentResponse> BulkDeleteAssessmentData(BulkDeleteAssessmentRequest request, string userId);
        Task<(int OutputCode, string OutputMsg, List<string> Data)> GetAssessmentCodesWithData(string userId);
    }
}