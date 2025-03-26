using System;
using System.Diagnostics;
using System.Drawing;
using ZTP;

namespace ImageProcessingApp
{
    class Program
    {
        static void Main(string[] args)
        {
            TestMatrixMultiplication();
            TestImageProcessing();

            Console.WriteLine("Naciśnij dowolny klawisz, aby zakończyć.");
            Console.ReadKey();
        }

        private static void TestMatrixMultiplication()
        {
            Console.WriteLine("\n--- Test: Mnożenie macierzy ---");

            int largeRows = 3000;
            int largeCols = 200;
            int smallRows = 200;
            int smallCols = 5;

            double[,] largeMatrix = GenerateMatrix(largeRows, largeCols);
            double[,] smallMatrix = GenerateMatrix(smallRows, smallCols);

            Stopwatch sw = Stopwatch.StartNew();
            double[,] result = MatrixMultiplication.Multiply(largeMatrix, smallMatrix);
            sw.Stop();

            Console.WriteLine($"Mnożenie macierzy zajęło: {sw.ElapsedMilliseconds} ms");
        }

        private static double[,] GenerateMatrix(int rows, int cols)
        {
            double[,] matrix = new double[rows, cols];
            Random rnd = new Random();
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    matrix[i, j] = rnd.NextDouble() * 10;
            return matrix;
        }

        private static void TestImageProcessing()
        {
            Console.WriteLine("\n--- Test: Przetwarzanie obrazów w folderze ---");

            string inputFolder = "Images";  
            string outputFolder = "output"; 

            if (!Directory.Exists(inputFolder))
            {
                Console.WriteLine($"Nie znaleziono folderu wejściowego: {inputFolder}");
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            Stopwatch sw = Stopwatch.StartNew();
            ImageProcessor.ProcessImages(inputFolder, outputFolder);
            sw.Stop();

            Console.WriteLine($"Przetwarzanie obrazów zakończone w: {sw.ElapsedMilliseconds} ms");
        }
    }
}
