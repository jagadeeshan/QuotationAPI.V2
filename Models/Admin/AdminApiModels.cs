namespace QuotationAPI.V2.Models.Admin;

public class AdminUser
{
    public string Id { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Role { get; set; } = "User";
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string GroupsJson { get; set; } = "[]";
    public bool IsDeleted { get; set; } = false;
}

public class AdminUserGroup
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string PermissionsJson { get; set; } = "[]";
    public string? ParentGroup { get; set; }
    public string MembersJson { get; set; } = "[]";
    public bool IsDeleted { get; set; } = false;
}

public class AdminFeature
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Key { get; set; } = "";
    public bool IsActive { get; set; }
    public string EnabledRolesJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}

public class AdminPermission
{
    public string Id { get; set; } = "";
    public string GroupId { get; set; } = "";
    public string FeatureId { get; set; } = "";
    public string PermissionsJson { get; set; } = "[]";
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public string GrantedBy { get; set; } = "system";
    public bool IsDeleted { get; set; } = false;
}

public class AdminSystemSetting
{
    public string Id { get; set; } = "";
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "General";
    public string Type { get; set; } = "string";
    public bool IsEditable { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "system";
    public bool IsDeleted { get; set; } = false;
}

public class AdminAuditLog
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Action { get; set; } = "";
    public string Resource { get; set; } = "";
    public string? ResourceId { get; set; }
    public string Details { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Severity { get; set; } = "info";
}

public class AdminCompanyProfile
{
    public string Id { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string Address { get; set; } = "";
    public string GstNo { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "system";
    public bool IsDeleted { get; set; } = false;
}
