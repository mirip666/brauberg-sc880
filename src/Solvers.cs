using System;
using System.Collections.Generic;

namespace SC880
{
    public sealed class SolveResult
    {
        public double Root;
        public double Residual;
        public int Iterations;
        public string Method;
    }

    public sealed class VerifyResult
    {
        public bool IsTrue;
        public double Left;
        public double Right;
        public double Difference;
        public string Operator;
    }

    public static class Numerics
    {
        public static double EvalReal(string expression, CalcState state, char variable, double x)
        {
            Cplx old;
            bool had = state.Vars.TryGetValue(variable, out old);
            state.Vars[variable] = Cplx.Real(x);
            try
            {
                EvalResult r = Engine.Evaluate(expression, state);
                if (Math.Abs(r.Value.Im) > 1e-9 * Math.Max(1, r.Value.Magnitude))
                    throw new CalcError("Получено комплексное значение");
                return r.Value.Re;
            }
            finally
            {
                if (had) state.Vars[variable] = old;
                else state.Vars[variable] = Cplx.Zero;
            }
        }

        private static string PrepareFunction(string expression)
        {
            int eq = TopLevelEquals(expression);
            if (eq >= 0)
            {
                string left = expression.Substring(0, eq);
                string right = expression.Substring(eq + 1);
                return "(" + left + ")-(" + right + ")";
            }
            return expression;
        }

        private static int TopLevelEquals(string text)
        {
            int depth = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (c == '=' && depth == 0) return i;
            }
            return -1;
        }

        public static SolveResult Solve(string expression, CalcState state, char variable, double guess)
        {
            string f = PrepareFunction(expression);
            SolveResult result = new SolveResult();
            double x = guess;
            int iter = 0;
            double fx = SafeEval(f, state, variable, x);
            for (int i = 0; i < 60; i++)
            {
                iter++;
                if (fx == 0) break;
                double h = 1e-6 * Math.Max(1.0, Math.Abs(x));
                double d1 = SafeEval(f, state, variable, x + h);
                double d2 = SafeEval(f, state, variable, x - h);
                double derivative = (d1 - d2) / (2 * h);
                if (double.IsNaN(derivative) || Math.Abs(derivative) < 1e-14)
                {
                    derivative = (d1 - fx) / h;
                }
                if (double.IsNaN(derivative) || Math.Abs(derivative) < 1e-14) break;
                double next = x - fx / derivative;
                if (double.IsNaN(next) || double.IsInfinity(next) || Math.Abs(next - x) > 1e12) break;
                if (Math.Abs(next - x) < 1e-12 * Math.Max(1.0, Math.Abs(next)))
                {
                    x = next;
                    fx = SafeEval(f, state, variable, x);
                    if (Math.Abs(fx) < 1e-7 * Math.Max(1.0, Math.Abs(x))) break;
                }
                x = next;
                fx = SafeEval(f, state, variable, x);
                if (Math.Abs(fx) < 1e-12 * Math.Max(1.0, Math.Abs(x)))
                {
                    result.Root = x;
                    result.Residual = fx;
                    result.Iterations = iter;
                    result.Method = "Newton";
                    return result;
                }
            }
            if (Math.Abs(fx) < 1e-8 * Math.Max(1.0, Math.Abs(x)))
            {
                result.Root = x;
                result.Residual = fx;
                result.Iterations = iter;
                result.Method = "Newton";
                return result;
            }

            double lo = guess - 1000.0;
            double hi = guess + 1000.0;
            int steps = 4000;
            double prevX = lo;
            double prevF = SafeEval(f, state, variable, prevX);
            for (int i = 1; i <= steps; i++)
            {
                double nx = lo + (hi - lo) * i / steps;
                double nf = SafeEval(f, state, variable, nx);
                if (double.IsNaN(nf)) { prevX = nx; prevF = nf; continue; }
                if (!double.IsNaN(prevF) && prevF * nf <= 0 && (prevF != 0 || nf != 0))
                {
                    double a = prevX, b = nx;
                    double fa = prevF;
                    for (int k = 0; k < 200; k++)
                    {
                        double mid = (a + b) / 2.0;
                        double fm = SafeEval(f, state, variable, mid);
                        if (Math.Abs(fm) < 1e-12 * Math.Max(1.0, Math.Abs(mid)) || Math.Abs(b - a) < 1e-12 * Math.Max(1.0, Math.Abs(mid)))
                        {
                            result.Root = mid;
                            result.Residual = fm;
                            result.Iterations = iter + k;
                            result.Method = "Бисекция";
                            return result;
                        }
                        if (fa * fm <= 0) b = mid;
                        else { a = mid; fa = fm; }
                    }
                    result.Root = (a + b) / 2.0;
                    result.Residual = SafeEval(f, state, variable, result.Root);
                    result.Iterations = iter + 200;
                    result.Method = "Бисекция";
                    return result;
                }
                prevX = nx;
                prevF = nf;
            }
            throw new CalcError("Корень не найден");
        }

