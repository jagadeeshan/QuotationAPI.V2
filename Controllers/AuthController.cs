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

    public AuthController(QuotationDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = await _db.Users
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Username == request.Username && x.IsActive);

        if (user is null || user.PasswordHash != request.Password)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var roles = user.Roles.Select(x => x.Role?.Name ?? "User").ToList();
        var accessToken = _tokenService.CreateAccessToken(user, roles);
        var refreshToken = _tokenService.CreateRefreshToken();

        var response = new LoginResponse(
            accessToken,
            refreshToken,
            3600,
            new UserInfoDto(
                user.Id,
                user.Username,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Roles.Select(r => new UserRoleDto(r.RoleId, r.Role?.Name ?? "User", r.Role?.Description ?? "")),
                new[] { "quotation.read", "quotation.write", "accounts.read", "accounts.write" },
                user.IsActive,
                user.LastLogin
            )
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

        if (await _db.Users.AnyAsync(x => x.Username == request.Username || x.Email == request.Email))
        {
            return BadRequest(new { message = "Username or email already exists" });
        }

        var userRole = await _db.Roles.FirstOrDefaultAsync(x => x.Name == "User");
        if (userRole is null)
        {
            userRole = new AppRole { Name = "User", Description = "Standard user" };
            _db.Roles.Add(userRole);
            await _db.SaveChangesAsync();
        }

        var user = new AppUser
        {
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PasswordHash = request.Password,
            IsActive = true,
            LastLogin = DateTime.UtcNow
        };

        _db.Users.Add(user);
        _db.UserRoles.Add(new AppUserRole { User = user, Role = userRole });
        await _db.SaveChangesAsync();

        var accessToken = _tokenService.CreateAccessToken(user, new[] { userRole.Name });
        var refreshToken = _tokenService.CreateRefreshToken();

        return Ok(new LoginResponse(
            accessToken,
            refreshToken,
            3600,
            new UserInfoDto(
                user.Id,
                user.Username,
                user.Email,
                user.FirstName,
                user.LastName,
                new[] { new UserRoleDto(userRole.Id, userRole.Name, userRole.Description) },
                new[] { "quotation.read", "accounts.read" },
                user.IsActive,
                user.LastLogin
            )
        ));
    }

    [HttpPost("refresh-token")]
    public ActionResult<RefreshTokenResponse> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { message = "Invalid refresh token" });
        }

        return Ok(new RefreshTokenResponse(_tokenService.CreateRefreshToken(), 3600));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out successfully" });
    }
}
