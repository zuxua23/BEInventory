using InventoryControl.Models;

namespace InventoryControl.Handler;

public interface ICommandHandler
{
    string Command { get; }
    Task HandleAsync(EventMessage<object> message);
}
