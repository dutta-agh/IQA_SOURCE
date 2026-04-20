using System.Text.Json.Serialization;

namespace IQA_SOURCE.Models.Admin
{

    public class ChangePasswordModel
    {
        public string UserId { get; set; }
        public string NewPassword { get; set; }
    }
    public class AdminUser
    {
        [JsonPropertyName("auId")]
        public string? AuId { get; set; }
        
        [JsonPropertyName("username")]
        public string? AuUsername { get; set; }
        
        [JsonPropertyName("user_name")]
        public string? AuUserName { get; set; }
        
        [JsonPropertyName("email")]
        public string? AuEmail { get; set; }
        
        [JsonPropertyName("user_role")]
        public string? AuRole { get; set; }
        
        [JsonPropertyName("is_active")]
        public string? AuActive { get; set; }
        
        [JsonPropertyName("password_hash")]
        public string? AuPasswordHash { get; set; }
        
        [JsonPropertyName("created_date")]
        public DateTime? AuCreatedDate { get; set; }
        
        [JsonPropertyName("created_user")]
        public string? AuCreatedUser { get; set; }
        
        [JsonPropertyName("modified_date")]
        public DateTime? AuModifiedDate { get; set; }
        
        [JsonPropertyName("modified_user")]
        public string? AuModifiedUser { get; set; }
        
        [JsonPropertyName("last_login")]
        public DateTime? AuLastLogin { get; set; }
    }

    public class AdminUserResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; } = string.Empty;
        public List<AdminUser> Data { get; set; } = new();
    }

    public class AdminUserDetailResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; } = string.Empty;
        public AdminUser Data { get; set; }
    }
}
