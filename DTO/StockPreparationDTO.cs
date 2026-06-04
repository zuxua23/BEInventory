namespace InventoryControl.DTO;

public class StockPreparationRequestDto
{
    public string DoId { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string ScannerType { get; set; }
    public string LocId { get; set; } = null!;

}
public class StockPreparationBulkRequestDto
{
    public string DoId { get; set; } = null!;
    public List<string> ScannedCodes { get; set; } = new();
    public string ScannerType { get; set; } = null!;
    public string LocId { get; set; } = null!;
}