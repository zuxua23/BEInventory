using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using System.Text.Json;

namespace InventoryControl.Handler;

public class DOHandler : ICommandHandler
{
    private readonly IDOService _service;
    private readonly Dictionary<string, Func<JsonElement, Task>> _actions;

    public DOHandler(IDOService service)
    {
        _service = service;
        _actions = new Dictionary<string, Func<JsonElement, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            { "CREATE", CreateDO },
            { "UPDATE", UpdateDO },
            { "UPDATE_STATUS", UpdateDOStatus },
            { "DELETE", DeleteDO },
        };
    }

    public string TrxType => "DO";

    public async Task HandleAsync(string action, JsonElement data)
    {
        if (!_actions.TryGetValue(action, out var handler))
            throw new Exception($"Action {action} not supported for DO");
        await handler(data);
    }

    private async Task CreateDO(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<DODTO>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid DO data");
        await _service.CreateAsync(dto, "system");
    }

    private async Task UpdateDO(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<DOUpdateDTO>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid DO data");
        await _service.UpdateAsync(dto.DoId, dto);
    }

    private async Task UpdateDOStatus(JsonElement data)
    {
        var id = data.GetProperty("id").GetString();
        var status = data.GetProperty("status").GetString();
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(status))
            throw new Exception("Invalid DO id or status");
        await _service.UpdateStatusAsync(id, status);
    }

    private async Task DeleteDO(JsonElement data)
    {
        var id = data.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(id))
            throw new Exception("Invalid DO id");
        await _service.DeleteAsync(id);
    }
}
