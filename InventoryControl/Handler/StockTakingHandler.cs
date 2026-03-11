using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using System.Text.Json;

namespace InventoryControl.Handler;

public class StockTakingHandler : ICommandHandler
{
    private readonly IStockTakingService _service;
    private readonly Dictionary<string, Func<JsonElement, Task>> _actions;

    public StockTakingHandler(IStockTakingService service)
    {
        _service = service;
        _actions = new Dictionary<string, Func<JsonElement, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            { "CREATE", CreateStockTaking },
            { "SCAN", ScanStockTaking },
            { "REMOVE", RemoveStockTaking },
            { "MANUAL", ManualStockTaking },
            { "FINALIZE", FinalizeStockTaking },
        };
    }

    public string TrxType => "STOCKTAKING";

    public async Task HandleAsync(string action, JsonElement data)
    {
        if (!_actions.TryGetValue(action, out var handler))
            throw new Exception($"Action {action} not supported for STOCKTAKING");
        await handler(data);
    }

    private async Task CreateStockTaking(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<StockTakingCreateDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid stock taking data");
        await _service.CreateAsync(dto, "system");
    }

    private async Task ScanStockTaking(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<StockTakingScanDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid stock taking scan data");
        await _service.ScanAsync(dto);
    }

    private async Task RemoveStockTaking(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<StockTakingRemoveDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid stock taking scan data");
        await _service.RemoveAsync(dto);
    }


    private async Task ManualStockTaking(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<StockTakingManualAddDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid stock taking manual data");
        await _service.ManualAddAsync(dto);
    }

    private async Task FinalizeStockTaking(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<StockTakingFinalizeDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid stock taking manual data");
        await _service.FinalizeAsync(dto, "system");
    }

}
