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

    public DateTime? LastLogin { get; set; }

    public ICollection<AppUserRole> Roles { get; set; } = new List<AppUserRole>();
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
}

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
    DateTime? LastLogin
);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserInfoDto User
);

public record RefreshTokenResponse(string AccessToken, int ExpiresIn);
