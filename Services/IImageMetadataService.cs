using IQA_SOURCE.Models.Admin;

namespace IQA_SOURCE.Services
{
    public interface IImageMetadataService
    {
        Task<ImageMetadata> ExtractMetadata(string filePath);
    }
}