        private static double SafeEval(string expression, CalcState state, char variable, double x)
        {
            try
            {
                return EvalReal(expression, state, variable, x);
            }
            catch
            {
                return double.NaN;
            }
        }

        public static double Integral(string expression, CalcState state, char variable, double a, double b, int n)
        {
            if (n < 2) n = 2;
            if (n % 2 != 0) n++;
            if (n > 200000) n = 200000;
            double h = (b - a) / n;
            double sum = 0;
            for (int i = 0; i <= n; i++)
            {
                double x = a + i * h;
                double y = EvalReal(expression, state, variable, x);
                if (double.IsNaN(y) || double.IsInfinity(y)) throw new CalcError("Функция не определена в точке " + x.ToString("G6"));
                if (i == 0 || i == n) sum += y;
                else sum += (i % 2 == 0 ? 2 : 4) * y;
            }
            return sum * h / 3.0;
        }

        public static double Derivative(string expression, CalcState state, char variable, double x)
        {
            double h = 1e-5 * Math.Max(1.0, Math.Abs(x));
            double d1 = (EvalReal(expression, state, variable, x + h) - EvalReal(expression, state, variable, x - h)) / (2 * h);
            double h2 = h / 2;
            double d2 = (EvalReal(expression, state, variable, x + h2) - EvalReal(expression, state, variable, x - h2)) / (2 * h2);
            return (4 * d2 - d1) / 3.0;
        }

        public static double Sum(string expression, CalcState state, char variable, double from, double to)
        {
            if (Math.Abs(from - Math.Round(from)) > 1e-9 || Math.Abs(to - Math.Round(to)) > 1e-9)
                throw new CalcError("Σ: границы должны быть целыми");
            long a = (long)Math.Round(from);
            long b = (long)Math.Round(to);
            if (b < a) throw new CalcError("Σ: верхняя граница меньше нижней");
            if (b - a > 200000) throw new CalcError("Σ: слишком много слагаемых");
            double sum = 0;
            for (long i = a; i <= b; i++) sum += EvalReal(expression, state, variable, i);
            return sum;
        }

        public static double[] SolveLinear(double[,] a, double[] b)
        {
            int n = b.Length;
            if (a.GetLength(0) != n || a.GetLength(1) != n) throw new CalcError("Неверный размер системы");
            double[,] d = (double[,])a.Clone();
            double[] x = (double[])b.Clone();
            for (int col = 0; col < n; col++)
            {
                int piv = col;
                for (int row = col + 1; row < n; row++)
                    if (Math.Abs(d[row, col]) > Math.Abs(d[piv, col])) piv = row;
                if (Math.Abs(d[piv, col]) < 1e-12) throw new CalcError("Система не имеет единственного решения");
                if (piv != col)
                {
                    for (int k = 0; k < n; k++)
                    {
                        double t = d[col, k]; d[col, k] = d[piv, k]; d[piv, k] = t;
                    }
                    double u = x[col]; x[col] = x[piv]; x[piv] = u;
                }
                for (int row = col + 1; row < n; row++)
                {
                    double f = d[row, col] / d[col, col];
                    if (Math.Abs(f) < 1e-300) continue;
                    for (int k = col; k < n; k++) d[row, k] -= f * d[col, k];
                    x[row] -= f * x[col];
                }
            }
            for (int row = n - 1; row >= 0; row--)
            {
                double s = x[row];
                for (int k = row + 1; k < n; k++) s -= d[row, k] * x[k];
                x[row] = s / d[row, row];
            }
            return x;
        }

