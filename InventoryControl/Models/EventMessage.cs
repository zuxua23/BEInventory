using System.Text.Json;
using System.Text.Json.Serialization;

namespace InventoryControl.Models;

public class Message
{
    [JsonPropertyName("trx_type")]
    public string TrxType { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; }

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }

    public int RetryCount { get; set; } = 0;
}

public class RedisStreamOptions
{
    public string Stream { get; set; } = null!;
    public string Group { get; set; } = null!;
}