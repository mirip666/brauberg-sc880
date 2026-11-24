using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SC880
{
    public sealed class StatPoint
    {
        public double X;
        public double Y;
        public double Freq;

        public StatPoint()
        {
            Freq = 1;
        }

        public StatPoint(double x, double y, double freq)
        {
            X = x;
            Y = y;
            Freq = freq;
        }
    }

    public enum RegressionType
    {
        Linear,
        Quadratic,
        Logarithmic,
        Exponential,
        Power,
        Inverse
    }

    public sealed class StatResult
    {
        public int Count;
        public double SumX;
        public double SumX2;
        public double SumY;
        public double SumY2;
        public double SumXY;
        public double MeanX;
        public double MeanY;
        public double SampleSdX;
        public double SampleSdY;
        public double PopSdX;
        public double PopSdY;
        public double MinX;
        public double Q1;
        public double Median;
        public double Q3;
        public double MaxX;
        public double A;
        public double B;
        public double C;
        public double R;
        public string Equation = "";
    }

    public static class Statistics
    {
        public static StatResult Compute(List<StatPoint> raw, bool twoVar, bool useFrequency, RegressionType regression)
        {
            if (raw == null || raw.Count == 0) throw new CalcError("Нет данных");
            List<StatPoint> pts = new List<StatPoint>();
            double n = 0;
            for (int i = 0; i < raw.Count; i++)
            {
                StatPoint p = raw[i];
                if (double.IsNaN(p.X) || double.IsInfinity(p.X)) continue;
                if (twoVar && (double.IsNaN(p.Y) || double.IsInfinity(p.Y))) continue;
                double w = useFrequency ? p.Freq : 1;
                if (double.IsNaN(w) || double.IsInfinity(w)) continue;
                if (w <= 0) continue;
                n += w;
                pts.Add(p);
            }
            if (pts.Count == 0 || n <= 0) throw new CalcError("Нет данных");

            StatResult r = new StatResult();
            r.Count = (int)Math.Round(n);
            double sumX = 0, sumX2 = 0, sumY = 0, sumY2 = 0, sumXY = 0;
            double min = double.MaxValue, max = double.MinValue;
            List<double> expanded = new List<double>();
            bool expand = n <= 200000;
            for (int i = 0; i < pts.Count; i++)
            {
                StatPoint p = pts[i];
                double w = useFrequency ? p.Freq : 1;
                sumX += p.X * w;
                sumX2 += p.X * p.X * w;
                if (p.X < min) min = p.X;
                if (p.X > max) max = p.X;
                if (twoVar)
                {
                    sumY += p.Y * w;
                    sumY2 += p.Y * p.Y * w;
                    sumXY += p.X * p.Y * w;
                }
                if (expand)
                {
                    int times = (int)Math.Round(w);
                    if (times < 1) times = 1;
                    if (times > 100000) expand = false;
                    else for (int k = 0; k < times; k++) expanded.Add(p.X);
                }
            }

            r.SumX = sumX;
            r.SumX2 = sumX2;
            r.SumY = sumY;
            r.SumY2 = sumY2;
            r.SumXY = sumXY;
            r.MeanX = sumX / n;
            double varX = (sumX2 - sumX * sumX / n) / (n - 1);
            r.SampleSdX = n > 1 ? Math.Sqrt(Math.Max(0, varX)) : 0;
            r.PopSdX = Math.Sqrt(Math.Max(0, sumX2 / n - r.MeanX * r.MeanX));
            r.MinX = min;
            r.MaxX = max;
            if (expand && expanded.Count > 0)
            {
                expanded.Sort();
                r.Q1 = Quantile(expanded, 0.25);
                r.Median = Quantile(expanded, 0.5);
                r.Q3 = Quantile(expanded, 0.75);
            }
            else
            {
                r.Q1 = r.Median = r.Q3 = double.NaN;
            }

            if (twoVar)
            {
                r.MeanY = sumY / n;
                double varY = (sumY2 - sumY * sumY / n) / (n - 1);
                r.SampleSdY = n > 1 ? Math.Sqrt(Math.Max(0, varY)) : 0;
                r.PopSdY = Math.Sqrt(Math.Max(0, sumY2 / n - r.MeanY * r.MeanY));
                Fit(pts, useFrequency, regression, r);
            }
            return r;
        }

        private static double Quantile(List<double> sorted, double q)
        {
            if (sorted.Count == 0) return double.NaN;
            double pos = q * (sorted.Count - 1);
            int lo = (int)Math.Floor(pos);
            int hi = (int)Math.Ceiling(pos);
            if (lo == hi) return sorted[lo];
            double t = pos - lo;
            return sorted[lo] * (1 - t) + sorted[hi] * t;
        }

        private static void Fit(List<StatPoint> pts, bool useFrequency, RegressionType type, StatResult r)
        {
            List<double> u = new List<double>();
            List<double> v = new List<double>();
            List<double> w = new List<double>();
            for (int i = 0; i < pts.Count; i++)
            {
                StatPoint p = pts[i];
                double weight = useFrequency ? p.Freq : 1;
                double uu = p.X;
                double vv = p.Y;
                bool ok = true;
                switch (type)
                {
                    case RegressionType.Logarithmic:
                        if (uu <= 0) ok = false; else uu = Math.Log(uu);
                        break;
                    case RegressionType.Exponential:
                        if (vv <= 0) ok = false; else vv = Math.Log(vv);
                        break;
                    case RegressionType.Power:
                        if (uu <= 0 || vv <= 0) ok = false;
                        else { uu = Math.Log(uu); vv = Math.Log(vv); }
                        break;
                    case RegressionType.Inverse:
                        if (uu == 0) ok = false; else uu = 1.0 / uu;
                        break;
                }
                if (!ok) continue;
                u.Add(uu);
                v.Add(vv);
                w.Add(weight);
            }

            if (type == RegressionType.Quadratic)
            {
                FitQuadratic(pts, useFrequency, r);
                return;
            }

            if (u.Count < 2) throw new CalcError("Недостаточно данных для регрессии");
            double sw = 0, sx = 0, sy = 0, sxx = 0, syy = 0, sxy = 0;
            for (int i = 0; i < u.Count; i++)
            {
                sw += w[i];
                sx += u[i] * w[i];
                sy += v[i] * w[i];
                sxx += u[i] * u[i] * w[i];
                syy += v[i] * v[i] * w[i];
                sxy += u[i] * v[i] * w[i];
            }
            double denom = sw * sxx - sx * sx;
            if (Math.Abs(denom) < 1e-300) throw new CalcError("Регрессия не определена");
            double b = (sw * sxy - sx * sy) / denom;
            double a = (sy - b * sx) / sw;
            double cov = sw * sxy - sx * sy;
            double dx = sw * sxx - sx * sx;
            double dy = sw * syy - sy * sy;
            double rr = (dx > 0 && dy > 0) ? cov / Math.Sqrt(dx * dy) : 0;

            r.A = a;
            r.B = b;
            r.R = rr;
            string na = Num(a), nb = Num(b);
            switch (type)
            {
                case RegressionType.Linear:
                    r.Equation = "y=" + na + "+" + nb + "x";
                    break;
                case RegressionType.Logarithmic:
                    r.Equation = "y=" + na + "+" + nb + "·ln x";
                    break;
                case RegressionType.Exponential:
                    r.A = Math.Exp(a);
                    r.B = b;
                    r.Equation = "y=" + Num(r.A) + "·e^(" + nb + "x)";
                    break;
                case RegressionType.Power:
                    r.A = Math.Exp(a);
                    r.B = b;
                    r.Equation = "y=" + Num(r.A) + "·x^" + nb;
                    break;
                case RegressionType.Inverse:
                    r.Equation = "y=" + na + "+" + nb + "/x";
                    break;
            }
        }

        private static void FitQuadratic(List<StatPoint> pts, bool useFrequency, StatResult r)
        {
            double s0 = 0, s1 = 0, s2 = 0, s3 = 0, s4 = 0;
            double t0 = 0, t1 = 0, t2 = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                double w = useFrequency ? pts[i].Freq : 1;
                double x = pts[i].X;
                double y = pts[i].Y;
                s0 += w;
                s1 += x * w;
                s2 += x * x * w;
                s3 += x * x * x * w;
                s4 += x * x * x * x * w;
                t0 += y * w;
                t1 += x * y * w;
                t2 += x * x * y * w;
            }
            double[,] m = new double[3, 3];
            double[] b = new double[3];
            m[0, 0] = s0; m[0, 1] = s1; m[0, 2] = s2;
            m[1, 0] = s1; m[1, 1] = s2; m[1, 2] = s3;
            m[2, 0] = s2; m[2, 1] = s3; m[2, 2] = s4;
            b[0] = t0; b[1] = t1; b[2] = t2;
            double[] sol = LinearSolve(m, b);
            r.A = sol[0];
            r.B = sol[1];
            r.C = sol[2];
            r.Equation = "y=" + Num(r.A) + "+" + Num(r.B) + "x+" + Num(r.C) + "x²";
            double mean = t0 / s0;
            double ssTot = 0, ssRes = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                double w = useFrequency ? pts[i].Freq : 1;
                double x = pts[i].X;
                double y = pts[i].Y;
                double fit = r.A + r.B * x + r.C * x * x;
                ssTot += w * (y - mean) * (y - mean);
                ssRes += w * (y - fit) * (y - fit);
            }
            r.R = ssTot > 0 ? Math.Sqrt(Math.Max(0, 1 - ssRes / ssTot)) : 0;
        }

        public static double[] LinearSolve(double[,] m, double[] b)
        {
            int n = b.Length;
            double[,] a = (double[,])m.Clone();
            double[] x = (double[])b.Clone();
            for (int col = 0; col < n; col++)
            {
                int pivot = col;
                for (int row = col + 1; row < n; row++)
                    if (Math.Abs(a[row, col]) > Math.Abs(a[pivot, col])) pivot = row;
                if (Math.Abs(a[pivot, col]) < 1e-14) throw new CalcError("Система не имеет единственного решения");
                if (pivot != col)
                {
                    for (int k = 0; k < n; k++)
                    {
                        double t = a[col, k]; a[col, k] = a[pivot, k]; a[pivot, k] = t;
                    }
                    double tb = x[col]; x[col] = x[pivot]; x[pivot] = tb;
                }
                for (int row = col + 1; row < n; row++)
                {
                    double f = a[row, col] / a[col, col];
                    if (f == 0) continue;
                    for (int k = col; k < n; k++) a[row, k] -= f * a[col, k];
                    x[row] -= f * x[col];
                }
            }
            for (int row = n - 1; row >= 0; row--)
            {
                double sum = x[row];
                for (int k = row + 1; k < n; k++) sum -= a[row, k] * x[k];
                x[row] = sum / a[row, row];
            }
            return x;
        }

        public static string Num(double v)
        {
            if (Math.Abs(v) < 5e-13) v = 0;
            return v.ToString("G10", CultureInfo.InvariantCulture).Replace('-', '−');
        }

        public static string Report(StatResult r, bool twoVar, bool freq)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("n = " + r.Count);
            sb.AppendLine("Σx = " + Num(r.SumX));
            sb.AppendLine("Σx² = " + Num(r.SumX2));
            sb.AppendLine("x̄ = " + Num(r.MeanX));
            sb.AppendLine("σx = " + Num(r.PopSdX));
            sb.AppendLine("sx = " + Num(r.SampleSdX));
            sb.AppendLine("minX = " + Num(r.MinX));
            sb.AppendLine("Q1 = " + Num(r.Q1));
            sb.AppendLine("med = " + Num(r.Median));
            sb.AppendLine("Q3 = " + Num(r.Q3));
            sb.AppendLine("maxX = " + Num(r.MaxX));
            if (twoVar)
            {
                sb.AppendLine();
                sb.AppendLine("Σy = " + Num(r.SumY));
                sb.AppendLine("Σy² = " + Num(r.SumY2));
                sb.AppendLine("Σxy = " + Num(r.SumXY));
                sb.AppendLine("ȳ = " + Num(r.MeanY));
                sb.AppendLine("σy = " + Num(r.PopSdY));
                sb.AppendLine("sy = " + Num(r.SampleSdY));
                sb.AppendLine("a = " + Num(r.A));
                sb.AppendLine("b = " + Num(r.B));
                if (!string.IsNullOrEmpty(r.Equation)) sb.AppendLine(r.Equation);
                sb.AppendLine("r = " + Num(r.R));
            }
            return sb.ToString();
        }
    }
}
