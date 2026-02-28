using IQA_SOURCE.Models.Admin;

namespace IQA_SOURCE.Data
{
    public interface IQuestionMasterRepository
    {
        Task<QuestionMasterResponse> GetAllQuestions(string userId);
        Task<QuestionWithOptionsResponse> GetQuestionWithOptions(int qId, string userId);
        Task<QuestionMasterResponse> InsertQuestion(QuestionMaster question, string userId);
        Task<QuestionMasterResponse> UpdateQuestion(QuestionMaster question, string userId);
        Task<QuestionMasterResponse> DeleteQuestion(int qId, string userId);
        Task<QuestionMasterResponse> InsertQuestionOption(QuestionOption option, string userId);
        Task<QuestionMasterResponse> UpdateQuestionOption(QuestionOption option, string userId);
        Task<QuestionMasterResponse> DeleteQuestionOption(int qoId, string userId);
        Task<List<QuestionOption>> GetQuestionOptions(int qId, string userId);
    }
}