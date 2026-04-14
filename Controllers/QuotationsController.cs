using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.Quotations;

namespace QuotationAPI.V2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuotationsController : ControllerBase
{
    private readonly QuotationDbContext _db;

    public QuotationsController(QuotationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<QuotationListResponse>> GetAll(
        [FromQuery] string? searchText,
        [FromQuery] QuotationStatus? status,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = "desc")
    {
        var query = _db.Quotations.Include(x => x.LineItems).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(x =>
                x.CustomerName.Contains(searchText) ||
                x.QuoteNumber.Contains(searchText) ||
                x.Description.Contains(searchText));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.CreatedDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            var endOfDay = dateTo.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.CreatedDate <= endOfDay);
        }

        query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
        {
            ("amount", "asc") => query.OrderBy(x => x.Amount),
            ("amount", _) => query.OrderByDescending(x => x.Amount),
            ("customername", "asc") => query.OrderBy(x => x.CustomerName),
            ("customername", _) => query.OrderByDescending(x => x.CustomerName),
            ("quotenumber", "asc") => query.OrderBy(x => x.QuoteNumber),
            ("quotenumber", _) => query.OrderByDescending(x => x.QuoteNumber),
            ("status", "asc") => query.OrderBy(x => x.Status),
            ("status", _) => query.OrderByDescending(x => x.Status),
            (_, "asc") => query.OrderBy(x => x.CreatedDate),
            _ => query.OrderByDescending(x => x.CreatedDate)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new QuotationListResponse(items, total, pageNumber, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Quotation>> GetById(string id)
    {
        var item = await _db.Quotations
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item is null)
        {
            return NotFound(new { message = "Quotation not found" });
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Quotation>> Create([FromBody] CreateQuotationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = new Quotation
        {
            QuoteNumber = $"Q-{DateTime.UtcNow:yyyy}-{await _db.Quotations.CountAsync() + 1:000}",
            CustomerName = request.CustomerName,
            Email = request.Email,
            Amount = request.Amount,
            Description = request.Description,
            ValidityDays = request.ValidityDays,
            Status = QuotationStatus.Draft,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "demo"
        };

        if (request.LineItems is not null)
        {
            foreach (var li in request.LineItems)
            {
                entity.LineItems.Add(new QuotationLineItem
                {
                    ItemDescription = li.ItemDescription,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    TotalPrice = li.Quantity * li.UnitPrice
                });
            }
        }

        _db.Quotations.Add(entity);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Quotation>> Update(string id, [FromBody] UpdateQuotationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await _db.Quotations
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity is null)
        {
            return NotFound(new { message = "Quotation not found" });
        }

        entity.CustomerName = request.CustomerName;
        entity.Email = request.Email;
        entity.Amount = request.Amount;
        entity.Description = request.Description;
        entity.ValidityDays = request.ValidityDays;
        if (request.Status.HasValue)
        {
            entity.Status = request.Status.Value;
        }
        entity.ModifiedDate = DateTime.UtcNow;
        entity.ModifiedBy = "demo";

        entity.LineItems.Clear();
        if (request.LineItems is not null)
        {
            foreach (var li in request.LineItems)
            {
                entity.LineItems.Add(new QuotationLineItem
                {
                    QuotationId = entity.Id,
                    ItemDescription = li.ItemDescription,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    TotalPrice = li.Quantity * li.UnitPrice
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var entity = await _db.Quotations
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(new { message = "Quotation not found" });
        }

        entity.IsDeleted = true;
        foreach (var line in entity.LineItems)
        {
            line.IsDeleted = true;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult<Quotation>> Approve(string id)
    {
        return await UpdateStatus(id, QuotationStatus.Approved);
    }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult<Quotation>> Reject(string id)
    {
        return await UpdateStatus(id, QuotationStatus.Rejected);
    }

    [HttpPost("{id}/send")]
    public async Task<ActionResult<Quotation>> Send(string id)
    {
        return await UpdateStatus(id, QuotationStatus.Sent);
    }

    [HttpGet("statistics/count-by-status")]
    public async Task<ActionResult<object>> CountByStatus()
    {
        var data = await _db.Quotations
            .GroupBy(x => x.Status)
            .Select(g => new { status = g.Key.ToString(), count = g.Count() })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("{id}/export/pdf")]
    public IActionResult ExportPdf(string id)
    {
        var content = System.Text.Encoding.UTF8.GetBytes($"Quotation export placeholder for {id}");
        return File(content, "application/pdf", $"quotation-{id}.pdf");
    }

    private async Task<ActionResult<Quotation>> UpdateStatus(string id, QuotationStatus status)
    {
        var entity = await _db.Quotations.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(new { message = "Quotation not found" });
        }

        entity.Status = status;
        entity.ModifiedDate = DateTime.UtcNow;
        entity.ModifiedBy = "demo";
        await _db.SaveChangesAsync();

        return Ok(entity);
    }
}
