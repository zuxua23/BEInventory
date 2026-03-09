using InventoryControl.Models;

namespace InventoryControl.Handler;

public class CommandDispatcher
{
    private readonly UserHandler _userHandler;
    public CommandDispatcher(UserHandler userHandler)
    {
        _userHandler = userHandler;
    }

    public async Task DispatchAsync(string command, EventMessage<object> message)
    {
        switch (message.Service.ToLower())
        {
            case "user":
                await _userHandler.HandleAsync(message);
                break;

            default:
                throw new Exception($"Service handler not found for {message.Service}");
        }
    }
}