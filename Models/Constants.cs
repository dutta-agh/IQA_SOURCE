namespace IQA_SOURCE
{
    public static class Constants
    {
        private static string? _subApplicationPathJS;
        private static string? _assessmentBaseUrl;

        public static string SubApplicationPathJS
        {
            get => _subApplicationPathJS ?? "/IQA";
            set => _subApplicationPathJS = value;
        }

        public static string AssessmentBaseUrl
        {
            get => _assessmentBaseUrl ?? "https://arete.tele.agh.edu.pl";
            set => _assessmentBaseUrl = value;
        }

        public static void Initialize(IConfiguration configuration)
        {
            SubApplicationPathJS = configuration["AppSettings:SubApplicationPath"] ?? "/IQA";
            AssessmentBaseUrl = configuration["AppSettings:AssessmentBaseUrl"] ?? "https://arete.tele.agh.edu.pl";
        }
    }
}