using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SC880
{
    public enum AngleUnit { Degrees, Radians, Gradians }
    public enum CalcMode { Comp, Cmplx, BaseN, Stat, Eqn, Matrix, Vector, Table, Verif }
    public enum NumFormat { Norm1, Norm2, Fix, Sci }
    public enum ComplexView { Rectangular, Polar }

    public sealed class CalcState
    {
        public AngleUnit Angle = AngleUnit.Degrees;
        public CalcMode Mode = CalcMode.Comp;
        public NumFormat Format = NumFormat.Norm2;
        public int Digits = 2;
        public ComplexView ComplexDisplay = ComplexView.Rectangular;
        public bool FractionResult = false;
        public bool StatFrequency = false;
        public bool InsertMode = true;

        public Cplx Ans = Cplx.Zero;
        public Cplx Memory = Cplx.Zero;
        public Dictionary<char, Cplx> Vars = new Dictionary<char, Cplx>();

        public CalcState()
        {
            for (char c = 'A'; c <= 'F'; c++) Vars[c] = Cplx.Zero;
            Vars['X'] = Cplx.Zero;
            Vars['Y'] = Cplx.Zero;
        }

        public double ToRadiansFactor
        {
            get
            {
                if (Angle == AngleUnit.Degrees) return Math.PI / 180.0;
                if (Angle == AngleUnit.Gradians) return Math.PI / 200.0;
                return 1.0;
            }
        }

        public double FromRadiansFactor
        {
            get
            {
                if (Angle == AngleUnit.Degrees) return 180.0 / Math.PI;
                if (Angle == AngleUnit.Gradians) return 200.0 / Math.PI;
                return 1.0;
            }
        }

        public double ToRadians(double value)
        {
            return value * ToRadiansFactor;
        }

        public string AngleName
        {
            get
            {
                if (Angle == AngleUnit.Degrees) return "DEG";
                if (Angle == AngleUnit.Gradians) return "GRAD";
                return "RAD";
            }
        }

        public string ModeName
        {
            get
            {
                switch (Mode)
                {
                    case CalcMode.Cmplx: return "CMPLX";
                    case CalcMode.BaseN: return "BASE";
                    case CalcMode.Stat: return "STAT";
                    case CalcMode.Eqn: return "EQN";
                    case CalcMode.Matrix: return "MATRIX";
                    case CalcMode.Vector: return "VECTOR";
                    case CalcMode.Table: return "TABLE";
                    case CalcMode.Verif: return "VERIF";
                    default: return "COMP";
                }
            }
        }
    }

    public static class NumberText
    {
        private static readonly char[] SupDigits = { '⁰', '¹', '²', '³', '⁴', '⁵', '⁶', '⁷', '⁸', '⁹' };

        public static string Superscript(int value)
        {
            StringBuilder sb = new StringBuilder();
            if (value < 0)
            {
                sb.Append('⁻');
                value = -value;
            }
            string digits = value.ToString(CultureInfo.InvariantCulture);
            for (int i = 0; i < digits.Length; i++) sb.Append(SupDigits[digits[i] - '0']);
            return sb.ToString();
        }

        public static string Format(Cplx value, CalcState state)
        {
            if (!value.IsFinite) throw new CalcError("Результат вне диапазона");
            double scale = Math.Max(1.0, value.Magnitude);
            bool hasImaginary = Math.Abs(value.Im) > 1e-12 * scale;

            if (hasImaginary && state.Mode != CalcMode.Cmplx)
                throw new CalcError("Комплексное число");

            if (!hasImaginary) return FormatReal(value.Re, state);

            if (state.ComplexDisplay == ComplexView.Polar)
            {
                string radius = FormatReal(value.Magnitude, state);
                double angle = value.Arg * state.FromRadiansFactor;
                string unit = state.Angle == AngleUnit.Degrees ? "°" : (state.Angle == AngleUnit.Gradians ? " grad" : " rad");
                return radius + "∠" + FormatReal(angle, state) + unit;
            }

            string re = FormatReal(value.Re, state);
            string im = FormatReal(Math.Abs(value.Im), state);
            if (Math.Abs(value.Re) < 1e-12 * scale)
                return (value.Im < 0 ? "−" : "") + (Math.Abs(value.Im - 1) < 1e-12 ? "" : im) + "i";
            return re + (value.Im < 0 ? "−" : "+") + (Math.Abs(Math.Abs(value.Im) - 1) < 1e-12 ? "" : im) + "i";
        }

        public static string FormatReal(double value, CalcState state)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new CalcError("Результат вне диапазона");
            if (Math.Abs(value) < 5e-13) value = 0;

            string fraction;
            if (state.FractionResult && TryFormatFraction(value, state, out fraction))
                return fraction;

            switch (state.Format)
            {
                case NumFormat.Fix:
                    return CleanNegative(value.ToString("F" + state.Digits, CultureInfo.InvariantCulture));
                case NumFormat.Sci:
                    return Scientific(value, state.Digits);
                case NumFormat.Norm1:
                    if (value != 0 && (Math.Abs(value) < 1e-2 || Math.Abs(value) >= 1e10))
                        return ScientificNormal(value);
                    return Normal(value);
                default:
                    if (value != 0 && (Math.Abs(value) < 1e-9 || Math.Abs(value) >= 1e10))
                        return ScientificNormal(value);
                    return Normal(value);
            }
        }

        private static string CleanNegative(string s)
        {
            return s.Replace('-', '−');
        }

        private static string Normal(double value)
        {
            if (value == 0) return "0";
            string result = value.ToString("G10", CultureInfo.InvariantCulture);
            if (result.IndexOf('E') >= 0) return ScientificNormal(value);
            return CleanNegative(result);
        }

        private static string ScientificNormal(double value)
        {
            if (value == 0) return "0";
            int exponent = (int)Math.Floor(Math.Log10(Math.Abs(value)));
            double mantissa = value / Math.Pow(10, exponent);
            string m = TrimMantissa(mantissa, 9);
            return CleanNegative(m) + "×10" + Superscript(exponent);
        }

        private static string Scientific(double value, int digits)
        {
            if (value == 0)
            {
                if (digits <= 0) return "0";
                return "0." + new string('0', digits) + "×10⁰";
            }
            int exponent = (int)Math.Floor(Math.Log10(Math.Abs(value)));
            double mantissa = value / Math.Pow(10, exponent);
            string m = mantissa.ToString("F" + digits, CultureInfo.InvariantCulture);
            return CleanNegative(m) + "×10" + Superscript(exponent);
        }

        private static string TrimMantissa(double mantissa, int maxDecimals)
        {
            string s = mantissa.ToString("F" + maxDecimals, CultureInfo.InvariantCulture);
            if (s.IndexOf('.') >= 0)
            {
                s = s.TrimEnd('0');
                s = s.TrimEnd('.');
            }
            return s;
        }

        private static bool TryFormatFraction(double value, CalcState state, out string text)
        {
            text = null;
            if (Math.Abs(value) >= 1e12) return false;
            long num, den;
            if (!TryApproximate(value, out num, out den)) return false;
            if (den == 1) return false;

            if (Math.Abs(num) > den)
            {
                long whole = Math.Abs(num) / den;
                long rest = Math.Abs(num) % den;
                string sign = value < 0 ? "−" : "";
                if (rest == 0)
                {
                    text = sign + whole.ToString(CultureInfo.InvariantCulture);
                    return false;
                }
                text = sign + whole.ToString(CultureInfo.InvariantCulture) + " " + rest + "/" + den;
                return true;
            }

            text = (value < 0 ? "−" : "") + Math.Abs(num) + "/" + den;
            return true;
        }

        public static bool TryApproximate(double value, out long numerator, out long denominator)
        {
            numerator = 0;
            denominator = 1;
            if (double.IsNaN(value) || double.IsInfinity(value)) return false;
            bool negative = value < 0;
            double x = Math.Abs(value);
            long maxDen = 1000000;
            long h1 = 1, h2 = 0, k1 = 0, k2 = 1;
            double b = x;
            for (int i = 0; i < 40; i++)
            {
                long a = (long)Math.Floor(b);
                long h = a * h1 + h2;
                long k = a * k1 + k2;
                if (k > maxDen) break;
                h2 = h1; h1 = h;
                k2 = k1; k1 = k;
                double approx = (double)h1 / (double)k1;
                if (Math.Abs(approx - x) <= 1e-10 * Math.Max(1.0, x))
                {
                    numerator = negative ? -h1 : h1;
                    denominator = k1;
                    return true;
                }
                double frac = b - a;
                if (frac < 1e-15) break;
                b = 1.0 / frac;
            }
            double best = (double)h1 / (double)k1;
            if (Math.Abs(best - x) <= 1e-10 * Math.Max(1.0, x) && k1 > 0)
            {
                numerator = negative ? -h1 : h1;
                denominator = k1;
                return true;
            }
            return false;
        }
    }
}
