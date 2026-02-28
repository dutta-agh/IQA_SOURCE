namespace IQA_SOURCE.Models.Admin
{
    public class AssessmentTypeMaster
    {
        public string AtmCode { get; set; }
        public string AtmName { get; set; }
        public string AtmUrlSlug { get; set; }
        public string AtmDescription { get; set; }
        public int AtmDurationMinutes { get; set; }
        public string AtmCreatedUser { get; set; }
        public DateTime? AtmCreatedDate { get; set; }
        public string AtmModifiedUser { get; set; }
        public DateTime? AtmModifiedDate { get; set; }
        public string AtmActive { get; set; }
    }

    public class AssessmentTypeMasterResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; }
        public List<AssessmentTypeMaster> Data { get; set; }
    }
}