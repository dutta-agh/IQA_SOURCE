namespace IQA_SOURCE.Models.Admin
{
    public class ContentMaster
    {
        public string CmCode { get; set; }
        public string CmContent { get; set; }
        public string CmCreatedUser { get; set; }
        public DateTime? CmCreatedDate { get; set; }
        public string CmModifiedUser { get; set; }
        public DateTime? CmModifiedDate { get; set; }
        public string CmActive { get; set; }
    }

    public class ContentMasterResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; }
        public List<ContentMaster> Data { get; set; }
    }
}