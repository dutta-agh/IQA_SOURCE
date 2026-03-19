namespace IQA_SOURCE.Models.Admin
{
    public class AdminMenu
    {
        public int AmId { get; set; }
        public string AmCode { get; set; } = string.Empty;
        public string AmName { get; set; } = string.Empty;
        public string AmIcon { get; set; } = string.Empty;
        public string AmAction { get; set; } = string.Empty;
        public string AmController { get; set; } = string.Empty;
        public int AmOrderNo { get; set; }
        public int? AmParentId { get; set; }  // For hierarchical menus
        public string AmActive { get; set; } = "Y";
        public string AmMenuType { get; set; } = "page";  // page, divider, section
        public DateTime? AmCreatedDate { get; set; }
        public string AmCreatedUser { get; set; } = string.Empty;
        public DateTime? AmModifiedDate { get; set; }
        public string AmModifiedUser { get; set; } = string.Empty;
    }

    public class MenuRoleAccess
    {
        public int MraId { get; set; }
        public int MraMenuId { get; set; }
        public string MraRole { get; set; } = string.Empty;  // Admin, Manager, Operator, User
        public string MraCanRead { get; set; } = "N";
        public string MraCanCreate { get; set; } = "N";
        public string MraCanEdit { get; set; } = "N";
        public string MraCanDelete { get; set; } = "N";
        public DateTime? MraCreatedDate { get; set; }
    }

    public class AdminMenuResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; } = string.Empty;
        public List<AdminMenu> Data { get; set; } = new();
    }

    public class MenuRoleAccessResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; } = string.Empty;
        public List<MenuRoleAccess> Data { get; set; } = new();
    }
}
