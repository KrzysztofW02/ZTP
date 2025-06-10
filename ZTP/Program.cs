using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Shared;

namespace ZTP
{
    class Program
    {
        static void Main(string[] args)
        {
            RunMatrixTest();

            var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ__HOSTNAME") ?? "localhost";
            var imagesPath = Environment.GetEnvironmentVariable("IMAGE_FOLDER")
                              ?? throw new Exception("IMAGE_FOLDER not set");
            using var publisher = new ImagePublisher(rabbitHost);

            int total = publisher.PublishAll(imagesPath);
            Console.WriteLine($"Published {total} images, awaiting processing results...");

            var factory = new ConnectionFactory { HostName = rabbitHost };
            using var conn = factory.CreateConnection();
            using var chan = conn.CreateModel();
            chan.QueueDeclare("image_results", durable: true, exclusive: false, autoDelete: false);

            int processed = 0;
            var consumer = new EventingBasicConsumer(chan);
            consumer.Received += (ch, ea) =>
            {
                var fileName = Encoding.UTF8.GetString(ea.Body.ToArray());
                processed++;
                Console.WriteLine($"Result {processed}/{total}: {fileName}");
                chan.BasicAck(ea.DeliveryTag, multiple: false);

                if (processed >= total)
                {
                    Console.WriteLine("All images processed. Exiting.");
                    Environment.Exit(0);
                }
            };
            chan.BasicConsume("image_results", autoAck: false, consumer: consumer);

            Console.WriteLine("Waiting for processing results...");
            Console.ReadLine();
        }

        private static void RunMatrixTest()
        {
            Console.WriteLine("\n--- Test: Matrix Multiplication ---");
            var rnd = new Random();
            var A = GenerateMatrix(3000, 200, rnd);
            var B = GenerateMatrix(200, 5, rnd);

            var sw = Stopwatch.StartNew();
            var C = MatrixMultiplication.Multiply(A, B);
            sw.Stop();

            Console.WriteLine($"Matrix multiplication took: {sw.ElapsedMilliseconds} ms");
        }

        private static double[,] GenerateMatrix(int r, int c, Random rnd)
        {
            var m = new double[r, c];
            for (int i = 0; i < r; i++)
                for (int j = 0; j < c; j++)
                    m[i, j] = rnd.NextDouble() * 10;
            return m;
        }
    }
}
