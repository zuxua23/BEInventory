using InventoryControl.Models;

namespace InventoryControl.DTO;
public class TransactionHistoryDto
{
    public string Id { get; set; }
    public DateTime? TxDate { get; set; }
    public string TxType { get; set; }
    public TransactionType TrsType { get; set; }
    public string DoNumber { get; set; }
    public string ReaderName { get; set; }
    public int TotalTag { get; set; }
}

public class TransactionHistoryDetailDto
{
    public string TagId { get; set; }
    public string ItemName { get; set; }
    public string LocationName { get; set; }
}

public class TransactionHistoryDetailResponseDto
{
    public string Id { get; set; }
    public DateTime? TxDate { get; set; }
    public string TxType { get; set; }
    public string DoNumber { get; set; }
    public string ReaderName { get; set; }
    public int TotalTag { get; set; }
    public List<TransactionHistoryDetailDto> Details { get; set; } = new();
}