using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using RabbitMQ.Client;
using Shared;

namespace ZTP
{
    class Program
    {
        static void Main(string[] args)
        {
            RunMatrixTest();

            var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ__HOSTNAME") ?? "localhost";
            var imagesPath = Environment.GetEnvironmentVariable("IMAGE_FOLDER") ?? throw new Exception("There is no variable for image folder");
            var publisher = new ImagePublisher(rabbitHost);
            publisher.PublishAll(imagesPath);

            Console.WriteLine("All jobs published. Exiting.");
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
