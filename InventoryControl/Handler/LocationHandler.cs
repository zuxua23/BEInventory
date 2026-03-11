using System.Text.Json;
using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;

namespace InventoryControl.Handler;

public class LocationHandler : ICommandHandler
{
    private readonly ILocationService _service;
    private readonly Dictionary<string, Func<JsonElement, Task>> _actions;

    public LocationHandler(ILocationService service)
    {
        _service = service;
        _actions = new Dictionary<string, Func<JsonElement, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            { "CREATE", CreateLocation },
            { "UPDATE", UpdateLocation },
            { "DELETE", DeleteLocation },
        };
    }

    public string TrxType => "LOCATION";

    public async Task HandleAsync(string action, JsonElement data)
    {
        if (!_actions.TryGetValue(action, out var handler))
            throw new Exception($"Action {action} not supported for LOCATION");
        await handler(data);
    }

    private async Task CreateLocation(JsonElement data)
    {
        Console.WriteLine("RAW DATA:");
        Console.WriteLine(data.GetRawText());

        var dto = JsonSerializer.Deserialize<LocationDTO>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid location data");
        await _service.CreateAsync(dto, "system");
    }

    private async Task UpdateLocation(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<LocationDTO>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid location data");
        await _service.UpdateAsync(dto.LocId, dto, "system");
    }

    private async Task DeleteLocation(JsonElement data)
    {
        var id = data.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(id))
            throw new Exception("Invalid location ID");
        await _service.DeleteAsync(id);
    }
}
