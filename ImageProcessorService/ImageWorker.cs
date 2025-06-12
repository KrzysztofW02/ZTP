using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;

namespace ImageProcessorService
{
    public class ImageWorker : IDisposable
    {
        private const string ExchangeName = "cpu_jobs";         
        private const string JobQueue = "cpu_jobs";              
        private const string ResultQueue = "image_results";

        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _baseFolder;

        public ImageWorker(string host, string baseFolder)
        {
            _baseFolder = baseFolder;
            var factory = new ConnectionFactory { HostName = host };
            _connection = ConnectionHelper.CreateConnectionWithRetry(factory);
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: true);
            _channel.QueueDeclare(JobQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(JobQueue, ExchangeName, routingKey: string.Empty);
            _channel.QueueDeclare(ResultQueue, durable: true, exclusive: false, autoDelete: false);
        }

        public void Run()
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnMessage;
            _channel.BasicQos(0, 1, false); 
            _channel.BasicConsume(JobQueue, autoAck: false, consumer: consumer);

            Console.WriteLine("[CPU Worker] Waiting for images. Ctrl+C to exit.");
            System.Threading.Thread.Sleep(Timeout.Infinite);
        }

        private void OnMessage(object sender, BasicDeliverEventArgs ea)
        {
            var fileName = Encoding.UTF8.GetString(ea.Body.ToArray());
            Console.WriteLine($"[CPU Worker] Received filename: {fileName}");

            try
            {
                var inputPath = Path.Combine(_baseFolder, fileName);
                var outputDir = Path.Combine(_baseFolder, "processed_cpu");
                Directory.CreateDirectory(outputDir);
                var outputPath = Path.Combine(outputDir, fileName);

                using var processed = ImageProcessor.ProcessImage(inputPath); 
                processed.Save(outputPath, ImageFormat.Jpeg);

                Console.WriteLine($"[CPU Worker] Saved processed: {outputPath}");

                var resultMsg = $"CPU::{fileName}";
                var body = Encoding.UTF8.GetBytes(resultMsg);
                var props = _channel.CreateBasicProperties();
                props.Persistent = true;

                _channel.BasicPublish("", ResultQueue, props, body);
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CPU Worker] ERROR: {ex.Message}");
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
