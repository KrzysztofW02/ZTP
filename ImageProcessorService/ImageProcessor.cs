using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Buffers;
using System.Numerics;

namespace ImageProcessorService
{
    public static class ImageProcessor
    {
        public static void ProcessImages(string inputFolder, string outputFolder)
        {
            if (!Directory.Exists(inputFolder))
            {
                Console.WriteLine($"Folder {inputFolder} nie istnieje.");
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            string[] imageFiles = Directory.GetFiles(inputFolder, "*.*", SearchOption.TopDirectoryOnly);

            Parallel.ForEach(imageFiles, file =>
            {
                try
                {
                    Console.WriteLine($"Przetwarzanie obrazu: {file}");
                    using (Bitmap processed = ProcessImage(file))
                    {
                        string outputFile = Path.Combine(outputFolder, Path.GetFileName(file));
                        processed.Save(outputFile);
                        Console.WriteLine($"Zapisano: {outputFile}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd przetwarzania {file}: {ex.Message}");
                }
            });
        }

        public static unsafe Bitmap ProcessImage(string photoFile)
        {
            Bitmap bitmap = new Bitmap(photoFile);
            Bitmap result = new Bitmap(bitmap.Width, bitmap.Height, bitmap.PixelFormat);

            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData srcData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
            BitmapData dstData = result.LockBits(rect, ImageLockMode.WriteOnly, result.PixelFormat);

            int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            int height = bitmap.Height;
            int width = bitmap.Width;

            int[,] kernel = new int[,]
            {
                { 0, -1, 0 },
                { -1, 4, -1 },
                { 0, -1, 0 }
            };

            unsafe
            {
                byte* srcPtr = (byte*)srcData.Scan0;
                byte* dstPtr = (byte*)dstData.Scan0;
                int stride = srcData.Stride;

                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int newR = 0, newG = 0, newB = 0;

                        for (int ky = -1; ky <= 1; ky++)
                        {
                            for (int kx = -1; kx <= 1; kx++)
                            {
                                int pixelX = x + kx;
                                int pixelY = y + ky;
                                byte* p = srcPtr + pixelY * stride + pixelX * bytesPerPixel;
                                int kernelValue = kernel[ky + 1, kx + 1];
                                newR += p[0] * kernelValue;
                                newG += p[1] * kernelValue;
                                newB += p[2] * kernelValue;
                            }
                        }

                        newR = Math.Min(255, Math.Max(0, newR));
                        newG = Math.Min(255, Math.Max(0, newG));
                        newB = Math.Min(255, Math.Max(0, newB));

                        byte* dstPixel = dstPtr + y * stride + x * bytesPerPixel;
                        dstPixel[0] = (byte)newR;
                        dstPixel[1] = (byte)newG;
                        dstPixel[2] = (byte)newB;
                        if (bytesPerPixel == 4)
                        {
                            dstPixel[3] = 255;
                        }
                    }
                }
            }

            bitmap.UnlockBits(srcData);
            result.UnlockBits(dstData);
            bitmap.Dispose();

            return result;
        }

        public static Bitmap ProcessImageManaged(string photoFile)
        {
            Bitmap bitmap = new Bitmap(photoFile);
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData srcData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

            int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            int byteCount = srcData.Stride * bitmap.Height;
            byte[] pixelBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
            System.Runtime.InteropServices.Marshal.Copy(srcData.Scan0, pixelBuffer, 0, byteCount);
            bitmap.UnlockBits(srcData);

            int width = bitmap.Width;
            int height = bitmap.Height;
            int stride = srcData.Stride;
            byte[,,] pixels = new byte[height, width, bytesPerPixel];

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * stride;
                for (int x = 0; x < width; x++)
                {
                    int pos = rowOffset + x * bytesPerPixel;
                    for (int c = 0; c < bytesPerPixel; c++)
                    {
                        pixels[y, x, c] = pixelBuffer[pos + c];
                    }
                }
            }

            int[,] kernel = new int[,]
            {
                { 0, -1, 0 },
                { -1, 4, -1 },
                { 0, -1, 0 }
            };