        public static Cplx[] PolynomialRoots(double[] coeff)
        {
            int n = coeff.Length - 1;
            if (n < 1) throw new CalcError("Многочлен нулевой степени");
            if (Math.Abs(coeff[0]) < 1e-14) throw new CalcError("Старший коэффициент равен нулю");
            if (n == 1)
            {
                return new Cplx[] { Cplx.Real(-coeff[1] / coeff[0]) };
            }
            if (n == 2)
            {
                double a = coeff[0], b = coeff[1], c = coeff[2];
                double disc = b * b - 4 * a * c;
                if (disc >= 0)
                {
                    double s = Math.Sqrt(disc);
                    double q = -0.5 * (b + Math.Sign(b) * s);
                    double r1 = q / a;
                    double r2 = Math.Abs(q) < 1e-14 ? (-b + s) / (2 * a) : c / q;
                    if (r1 > r2) { double t = r1; r1 = r2; r2 = t; }
                    return new Cplx[] { Cplx.Real(r1), Cplx.Real(r2) };
                }
                else
                {
                    double re = -b / (2 * a);
                    double im = Math.Sqrt(-disc) / (2 * a);
                    return new Cplx[] { new Cplx(re, im), new Cplx(re, -im) };
                }
            }

            double[] a2 = new double[n + 1];
            for (int i = 0; i <= n; i++) a2[i] = coeff[i] / coeff[0];
            double radius = 1 + MaxAbs(a2);
            Cplx[] roots = new Cplx[n];
            for (int i = 0; i < n; i++)
            {
                double angle = 2 * Math.PI * i / n + 0.4;
                roots[i] = Cplx.FromPolar(radius, angle);
            }
            for (int iter = 0; iter < 500; iter++)
            {
                double maxDelta = 0;
                for (int i = 0; i < n; i++)
                {
                    Cplx denom = Cplx.One;
                    for (int j = 0; j < n; j++)
                    {
                        if (i == j) continue;
                        denom = denom * (roots[i] - roots[j]);
                    }
                    if (Math.Abs(denom.Re) + Math.Abs(denom.Im) < 1e-30)
                        denom = new Cplx(1e-15, 1e-15);
                    Cplx p = PolyEval(a2, roots[i]);
                    Cplx delta = p / denom;
                    roots[i] = roots[i] - delta;
                    double d = Math.Abs(delta.Re) + Math.Abs(delta.Im);
                    if (d > maxDelta) maxDelta = d;
                }
                if (maxDelta < 1e-14) break;
            }
            for (int i = 0; i < n; i++)
            {
                if (Math.Abs(roots[i].Im) < 1e-8) roots[i] = Cplx.Real(roots[i].Re);
                if (Math.Abs(roots[i].Re) < 1e-12) roots[i] = new Cplx(0, roots[i].Im);
            }
            Array.Sort(roots, delegate(Cplx x, Cplx y)
            {
                int c = x.Re.CompareTo(y.Re);
                if (c != 0) return c;
                return x.Im.CompareTo(y.Im);
            });
            return roots;
        }

        private static double MaxAbs(double[] a)
        {
            double m = 0;
            for (int i = 0; i < a.Length; i++) m = Math.Max(m, Math.Abs(a[i]));
            return m;
        }

        private static Cplx PolyEval(double[] a, Cplx x)
        {
            Cplx v = Cplx.Zero;
            for (int i = 0; i < a.Length; i++) v = v * x + Cplx.Real(a[i]);
            return v;
        }

        public static VerifyResult Verify(string text, CalcState state)
        {
            string[] ops = { "==", "≠", "≤", "≥", "=", "<", ">" };
            int depth = 0;
            int opPos = -1;
            string op = null;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (depth == 0)
                {
                    for (int k = 0; k < ops.Length; k++)
                    {
                        string o = ops[k];
                        if (string.CompareOrdinal(text, i, o, 0, o.Length) == 0)
                        {
                            opPos = i;
                            op = o;
                            break;
                        }
                    }
                    if (opPos >= 0) break;
                }
            }
            if (opPos < 0) throw new CalcError("Нужен оператор сравнения");
            string left = text.Substring(0, opPos);
            string right = text.Substring(opPos + op.Length);
            EvalResult l = Engine.Evaluate(left, state);
            EvalResult r = Engine.Evaluate(right, state);
            if (Math.Abs(l.Value.Im) > 1e-12 || Math.Abs(r.Value.Im) > 1e-12)
                throw new CalcError("Сравнение только для действительных");
            VerifyResult v = new VerifyResult();
            v.Left = l.Value.Re;
            v.Right = r.Value.Re;
            v.Difference = v.Left - v.Right;
            v.Operator = op;
            double tol = 1e-9 * Math.Max(1.0, Math.Max(Math.Abs(v.Left), Math.Abs(v.Right)));
            switch (op)
            {
                case "=":
                case "==":
                    v.IsTrue = Math.Abs(v.Difference) <= tol;
                    break;
                case "≠":
                    v.IsTrue = Math.Abs(v.Difference) > tol;
                    break;
                case "<":
                    v.IsTrue = v.Difference < -tol;
                    break;
                case ">":
                    v.IsTrue = v.Difference > tol;
                    break;
                case "≤":
                    v.IsTrue = v.Difference <= tol;
                    break;
                case "≥":
                    v.IsTrue = v.Difference >= -tol;
                    break;
            }
            return v;
        }

        public static double ConvertAngle(double value, AngleUnit from, AngleUnit to)
        {
            double deg;
            if (from == AngleUnit.Degrees) deg = value;
            else if (from == AngleUnit.Gradians) deg = value * 0.9;
            else deg = value * 180.0 / Math.PI;
            if (to == AngleUnit.Degrees) return deg;
            if (to == AngleUnit.Gradians) return deg / 0.9;
            return deg * Math.PI / 180.0;
        }
    }
}
