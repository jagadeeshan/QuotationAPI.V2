using System.ComponentModel.DataAnnotations;

namespace QuotationAPI.V2.Models.Auth;

public class AppUser
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Required, MaxLength(20)]
    public string AccessStatus { get; set; } = "Pending"; // Pending | Approved | Rejected

    [MaxLength(50)]
    public string? RequestedRoleName { get; set; }

    [MaxLength(500)]
    public string? AccessRequestNotes { get; set; }

    public DateTime? AccessRequestedAt { get; set; }

    [MaxLength(80)]
    public string? AccessReviewedBy { get; set; }

    [MaxLength(500)]
    public string? AccessReviewNotes { get; set; }

    public DateTime? AccessReviewedAt { get; set; }

    public DateTime? LastLogin { get; set; }

    public ICollection<AppUserRole> Roles { get; set; } = new List<AppUserRole>();
    public ICollection<AppRefreshToken> RefreshTokens { get; set; } = new List<AppRefreshToken>();
}

public class AppRefreshToken
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string TokenId { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    [MaxLength(128)]
    public string? ReplacedByTokenId { get; set; }

    [MaxLength(64)]
    public string? RevokedReason { get; set; }

    [MaxLength(256)]
    public string? CreatedByIp { get; set; }

    public AppUser? User { get; set; }
}

public class AppRole
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Description { get; set; } = string.Empty;

    public ICollection<AppUserRole> Users { get; set; } = new List<AppUserRole>();
}

public class AppUserRole
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string RoleId { get; set; } = string.Empty;

    public AppUser? User { get; set; }

    public AppRole? Role { get; set; }
}

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class RegisterRequest
{
    [Required, MinLength(3)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(2)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MinLength(2)]
    public string LastName { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? RequestedRoleName { get; set; }

    [MaxLength(500)]
    public string? AccessRequestNotes { get; set; }
}

public class RoleAccessRequestDto
{
    [Required, MinLength(2), MaxLength(50)]
    public string RequestedRoleName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class RoleAccessApprovalDto
{
    [Required, MinLength(2), MaxLength(50)]
    public string ApprovedRoleName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class RoleAccessRejectDto
{
    [MaxLength(500)]
    public string? Notes { get; set; }
}

public record AccessRequestDto(
    string UserId,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string AccessStatus,
    string? RequestedRoleName,
    string? AccessRequestNotes,
    DateTime? AccessRequestedAt,
    string? AccessReviewedBy,
    string? AccessReviewNotes,
    DateTime? AccessReviewedAt
);

public record AdminNotificationDto(
    string Id,
    string Type,
    string Title,
    string Message,
    string ResourceId,
    DateTime CreatedAt,
    bool IsRead
);

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public record UserRoleDto(string Id, string Name, string Description);

public record UserInfoDto(
    string Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    IEnumerable<UserRoleDto> Roles,
    IEnumerable<string> Permissions,
    bool IsActive,
    DateTime? LastLogin,
    string AccessStatus,
    string? RequestedRoleName,
    DateTime? AccessRequestedAt
);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserInfoDto User
);

public record RefreshTokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);
