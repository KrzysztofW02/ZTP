using System;
using System.IO;
using System.Text;
using RabbitMQ.Client;

namespace ZTP
{
    public class ImagePublisher : IDisposable
    {
        private const string CpuExchange = "cpu_jobs";
        private const string GpuExchange = "gpu_jobs";

        private readonly IConnection _connection;
        private readonly IModel _channel;

        public ImagePublisher(string host)
        {
            var factory = new ConnectionFactory { HostName = host };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(CpuExchange, ExchangeType.Fanout, durable: true);
            _channel.ExchangeDeclare(GpuExchange, ExchangeType.Fanout, durable: true);
        }

        public int PublishAll(string inputFolder)
        {
            if (!Directory.Exists(inputFolder))
            {
                Console.WriteLine($"Folder not found: {inputFolder}");
                return 0;
            }

            int count = 0;
            foreach (var file in Directory.EnumerateFiles(inputFolder, "*.jpg"))
            {
                var fileName = Path.GetFileName(file);
                var body = Encoding.UTF8.GetBytes(fileName);
                var props = _channel.CreateBasicProperties();
                props.Persistent = true;

                _channel.BasicPublish(CpuExchange, "", props, body);
                _channel.BasicPublish(GpuExchange, "", props, body);

                Console.WriteLine($"Published to CPU & GPU: {fileName}");
                count += 2;
            }
            return count;
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
