using InventoryControl.Handler;
using InventoryControl.Models;
using InventoryControl.Utility;
using StackExchange.Redis;
using System.Text.Json;
namespace InventoryControl.Consumer;

public class RedisStreamConsumer : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly List<RedisStreamOptions> _streams;

    private readonly string _consumerName =
        Environment.MachineName + "-" + Guid.NewGuid();

    public RedisStreamConsumer(
        IConnectionMultiplexer redis,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _redis = redis;
        _scopeFactory = scopeFactory;

        _streams = configuration
            .GetSection("RedisStreams")
            .Get<List<RedisStreamOptions>>() ?? new();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();

        foreach (var stream in _streams)
        {
            await EnsureGroupExists(db, stream.Stream, stream.Group);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var stream in _streams)
            {
                var entries = await db.StreamReadGroupAsync(
                    stream.Stream,
                    stream.Group,
                    _consumerName,
                    ">",
                    count: 10
                );

                foreach (var entry in entries)
                {
                    await ProcessMessage(db, stream, entry);
                }
            }

            await Task.Delay(100, stoppingToken);
        }
    }

    private async Task ProcessMessage(
        IDatabase db,
        RedisStreamOptions stream,
        StreamEntry entry)
    {
        var json = entry.Values
            .FirstOrDefault(v => v.Name == "data")
            .Value;

        if (json.IsNullOrEmpty)
        {
            Console.WriteLine("Invalid message format");
            return;
        }

        var message = JsonSerializer.Deserialize<Message>(json);

        Console.WriteLine($"TrxType: {message.TrxType}");
        Console.WriteLine($"Action: {message.Action}");

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var dispatcher = scope.ServiceProvider
                .GetRequiredService<CommandDispatcher>();

            await dispatcher.DispatchAsync(message);

            await db.StreamAcknowledgeAsync(
                stream.Stream,
                stream.Group,
                entry.Id
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error processing message: {ex.Message}");
            Console.WriteLine(ex);

            await HandleRetry(db, stream.Stream, message);
        }
    }

    private async Task EnsureGroupExists(
        IDatabase db,
        string stream,
        string group)
    {
        try
        {
            await db.StreamCreateConsumerGroupAsync(
                stream,
                group,
                "0"
            );
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
        }
    }

    private async Task HandleRetry(
        IDatabase db,
        string stream,
        Message message)
    {
        const int MAX_RETRY = 3;

        if (message.RetryCount < MAX_RETRY)
        {
            message.RetryCount++;

            var retryMessage = new Message
            {
                TrxType = message.TrxType,
                Action = message.Action,
                Data = message.Data.Clone(),
                RetryCount = message.RetryCount
            };

            var json = JsonSerializer.Serialize(retryMessage);

            await db.StreamAddAsync(
                stream,
                new NameValueEntry[]
                {
                    new NameValueEntry("data", json)
                });

            Console.WriteLine($"Retry {message.RetryCount}");
        }
        else
        {
            var json = JsonSerializer.Serialize(message);

            await db.StreamAddAsync(
                stream + ":dlq",
                new NameValueEntry[]
                {
                    new NameValueEntry("data", json)
                });

            Console.WriteLine("Moved to Dead Letter Queue");
        }
    }
}