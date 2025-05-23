namespace ImageProcessorService
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using var service = new ImageWorker("localhost");
            service.Run();
        }
    }
}
