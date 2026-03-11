using InventoryControl.DTO;
using System.Text.Json;

namespace InventoryControl.Handler; 
public class UserHandler : ICommandHandler
{
    private readonly IUserService _service;

    private readonly Dictionary<string, Func<JsonElement, Task>> _actions;

    public UserHandler(IUserService service)
    {
        _service = service;

        _actions = new Dictionary<string, Func<JsonElement, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            { "CREATE", CreateUser },
            { "UPDATE", UpdateUser },
            { "DELETE", DeleteUser },
        };
    }

    public string TrxType => "USER";

    public async Task HandleAsync(string action, JsonElement data)
    {
        if (!_actions.TryGetValue(action, out var handler))
            throw new Exception($"Action {action} not supported for USER");

        await handler(data);
    }

    private async Task CreateUser(JsonElement data)
    {
        Console.WriteLine("RAW DATA:");
        Console.WriteLine(data.GetRawText());

        var dto = JsonSerializer.Deserialize<UserDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (dto == null)
            throw new Exception("Invalid user data");

        await _service.CreateAsync(dto, "system");
    }

    private async Task UpdateUser(JsonElement data)
    {
        var dto = JsonSerializer.Deserialize<UpdateUserDto>(
            data.GetRawText(),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        await _service.UpdateAsync(dto.UserId, dto, "system");
    }

    private async Task DeleteUser(JsonElement data)
    {
        var id = data.GetProperty("userId").GetString();
        await _service.DeleteAsync(id);
    }

}