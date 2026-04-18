using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Data;

namespace QuotationAPI.V2.Controllers;

[ApiController]
[Route("api/admin-module/company-profiles")]
[Authorize]
public class CompanyProfilesController : ControllerBase
{
    private readonly QuotationDbContext _db;

    public CompanyProfilesController(QuotationDbContext db)
    {
        _db = db;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveCompanyProfile()
    {
        var profile = await _db.AdminCompanyProfiles
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.UpdatedAt)
            .Select(x => new
            {
                x.Id,
                x.CompanyName,
                x.Address,
                x.GstNo,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt,
                x.UpdatedBy
            })
            .FirstOrDefaultAsync();

        return profile == null ? NoContent() : Ok(profile);
    }
}