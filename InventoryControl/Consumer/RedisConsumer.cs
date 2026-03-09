using InventoryControl.Handler;
using InventoryControl.Models;
using InventoryControl.Utility;
using StackExchange.Redis;
using System.Text.Json;

namespace InventoryControl.Consumers;
public class RedisConsumer : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceScopeFactory _scopeFactory;

    private const string STREAM = "stream:user";
    private const string GROUP = "user-group";

    private readonly string _consumerName =
        Environment.MachineName + "-" + Guid.NewGuid();

    public RedisConsumer(IConnectionMultiplexer redis, IServiceScopeFactory scopeFactory)
    {
                _redis = redis;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();

        await EnsureGroupExists(db);

        while (!stoppingToken.IsCancellationRequested)
        {
            var entries = await db.StreamReadGroupAsync(
                STREAM,
                GROUP,
                _consumerName,
                ">",
                count: 10
            );

            foreach (var entry in entries)
            {
                var json = entry.Values.First().Value;

                var message = JsonSerializer.Deserialize<EventMessage<object>>(json);

                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var dispatcher = scope.ServiceProvider.GetRequiredService<CommandDispatcher>();

                    await dispatcher.DispatchAsync(message.Command, message);

                    await db.StreamAcknowledgeAsync(STREAM, GROUP, entry.Id);
                }
                catch (Exception ex)
                {
                    DailyFileLogger.Error($"Error processing message: {ex.Message}");
                    await HandleRetry(db, message);

                }
            }
        }
    }

    private async Task EnsureGroupExists(IDatabase db)
    {
        try
        {
            await db.StreamCreateConsumerGroupAsync(
                STREAM,
                GROUP,
                "0"
            );
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
        }
    }
    private async Task HandleRetry(IDatabase db, EventMessage<object> message)
{
    const int MAX_RETRY = 3;

    if (message.RetryCount < MAX_RETRY)
    {
        message.RetryCount++;

        var json = JsonSerializer.Serialize(message);

        await db.StreamAddAsync(
            STREAM,
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
            STREAM + ":dlq",
            new NameValueEntry[]
            {
                new NameValueEntry("data", json)
            });

        Console.WriteLine("Moved to Dead Letter Queue");
    }
}
}