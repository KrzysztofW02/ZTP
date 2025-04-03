using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZTP
{
    public class MatrixMultiplication
    {
        public static double[,] Multiply(double[,] largeMatrix, double[,] smallMatrix)
        {
            int largeRows = largeMatrix.GetLength(0);
            int largeCols = largeMatrix.GetLength(1);
            int smallRows = smallMatrix.GetLength(0);
            int smallCols = smallMatrix.GetLength(1);

            if (largeCols != smallRows)
                throw new ArgumentException("Liczba kolumn pierwszej macierzy musi być równa liczbie wierszy drugiej.");

            double[,] result = new double[largeRows, smallCols];

            Parallel.For(0, largeRows, i =>
            {
                for (int j = 0; j < smallCols; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < largeCols; k++)
                    {
                        sum += largeMatrix[i, k] * smallMatrix[k, j];
                    }
                    result[i, j] = sum;
                }
            });

            return result;
        }
    }

}
