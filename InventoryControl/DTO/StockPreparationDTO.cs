namespace InventoryControl.DTO;

public class StockPreparationRequestDto
{
    public string DoId { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string ScannerType { get; set; }
    public string LocId { get; set; } = null!;

}
//public class StockPreparationDto
//{
//    public string DoId { get; set; } = null!;
//    public List<string> TagIds { get; set; } = new();
//    public string ReaderId { get; set; } = null!;
//}