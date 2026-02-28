using IQA_SOURCE.Models;

namespace IQA_SOURCE.Data
{
    public interface IQuestionAnswerRepository
    {
        Task<QuestionPageViewModel> GetQuestionsForAssessment(string assessmentCode, string userId);
        Task<SubmissionResponse> SaveQuestionAnswers(QuestionSubmissionModel submission, string userId);
        Task<QuestionAnswerResponse> GetOrCreateQuestionAnswer(string sessionId, string assessmentCode, string ipAddress, string userAgent, string userId);
        Task<QuestionAnswerResultsResponse> GetAllQuestionAnswers(string userId);
        Task<QuestionAnswerResultsResponse> GetQuestionAnswersByCode(string assessmentCode, string userId);
        Task<List<QuestionAnswerExcelRow>> GetQuestionAnswersForExcel(string assessmentCode, string userId);
    }
}