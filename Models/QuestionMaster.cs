namespace IQA_SOURCE.Models.Admin
{
    public class QuestionMaster
    {
        public int QId { get; set; }
        public string QsCode { get; set; }
        public string QsText { get; set; }
        public string QsType { get; set; }  // SINGLE or MULTIPLE
        public string QsCategory { get; set; }
        public int? QsMaxSelections { get; set; }
        public int? QsOrderNo { get; set; }
        public int QsActive { get; set; }  // Changed from string to int (bit field: 1=Active, 0=Inactive)
        public string QsCreatedBy { get; set; }
        public DateTime? QsCreatedDate { get; set; }
        public string QsModifiedBy { get; set; }
        public DateTime? QsModifiedDate { get; set; }
    }

    public class QuestionOption
    {
        public int QoId { get; set; }
        public int QoQId { get; set; }
        public string QoText { get; set; }
        public string QoValue { get; set; }
        public int? QoOrderNo { get; set; }
        public int QoActive { get; set; }  // Changed from string to int (bit field: 1=Active, 0=Inactive)
        public DateTime? QoCreatedDate { get; set; }
        public DateTime? QoModifiedDate { get; set; }
    }

    public class QuestionMasterWithOptions
    {
        public QuestionMaster Question { get; set; }
        public List<QuestionOption> Options { get; set; }
    }

    public class QuestionMasterResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; }
        public List<QuestionMaster> Data { get; set; }
    }

    public class QuestionWithOptionsResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; }
        public QuestionMasterWithOptions Data { get; set; }
    }
}