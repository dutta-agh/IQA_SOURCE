namespace IQA_SOURCE.Models
{
    public class UserActionLog
    {
        public long LogId { get; set; }
        public string? SessionId { get; set; }
        public string? AssessmentType { get; set; }
        public string? ActionType { get; set; }
        public string? ActionName { get; set; }
        public string? ControllerName { get; set; }
        public string? ActionMethod { get; set; }
        public string? RequestUrl { get; set; }
        public string? HttpMethod { get; set; }
        public string? RequestPayload { get; set; }
        public string? ResponsePayload { get; set; }
        public int? StatusCode { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime ActionTimestamp { get; set; }
        public int? DurationMs { get; set; }
        public string? ErrorMessage { get; set; }
        public string? AdditionalData { get; set; }
    }

    public class ActionLogResponse
    {
        public int OutputCode { get; set; }
        public string? OutputMsg { get; set; }
        public long? LogId { get; set; }
    }
}