using System;
using System.Globalization;
using System.Text;

namespace SC880
{
    public sealed class MatrixData
    {
        public int Rows;
        public int Cols;
        public double[,] Data;

        public MatrixData(int rows, int cols)
        {
            if (rows < 1 || cols < 1 || rows > 6 || cols > 6) throw new CalcError("Размер матрицы 1…6");
            Rows = rows;
            Cols = cols;
            Data = new double[rows, cols];
        }

        public MatrixData(double[,] data)
        {
            Rows = data.GetLength(0);
            Cols = data.GetLength(1);
            Data = (double[,])data.Clone();
        }

        public static MatrixData Identity(int n)
        {
            MatrixData m = new MatrixData(n, n);
            for (int i = 0; i < n; i++) m.Data[i, i] = 1;
            return m;
        }

        public MatrixData Clone()
        {
            return new MatrixData(Data);
        }

        public static MatrixData Add(MatrixData a, MatrixData b)
        {
            CheckSame(a, b);
            MatrixData r = new MatrixData(a.Rows, a.Cols);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Cols; j++) r.Data[i, j] = a.Data[i, j] + b.Data[i, j];
            return r;
        }

        public static MatrixData Sub(MatrixData a, MatrixData b)
        {
            CheckSame(a, b);
            MatrixData r = new MatrixData(a.Rows, a.Cols);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Cols; j++) r.Data[i, j] = a.Data[i, j] - b.Data[i, j];
            return r;
        }

        public static MatrixData Mul(MatrixData a, MatrixData b)
        {
            if (a.Cols != b.Rows) throw new CalcError("Размеры не совпадают: A×B");
            MatrixData r = new MatrixData(a.Rows, b.Cols);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < b.Cols; j++)
                {
                    double s = 0;
                    for (int k = 0; k < a.Cols; k++) s += a.Data[i, k] * b.Data[k, j];
                    r.Data[i, j] = s;
                }
            return r;
        }

        public static MatrixData Scalar(MatrixData a, double value)
        {
            MatrixData r = new MatrixData(a.Rows, a.Cols);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Cols; j++) r.Data[i, j] = a.Data[i, j] * value;
            return r;
        }

        public MatrixData Transpose()
        {
            MatrixData r = new MatrixData(Cols, Rows);
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++) r.Data[j, i] = Data[i, j];
            return r;
        }

        public double Determinant()
        {
            if (Rows != Cols) throw new CalcError("Определитель: нужна квадратная матрица");
            int n = Rows;
            double[,] a = (double[,])Data.Clone();
            double det = 1;
            for (int col = 0; col < n; col++)
            {
                int pivot = col;
                for (int row = col + 1; row < n; row++)
                    if (Math.Abs(a[row, col]) > Math.Abs(a[pivot, col])) pivot = row;
                if (Math.Abs(a[pivot, col]) < 1e-14) return 0;
                if (pivot != col)
                {
                    for (int k = 0; k < n; k++)
                    {
                        double t = a[col, k]; a[col, k] = a[pivot, k]; a[pivot, k] = t;
                    }
                    det = -det;
                }
                det *= a[col, col];
                for (int row = col + 1; row < n; row++)
                {
                    double f = a[row, col] / a[col, col];
                    for (int k = col; k < n; k++) a[row, k] -= f * a[col, k];
                }
            }
            return det;
        }

        public MatrixData Inverse()
        {
            if (Rows != Cols) throw new CalcError("Обратная матрица: нужна квадратная");
            int n = Rows;
            double[,] a = (double[,])Data.Clone();
            double[,] inv = new double[n, n];
            for (int i = 0; i < n; i++) inv[i, i] = 1;
            for (int col = 0; col < n; col++)
            {
                int pivot = col;
                for (int row = col + 1; row < n; row++)
                    if (Math.Abs(a[row, col]) > Math.Abs(a[pivot, col])) pivot = row;
                if (Math.Abs(a[pivot, col]) < 1e-14) throw new CalcError("Матрица вырождена");
                if (pivot != col)
                {
                    for (int k = 0; k < n; k++)
                    {
                        double t = a[col, k]; a[col, k] = a[pivot, k]; a[pivot, k] = t;
                        double u = inv[col, k]; inv[col, k] = inv[pivot, k]; inv[pivot, k] = u;
                    }
                }
                double d = a[col, col];
                for (int k = 0; k < n; k++)
                {
                    a[col, k] /= d;
                    inv[col, k] /= d;
                }
                for (int row = 0; row < n; row++)
                {
                    if (row == col) continue;
                    double f = a[row, col];
                    if (Math.Abs(f) < 1e-300) continue;
                    for (int k = 0; k < n; k++)
                    {
                        a[row, k] -= f * a[col, k];
                        inv[row, k] -= f * inv[col, k];
                    }
                }
            }
            return new MatrixData(inv);
        }

        public static void CheckSame(MatrixData a, MatrixData b)
        {
            if (a.Rows != b.Rows || a.Cols != b.Cols) throw new CalcError("Размеры матриц не совпадают");
        }

        public string Format()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    if (j > 0) sb.Append("  ");
                    sb.Append(Statistics.Num(Data[i, j]));
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }

    public sealed class VectorData
    {
        public double[] Values;

        public VectorData(int dimension)
        {
            if (dimension < 2 || dimension > 3) throw new CalcError("Размерность вектора 2 или 3");
            Values = new double[dimension];
        }

        public VectorData(params double[] values)
        {
            if (values.Length < 2 || values.Length > 3) throw new CalcError("Размерность вектора 2 или 3");
            Values = (double[])values.Clone();
        }

        public int Dimension
        {
            get { return Values.Length; }
        }

        public VectorData Clone()
        {
            return new VectorData(Values);
        }

        public double Norm()
        {
            double s = 0;
            for (int i = 0; i < Values.Length; i++) s += Values[i] * Values[i];
            return Math.Sqrt(s);
        }

        public static VectorData Add(VectorData a, VectorData b)
        {
            Check(a, b);
            double[] v = new double[a.Dimension];
            for (int i = 0; i < v.Length; i++) v[i] = a.Values[i] + b.Values[i];
            return new VectorData(v);
        }

        public static VectorData Sub(VectorData a, VectorData b)
        {
            Check(a, b);
            double[] v = new double[a.Dimension];
            for (int i = 0; i < v.Length; i++) v[i] = a.Values[i] - b.Values[i];
            return new VectorData(v);
        }

        public static double Dot(VectorData a, VectorData b)
        {
            Check(a, b);
            double s = 0;
            for (int i = 0; i < a.Dimension; i++) s += a.Values[i] * b.Values[i];
            return s;
        }

        public static VectorData Cross(VectorData a, VectorData b)
        {
            if (a.Dimension != 3 || b.Dimension != 3) throw new CalcError("Векторное произведение: 3D");
            double[] v = new double[3];
            v[0] = a.Values[1] * b.Values[2] - a.Values[2] * b.Values[1];
            v[1] = a.Values[2] * b.Values[0] - a.Values[0] * b.Values[2];
            v[2] = a.Values[0] * b.Values[1] - a.Values[1] * b.Values[0];
            return new VectorData(v);
        }

        public static double Angle(VectorData a, VectorData b)
        {
            double na = a.Norm(), nb = b.Norm();
            if (na == 0 || nb == 0) throw new CalcError("Нулевой вектор");
            double c = Dot(a, b) / (na * nb);
            if (c > 1) c = 1;
            if (c < -1) c = -1;
            return Math.Acos(c);
        }

        public VectorData Unit()
        {
            double n = Norm();
            if (n == 0) throw new CalcError("Нулевой вектор");
            double[] v = new double[Dimension];
            for (int i = 0; i < Dimension; i++) v[i] = Values[i] / n;
            return new VectorData(v);
        }

        public string Format()
        {
            StringBuilder sb = new StringBuilder("(");
            for (int i = 0; i < Values.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(Statistics.Num(Values[i]));
            }
            sb.Append(")");
            return sb.ToString();
        }

        private static void Check(VectorData a, VectorData b)
        {
            if (a.Dimension != b.Dimension) throw new CalcError("Размерность векторов не совпадает");
        }
    }
}
