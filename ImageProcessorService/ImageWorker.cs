using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace ImageProcessorService
{
    public class ImageWorker : IDisposable
    {
        private const string JobQueue = "image_jobs";
        private const string ResultQueue = "image_results";

        private readonly IConnection _conn;
        private readonly IModel _channel;

        public ImageWorker(string host)
        {
            var factory = new ConnectionFactory { HostName = host };
            _conn = factory.CreateConnection();
            _channel = _conn.CreateModel();
            _channel.QueueDeclare(JobQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueDeclare(ResultQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
        }

        public void Run()
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                string folderPath = Encoding.UTF8.GetString(ea.Body.ToArray());
                Console.WriteLine($"[Worker] Received: {folderPath}");

                string outputFolder = Path.Combine(folderPath, "processed");
                ImageProcessor.ProcessImages(folderPath, outputFolder);

                var resultMsg = $"Processed: {Path.GetFileName(folderPath)}";
                var body = Encoding.UTF8.GetBytes(resultMsg);
                var props = _channel.CreateBasicProperties();
                props.Persistent = true;
                _channel.BasicPublish("", ResultQueue, props, body);
                Console.WriteLine($"[Worker] Sent result: {resultMsg}");

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            };

            _channel.BasicQos(0, 1, false);
            _channel.BasicConsume(JobQueue, autoAck: false, consumer: consumer);

            Console.WriteLine("[Worker] Waiting for jobs. Press Enter to exit.");
            Console.ReadLine();
        }

        public void Dispose()
        {
            _channel.Close();
            _conn.Close();
        }
    }
}
