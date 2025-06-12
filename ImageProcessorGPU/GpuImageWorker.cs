using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Algorithms;

namespace ImageProcessorGpuService
{
    public class GpuImageWorker : IDisposable
    {
        private const string ExchangeName = "gpu_jobs";
        private const string JobQueue = "gpu_jobs";
        private const string ResultQueue = "image_results";

        private readonly IConnection _conn;
        private readonly IModel _ch;
        private readonly string _baseFolder;
        private readonly Context _context;
        private readonly Accelerator _acc;
        private readonly Action<Index2D, int, int, ArrayView<byte>, ArrayView<byte>> _kernel;

        public GpuImageWorker(string host, string baseFolder)
        {
            _baseFolder = baseFolder;

            var factory = new ConnectionFactory { HostName = host };
            _conn = factory.CreateConnection();
            _ch = _conn.CreateModel();
            _ch.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: true);
            _ch.QueueDeclare(JobQueue, durable: true, exclusive: false, autoDelete: false);
            _ch.QueueBind(JobQueue, ExchangeName, "");
            _ch.QueueDeclare(ResultQueue, durable: true, exclusive: false, autoDelete: false);

            _context = Context.CreateDefault();                             
            var device = _context.GetPreferredDevice(preferCPU: false);    
            _acc = device.CreateAccelerator(_context);
            Console.WriteLine($"[GPU Worker] Using device: {_acc.Name}");

            _kernel = _acc.LoadAutoGroupedStreamKernel<Index2D, int, int, ArrayView<byte>, ArrayView<byte>>(SharpenKernel);
        }

        public void Run()
        {
            var consumer = new EventingBasicConsumer(_ch);
            consumer.Received += OnMessage;
            _ch.BasicQos(0, 1, false);
            _ch.BasicConsume(JobQueue, autoAck: false, consumer: consumer);

            Console.WriteLine("[GPU Worker] Waiting for jobs. Ctrl+C to exit");
            System.Threading.Thread.Sleep(Timeout.Infinite);
        }

        private void OnMessage(object sender, BasicDeliverEventArgs ea)
        {
            var fileName = Encoding.UTF8.GetString(ea.Body.ToArray());
            Console.WriteLine($"[GPU Worker] Received: {fileName}");

            try
            {
                // Load bitmap to byte[]
                var inputPath = Path.Combine(_baseFolder, fileName);
                using var bmp = new Bitmap(inputPath);
                int w = bmp.Width;
                int h = bmp.Height;
                int bpp = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
                int total = w * h * bpp;
                var buffer = new byte[total];
                var srcData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, bmp.PixelFormat);
                System.Runtime.InteropServices.Marshal.Copy(srcData.Scan0, buffer, 0, total);
                bmp.UnlockBits(srcData);

                // GPU buffers
                using var srcBuf = _acc.Allocate1D(buffer);
                using var dstBuf = _acc.Allocate1D<byte>(total);

                // Run kernel
                var sw = Stopwatch.StartNew();
                _kernel((h, w), w, h, srcBuf.View, dstBuf.View);
                _acc.Synchronize();
                sw.Stop();

                dstBuf.CopyToCPU(buffer);

                var outDir = Path.Combine(_baseFolder, "processed_gpu");
                Directory.CreateDirectory(outDir);
                var outputPath = Path.Combine(outDir, fileName);
                using var outBmp = new Bitmap(w, h, bmp.PixelFormat);
                var dstData = outBmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, outBmp.PixelFormat);
                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, dstData.Scan0, total);
                outBmp.UnlockBits(dstData);
                outBmp.Save(outputPath, ImageFormat.Jpeg);

                Console.WriteLine($"[GPU Worker] Saved {fileName} ({sw.ElapsedMilliseconds} ms)");

                var result = $"{fileName}|GPU|{sw.ElapsedMilliseconds}";
                var body = Encoding.UTF8.GetBytes(result);
                var props = _ch.CreateBasicProperties();
                props.Persistent = true;
                _ch.BasicPublish("", ResultQueue, props, body);

                _ch.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GPU Worker] ERROR: {ex.Message}");
                _ch.BasicNack(ea.DeliveryTag, false, true);
            }
        }

        private static void SharpenKernel(Index2D idx, int width, int height, ArrayView<byte> src, ArrayView<byte> dst)
        {
            int y = idx.X, x = idx.Y;
            int bpp = 4;
            int i = (y * width + x) * bpp;

            if (y == 0 || x == 0 || y == height - 1 || x == width - 1)
            {
                for (int c = 0; c < bpp; c++)
                    dst[i + c] = src[i + c];
                return;
            }

            int[,] K = { { 0, -1, 0 }, { -1, 5, -1 }, { 0, -1, 0 } };
            for (int c = 0; c < 3; c++)
            {
                int sum = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int j = ((y + dy) * width + (x + dx)) * bpp + c;
                        sum += src[j] * K[dy + 1, dx + 1];
                    }
                dst[i + c] = (byte)XMath.Clamp(sum, 0, 255);
            }
            dst[i + 3] = 255;
        }

        public void Dispose()
        {
            _ch.Close();
            _conn.Close();
            _acc.Dispose();
            _context.Dispose();
        }
    }
}
