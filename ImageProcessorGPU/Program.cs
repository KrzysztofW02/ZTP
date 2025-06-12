using System;

namespace ImageProcessorGpuService
{
    class Program
    {
        static void Main()
        {
            var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ__HOSTNAME") ?? "localhost";
            var imageFolder = Environment.GetEnvironmentVariable("IMAGE_FOLDER")
                              ?? throw new Exception("IMAGE_FOLDER not set");

            using var worker = new GpuImageWorker(rabbitHost, imageFolder);
            worker.Run();
        }
    }
}
