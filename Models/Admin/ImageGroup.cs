namespace IQA_SOURCE.Models.Admin
{
    public class ImageGroup
    {
        public int IgId { get; set; }
        public string IgCode { get; set; } = string.Empty;
        public string IgName { get; set; } = string.Empty;
        public string IgDescription { get; set; } = string.Empty;
        public string IgActive { get; set; } = "Y";
        public string IgIsCurrentActive { get; set; } = "N";
        public string IgAssessmentType { get; set; } = string.Empty;  // NEW: Assessment type scope
        public string IgCreatedUser { get; set; } = string.Empty;
        public DateTime? IgCreatedDate { get; set; }
        public string IgModifiedUser { get; set; } = string.Empty;
        public DateTime? IgModifiedDate { get; set; }
    }

    public class ImageGroupResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; } = string.Empty;
        public List<ImageGroup> Data { get; set; } = new();
    }

    public class BulkDeleteByGroupRequest
    {
        public string GroupCode { get; set; } = string.Empty;
        public string AssessmentType { get; set; } = string.Empty;
    }

    public class BulkDeleteResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; } = string.Empty;
        public BulkDeleteResult Data { get; set; } = new();
    }

    public class BulkDeleteResult
    {
        public int DeletedMasterImages { get; set; }
        public int DeletedLinkedImages { get; set; }
        public int TotalDeleted { get; set; }
        public List<string> ErrorMessages { get; set; } = new();
    }
}