            byte[,,] resultPixels = new byte[height, width, bytesPerPixel];

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int[] newColor = new int[3] { 0, 0, 0 };

                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            int kernelValue = kernel[ky + 1, kx + 1];
                            newColor[0] += pixels[y + ky, x + kx, 0] * kernelValue;
                            newColor[1] += pixels[y + ky, x + kx, 1] * kernelValue;
                            newColor[2] += pixels[y + ky, x + kx, 2] * kernelValue;
                        }
                    }
                    for (int c = 0; c < 3; c++)
                    {
                        newColor[c] = Math.Min(255, Math.Max(0, newColor[c]));
                    }

                    resultPixels[y, x, 0] = (byte)newColor[0];
                    resultPixels[y, x, 1] = (byte)newColor[1];
                    resultPixels[y, x, 2] = (byte)newColor[2];
                    if (bytesPerPixel == 4)
                        resultPixels[y, x, 3] = 255;
                }
            }

            Bitmap resultBitmap = new Bitmap(width, height, bitmap.PixelFormat);
            BitmapData dstData = resultBitmap.LockBits(rect, ImageLockMode.WriteOnly, resultBitmap.PixelFormat);

            byte[] resultBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * stride;
                for (int x = 0; x < width; x++)
                {
                    int pos = rowOffset + x * bytesPerPixel;
                    for (int c = 0; c < bytesPerPixel; c++)
                    {
                        resultBuffer[pos + c] = resultPixels[y, x, c];
                    }
                }
            }
            System.Runtime.InteropServices.Marshal.Copy(resultBuffer, 0, dstData.Scan0, byteCount);
            resultBitmap.UnlockBits(dstData);
            bitmap.Dispose();

            ArrayPool<byte>.Shared.Return(pixelBuffer);
            ArrayPool<byte>.Shared.Return(resultBuffer);

            return resultBitmap;
        }

        public static Bitmap ProcessImageSIMD(string photoFile)
        {
            Bitmap bitmap = new Bitmap(photoFile);
            Bitmap result = new Bitmap(bitmap.Width, bitmap.Height, bitmap.PixelFormat);
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData srcData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
            BitmapData dstData = result.LockBits(rect, ImageLockMode.WriteOnly, result.PixelFormat);

            int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            int width = bitmap.Width;
            int height = bitmap.Height;
            int stride = srcData.Stride;
            int byteCount = stride * height;

            byte[] pixelBuffer = new byte[byteCount];
            Marshal.Copy(srcData.Scan0, pixelBuffer, 0, byteCount);
            byte[] resultBuffer = new byte[byteCount];

            float[] redChannel = new float[width * height];
            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * stride;
                for (int x = 0; x < width; x++)
                {
                    int pos = rowOffset + x * bytesPerPixel;
                    redChannel[y * width + x] = pixelBuffer[pos];
                }
            }

            float[,] kernel = new float[3, 3]
            {
                { 0f, -1f, 0f },
                { -1f, 4f, -1f },
                { 0f, -1f, 0f }
            };
            int kCenter = 1;

            int vectorSize = Vector<float>.Count;
            float[] outputRed = new float[width * height];

            for (int y = kCenter; y < height - kCenter; y++)
            {
                for (int x = kCenter; x <= width - kCenter - vectorSize; x += vectorSize)
                {
                    Vector<float> sum = Vector<float>.Zero;
                    for (int ky = -kCenter; ky <= kCenter; ky++)
                    {
                        int srcRow = (y + ky) * width;
                        for (int kx = -kCenter; kx <= kCenter; kx++)
                        {
                            float kVal = kernel[ky + kCenter, kx + kCenter];
                            int index = srcRow + (x + kx);
                            Vector<float> vec = new Vector<float>(redChannel, index);
                            sum += vec * new Vector<float>(kVal);
                        }
                    }
                    sum.CopyTo(outputRed, y * width + x);
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y < kCenter || y >= height - kCenter || x < kCenter || x >= width - kCenter)
                        outputRed[y * width + x] = redChannel[y * width + x];
                }
            }

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * stride;
                for (int x = 0; x < width; x++)
                {
                    int pos = rowOffset + x * bytesPerPixel;
                    int redVal = (int)Math.Min(255, Math.Max(0, outputRed[y * width + x]));
                    resultBuffer[pos] = (byte)redVal;
                    resultBuffer[pos + 1] = pixelBuffer[pos + 1];
                    resultBuffer[pos + 2] = pixelBuffer[pos + 2];
                    if (bytesPerPixel == 4)
                        resultBuffer[pos + 3] = pixelBuffer[pos + 3];
                }
            }

            Marshal.Copy(resultBuffer, 0, dstData.Scan0, byteCount);
            bitmap.UnlockBits(srcData);
            result.UnlockBits(dstData);
            bitmap.Dispose();

            return result;
        }
    }
}
