namespace InventoryControl.DTO;


public class TagResponseDto
{
    public string TagId { get; set; } = null!;
    public string? ItemId { get; set; }
    public string? ItemName { get; set; }
    public string? Epc { get; set; }
    public string? Status { get; set; }
    public string? Location { get; set; }
}

public class TagRegistrationDto
{
    public List<string> TagIds { get; set; } = new();
}
public class PrintTagDto
{
    public string ItemId { get; set; } = null!;
    public int Qty { get; set; }
}

public class PrintHistoryResponseDto
{
    public string TagId { get; set; } = null!;
    public string ItemId { get; set; } = null!;
    public string ItemName { get; set; } = null!;
    public string LocId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string BatchNo { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

//public class PrintDto
//{
//    public string ItemName { get; set; }
//    public int Qty { get; set; }
//}

public class StockResponseDto
{
    public string ItemId { get; set; }
    public string ItemName { get; set; }
    public int TotalStock { get; set; }
    public int MinimumStock { get; set; }
}

public class StockQRDto
{
    public string TagId { get; set; } = null!;
    public string? ItemName { get; set; }
    public string? Location { get; set; }
    public int TotalStock { get; set; }
}

public class TagStatusCountDto
{
    public int InStock { get; set; }
    public int Reserved { get; set; }
    public int Out { get; set; }
    public int Others { get; set; }
    public int Total { get; set; }
}

public class UpdatePrinterDto
{
    public string PrinterName { get; set; }
}