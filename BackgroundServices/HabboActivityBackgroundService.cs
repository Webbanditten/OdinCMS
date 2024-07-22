﻿using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    public class ChatlogEventMessageModel
    {
        public int PlayerId { get; set; }
        public string Message { get; set; }
        public string ChatMessageType { get; set; }
        public int RoomId { get; set; }
        public long SentTime { get; set; }
    }

    public class HabboActivityBackgroundService : BackgroundService
    {
        private readonly ILogger<HabboActivityBackgroundService> _logger;
        private readonly string _exchangeName;
        private IConnection _connection;
        private IModel _channel;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<ChatLogHub> _chatlogHub;

        public HabboActivityBackgroundService(IConfiguration configuration, ILogger<HabboActivityBackgroundService> logger, IHubContext<ChatLogHub> chatlogHubContext)
        {
            _configuration = configuration;
            _exchangeName = "habbo_activity";
            _logger = logger;
            _chatlogHub = chatlogHubContext;
            InitRabbitMq();
        }

        private void InitRabbitMq()
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration.GetSection("keplercms:rabbitmq:hostname").Value,
                Port = int.Parse(_configuration.GetSection("keplercms:rabbitmq:port").Value),
                UserName = _configuration.GetSection("keplercms:rabbitmq:username").Value,
                Password = _configuration.GetSection("keplercms:rabbitmq:password").Value,
                ClientProvidedName = "HabboActivityBackgroundService"
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(exchange: "habbo_activity", type: ExchangeType.Direct, durable: true);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            // Declare and bind the queue to the exchange within ExecuteAsync
            var queueName = _channel.QueueDeclare("activity", false, false, false, null).QueueName;
            _channel.QueueBind(queue: queueName, exchange: "habbo_activity", routingKey: "chat");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            { var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                switch(ea.RoutingKey) {
                    case "chat":
                    {
                        try
                        {
                            var messageObject = JsonConvert.DeserializeObject<ChatlogEventMessageModel>(messageJson);

                            _logger.LogInformation($"Received message from user: {messageObject.PlayerId}, message: {messageObject.Message}, room: {messageObject.RoomId}");
                            // Process the message here

                            await _chatlogHub.Clients.Groups(new List<string> { "room_" + messageObject.RoomId }).SendAsync("ReceiveMessage", messageObject.PlayerId, messageObject.Message, stoppingToken);
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError($"Error parsing message: {ex.Message}");
                        }
                    }
                    break;
                    default:
                    {
                        _logger.LogWarning($"Received message with unknown routing key: {ea.RoutingKey}");
                    }
                    break;
                }

               
            };

            _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel.Close();
            _connection.Close();
            base.Dispose();
        }
    }
}
