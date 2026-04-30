namespace InventoryControl.DTO;

public class StockTakingCreateDto
{
    public string? Remark { get; set; }
    public List<string>? LocationIds { get; set; }
}

public class StockTakingScanDto
{
    public string SttId { get; set; }
    public string Epc { get; set; }
}
public class StockTakingRemoveDto
{
    public string SttId { get; set; }
    public string TagId { get; set; }
}
public class StockTakingManualAddDto
{
    public string SttId { get; set; }
    public string ItemId { get; set; }
    public string Remark { get; set; }
}
public class StockTakingFinalizeDto
{
    public string SttId { get; set; }
}
public class StockTakingBulkScanDto
{
    public string SttId { get; set; } = null!;
    public List<BulkItemDto> Items { get; set; } = new();
}

public class BulkItemDto
{
    public string Epc { get; set; } = null!;
}