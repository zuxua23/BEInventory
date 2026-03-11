using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using System.Text.Json;

namespace InventoryControl.Handler;

public class StockOutHandler : ICommandHandler
{
    private readonly IStockOutService _service;
    private readonly Dictionary<string, Func<JsonElement, Task>> _actions;

    public StockOutHandler(IStockOutService service)
    {
        _service = service;
        _actions = new Dictionary<string, Func<JsonElement, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            { "CREATE", CreateStockOut },
            { "SCAN", ScanStockOut },
        };
    }

    public string TrxType => "STOCKOUT";

    public async Task HandleAsync(string action, JsonElement data)
    {
        if (!_actions.TryGetValue(action, out var handler))
            throw new Exception($"Action {action} not supported for STOCKOUT");
        await handler(data);
    }

    private async Task CreateStockOut(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<StockOutDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid stock out data");
        await _service.StockOutAsync(dto, "system");
    }

    private async Task ScanStockOut(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<StockOutResponseDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid stock out scan data");
        await _service.ScanStockOutAsync(dto, "system");
    }
}
