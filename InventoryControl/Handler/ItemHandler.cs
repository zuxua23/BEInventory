using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using System.Text.Json;

namespace InventoryControl.Handler;

public class ItemHandler : ICommandHandler
{
    private readonly IItemService _service;
    private readonly Dictionary<string, Func<JsonElement, Task>> _actions;

    public ItemHandler(IItemService service)
    {
        _service = service;
        _actions = new Dictionary<string, Func<JsonElement, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            { "CREATE", CreateItem },
            { "UPDATE", UpdateItem },
            { "DELETE", DeleteItem },
        };
    }

    public string TrxType => "ITEM";

    public async Task HandleAsync(string action, JsonElement data)
    {
        if (!_actions.TryGetValue(action, out var handler))
            throw new Exception($"Action {action} not supported for ITEM");
        await handler(data);
    }

    private async Task CreateItem(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<ItemDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid item data");
        await _service.CreateAsync(dto, "system");
    }

    private async Task UpdateItem(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<ItemDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid item data");
        await _service.UpdateAsync(dto.ItemId, dto, "system");
    }

    private async Task DeleteItem(JsonElement data)
    {
        var id = data.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(id))
            throw new Exception("Item ID is required for deletion");
        await _service.DeleteAsync(id);
    }

}
