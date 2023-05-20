using System;
using KeplerCMS.Data;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace KeplerCMS.Services
{
    public class CommandQueueService : ICommandQueueService
    {
        private readonly IConfiguration _configuration;

        public CommandQueueService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void QueueCommand(CommandQueueType command, CommandTemplate template)
        {
            try
            {
                var json = JsonConvert.SerializeObject(template);
                var factory = new ConnectionFactory
                {
                    HostName = _configuration.GetSection("keplercms:rabbitmq:hostname").Value, 
                    Port = int.Parse(_configuration.GetSection("keplercms:rabbitmq:port").Value),
                    UserName = _configuration.GetSection("keplercms:rabbitmq:username").Value,
                    Password = _configuration.GetSection("keplercms:rabbitmq:password").Value
                };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                channel.QueueDeclare("command_queue", false, false, false, null);
                channel.ExchangeDeclare(exchange: "commands", type: ExchangeType.Direct, true);

                var body = Encoding.UTF8.GetBytes(json);

                channel.BasicPublish(exchange: "commands",
                    routingKey: Enum.GetName(typeof(CommandQueueType), command),
                    basicProperties: null,
                    body: body);
            } catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            /*
            
            _context.CommandQueue.Add(new CommandQueue { Executed = 0, Command = Enum.GetName(typeof(CommandQueueType), command), Arguments = json });
            _context.SaveChanges();*/
        }
    }
}
