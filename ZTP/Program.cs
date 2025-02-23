using System;

namespace ZTP
{
    class Program
    {
        static void Main()
        {
            int[,] image = {
            { 1, 2, 3, 4, 5 },
            { 6, 7, 8, 9, 10 },
            { 11, 12, 13, 14, 15 },
            { 16, 17, 18, 19, 20 },
            { 21, 22, 23, 24, 25 }
            };

            int[,] kernel = {
            { -1, -1, -1 },
            { -1,  8, -1 },
            { -1, -1, -1 }
            };

            int[,] result = ApplyConvolution(image, kernel);

            PrintMatrix(result);
        }

        static int[,] ApplyConvolution(int[,] input, int[,] kernel)
        {
            int height = input.GetLength(0);
            int width = input.GetLength(1);
            int kSize = kernel.GetLength(0);
            int offset = kSize / 2;

            int[,] output = new int[height, width];

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int sum = 0;

                    for (int ki = 0; ki < kSize; ki++)
                    {
                        for (int kj = 0; kj < kSize; kj++)
                        {
                            int imgX = i + ki - offset;
                            int imgY = j + kj - offset;

                            if (imgX < 0 || imgX >= height || imgY < 0 || imgY >= width)
                                continue;

                            sum += input[imgX, imgY] * kernel[ki, kj];
                        }
                    }

                    output[i, j] = sum;
                }
            }

            return output;
        }

        static void PrintMatrix(int[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Console.Write(matrix[i, j].ToString("D2") + " ");
                }
                Console.WriteLine();
            }
        }
    }
}
