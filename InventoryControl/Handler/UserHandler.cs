using InventoryControl.DTO;
using InventoryControl.Models;
using System.Text.Json;

namespace InventoryControl.Handler;

public class UserHandler 
{
    private readonly IUserService _service;

    public UserHandler(IUserService service)
    {
        _service = service;
    }

    public async Task HandleAsync(EventMessage<object> message)
    {
        switch (message.Command)
        {
            case "CreateUser":
                await CreateUser(message);
                break;

            case "UpdateUser":
                await UpdateUser(message);
                break;

            case "DeleteUser":
                await DeleteUser(message);
                break;
            default:
                throw new Exception($"Command not supported: {message.Command}");
        }
    }

    private async Task CreateUser(EventMessage<object> message)
    {
        var dto = JsonSerializer.Deserialize<UserDto>(
            JsonSerializer.Serialize(message.Payload)
        );

        await _service.CreateAsync(dto, message.UserId);
    }

    private async Task UpdateUser(EventMessage<object> message)
    {
        var dto = JsonSerializer.Deserialize<UpdateUserDto>(
            JsonSerializer.Serialize(message.Payload)
        );

        await _service.UpdateAsync(dto.UserId, dto, message.UserId);
    }

    private async Task DeleteUser(EventMessage<object> message)
    {
        var id = message.Payload.ToString();

        await _service.DeleteAsync(id);
    }
}