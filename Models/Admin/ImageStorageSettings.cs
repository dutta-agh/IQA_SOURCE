namespace IQA_SOURCE.Models.Admin
{
    public class ImageStorageSettings
    {
        public string BasePath { get; set; } = "/var/www/images";
        public string WebBasePath { get; set; } = "/images";
        public string[] AllowedExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".gif" };
        public int MaxFileSizeMB { get; set; } = 50;
    }
}