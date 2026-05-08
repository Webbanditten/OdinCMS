---
title: MUS Protocol Communication
impact: MEDIUM
impactDescription: Incorrect MUS protocol usage can crash the game server or fail silently
tags: [server, mus, protocol, communication]
---

# MUS Protocol Communication

The MUS (Multi User Server) is a secondary TCP socket on the Kepler game server that accepts commands from trusted hosts like the CMS.

## How MUS Works

1. Kepler runs a MUS listener on a configured port (separate from the game port)
2. The CMS connects via TCP and sends plain-text commands
3. Only connections from whitelisted IPs are accepted (configured in Kepler's settings)
4. Commands are processed immediately (synchronous)

## MUS Command Format

MUS commands are newline-delimited text messages:

```
COMMAND_NAME\tPARAM1\tPARAM2\n
```

## Common MUS Operations

| Command | Purpose | Parameters |
|---------|---------|------------|
| `ALERT` | Send alert to user | username, message |
| `KICK` | Disconnect user | username |
| `UPDATE_CREDITS` | Refresh user's credit display | username |
| `REFRESH_CATALOGUE` | Reload catalogue data | (none) |
| `HOTEL_ALERT` | Broadcast to all users | message |

## CMS Implementation Pattern

```csharp
public class MusService : IMusService
{
    private readonly string _host;
    private readonly int _port;

    public MusService(IConfiguration config)
    {
        _host = config["Mus:Host"];
        _port = int.Parse(config["Mus:Port"]);
    }

    public async Task SendCommand(string command, params string[] args)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port);
            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream);
            
            var message = command + "\t" + string.Join("\t", args) + "\n";
            await writer.WriteAsync(message);
            await writer.FlushAsync();
        }
        catch (Exception ex)
        {
            // Log but don't crash - MUS being unavailable shouldn't break CMS
            _logger.LogError(ex, "Failed to send MUS command: {Command}", command);
        }
    }
}
```

## When to Use MUS vs RabbitMQ

| Use MUS When | Use RabbitMQ When |
|-------------|-------------------|
| Need immediate response | Fire-and-forget is acceptable |
| Simple one-off commands | Complex multi-step operations |
| Testing/development | Production with message persistence |
| Single server setup | Multi-server/clustered setup |

## Rules

1. Always handle MUS connection failures gracefully (try/catch)
2. MUS is best for simple, immediate operations
3. Never expose MUS port to the internet — only whitelist the CMS server IP
4. Tab-separate parameters, newline-terminate commands
5. If MUS is unavailable, fall back to direct database updates where possible
