using InventoryControl.Service.Interfaces;
using System.Text.Json;
using InventoryControl.DTO;
namespace InventoryControl.Handler;

public class PrintTagRegisHandler : ICommandHandler
{
    private readonly IPrintTagRegisService _service;
    private readonly Dictionary<string, Func<JsonElement, Task>> _actions;

    public PrintTagRegisHandler(IPrintTagRegisService service)
    {
        _service = service;
        _actions = new Dictionary<string, Func<JsonElement, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            { "PRINT", PrintTagRegis },
            { "REGIS", RegisTagRegis },
        };
    }

    public string TrxType => "PRINT_TAG_REGIS";

    public async Task HandleAsync(string action, JsonElement data)
    {
        if (!_actions.TryGetValue(action, out var handler))
            throw new Exception($"Action {action} not supported for PRINT_TAG_REGIS");
        await handler(data);
    }

    private async Task PrintTagRegis(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<PrintTagDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid print tag regis data");
        await _service.PrintAsync(dto, "system");
    }

    private async Task RegisTagRegis(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<TagRegistrationDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (dto == null)
            throw new Exception("Invalid print tag regis data");
        await _service.RegisterAsync(dto, "system");
    }

}
