---
title: RabbitMQ Game Server Communication
impact: HIGH
impactDescription: Incorrect server communication can cause game disruptions or data inconsistencies
tags: [server, rabbitmq, integration, game-server]
---

# RabbitMQ Game Server Communication

The CMS communicates with the Kepler Java game server via RabbitMQ for real-time operations.

## Architecture

```
[KeplerCMS (C#)] --publish--> [RabbitMQ] --consume--> [Kepler Server (Java)]
```

The Kepler server's `CommandQueueManager` listens on a `commands` exchange and processes incoming messages.

## Publishing Commands from CMS

```csharp
using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json;

public class GameServerService : IGameServerService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public GameServerService(IConfiguration config)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"],
            UserName = config["RabbitMQ:Username"],
            Password = config["RabbitMQ:Password"]
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public void AlertUser(string username, string message)
    {
        Publish(new { action = "alert", username, message });
    }

    public void KickUser(string username, string message)
    {
        Publish(new { action = "kick", username, message });
    }

    public void RefreshCatalogue()
    {
        Publish(new { action = "refresh_catalogue" });
    }

    public void UpdateRoom(int roomId)
    {
        Publish(new { action = "refresh_room", room_id = roomId });
    }

    private void Publish(object command)
    {
        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(command));
        _channel.BasicPublish(
            exchange: "commands",
            routingKey: "",
            basicProperties: null,
            body: body);
    }
}
```

## Common Commands

| Action | Description | Parameters |
|--------|-------------|------------|
| `alert` | Send alert popup to user | `username`, `message` |
| `kick` | Disconnect user | `username`, `message` |
| `ban` | Ban and disconnect | `username`, `message`, `duration` |
| `refresh_catalogue` | Reload catalogue in-memory | none |
| `refresh_room` | Reload room data | `room_id` |
| `broadcast` | Message all online users | `message` |

## Rules

1. RabbitMQ is fire-and-forget from the CMS side — no response expected
2. Always serialize commands as JSON
3. Use descriptive `action` field names
4. Handle connection failures gracefully (log but don't crash)
5. For operations that only modify the database (and game server will pick up on next access), RabbitMQ is not needed
6. Only use RabbitMQ for immediate/real-time actions (alerts, kicks, refreshes)
