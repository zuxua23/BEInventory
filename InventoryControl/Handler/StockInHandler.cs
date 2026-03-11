using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using System.Text.Json;

namespace InventoryControl.Handler;

public class StockInHandlerc: ICommandHandler
{
    private readonly IStockInService _service;
    private readonly Dictionary<string, Func<JsonElement, Task>> _actions;

    public StockInHandlerc(IStockInService service)
    {
        _service = service;
        _actions = new Dictionary<string, Func<JsonElement, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            { "CREATE", CreateStockIn },
        };
    }

    public string TrxType => "STOCKIN";

    public async Task HandleAsync(string action, JsonElement data)
    {
        if (!_actions.TryGetValue(action, out var handler))
            throw new Exception($"Action {action} not supported for STOCKIN");
        await handler(data);
    }

    private async Task CreateStockIn(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<StockInDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid stock in data");
        await _service.StockInAsync(dto, "system");
    }
}
