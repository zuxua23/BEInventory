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
    public string NewTagId { get; set; }
    public string? Remark { get; set; }
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

public class StockTakingExportDto
{
    public string ItemId { get; set; }
    public int Qty { get; set; }
    public string Location { get; set; }
    public string Status { get; set; }
}

public class StockTakingCompareExportDto
{
    public string ItemId { get; set; }
    public string Location { get; set; }

    public int QtySystem { get; set; }
    public int QtyScan { get; set; }

    public int Difference { get; set; }

    public string Status { get; set; }
}

public class StockTakingSessionTagDto
{
    public string TagId { get; set; }
    public string EpcTag { get; set; }
    public string ItemId { get; set; }
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public string LocationId { get; set; }
    public string Location { get; set; }
}