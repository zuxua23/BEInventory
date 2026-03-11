using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using System.Text.Json;

namespace InventoryControl.Handler;

public class StockPreparationHandler : ICommandHandler
{
    private readonly IStockPreparationService _service;
    private readonly Dictionary<string, Func<JsonElement, Task>> _actions;
    public StockPreparationHandler(IStockPreparationService service)
    {
        _service = service;
        _actions = new Dictionary<string, Func<JsonElement, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            { "CREATE", CreateStockPreparation },
        };
    }
    public string TrxType => "STOCK_PREPARATION";
    public async Task HandleAsync(string action, JsonElement data)
    {
        if (!_actions.TryGetValue(action, out var handler))
            throw new Exception($"Action {action} not supported for STOCK_PREPARATION");
        await handler(data);
    }
    private async Task CreateStockPreparation(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<StockPreparationRequestDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid stock preparation data");
        await _service.PrepareAsync(dto, "system");
    }

}
