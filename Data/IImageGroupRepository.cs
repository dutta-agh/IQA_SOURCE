using IQA_SOURCE.Models.Admin;

namespace IQA_SOURCE.Data
{
    public interface IImageGroupRepository
    {
        Task<ImageGroupResponse> GetAllImageGroups(string userId);
        Task<ImageGroupResponse> GetImageGroupByCode(string groupCode, string userId);
        Task<ImageGroupResponse> GetImageGroupsByAssessment(string assessmentType, string userId);  // NEW
        Task<ImageGroupResponse> GetActiveImageGroup(string userId);
        Task<ImageGroupResponse> SaveImageGroup(ImageGroup group, string userId);
        Task<ImageGroupResponse> SetActiveGroup(string groupCode, string userId);
        Task<ImageGroupResponse> DeleteImageGroup(string groupCode, string userId);
        Task<BulkDeleteResponse> BulkDeleteImagesByGroup(string groupCode, string assessmentType, string userId);
    }
}