using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KeplerCMS.BackgroundServices.HabboActivityModels;
using KeplerCMS.Helpers;
using KeplerCMS.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace KeplerCMS.BackgroundServices
{
    /// <summary>
    /// Consumes the Odin server's "habbo_activity" fanout exchange (chat lines, infobus updates)
    /// and relays them to the housekeeping SignalR hubs.
    /// </summary>
    public class HabboActivityBackgroundService : BackgroundService
    {
        private const string ExchangeName = "habbo_activity";
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

        private readonly ILogger<HabboActivityBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<ChatLogHub> _chatlogHub;
        private readonly IHubContext<InfobusHub> _infobusHub;
        private IConnection _connection;
        private IChannel _channel;

        public HabboActivityBackgroundService(IConfiguration configuration, ILogger<HabboActivityBackgroundService> logger, IHubContext<ChatLogHub> chatlogHubContext, IHubContext<InfobusHub> infobusHub)
        {
            _configuration = configuration;
            _logger = logger;
            _chatlogHub = chatlogHubContext;
            _infobusHub = infobusHub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Connecting used to happen in the constructor, so a RabbitMQ outage at boot stopped the whole
            // CMS from starting. Now the site starts regardless and this keeps retrying in the background.
            // Once connected, the client's automatic recovery handles later broker restarts.
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectAndConsumeAsync(stoppingToken);
                    return;
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Could not subscribe to RabbitMQ exchange {Exchange}; retrying in {Delay}s", ExchangeName, RetryDelay.TotalSeconds);
                    await CloseAsync();
                    await Task.Delay(RetryDelay, stoppingToken);
                }
            }
        }

        private async Task ConnectAndConsumeAsync(CancellationToken stoppingToken)
        {
            _connection = await RabbitMqConnectionFactory.Create(_configuration, "HabboActivityBackgroundService").CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Fanout, durable: false, cancellationToken: stoppingToken);

            // Exclusive + auto-delete: the broker removes this server-named queue when the CMS disconnects.
            // It used to be neither, so every CMS restart left an orphaned queue on the fanout exchange
            // that collected every chat message forever with no consumer.
            var queue = await _channel.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true, arguments: null, cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(queue: queue.QueueName, exchange: ExchangeName, routingKey: "", cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += (_, ea) => HandleMessageAsync(ea, stoppingToken);
            await _channel.BasicConsumeAsync(queue: queue.QueueName, autoAck: true, consumer: consumer, cancellationToken: stoppingToken);

            _logger.LogInformation("Subscribed to RabbitMQ exchange {Exchange}", ExchangeName);
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
        {
            try
            {
                var messageJson = Encoding.UTF8.GetString(ea.Body.Span);
                var messageTempObject = JsonConvert.DeserializeObject<EventMessage>(messageJson);

                switch (messageTempObject?.EventType)
                {
                    case "chat":
                    {
                        var messageObject = JsonConvert.DeserializeObject<ChatlogEventMessageModel>(messageJson);
                        _logger.LogDebug($"Received message from user: {messageObject.PlayerId}, message: {messageObject.Message}, room: {messageObject.RoomId}");
                        await _chatlogHub.Clients.Groups(new List<string> { "room_" + messageObject.RoomId }).SendAsync("ReceiveMessage", messageObject.PlayerId, messageObject.Username, messageObject.Message, messageObject.SentTime, stoppingToken);
                        break;
                    }
                    case "infobus":
                    {
                        var messageObject = JsonConvert.DeserializeObject<InfobusStatusEventMessage>(messageJson);
                        _logger.LogInformation($"Received message from infobus updater: {messageJson}");
                        await _infobusHub.Clients.All.SendAsync("ReceiveUpdate", messageObject, stoppingToken);
                        break;
                    }
                    default:
                        _logger.LogWarning($"Received message with unknown routing key: {ea.RoutingKey}");
                        break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Error parsing message: {ex.Message}");
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error handling habbo_activity message");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            await CloseAsync();
        }

        private async Task CloseAsync()
        {
            try
            {
                if (_channel != null) await _channel.DisposeAsync();
                if (_connection != null) await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error while closing RabbitMQ connection");
            }
            finally
            {
                _channel = null;
                _connection = null;
            }
        }
    }
}
