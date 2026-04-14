namespace QuotationAPI.V2.Models.LOV;

public class LovItem
{
    public int Id { get; set; }
    public string? Parentname { get; set; }
    public int? Parentvalue { get; set; }
    public string Name { get; set; } = "";
    public int? Value { get; set; }
    public string? Description { get; set; }
    public string Itemtype { get; set; } = "";
    public int Displayorder { get; set; } = 1;
    public string Isactive { get; set; } = "Y";
    public string Createdby { get; set; } = "system";
    public string Updatedby { get; set; } = "system";
    public string Createddt { get; set; } = "";
    public string Updateddt { get; set; } = "";
    public bool IsDeleted { get; set; } = false;
}

public class SaveLovRequest
{
    public string? Parentname { get; set; }
    public int? Parentvalue { get; set; }
    public string Name { get; set; } = "";
    public int? Value { get; set; }
    public string? Description { get; set; }
    public string Itemtype { get; set; } = "";
    public int Displayorder { get; set; } = 1;
    public string Isactive { get; set; } = "Y";
}
