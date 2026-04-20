namespace IQA_SOURCE.Models.Colorblindness
{
    /// <summary>
    /// Records user's response to a colorblindness test image
    /// </summary>
    public class UserColorblindnessResponse
    {
        public string SessionId { get; set; }
        public string AssessmentType { get; set; }
        public int ImageId { get; set; }
        public int? ImageSequence { get; set; }
        public string SelectedAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public int? TimeTakenSeconds { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }

    public class ColorblindnessTestResult
    {
        public string SessionId { get; set; }
        public string AssessmentType { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public int SkippedQuestions { get; set; }
        public int CantReadAnswers { get; set; }
        public double AccuracyPercentage { get; set; }
        public double AverageTimePerQuestion { get; set; }
        public DateTime TestDateTime { get; set; }
        public DateTime? TestEndTime { get; set; }
        public string IpAddress { get; set; }
        public string TestStatus { get; set; }
        public string ColorblindType { get; set; }
        public bool IsPassed { get; set; }
        public string Notes { get; set; }
        public List<ImageWiseResult> ImageWiseResults { get; set; } = new();
    }

    /// <summary>
    /// Represents result details for a single image in the test
    /// </summary>
    public class ImageWiseResult
    {
        public int ImageSequence { get; set; }
        public int ImageId { get; set; }
        public string ImageUrl { get; set; }
        public string CorrectAnswer { get; set; }
        public string SelectedAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public string ResultStatus { get; set; } // "Correct", "Incorrect", or "Skipped"
        public int TimeTakenSeconds { get; set; }
    }

    public class ColorblindnessTestResultResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<ColorblindnessTestResult> Data { get; internal set; }
    }
}