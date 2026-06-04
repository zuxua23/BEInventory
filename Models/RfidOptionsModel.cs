namespace InventoryControl.Models;

public class RfidOptionsModel
{
    public int ConnectionRetry { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public int CacheTTLSeconds { get; set; } = 60;
}
