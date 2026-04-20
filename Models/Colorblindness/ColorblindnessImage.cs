namespace IQA_SOURCE.Models.Colorblindness
{
    /// <summary>
    /// Represents a colorblindness test image with answer options
    /// </summary>
    public class ColorblindnessImage
    {
        public int CbImageId { get; set; }
        public string AssessmentType { get; set; }
        public int ImageSequence { get; set; }
        public string ImageUrl { get; set; }
        public string CorrectAnswer { get; set; }
        public string IncorrectOptionOne { get; set; }
        public string IncorrectOptionTwo { get; set; }
        public string CantReadOption { get; set; } = "Can't Read";
        public string Description { get; set; }
        public int IsActive { get; set; } = 1;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class ColorblindnessImageResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; }
        public List<ColorblindnessImage> Data { get; set; } = new();
    }

    /// <summary>
    /// DTO for test display - includes correct answer for backend validation
    /// </summary>
    public class ColorblindnessImageForTest
    {
        public int CbImageId { get; set; }
        public int ImageSequence { get; set; }
        public string ImageUrl { get; set; }
        public string CorrectAnswer { get; set; }
        public List<string> Options { get; set; } = new();
    }

    public class ColorblindnessTestResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; }
        public List<ColorblindnessImageForTest> Data { get; set; } = new();
    }
}