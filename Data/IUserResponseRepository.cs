using IQA_SOURCE.Models;

namespace IQA_SOURCE.Data
{
    public interface IUserResponseRepository
    {
        Task<QuestionPageViewModel> GetQuestionsForAssessment(string assessmentCode, string userId);
        Task<SubmissionResponse> SaveUserResponses(QuestionSubmissionModel submission, string userId);
        Task<QuestionAnswerResponse> GetOrCreateUserResponse(string sessionId, string assessmentCode, string ipAddress, string userAgent, string userId);
    }
}