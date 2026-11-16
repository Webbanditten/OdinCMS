using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace KeplerCMS.Helpers
{
    /// <summary>Builds a RabbitMQ <see cref="ConnectionFactory"/> from the keplercms:rabbitmq:* settings.</summary>
    public static class RabbitMqConnectionFactory
    {
        public static ConnectionFactory Create(IConfiguration configuration, string clientName)
        {
            return new ConnectionFactory
            {
                HostName = configuration.GetSection("keplercms:rabbitmq:hostname").Value,
                Port = int.Parse(configuration.GetSection("keplercms:rabbitmq:port").Value),
                UserName = configuration.GetSection("keplercms:rabbitmq:username").Value,
                Password = configuration.GetSection("keplercms:rabbitmq:password").Value,
                ClientProvidedName = clientName,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
            };
        }
    }
}
