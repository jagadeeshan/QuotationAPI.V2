using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.LOV;

namespace QuotationAPI.V2.Controllers;

[ApiController]
[Route("api/list-of-values")]
public class LovController : ControllerBase
{
    private readonly QuotationDbContext _db;
    private const string UiPlaceholderCategory = "__add_category__";

    public LovController(QuotationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LovItem>>> GetAll()
    {
        var items = await _db.LovItems
            .OrderBy(x => x.Parentvalue.HasValue ? 1 : 0)
            .ThenBy(x => x.Parentvalue)
            .ThenBy(x => x.Displayorder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LovItem>> GetById(int id)
    {
        var item = await _db.LovItems.FindAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("by-category/{categoryName}")]
    public async Task<ActionResult<IEnumerable<string>>> GetValuesByCategory(string categoryName)
    {
        var category = await _db.LovItems
            .FirstOrDefaultAsync(x => x.Parentvalue == null && x.Name == categoryName);

        if (category == null)
        {
            return Ok(Array.Empty<string>());
        }

        var values = await _db.LovItems
            .Where(x => x.Parentvalue == category.Id && x.Isactive == "Y" && x.Name != UiPlaceholderCategory)
            .OrderBy(x => x.Displayorder)
            .ThenBy(x => x.Name)
            .Select(x => x.Name)
            .ToListAsync();

        return Ok(values);
    }

    [HttpGet("by-category/{categoryName}/items")]
    public async Task<ActionResult<IEnumerable<LovItem>>> GetItemsByCategory(string categoryName)
    {
        var category = await _db.LovItems
            .FirstOrDefaultAsync(x => x.Parentvalue == null && x.Name == categoryName);

        if (category == null)
        {
            return Ok(Array.Empty<LovItem>());
        }

        var values = await _db.LovItems
            .Where(x => x.Parentvalue == category.Id && x.Isactive == "Y" && x.Name != UiPlaceholderCategory)
            .OrderBy(x => x.Displayorder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return Ok(values);
    }

    [HttpPost]
    public async Task<ActionResult<LovItem>> Create([FromBody] SaveLovRequest request)
    {
        if (string.Equals(request.Name, UiPlaceholderCategory, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Invalid LOV value name." });
        }

        var now = DateTime.UtcNow.ToString("o");
        var item = new LovItem
        {
            Parentname = request.Parentname,
            Parentvalue = request.Parentvalue,
            Name = request.Name,
            Value = request.Value,
            Description = request.Description,
            Itemtype = request.Itemtype,
            Displayorder = request.Displayorder,
            Isactive = request.Isactive,
            Createdby = "system",
            Updatedby = "system",
            Createddt = now,
            Updateddt = now
        };

        _db.LovItems.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LovItem>> Update(int id, [FromBody] SaveLovRequest request)
    {
        if (string.Equals(request.Name, UiPlaceholderCategory, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Invalid LOV value name." });
        }

        var item = await _db.LovItems.FindAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        item.Parentname = request.Parentname;
        item.Parentvalue = request.Parentvalue;
        item.Name = request.Name;
        item.Value = request.Value;
        item.Description = request.Description;
        item.Itemtype = request.Itemtype;
        item.Displayorder = request.Displayorder;
        item.Isactive = request.Isactive;
        item.Updatedby = "system";
        item.Updateddt = DateTime.UtcNow.ToString("o");

        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.LovItems.FindAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        var children = await _db.LovItems
            .Where(x => x.Parentvalue == id)
            .ToListAsync();

        if (children.Count > 0)
        {
            foreach (var child in children)
            {
                child.IsDeleted = true;
            }
        }

        item.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}