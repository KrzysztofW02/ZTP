using System;
using System.IO;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Shared;   

namespace ZTP
{
    public class ImagePublisher : IDisposable
    {
        private const string ExchangeName = "image_jobs";
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public ImagePublisher(string host)
        {
            var factory = new ConnectionFactory
            {
                HostName = host
            };
            _connection = ConnectionHelper.CreateConnectionWithRetry(factory);
            _channel = _connection.CreateModel();        
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Fanout,
                durable: true
            );
        }

        public void PublishAll(string inputFolder)
        {
            if (!Directory.Exists(inputFolder))
            {
                Console.WriteLine($"Folder: {inputFolder} not found");
                return;
            }

            foreach (var file in Directory.EnumerateFiles(inputFolder, "*.jpg"))
            {
                var fileName = Path.GetFileName(file);
                byte[] body = Encoding.UTF8.GetBytes(fileName);

                IBasicProperties props = _channel.CreateBasicProperties();
                props.Persistent = true;

                _channel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: "",
                    basicProperties: props,
                    body: body
                );

                Console.WriteLine($"Published filename: {fileName}");
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
