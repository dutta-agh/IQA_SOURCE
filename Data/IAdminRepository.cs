using IQA_SOURCE.Models.Admin;

    namespace IQA_SOURCE.Data
{
    public interface IAdminRepository
    {
        Task<LoginResponse> AuthenticateUser(string username, string password);
        Task<ContentMasterResponse> GetAllContents(string userId);
        Task<ContentMasterResponse> GetContentByCode(string cmCode, string userId);
        Task<ContentMasterResponse> InsertContent(ContentMaster content, string userId);
        Task<ContentMasterResponse> UpdateContent(ContentMaster content, string userId);
        Task<ContentMasterResponse> DeleteContent(string cmCode, string userId);
    }
}