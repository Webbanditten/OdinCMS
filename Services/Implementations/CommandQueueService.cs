using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KeplerCMS.Helpers;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace KeplerCMS.Services
{
    /// <summary>
    /// Publishes commands for the Odin server to the "commands" exchange. Registered as a singleton,
    /// so it keeps one connection open (with automatic recovery) and opens a short-lived channel per
    /// publish, instead of a full TCP connection per command.
    /// </summary>
    public class CommandQueueService : ICommandQueueService, IAsyncDisposable
    {
        private const string ExchangeName = "commands";
        private const string QueueName = "command_queue";

        private readonly IConfiguration _configuration;
        private readonly ILogger<CommandQueueService> _logger;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);
        private IConnection _connection;

        public CommandQueueService(IConfiguration configuration, ILogger<CommandQueueService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // Kept synchronous for the existing call sites. ASP.NET Core has no SynchronizationContext,
        // so blocking on the async publish cannot deadlock.
        public void QueueCommand(CommandQueueType command, CommandTemplate template)
        {
            QueueCommandAsync(command, template).GetAwaiter().GetResult();
        }

        public async Task QueueCommandAsync(CommandQueueType command, CommandTemplate template)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(template));
                var connection = await GetConnectionAsync();

                await using var channel = await connection.CreateChannelAsync();
                await channel.QueueDeclareAsync(QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Direct, durable: true);
                await channel.BasicPublishAsync(exchange: ExchangeName, routingKey: Enum.GetName(command), body: body);
            }
            catch (Exception e)
            {
                // Same contract as before: a broker problem must not break the page that triggered the command.
                _logger.LogError(e, "Failed to queue command {Command}", command);
            }
        }

        private async Task<IConnection> GetConnectionAsync()
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            await _connectionLock.WaitAsync();
            try
            {
                if (_connection is { IsOpen: true })
                {
                    return _connection;
                }

                if (_connection != null)
                {
                    await _connection.DisposeAsync();
                }

                _connection = await RabbitMqConnectionFactory.Create(_configuration, "CommandQueueService").CreateConnectionAsync();
                return _connection;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
            _connectionLock.Dispose();
        }
    }
}
