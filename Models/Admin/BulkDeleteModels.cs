namespace IQA_SOURCE.Models.Admin
{
    public class BulkDeleteAssessmentRequest
    {
        public string AssessmentCode { get; set; } = string.Empty;
        public bool DeleteQuestionAnswers { get; set; } = true;
        public bool DeleteImageRatings { get; set; } = true;
        public bool DeleteSpeedTestLogs { get; set; } = true;
    }

    public class BulkDeleteAssessmentResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; } = string.Empty;
        public BulkDeleteAssessmentResult Data { get; set; } = new();
    }

    public class BulkDeleteAssessmentResult
    {
        public string AssessmentCode { get; set; } = string.Empty;
        public int DeletedQuestionAnswers { get; set; }
        public int DeletedImageRatings { get; set; }
        public int DeletedSpeedTestLogs { get; set; }
        public int TotalRecordsDeleted { get; set; }
        public List<string> ErrorMessages { get; set; } = new();
        public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
    }
}