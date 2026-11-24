using System;

namespace SC880
{
    public struct Cplx
    {
        public double Re;
        public double Im;

        public Cplx(double re, double im)
        {
            Re = re;
            Im = im;
        }

        public static Cplx Real(double value)
        {
            return new Cplx(value, 0);
        }

        public static readonly Cplx Zero = new Cplx(0, 0);
        public static readonly Cplx One = new Cplx(1, 0);
        public static readonly Cplx I = new Cplx(0, 1);

        public bool IsReal
        {
            get { return Im == 0; }
        }

        public bool IsFinite
        {
            get { return !double.IsNaN(Re) && !double.IsInfinity(Re) && !double.IsNaN(Im) && !double.IsInfinity(Im); }
        }

        public double Magnitude
        {
            get { return Math.Sqrt(Re * Re + Im * Im); }
        }

        public double Arg
        {
            get { return Math.Atan2(Im, Re); }
        }

        public Cplx Conjugate
        {
            get { return new Cplx(Re, -Im); }
        }

        public static Cplx operator +(Cplx a, Cplx b)
        {
            return new Cplx(a.Re + b.Re, a.Im + b.Im);
        }

        public static Cplx operator -(Cplx a, Cplx b)
        {
            return new Cplx(a.Re - b.Re, a.Im - b.Im);
        }

        public static Cplx operator -(Cplx a)
        {
            return new Cplx(-a.Re, -a.Im);
        }

        public static Cplx operator *(Cplx a, Cplx b)
        {
            return new Cplx(a.Re * b.Re - a.Im * b.Im, a.Re * b.Im + a.Im * b.Re);
        }

        public static Cplx operator /(Cplx a, Cplx b)
        {
            double d = b.Re * b.Re + b.Im * b.Im;
            if (d == 0) throw new CalcError("Деление на ноль");
            return new Cplx((a.Re * b.Re + a.Im * b.Im) / d, (a.Im * b.Re - a.Re * b.Im) / d);
        }

        public static bool EqualsValue(Cplx a, Cplx b, double eps)
        {
            return Math.Abs(a.Re - b.Re) <= eps && Math.Abs(a.Im - b.Im) <= eps;
        }

        public static Cplx Exp(Cplx z)
        {
            double e = Math.Exp(z.Re);
            return new Cplx(e * Math.Cos(z.Im), e * Math.Sin(z.Im));
        }

        public static Cplx Log(Cplx z)
        {
            if (z.Re == 0 && z.Im == 0) throw new CalcError("ln(0) не определен");
            return new Cplx(Math.Log(z.Magnitude), z.Arg);
        }

        public static Cplx Pow(Cplx a, Cplx b)
        {
            if (a.Re == 0 && a.Im == 0)
            {
                if (b.Im != 0 || b.Re <= 0) throw new CalcError("Недопустимая степень");
                if (b.Re == 0) throw new CalcError("Недопустимая степень");
                return Zero;
            }
            if (b.Im == 0 && a.Im == 0 && a.Re > 0)
            {
                return Real(Math.Pow(a.Re, b.Re));
            }
            return Exp(b * Log(a));
        }

        public static Cplx Sqrt(Cplx z)
        {
            if (z.Re == 0 && z.Im == 0) return Zero;
            double r = z.Magnitude;
            double a = Math.Sqrt((r + z.Re) / 2.0);
            double b = Math.Sqrt((r - z.Re) / 2.0);
            if (z.Im < 0) b = -b;
            return new Cplx(a, b);
        }

        public static Cplx Sin(Cplx z)
        {
            return new Cplx(Math.Sin(z.Re) * Math.Cosh(z.Im), Math.Cos(z.Re) * Math.Sinh(z.Im));
        }

        public static Cplx Cos(Cplx z)
        {
            return new Cplx(Math.Cos(z.Re) * Math.Cosh(z.Im), -Math.Sin(z.Re) * Math.Sinh(z.Im));
        }

        public static Cplx Tan(Cplx z)
        {
            Cplx c = Cos(z);
            if (c.Re * c.Re + c.Im * c.Im < 1e-30) throw new CalcError("Не определено");
            return Sin(z) / c;
        }

        public static Cplx Sinh(Cplx z)
        {
            return new Cplx(Math.Sinh(z.Re) * Math.Cos(z.Im), Math.Cosh(z.Re) * Math.Sin(z.Im));
        }

        public static Cplx Cosh(Cplx z)
        {
            return new Cplx(Math.Cosh(z.Re) * Math.Cos(z.Im), Math.Sinh(z.Re) * Math.Sin(z.Im));
        }

        public static Cplx Tanh(Cplx z)
        {
            Cplx c = Cosh(z);
            if (c.Re * c.Re + c.Im * c.Im < 1e-30) throw new CalcError("Не определено");
            return Sinh(z) / c;
        }

        public static Cplx Asin(Cplx z)
        {
            Cplx inside = One - z * z;
            Cplx root = Sqrt(inside);
            return -I * Log(I * z + root);
        }

        public static Cplx Acos(Cplx z)
        {
            return new Cplx(Math.PI / 2, 0) - Asin(z);
        }

        public static Cplx Atan(Cplx z)
        {
            Cplx one = One;
            return new Cplx(0, 0.5) * (Log(one - I * z) - Log(one + I * z));
        }

        public static Cplx FromPolar(double radius, double angleRadians)
        {
            return new Cplx(radius * Math.Cos(angleRadians), radius * Math.Sin(angleRadians));
        }

        public override string ToString()
        {
            if (Im == 0) return Re.ToString();
            return Re.ToString() + (Im >= 0 ? "+" : "") + Im.ToString() + "i";
        }
    }

    public class CalcError : Exception
    {
        public CalcError(string message) : base(message)
        {
        }
    }
}
