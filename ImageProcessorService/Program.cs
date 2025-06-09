namespace ImageProcessorService
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AppContext.SetSwitch("System.Drawing.EnableUnixSupport", true);

            var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ__HOSTNAME") ?? "localhost";
            var imagesPath = Environment.GetEnvironmentVariable("IMAGE_FOLDER") ?? throw new Exception("There is no variable for image folder");
            using var service = new ImageWorker(rabbitHost, imagesPath);
            service.Run();
        }
    }
}
