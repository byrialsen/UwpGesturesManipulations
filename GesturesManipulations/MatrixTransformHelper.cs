using System;
using Windows.UI.Xaml.Media;

namespace GesturesManipulations
{
    public static class MatrixExtensions
    {
        public static double GetRotation(this Matrix matrix)
        {
            return Math.Atan2(matrix.M21, matrix.M11) * (180 / Math.PI);
        }

        public static double GetScale(this Matrix matrix)
        {
            return matrix.M11;
        }

        public static void SetScale(this Matrix matrix, double scale)
        {
            matrix.M11 = matrix.M22 = scale;
        }
    }
}
