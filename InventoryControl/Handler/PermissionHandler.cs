using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using System.Text.Json;

namespace InventoryControl.Handler;

public class PermissionHandler : ICommandHandler
{
    private readonly IPermissionService _service;
    private readonly Dictionary<string, Func<JsonElement, Task>> _actions;

    public PermissionHandler(IPermissionService service)
    {
        _service = service;
        _actions = new Dictionary<string, Func<JsonElement, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            { "CREATE", CreatePermission },
            { "UPDATE", UpdatePermission },
            { "DELETE", DeletePermission },
        };
    }

    public string TrxType => "PERMISSION";

    public async Task HandleAsync(string action, JsonElement data)
    {
        if (!_actions.TryGetValue(action, out var handler))
            throw new Exception($"Action {action} not supported for PERMISSION");
        await handler(data);
    }

    private async Task CreatePermission(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<PermissionDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid permission data");
        await _service.CreateAsync(dto, "system");
    }

    private async Task UpdatePermission(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<PermissionUpdateDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid permission data");
        await _service.UpdateAsync(dto.PermissionId, dto, "system");
    }

    private async Task DeletePermission(JsonElement data)
    {
        var id = data.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(id))
            throw new Exception("Permission ID is required for deletion");
        await _service.DeleteAsync(id);
    }
}
