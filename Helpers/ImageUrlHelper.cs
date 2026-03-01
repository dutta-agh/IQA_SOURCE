namespace IQA_SOURCE.Helpers
{
    /// <summary>
    /// Shared helper for prepending the sub-application path to image web URLs stored in the DB.
    /// </summary>
    public static class ImageUrlHelper
    {
        // Cached once at startup — trailing slash stripped to avoid double-slash issues.
        private static readonly string _subPath = Constants.SubApplicationPathJS.TrimEnd('/');

        /// <summary>
        /// Prepends the sub-application path (e.g. /IQA) to a DB image path (e.g. /images/1.jpg).
        /// Returns an empty string for null/empty input.
        /// Safe to call multiple times — will not double-prefix.
        /// </summary>
        public static string BuildImageUrl(string? dbPath)
        {
            if (string.IsNullOrWhiteSpace(dbPath)) return string.Empty;
            if (string.IsNullOrEmpty(_subPath) || dbPath.StartsWith(_subPath)) return dbPath;
            return _subPath + "/" + dbPath.TrimStart('/');
        }
    }
}