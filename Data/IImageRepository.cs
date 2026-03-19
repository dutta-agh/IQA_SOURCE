using IQA_SOURCE.Models.Admin;

namespace IQA_SOURCE.Data
{
    public interface IImageRepository
    {
        Task<ImageUploadResponse> SaveImages(List<ImageMaster> masterImages, List<ImageLinked> linkedImages, string userId);
        Task<ImageMasterResponse> GetImagesByAssessmentType(string assessmentType, string userId);
        Task<ImageMasterResponse> GetImageById(int imageId, string userId);
        Task<ImageMaster> GetMasterImageByFileName(string assessmentType, string fileName, string userId);
        Task<bool> CheckDuplicateFileName(string assessmentType, string fileName);
        Task<bool> CheckLinkedImageExists(int masterId, string fileName);
        Task<ImageUploadResponse> SaveLinkedImagesOnly(List<ImageLinked> linkedImages, string userId);
        Task<ImageMasterResponse> DeleteImage(int imageId, string userId);
        Task<ImageUploadResponse> ProcessFolderImages(string folderPath, string assessmentType, string userId);
        Task<ImageAuditTrailResponse> GetImagesWithAuditTrail(string assessmentType, string userId);
        Task<ImageMasterResponse> GetImagesByAssessmentTypeAndGroup(string assessmentType, string? groupCode, string userId);
    }
}