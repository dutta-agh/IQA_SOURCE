namespace IQA_SOURCE.Models.Colorblindness
{
    public class ColorblindnessTestSubmission
    {
        public string SessionId { get; set; }
        public string AssessmentType { get; set; }
        public int TotalImages { get; set; }
        public int SkippedCount { get; set; }
        public List<ColorblindnessResponse> Responses { get; set; }
    }

    public class ColorblindnessResponse
    {
        public int ImageId { get; set; }
        public string SelectedAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public int TimeTakenSeconds { get; set; }
        public bool IsCorrect { get; set; }
        public bool? Skipped { get; set; }
    }
}