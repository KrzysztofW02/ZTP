using System;
using System.IO;
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
            _connection = factory.CreateConnection();   
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
                Console.WriteLine($"Nie znaleziono folderu: {inputFolder}");
                return;
            }

            foreach (var file in Directory.EnumerateFiles(inputFolder, "*.jpg"))
            {
                var job = new ImageDTO
                {
                    FileName = Path.GetFileName(file),
                    ImageBytes = File.ReadAllBytes(file)
                };

                byte[] body = JsonSerializer.SerializeToUtf8Bytes(job);
                IBasicProperties props = _channel.CreateBasicProperties();
                props.Persistent = true;

                _channel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: "",
                    basicProperties: props,
                    body: body
                );

                Console.WriteLine($"Published: {job.FileName}");
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
