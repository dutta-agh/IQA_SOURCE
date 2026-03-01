namespace IQA_SOURCE.Models.Admin
{
    public class SystemCheckParam
    {
        public int    ScpId           { get; set; }
        public string ScpParamCode    { get; set; } = string.Empty;
        public string ScpParamName    { get; set; } = string.Empty;
        public string ScpParamValue   { get; set; } = string.Empty;
        public string ScpDataType     { get; set; } = "string";
        public string? ScpDescription { get; set; }
        public string ScpActive       { get; set; } = "Y";
        public string? ScpCreatedUser  { get; set; }
        public DateTime? ScpCreatedDate  { get; set; }
        public string? ScpModifiedUser { get; set; }
        public DateTime? ScpModifiedDate { get; set; }
    }

    public class SystemCheckParamResponse
    {
        public int    OutputCode { get; set; }
        public string OutputMsg  { get; set; } = string.Empty;
        public List<SystemCheckParam> Data { get; set; } = [];
    }

    /// <summary>Typed, resolved settings ready for consumption by the controller / JS.</summary>
    public class SystemCheckSettings
    {
        public int      MinScreenWidth    { get; set; } = 1024;
        public int      MinScreenHeight   { get; set; } = 768;
        public string[] AllowedDevices    { get; set; } = ["Desktop", "Laptop"];
        public decimal  MinDownloadMbps   { get; set; } = 2m;
        public bool     IncognitoRequired { get; set; } = true;
    }
}