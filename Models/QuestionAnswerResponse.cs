using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using IQA_SOURCE.Models.Admin;

namespace IQA_SOURCE.Models
{
    // Main question answer tracking (table: tbl_question_answers, columns: Ur*)
    public class QuestionAnswerResponse
    {
        public int UrId { get; set; }
        public string UrSessionId { get; set; }
        public string UrAssessmentCode { get; set; }
        public string UrIpAddress { get; set; }
        public string UrUserAgent { get; set; }
        public DateTime? UrStartTime { get; set; }
        public DateTime? UrSubmitTime { get; set; }
        public string UrStatus { get; set; }
        public DateTime? UrCreatedDate { get; set; }
    }

    // Individual question answer details (table: tbl_question_answer_details, columns: Urd*)
    public class QuestionAnswerDetail
    {
        public int UrdId { get; set; }
        public int UrdUrId { get; set; }
        public string UrdAssessmentCode { get; set; }
        public int UrdQId { get; set; }
        public string UrdAnswerText { get; set; }
        public int UrdAnswerValue { get; set; }
        public DateTime? UrdCreatedDate { get; set; }
    }

    // Submission model
    public class QuestionSubmissionModel
    {
        [Required(ErrorMessage = "Assessment code is required")]
        [JsonPropertyName("AssessmentCode")]
        public string AssessmentCode { get; set; } = string.Empty;

        [JsonPropertyName("SessionId")]
        public string SessionId { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one answer is required")]
        [JsonPropertyName("Answers")]
        public List<QuestionAnswer> Answers { get; set; } = new List<QuestionAnswer>();

        [JsonPropertyName("IpAddress")]
        public string? IpAddress { get; set; }
    }

    public class QuestionAnswer
    {
        [Required]
        [JsonPropertyName("QuestionId")]
        public int QuestionId { get; set; }

        [JsonPropertyName("QuestionCode")]
        public string QuestionCode { get; set; } = string.Empty;

        [JsonPropertyName("QuestionType")]
        public string QuestionType { get; set; } = string.Empty;

        [JsonPropertyName("SelectedOptionIds")]
        public List<int> SelectedOptionIds { get; set; } = new List<int>();

        [JsonPropertyName("SelectedOptionValues")]
        public List<string> SelectedOptionValues { get; set; } = new List<string>();
    }

    public class QuestionPageViewModel
    {
        public string AssessmentCode { get; set; }
        public string AssessmentName { get; set; }
        public List<QuestionWithOptions> Questions { get; set; }
    }

    public class QuestionWithOptions
    {
        public QuestionMaster Question { get; set; }
        public List<QuestionOption> Options { get; set; }
    }

    public class SubmissionResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; }
        public SubmissionResult Data { get; set; }
    }

    public class SubmissionResult
    {
        public int ResponseId { get; set; }
        public int TotalQuestions { get; set; }
        public int AnsweredQuestions { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class QuestionAnswerResultsResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; }
        public List<QuestionAnswerSummary> Data { get; set; }
    }

    public class QuestionAnswerSummary
    {
        public int ResponseId { get; set; }
        public string SessionId { get; set; }
        public string AssessmentCode { get; set; }
        public string AssessmentName { get; set; }
        public string IpAddress { get; set; }
        public string GroupName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? SubmitTime { get; set; }
        public string Status { get; set; }
        public int TotalQuestions { get; set; }
        public int AnsweredQuestions { get; set; }
        public List<ResponseAnswer> Answers { get; set; }
    }

    public class ResponseAnswer
    {
        public int QuestionId { get; set; }
        public string QuestionCode { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public List<string> SelectedOptions { get; set; }
        public string SelectedOptionsText { get; set; }
    }

    public class QuestionAnswerExcelRow
    {
        public string SessionId { get; set; }
        public string AssessmentCode { get; set; }
        public string IpAddress { get; set; }
        public DateTime? SubmitTime { get; set; }
        public string QuestionCode { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        /// <summary>
        /// Raw CSV of selected option texts returned by GROUP_CONCAT in the repository query.
        /// e.g. "Option A, Option C"
        /// </summary>
        public string SelectedOptions { get; set; }
        public Dictionary<string, string> OptionColumns { get; set; } = new Dictionary<string, string>();
    }

    public class ExcelExportData
    {
        public List<string> OptionNames { get; set; } = new List<string>();
        public List<QuestionAnswerExcelRow> Rows { get; set; } = new List<QuestionAnswerExcelRow>();
    }
}