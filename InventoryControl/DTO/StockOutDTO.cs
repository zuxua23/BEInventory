namespace InventoryControl.DTO;

public class StockOutDto
{
    public string DoId { get; set; } = null!;
    public string ReaderId { get; set; } = null!;
}

public class StockOutResponseDto
{
    public string DoId { get; set; } = null!;
    public string ReaderId { get; set; } = null!;
    public string Epc { get; set; } = null!;
}