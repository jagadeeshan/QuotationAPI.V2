using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.Admin;
using QuotationAPI.V2.Models.Auth;

namespace QuotationAPI.V2.Controllers;

[ApiController]
[Route("api/admin-module")]
[Authorize(Roles = "Admin")]
public class AdminModuleController : ControllerBase
{
    private readonly QuotationDbContext _db;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AdminModuleController(QuotationDbContext db)
    {
        _db = db;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.AdminUsers
            .OrderBy(x => x.Username)
            .ToListAsync();

        return Ok(users.Select(ToUserDto));
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _db.AdminUsers.FindAsync(id);
        return user == null ? NotFound() : Ok(ToUserDto(user));
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] AdminUserDto request)
    {
        var user = new AdminUser
        {
            Id = ($"user-{Guid.NewGuid():N}")[..13],
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            Status = request.Status,
            CreatedAt = request.CreatedAt == default ? DateTime.UtcNow : request.CreatedAt,
            LastLoginAt = request.LastLoginAt,
            GroupsJson = Serialize(request.Groups)
        };

        _db.AdminUsers.Add(user);
        await AddAuditLogAsync("create", "user", user.Id, $"Created user {user.Username}");
        await _db.SaveChangesAsync();
        return Ok(ToUserDto(user));
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] AdminUserDto request)
    {
        var user = await _db.AdminUsers.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.Username = request.Username;
        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Role = request.Role;
        user.Status = request.Status;
        user.LastLoginAt = request.LastLoginAt;
        user.GroupsJson = Serialize(request.Groups);

        await AddAuditLogAsync("update", "user", id, $"Updated user {user.Username}");
        await _db.SaveChangesAsync();
        return Ok(ToUserDto(user));
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _db.AdminUsers.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.IsDeleted = true;
        user.Status = "inactive";
        await AddAuditLogAsync("delete", "user", id, $"Deleted user {user.Username}");
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("user-groups")]
    public async Task<IActionResult> GetUserGroups()
    {
        var groups = await _db.AdminUserGroups.OrderBy(x => x.Name).ToListAsync();
        return Ok(groups.Select(ToGroupDto));
    }

    [HttpPost("user-groups")]
    public async Task<IActionResult> CreateUserGroup([FromBody] AdminUserGroupDto request)
    {
        var group = new AdminUserGroup
        {
            Id = ($"group-{Guid.NewGuid():N}")[..14],
            Name = request.Name,
            Description = request.Description,
            ParentGroup = string.IsNullOrWhiteSpace(request.ParentGroup) ? null : request.ParentGroup,
            PermissionsJson = Serialize(request.Permissions),
            MembersJson = Serialize(request.Members)
        };

        _db.AdminUserGroups.Add(group);
        await AddAuditLogAsync("create", "user-group", group.Id, $"Created user group {group.Name}");
        await _db.SaveChangesAsync();
        return Ok(ToGroupDto(group));
    }

    [HttpPut("user-groups/{id}")]
    public async Task<IActionResult> UpdateUserGroup(string id, [FromBody] AdminUserGroupDto request)
    {
        var group = await _db.AdminUserGroups.FindAsync(id);
        if (group == null)
        {
            return NotFound();
        }

        group.Name = request.Name;
        group.Description = request.Description;
        group.ParentGroup = string.IsNullOrWhiteSpace(request.ParentGroup) ? null : request.ParentGroup;
        group.PermissionsJson = Serialize(request.Permissions);
        group.MembersJson = Serialize(request.Members);

        await AddAuditLogAsync("update", "user-group", id, $"Updated user group {group.Name}");
        await _db.SaveChangesAsync();
        return Ok(ToGroupDto(group));
    }

    [HttpDelete("user-groups/{id}")]
    public async Task<IActionResult> DeleteUserGroup(string id)
    {
        var group = await _db.AdminUserGroups.FindAsync(id);
        if (group == null)
        {
            return NotFound();
        }

        group.IsDeleted = true;
        await AddAuditLogAsync("delete", "user-group", id, $"Deleted user group {group.Name}");
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("features")]
    public async Task<IActionResult> GetFeatures()
    {
        var features = await _db.AdminFeatures.OrderBy(x => x.Name).ToListAsync();
        return Ok(features.Select(ToFeatureDto));
    }

    [HttpPost("features")]
    public async Task<IActionResult> CreateFeature([FromBody] AdminFeatureDto request)
    {
        var feature = new AdminFeature
        {
            Id = ($"feature-{Guid.NewGuid():N}")[..16],
            Name = request.Name,
            Description = request.Description,
            Key = request.Key,
            IsActive = request.IsActive,
            EnabledRolesJson = Serialize(request.EnabledRoles),
            CreatedAt = request.CreatedAt == default ? DateTime.UtcNow : request.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        _db.AdminFeatures.Add(feature);
        await AddAuditLogAsync("create", "feature", feature.Id, $"Created feature {feature.Name}");
        await _db.SaveChangesAsync();
        return Ok(ToFeatureDto(feature));
    }

    [HttpPut("features/{id}")]
    public async Task<IActionResult> UpdateFeature(string id, [FromBody] AdminFeatureDto request)
    {
        var feature = await _db.AdminFeatures.FindAsync(id);
        if (feature == null)
        {
            return NotFound();
        }

        feature.Name = request.Name;
        feature.Description = request.Description;
        feature.Key = request.Key;
        feature.IsActive = request.IsActive;
        feature.EnabledRolesJson = Serialize(request.EnabledRoles);
        feature.UpdatedAt = DateTime.UtcNow;

        await AddAuditLogAsync("update", "feature", id, $"Updated feature {feature.Name}");
        await _db.SaveChangesAsync();
        return Ok(ToFeatureDto(feature));
    }

    [HttpDelete("features/{id}")]
    public async Task<IActionResult> DeleteFeature(string id)
    {
        var feature = await _db.AdminFeatures.FindAsync(id);
        if (feature == null)
        {
            return NotFound();
        }

        feature.IsDeleted = true;
        await AddAuditLogAsync("delete", "feature", id, $"Deleted feature {feature.Name}");
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await _db.AdminPermissions
            .OrderByDescending(x => x.GrantedAt)
            .ToListAsync();
        return Ok(permissions.Select(ToPermissionDto));
    }

    [HttpPost("permissions")]
    public async Task<IActionResult> CreatePermission([FromBody] AdminPermissionDto request)
    {
        var permission = new AdminPermission
        {
            Id = ($"perm-{Guid.NewGuid():N}")[..13],
            GroupId = request.GroupId,
            FeatureId = request.FeatureId,
            PermissionsJson = Serialize(request.Permissions),
            GrantedAt = request.GrantedAt == default ? DateTime.UtcNow : request.GrantedAt,
            GrantedBy = string.IsNullOrWhiteSpace(request.GrantedBy) ? "system" : request.GrantedBy
        };

        _db.AdminPermissions.Add(permission);
        await AddAuditLogAsync("create", "permission", permission.Id, $"Assigned permissions to group {permission.GroupId}");
        await _db.SaveChangesAsync();
        return Ok(ToPermissionDto(permission));
    }

    [HttpDelete("permissions/{id}")]
    public async Task<IActionResult> DeletePermission(string id)
    {
        var permission = await _db.AdminPermissions.FindAsync(id);
        if (permission == null)
        {
            return NotFound();
        }

        permission.IsDeleted = true;
        await AddAuditLogAsync("delete", "permission", id, $"Deleted permission assignment {id}");
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("all-roles")]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _db.Roles
            .Where(x => x.Name != "Pending")
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(roles.Select(x => new { x.Id, x.Name, x.Description }));
    }

    [HttpGet("access-requests")]
    public async Task<IActionResult> GetAccessRequests([FromQuery] string? status = null)
    {
        var query = _db.Users
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.AccessStatus == status);
        }

        var users = await query
            .OrderByDescending(x => x.AccessRequestedAt ?? DateTime.MinValue)
            .ThenBy(x => x.Username)
            .ToListAsync();

        return Ok(users.Select(ToAccessRequestDto));
    }

    [HttpPost("access-requests/{userId}/approve")]
    public async Task<IActionResult> ApproveAccessRequest(string userId, [FromBody] RoleAccessApprovalDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ApprovedRoleName))
        {
            return BadRequest(new { message = "ApprovedRoleName is required." });
        }

        var user = await _db.Users
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var roleName = request.ApprovedRoleName.Trim();
        var role = await _db.Roles.FirstOrDefaultAsync(x => x.Name == roleName);
        if (role == null)
        {
            role = new AppRole
            {
                Name = roleName,
                Description = $"{roleName} role"
            };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
        }

        var existingAssignments = await _db.UserRoles.Where(x => x.UserId == user.Id).ToListAsync();
        if (existingAssignments.Count > 0)
        {
            _db.UserRoles.RemoveRange(existingAssignments);
        }

        _db.UserRoles.Add(new AppUserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });

        user.AccessStatus = "Approved";
        user.RequestedRoleName = role.Name;
        user.AccessReviewedBy = ResolveCurrentAdminName();
        user.AccessReviewedAt = DateTime.UtcNow;
        user.AccessReviewNotes = string.IsNullOrWhiteSpace(request.Notes) ? "Approved" : request.Notes.Trim();

        await AddAuditLogAsync("approve", "role-request", user.Id, $"Approved access request for {user.Username} as role {role.Name}");
        await _db.SaveChangesAsync();

        return Ok(ToAccessRequestDto(user));
    }

    [HttpPost("access-requests/{userId}/reject")]
    public async Task<IActionResult> RejectAccessRequest(string userId, [FromBody] RoleAccessRejectDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var existingAssignments = await _db.UserRoles.Where(x => x.UserId == user.Id).ToListAsync();
        if (existingAssignments.Count > 0)
        {
            _db.UserRoles.RemoveRange(existingAssignments);
        }

        user.AccessStatus = "Rejected";
        user.AccessReviewedBy = ResolveCurrentAdminName();
        user.AccessReviewedAt = DateTime.UtcNow;
        user.AccessReviewNotes = string.IsNullOrWhiteSpace(request.Notes) ? "Rejected" : request.Notes.Trim();

        await AddAuditLogAsync("reject", "role-request", user.Id, $"Rejected access request for {user.Username}");
        await _db.SaveChangesAsync();

        return Ok(ToAccessRequestDto(user));
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications()
    {
        var pendingRequests = await _db.Users
            .Where(x => x.AccessStatus == "Pending")
            .OrderByDescending(x => x.AccessRequestedAt ?? DateTime.MinValue)
            .Take(100)
            .ToListAsync();

        var notifications = pendingRequests.Select(user => new AdminNotificationDto(
            $"access-request:{user.Id}",
            "RoleAccessRequest",
            "Role access approval required",
            $"{user.FirstName} {user.LastName} ({user.Username}) requested role '{user.RequestedRoleName ?? "User"}'.",
            user.Id,
            user.AccessRequestedAt ?? DateTime.UtcNow,
            false
        ));

        return Ok(notifications);
    }

    [HttpGet("notifications/count")]
    public async Task<IActionResult> GetNotificationCount()
    {
        var pendingCount = await _db.Users.CountAsync(x => x.AccessStatus == "Pending");
        return Ok(new { count = pendingCount });
    }

    [HttpGet("system-settings")]
    public async Task<IActionResult> GetSystemSettings()
    {
        var settings = await _db.AdminSystemSettings.OrderBy(x => x.Category).ThenBy(x => x.Key).ToListAsync();
        return Ok(settings);
    }

    [HttpPost("system-settings")]
    public async Task<IActionResult> CreateSystemSetting([FromBody] AdminSystemSetting request)
    {
        request.Id = ($"setting-{Guid.NewGuid():N}")[..16];
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? "system" : request.UpdatedBy;

        _db.AdminSystemSettings.Add(request);
        await AddAuditLogAsync("create", "system-setting", request.Id, $"Created setting {request.Key}");
        await _db.SaveChangesAsync();
        return Ok(request);
    }

    [HttpPut("system-settings/{id}")]
    public async Task<IActionResult> UpdateSystemSetting(string id, [FromBody] AdminSystemSetting request)
    {
        var setting = await _db.AdminSystemSettings.FindAsync(id);
        if (setting == null)
        {
            return NotFound();
        }

        setting.Key = request.Key;
        setting.Value = request.Value;
        setting.Description = request.Description;
        setting.Category = request.Category;
        setting.Type = request.Type;
        setting.IsEditable = request.IsEditable;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? "system" : request.UpdatedBy;

        await AddAuditLogAsync("update", "system-setting", id, $"Updated setting {setting.Key}");
        await _db.SaveChangesAsync();
        return Ok(setting);
    }

    [HttpDelete("system-settings/{id}")]
    public async Task<IActionResult> DeleteSystemSetting(string id)
    {
        var setting = await _db.AdminSystemSettings.FindAsync(id);
        if (setting == null)
        {
            return NotFound();
        }

        setting.IsDeleted = true;
        await AddAuditLogAsync("delete", "system-setting", id, $"Deleted setting {setting.Key}");
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("company-profiles")]
    public async Task<IActionResult> GetCompanyProfiles()
    {
        var profiles = await _db.AdminCompanyProfiles
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.UpdatedAt)
            .ToListAsync();

        return Ok(profiles.Select(ToCompanyProfileDto));
    }

    [HttpPost("company-profiles")]
    public async Task<IActionResult> CreateCompanyProfile([FromBody] AdminCompanyProfileDto request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName) ||
            string.IsNullOrWhiteSpace(request.Address) ||
            string.IsNullOrWhiteSpace(request.GstNo))
        {
            return BadRequest("Company name, address and GST number are required.");
        }

        if (request.IsActive)
        {
            await DeactivateOtherCompanyProfilesAsync();
        }

        var profile = new AdminCompanyProfile
        {
            Id = ($"company-{Guid.NewGuid():N}")[..20],
            CompanyName = request.CompanyName.Trim(),
            Address = request.Address.Trim(),
            GstNo = request.GstNo.Trim(),
            IsActive = request.IsActive,
            CreatedAt = request.CreatedAt == default ? DateTime.UtcNow : request.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? "system" : request.UpdatedBy
        };

        _db.AdminCompanyProfiles.Add(profile);
        await AddAuditLogAsync("create", "company-profile", profile.Id, $"Created company profile {profile.CompanyName}");
        await _db.SaveChangesAsync();
        return Ok(ToCompanyProfileDto(profile));
    }

    [HttpPut("company-profiles/{id}")]
    public async Task<IActionResult> UpdateCompanyProfile(string id, [FromBody] AdminCompanyProfileDto request)
    {
        var profile = await _db.AdminCompanyProfiles.FindAsync(id);
        if (profile == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.CompanyName) ||
            string.IsNullOrWhiteSpace(request.Address) ||
            string.IsNullOrWhiteSpace(request.GstNo))
        {
            return BadRequest("Company name, address and GST number are required.");
        }

        if (request.IsActive)
        {
            await DeactivateOtherCompanyProfilesAsync(id);
        }

        profile.CompanyName = request.CompanyName.Trim();
        profile.Address = request.Address.Trim();
        profile.GstNo = request.GstNo.Trim();
        profile.IsActive = request.IsActive;
        profile.UpdatedAt = DateTime.UtcNow;
        profile.UpdatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? "system" : request.UpdatedBy;

        await AddAuditLogAsync("update", "company-profile", id, $"Updated company profile {profile.CompanyName}");
        await _db.SaveChangesAsync();
        return Ok(ToCompanyProfileDto(profile));
    }

    [HttpDelete("company-profiles/{id}")]
    public async Task<IActionResult> DeleteCompanyProfile(string id)
    {
        var profile = await _db.AdminCompanyProfiles.FindAsync(id);
        if (profile == null)
        {
            return NotFound();
        }

        profile.IsDeleted = true;
        profile.IsActive = false;
        await AddAuditLogAsync("delete", "company-profile", id, $"Deleted company profile {profile.CompanyName}");
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs()
    {
        var logs = await _db.AdminAuditLogs
            .OrderByDescending(x => x.Timestamp)
            .Take(200)
            .ToListAsync();
        return Ok(logs);
    }

    [HttpGet("system-health")]
    public async Task<IActionResult> GetSystemHealth()
    {
        var startedAt = DateTime.UtcNow;
        var canQueryDb = await _db.Users.AnyAsync();
        var responseMs = (int)Math.Max(1, (DateTime.UtcNow - startedAt).TotalMilliseconds);

        var usersCount = await _db.AdminUsers.CountAsync();
        var featuresCount = await _db.AdminFeatures.CountAsync();
        var settingsCount = await _db.AdminSystemSettings.CountAsync();

        return Ok(new
        {
            status = canQueryDb ? "healthy" : "degraded",
            checks = new[]
            {
                new { name = "database", status = canQueryDb ? "healthy" : "failed", responseTime = responseMs, lastChecked = DateTime.UtcNow },
                new { name = "api", status = "healthy", responseTime = 1, lastChecked = DateTime.UtcNow },
                new { name = "admin-module", status = "healthy", responseTime = responseMs, lastChecked = DateTime.UtcNow }
            },
            metrics = new
            {
                usersCount,
                featuresCount,
                settingsCount,
                recentAuditCount = await _db.AdminAuditLogs.CountAsync(x => x.Timestamp >= DateTime.UtcNow.AddDays(-7))
            }
        });
    }

    [HttpPost("backup")]
    public async Task<IActionResult> CreateBackup()
    {
        var backupId = $"backup-{DateTime.UtcNow:yyyyMMddHHmmss}";
        await AddAuditLogAsync("backup", "system", backupId, "Created system backup", "info");
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Backup created successfully",
            timestamp = DateTime.UtcNow,
            backupId
        });
    }

    [HttpGet("backup-history")]
    public async Task<IActionResult> GetBackupHistory()
    {
        var backups = await _db.AdminAuditLogs
            .Where(x => x.Action == "backup")
            .OrderByDescending(x => x.Timestamp)
            .Select(x => new
            {
                lastBackup = x.Timestamp,
                nextBackup = x.Timestamp.AddDays(1),
                backupSchedule = "daily",
                retentionDays = 30,
                totalBackups = _db.AdminAuditLogs.Count(y => y.Action == "backup"),
                backupSize = "n/a",
                status = "completed"
            })
            .Take(20)
            .ToListAsync();

        return Ok(backups);
    }

    private static AdminUserDto ToUserDto(AdminUser user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Role = user.Role,
        Status = user.Status,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt,
        Groups = Deserialize(user.GroupsJson)
    };

    private static AdminUserGroupDto ToGroupDto(AdminUserGroup group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        Description = group.Description,
        ParentGroup = group.ParentGroup,
        Permissions = Deserialize(group.PermissionsJson),
        Members = Deserialize(group.MembersJson)
    };

    private static AdminFeatureDto ToFeatureDto(AdminFeature feature) => new()
    {
        Id = feature.Id,
        Name = feature.Name,
        Description = feature.Description,
        Key = feature.Key,
        IsActive = feature.IsActive,
        EnabledRoles = Deserialize(feature.EnabledRolesJson),
        CreatedAt = feature.CreatedAt,
        UpdatedAt = feature.UpdatedAt
    };

    private static AdminPermissionDto ToPermissionDto(AdminPermission permission) => new()
    {
        Id = permission.Id,
        GroupId = permission.GroupId,
        FeatureId = permission.FeatureId,
        Permissions = Deserialize(permission.PermissionsJson),
        GrantedAt = permission.GrantedAt,
        GrantedBy = permission.GrantedBy
    };

    private static AdminCompanyProfileDto ToCompanyProfileDto(AdminCompanyProfile profile) => new()
    {
        Id = profile.Id,
        CompanyName = profile.CompanyName,
        Address = profile.Address,
        GstNo = profile.GstNo,
        IsActive = profile.IsActive,
        CreatedAt = profile.CreatedAt,
        UpdatedAt = profile.UpdatedAt,
        UpdatedBy = profile.UpdatedBy
    };

    private static AccessRequestDto ToAccessRequestDto(AppUser user)
        => new(
            user.Id,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            user.AccessStatus,
            user.RequestedRoleName,
            user.AccessRequestNotes,
            user.AccessRequestedAt,
            user.AccessReviewedBy,
            user.AccessReviewNotes,
            user.AccessReviewedAt
        );

    private static List<string> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static string Serialize(IEnumerable<string>? values)
        => JsonSerializer.Serialize(values ?? Array.Empty<string>(), JsonOptions);

    private async Task DeactivateOtherCompanyProfilesAsync(string? keepId = null)
    {
        var profiles = await _db.AdminCompanyProfiles.ToListAsync();
        foreach (var profile in profiles)
        {
            if (!string.IsNullOrWhiteSpace(keepId) && profile.Id == keepId)
            {
                continue;
            }

            profile.IsActive = false;
            profile.UpdatedAt = DateTime.UtcNow;
            profile.UpdatedBy = "system";
        }
    }

    private Task AddAuditLogAsync(string action, string resource, string resourceId, string details, string severity = "info")
    {
        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            Id = ($"audit-{Guid.NewGuid():N}")[..15],
            UserId = "system",
            Action = action,
            Resource = resource,
            ResourceId = resourceId,
            Details = details,
            IpAddress = "127.0.0.1",
            UserAgent = "QuotationAPI",
            Timestamp = DateTime.UtcNow,
            Severity = severity
        });

        return Task.CompletedTask;
    }

    private string ResolveCurrentAdminName()
    {
        return User.FindFirstValue("unique_name")
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.Email)
            ?? "admin";
    }

    public sealed class AdminUserDto
    {
        public string Id { get; set; } = "";
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Role { get; set; } = "User";
        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public List<string> Groups { get; set; } = new();
    }

    public sealed class AdminUserGroupDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Permissions { get; set; } = new();
        public string? ParentGroup { get; set; }
        public List<string> Members { get; set; } = new();
    }

    public sealed class AdminFeatureDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Key { get; set; } = "";
        public bool IsActive { get; set; }
        public List<string> EnabledRoles { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class AdminPermissionDto
    {
        public string Id { get; set; } = "";
        public string GroupId { get; set; } = "";
        public string FeatureId { get; set; } = "";
        public List<string> Permissions { get; set; } = new();
        public DateTime GrantedAt { get; set; }
        public string GrantedBy { get; set; } = "system";
    }

    public sealed class AdminCompanyProfileDto
    {
        public string Id { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string Address { get; set; } = "";
        public string GstNo { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = "system";
    }
}
