using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.Auth;
using QuotationAPI.V2.Services;

namespace QuotationAPI.V2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly QuotationDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public AuthController(QuotationDbContext db, ITokenService tokenService, IConfiguration configuration, IHostEnvironment environment)
    {
        _db = db;
        _tokenService = tokenService;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await EnsureDefaultAdminAndRolesAsync();

        var user = await _db.Users
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Username == request.Username && x.IsActive);

        if (user is null || !VerifyAndUpgradePassword(user, request.Password))
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var roles = user.Roles.Select(x => x.Role?.Name ?? "User").ToList();
        var accessToken = _tokenService.CreateAccessToken(user, roles);
        var refreshToken = _tokenService.CreateRefreshToken(user, roles);
        await SaveRefreshTokenAsync(user.Id, refreshToken, HttpContext.Connection.RemoteIpAddress?.ToString());

        var response = new LoginResponse(
            accessToken,
            refreshToken.Token,
            3600,
            ToUserInfoDto(user)
        );

        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await EnsureDefaultAdminAndRolesAsync();

        if (await _db.Users.AnyAsync(x => x.Username == request.Username || x.Email == request.Email))
        {
            return BadRequest(new { message = "Username or email already exists" });
        }

        var requestedRole = string.IsNullOrWhiteSpace(request.RequestedRoleName)
            ? "User"
            : request.RequestedRoleName.Trim();

        var user = new AppUser
        {
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PasswordHash = HashPassword(userData: null, request.Password),
            IsActive = true,
            LastLogin = DateTime.UtcNow,
            AccessStatus = "Pending",
            RequestedRoleName = requestedRole,
            AccessRequestNotes = request.AccessRequestNotes,
            AccessRequestedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var registrationRoles = Array.Empty<string>();
        var accessToken = _tokenService.CreateAccessToken(user, registrationRoles);
        var refreshToken = _tokenService.CreateRefreshToken(user, registrationRoles);
        await SaveRefreshTokenAsync(user.Id, refreshToken, HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new LoginResponse(
            accessToken,
            refreshToken.Token,
            3600,
            ToUserInfoDto(user)
        ));
    }

    [Authorize]
    [HttpPost("request-role-access")]
    public async Task<ActionResult<AccessRequestDto>> RequestRoleAccess([FromBody] RoleAccessRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await EnsureDefaultAdminAndRolesAsync();

        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Unable to resolve authenticated user." });
        }

        var user = await _db.Users
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        user.AccessStatus = "Pending";
        user.RequestedRoleName = request.RequestedRoleName.Trim();
        user.AccessRequestNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        user.AccessRequestedAt = DateTime.UtcNow;
        user.AccessReviewedAt = null;
        user.AccessReviewedBy = null;
        user.AccessReviewNotes = null;

        // Remove existing assigned roles while request is pending.
        var assignedRoles = await _db.UserRoles.Where(x => x.UserId == user.Id).ToListAsync();
        if (assignedRoles.Count > 0)
        {
            _db.UserRoles.RemoveRange(assignedRoles);
        }

        await _db.SaveChangesAsync();

        return Ok(ToAccessRequestDto(user));
    }

    [Authorize]
    [HttpGet("my-access-request")]
    public async Task<ActionResult<AccessRequestDto>> GetMyAccessRequest()
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Unable to resolve authenticated user." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(ToAccessRequestDto(user));
    }

    [HttpGet("available-roles")]
    public async Task<ActionResult<IEnumerable<UserRoleDto>>> GetAvailableRoles()
    {
        await EnsureDefaultAdminAndRolesAsync();

        var blocked = new[] { "Pending" };
        var roles = await _db.Roles
            .Where(x => !blocked.Contains(x.Name))
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(roles.Select(x => new UserRoleDto(x.Id, x.Name, x.Description)));
    }

    [Authorize(Roles = "Admin,Owner")]
    [HttpGet("review-access-requests")]
    public async Task<ActionResult<IEnumerable<AccessRequestDto>>> ReviewAccessRequests([FromQuery] string? status = "Pending")
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.AccessStatus == status);
        }

        var isOwner = User.IsInRole("Owner") && !User.IsInRole("Admin");
        if (isOwner)
        {
            query = query.Where(x => (x.RequestedRoleName ?? "") != "Admin");
        }

        var requests = await query
            .OrderByDescending(x => x.AccessRequestedAt ?? DateTime.MinValue)
            .ThenBy(x => x.Username)
            .ToListAsync();

        return Ok(requests.Select(ToAccessRequestDto));
    }

    [Authorize(Roles = "Admin,Owner")]
    [HttpPost("review-access-requests/{userId}/approve")]
    public async Task<ActionResult<AccessRequestDto>> ApproveAccessRequest(string userId, [FromBody] RoleAccessApprovalDto request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var approvedRoleName = request.ApprovedRoleName.Trim();
        var isOwner = User.IsInRole("Owner") && !User.IsInRole("Admin");
        if (isOwner && string.Equals(approvedRoleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var user = await _db.Users
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (isOwner && string.Equals(user.RequestedRoleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var role = await _db.Roles.FirstOrDefaultAsync(x => x.Name == approvedRoleName);
        if (role == null)
        {
            role = new AppRole
            {
                Name = approvedRoleName,
                Description = $"{approvedRoleName} role"
            };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
        }

        var assignedRoles = await _db.UserRoles.Where(x => x.UserId == user.Id).ToListAsync();
        if (assignedRoles.Count > 0)
        {
            _db.UserRoles.RemoveRange(assignedRoles);
        }

        _db.UserRoles.Add(new AppUserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });

        var reviewer = User.FindFirstValue("unique_name")
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.Email)
            ?? "reviewer";

        user.AccessStatus = "Approved";
        user.RequestedRoleName = role.Name;
        user.AccessReviewedBy = reviewer;
        user.AccessReviewedAt = DateTime.UtcNow;
        user.AccessReviewNotes = string.IsNullOrWhiteSpace(request.Notes) ? "Approved" : request.Notes.Trim();

        await _db.SaveChangesAsync();
        return Ok(ToAccessRequestDto(user));
    }

    [Authorize(Roles = "Admin,Owner")]
    [HttpPost("review-access-requests/{userId}/reject")]
    public async Task<ActionResult<AccessRequestDto>> RejectAccessRequest(string userId, [FromBody] RoleAccessRejectDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var isOwner = User.IsInRole("Owner") && !User.IsInRole("Admin");
        if (isOwner && string.Equals(user.RequestedRoleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var assignedRoles = await _db.UserRoles.Where(x => x.UserId == user.Id).ToListAsync();
        if (assignedRoles.Count > 0)
        {
            _db.UserRoles.RemoveRange(assignedRoles);
        }

        var reviewer = User.FindFirstValue("unique_name")
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.Email)
            ?? "reviewer";

        user.AccessStatus = "Rejected";
        user.AccessReviewedBy = reviewer;
        user.AccessReviewedAt = DateTime.UtcNow;
        user.AccessReviewNotes = string.IsNullOrWhiteSpace(request.Notes) ? "Rejected" : request.Notes.Trim();

        await _db.SaveChangesAsync();
        return Ok(ToAccessRequestDto(user));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { message = "Invalid refresh token" });
        }

        var principal = _tokenService.ValidateRefreshToken(request.RefreshToken);
        if (principal == null)
        {
            return Unauthorized(new { message = "Refresh token is invalid or expired" });
        }

        var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var tokenId = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Refresh token does not contain user information" });
        }

        if (string.IsNullOrWhiteSpace(tokenId))
        {
            return Unauthorized(new { message = "Refresh token is missing token identifier" });
        }

        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.UserId == userId && x.TokenId == tokenId);

        if (storedToken == null || storedToken.RevokedAt != null || storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Refresh token is invalid or revoked" });
        }

        if (!string.Equals(storedToken.TokenHash, HashToken(request.RefreshToken), StringComparison.Ordinal))
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedReason = "hash-mismatch";
            await _db.SaveChangesAsync();
            return Unauthorized(new { message = "Refresh token is invalid or revoked" });
        }

        var user = await _db.Users
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

        if (user == null)
        {
            return Unauthorized(new { message = "User not found or inactive" });
        }

        var roles = user.Roles.Select(x => x.Role?.Name ?? "User").ToList();
        var accessToken = _tokenService.CreateAccessToken(user, roles);
        var newRefreshToken = _tokenService.CreateRefreshToken(user, roles);
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.LastUsedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenId = newRefreshToken.TokenId;
        storedToken.RevokedReason = "rotated";
        await SaveRefreshTokenAsync(user.Id, newRefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString(), saveChanges: false);
        var accessExpiresSeconds = int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var minutes)
            ? minutes * 60
            : 3600;

        await _db.SaveChangesAsync();

        return Ok(new RefreshTokenResponse(accessToken, newRefreshToken.Token, accessExpiresSeconds));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request)
    {
        if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            var principal = _tokenService.ValidateRefreshToken(request.RefreshToken);
            var userId = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var tokenId = principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);

            if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(tokenId))
            {
                var storedToken = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.UserId == userId && x.TokenId == tokenId);
                if (storedToken != null && storedToken.RevokedAt == null)
                {
                    storedToken.RevokedAt = DateTime.UtcNow;
                    storedToken.RevokedReason = "logout";
                    storedToken.LastUsedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
            }
        }

        return Ok(new { message = "Logged out successfully" });
    }

    private async Task EnsureDefaultAdminAndRolesAsync()
    {
        var requiredRoles = new[]
        {
            (Name: "Admin", Description: "Application administrator with full access"),
            (Name: "Owner", Description: "Business owner with broad non-admin governance access"),
            (Name: "User", Description: "Standard business user"),
            (Name: "Manager", Description: "Manager role with broader operational access")
        };

        foreach (var role in requiredRoles)
        {
            if (!await _db.Roles.AnyAsync(x => x.Name == role.Name))
            {
                _db.Roles.Add(new AppRole
                {
                    Name = role.Name,
                    Description = role.Description
                });
            }
        }

        await _db.SaveChangesAsync();

        var adminUsername = _configuration["DefaultAdmin:Username"] ?? "admin";
        var adminEmail = _configuration["DefaultAdmin:Email"] ?? "admin@quotation.local";
        var adminPassword = _configuration["DefaultAdmin:Password"];
        var adminFirstName = _configuration["DefaultAdmin:FirstName"] ?? "System";
        var adminLastName = _configuration["DefaultAdmin:LastName"] ?? "Administrator";

        if (string.IsNullOrWhiteSpace(adminPassword) && _environment.IsDevelopment())
        {
            adminPassword = "Admin@123";
        }

        var adminUser = await _db.Users
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Username == adminUsername || x.Email == adminEmail);

        if (adminUser == null)
        {
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                return;
            }

            adminUser = new AppUser
            {
                Username = adminUsername,
                Email = adminEmail,
                FirstName = adminFirstName,
                LastName = adminLastName,
                PasswordHash = HashPassword(userData: null, adminPassword),
                IsActive = true,
                AccessStatus = "Approved",
                LastLogin = null
            };

            _db.Users.Add(adminUser);
            await _db.SaveChangesAsync();
        }
        else
        {
            adminUser.IsActive = true;
            adminUser.AccessStatus = "Approved";
            adminUser.RequestedRoleName = null;
            adminUser.AccessRequestNotes = null;
            adminUser.AccessRequestedAt = null;
            adminUser.AccessReviewedBy = "system";
            adminUser.AccessReviewedAt = DateTime.UtcNow;
            adminUser.AccessReviewNotes = "Default admin bootstrap";
        }

        var adminRole = await _db.Roles.FirstAsync(x => x.Name == "Admin");
        var hasAdminRole = await _db.UserRoles.AnyAsync(x => x.UserId == adminUser.Id && x.RoleId == adminRole.Id);
        if (!hasAdminRole)
        {
            _db.UserRoles.Add(new AppUserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            });
        }

        await _db.SaveChangesAsync();
    }

    private string HashPassword(AppUser? userData, string password)
    {
        var user = userData ?? new AppUser();
        return _passwordHasher.HashPassword(user, password);
    }

    private bool VerifyAndUpgradePassword(AppUser user, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return false;
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, providedPassword);
        if (verification == PasswordVerificationResult.Success || verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = HashPassword(user, providedPassword);
            }

            return true;
        }

        if (!LooksLikeHashedPassword(user.PasswordHash) && user.PasswordHash == providedPassword)
        {
            user.PasswordHash = HashPassword(user, providedPassword);
            return true;
        }

        return false;
    }

    private static bool LooksLikeHashedPassword(string passwordHash)
    {
        return passwordHash.StartsWith("AQAAAA", StringComparison.Ordinal);
    }

    private async Task SaveRefreshTokenAsync(string userId, RefreshTokenDescriptor refreshToken, string? ipAddress, bool saveChanges = true)
    {
        _db.RefreshTokens.Add(new AppRefreshToken
        {
            UserId = userId,
            TokenId = refreshToken.TokenId,
            TokenHash = HashToken(refreshToken.Token),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshToken.ExpiresAt,
            CreatedByIp = ipAddress,
        });

        if (saveChanges)
        {
            await _db.SaveChangesAsync();
        }
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static UserInfoDto ToUserInfoDto(AppUser user)
    {
        var roles = user.Roles
            .Where(r => r.Role != null)
            .Select(r => new UserRoleDto(r.RoleId, r.Role!.Name, r.Role.Description))
            .ToList();

        var roleNames = roles.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var permissions = roleNames.Contains("Admin")
            ? new[]
            {
                "admin.full",
                "quotation.read", "quotation.write",
                "accounts.read", "accounts.write", "accounts.approve",
                "employee.read", "employee.write",
                "inventory.read", "inventory.write",
                "customer.read", "customer.write",
                "sales.read", "sales.write",
                "access-requests.read", "access-requests.approve", "access-requests.approve-admin"
            }
            : roleNames.Contains("Owner")
                ? new[]
                {
                    "owner.full.nonadmin",
                    "quotation.read", "quotation.write",
                    "accounts.read", "accounts.write", "accounts.approve",
                    "employee.read", "employee.write",
                    "inventory.read", "inventory.write",
                    "customer.read", "customer.write",
                    "sales.read", "sales.write",
                    "access-requests.read", "access-requests.approve-nonadmin"
                }
                : roleNames.Contains("Manager")
                    ? new[]
                    {
                        "quotation.read", "quotation.write",
                        "accounts.read", "employee.read", "employee.write",
                        "inventory.read", "customer.read", "sales.read"
                    }
                    : new[] { "accounts.read", "accounts.submit" };

        return new UserInfoDto(
            user.Id,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            roles,
            permissions,
            user.IsActive,
            user.LastLogin,
            user.AccessStatus,
            user.RequestedRoleName,
            user.AccessRequestedAt
        );
    }

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
}
