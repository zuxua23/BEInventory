namespace InventoryControl.Models;
public class EventMessage<T>
{
    public string CorrelationId { get; set; }

    public string Service { get; set; }

    public string Command { get; set; }

    public string UserId { get; set; }

    public List<string>? Roles { get; set; }

    public List<string>? Permissions { get; set; }

    public DateTime Timestamp { get; set; }
    public int RetryCount { get; set; } = 0;

    public T Payload { get; set; }
}