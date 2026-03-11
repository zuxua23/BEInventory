using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using System.Text.Json;

namespace InventoryControl.Handler;

public class ReaderHandler : ICommandHandler
{
    private readonly IReaderService _service;
    private readonly Dictionary<string, Func<JsonElement, Task>> _actions;

    public ReaderHandler(IReaderService service)
    {
        _service = service;
        _actions = new Dictionary<string, Func<JsonElement, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            { "CREATE", CreateReader},
            { "UPDATE", UpdateReader },
            { "DELETE", DeleteReader },

        };
    }

    public string TrxType => "READER";

    public async Task HandleAsync(string action, JsonElement data)
    {
        if (!_actions.TryGetValue(action, out var handler))
            throw new Exception($"Action {action} not supported for READER");
        await handler(data);
    }

    private async Task CreateReader(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<ReaderDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid reader data");
        await _service.CreateAsync(dto, "system");
    }

    private async Task UpdateReader(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<ReaderDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid reader data");
        await _service.UpdateAsync(dto.RdrId, dto, "system");
    }

    private async Task DeleteReader(JsonElement data)
    {
        var id = data.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(id))
            throw new Exception("Invalid reader id");
        await _service.DeleteAsync(id);
    }


}
