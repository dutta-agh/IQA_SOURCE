namespace IQA_SOURCE.Models.Admin
{
    public class AssessmentTypeMaster
    {
        public string AtmCode { get; set; }
        public string AtmName { get; set; }
        public string AtmUrlSlug { get; set; }
        public string AtmDescription { get; set; }
        public int AtmDurationMinutes { get; set; }
        public DateTime? AtmStartDate { get; set; }
        public DateTime? AtmEndDate { get; set; }
        public string AtmCreatedUser { get; set; }
        public DateTime? AtmCreatedDate { get; set; }
        public string AtmModifiedUser { get; set; }
        public DateTime? AtmModifiedDate { get; set; }
        public string AtmActive { get; set; }

        // Helper property to check if assessment is currently available
        public bool IsAvailable
        {
            get
            {
                if (AtmActive != "Y") return false;
                
                var now = DateTime.Now;
                
                // Check start date
                if (AtmStartDate.HasValue && now < AtmStartDate.Value)
                    return false;
                
                // Check end date
                if (AtmEndDate.HasValue && now > AtmEndDate.Value)
                    return false;
                
                return true;
            }
        }
    }

    public class AssessmentTypeMasterResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; }
        public List<AssessmentTypeMaster> Data { get; set; }
    }
}