using IQA_SOURCE.Models.Admin;

namespace IQA_SOURCE.Data
{
    public interface IAssessmentTypeRepository
    {
        Task<AssessmentTypeMasterResponse> GetAllAssessmentTypes(string userId);
        Task<AssessmentTypeMasterResponse> GetAssessmentTypeByCode(string atmCode, string userId);
        Task<AssessmentTypeMasterResponse> GetAssessmentTypeByUrlSlug(string urlSlug);
        Task<AssessmentTypeMasterResponse> InsertAssessmentType(AssessmentTypeMaster assessmentType, string userId);
        Task<AssessmentTypeMasterResponse> UpdateAssessmentType(AssessmentTypeMaster assessmentType, string userId);
        Task<AssessmentTypeMasterResponse> DeleteAssessmentType(string atmCode, string userId);
    }
}