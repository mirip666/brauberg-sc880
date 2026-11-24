using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SC880
{
    public sealed class EvalResult
    {
        public Cplx Value;
        public string Expression;
        public string Display;
    }

    public static class Engine
    {
        private static readonly Regex DmsPattern = new Regex(
            @"(\d+(?:\.\d+)?)°(?:(\d+(?:\.\d+)?)′)?(?:(\d+(?:\.\d+)?)″)?");

        private static readonly Random Rng = new Random();

        public static EvalResult Evaluate(string expression, CalcState state)
        {
            if (expression == null || expression.Trim().Length == 0)
                throw new CalcError("Пустое выражение");

            string prepared = DmsPattern.Replace(expression, delegate(Match m)
            {
                string deg = m.Groups[1].Value;
                string min = m.Groups[2].Success ? m.Groups[2].Value : "0";
                string sec = m.Groups[3].Success ? m.Groups[3].Value : "0";
                return "(" + deg + "+" + min + "/60+" + sec + "/3600)°";
            });

            prepared = AutoClose(prepared);

            Parser parser = new Parser(prepared, state);
            Cplx value = parser.ParseAll();
            if (!value.IsFinite) throw new CalcError("Результат вне диапазона");

            if (state.Mode != CalcMode.Cmplx)
            {
                double tolerance = 1e-12 * Math.Max(1.0, value.Magnitude);
                if (Math.Abs(value.Im) > tolerance) throw new CalcError("Комплексное число");
                value = Cplx.Real(value.Re);
            }

            EvalResult result = new EvalResult();
            result.Value = value;
            result.Expression = expression;
            result.Display = NumberText.Format(value, state);
            return result;
        }

        private static string AutoClose(string text)
        {
            int open = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '(') open++;
                else if (text[i] == ')')
                {
                    open--;
                    if (open < 0) throw new CalcError("Лишняя скобка");
                }
            }
            if (open > 0) text += new string(')', open);
            return text;
        }

        private static double Round0(double v)
        {
            if (Math.Abs(v) < 5e-13) return 0;
            return Math.Round(v, 12);
        }

        private static Cplx Clean(Cplx v)
        {
            return new Cplx(Round0(v.Re), Round0(v.Im));
        }

        private static bool IsInteger(double v)
        {
            return Math.Abs(v - Math.Round(v)) < 1e-9;
        }

        private enum T
        {
            Num, Ident, Plus, Minus, Mul, Div, Pow,
            LP, RP, Comma, Fact, Percent, Square, Cube, Inv, Angle,
            Deg, Min, Sec, End
        }

        private struct Token
        {
            public T Type;
            public double Num;
            public string Id;
        }

        private sealed class Parser
        {
            private readonly string text;
            private readonly CalcState state;
            private int pos;
            private Token current;

            public Parser(string source, CalcState calcState)
            {
                text = Normalize(source);
                state = calcState;
                pos = 0;
                current = ReadToken();
            }

            private static string Normalize(string s)
            {
                StringBuilder sb = new StringBuilder(s.Length);
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    switch (c)
                    {
                        case '×': sb.Append('*'); break;
                        case '÷': sb.Append('/'); break;
                        case '−': sb.Append('-'); break;
                        case '–': sb.Append('-'); break;
                        case '—': sb.Append('-'); break;
                        case '∙': sb.Append('*'); break;
                        default: sb.Append(c); break;
                    }
                }
                return sb.ToString();
            }

            public Cplx ParseAll()
            {
                Cplx value = ParseExpression();
                if (current.Type != T.End) throw new CalcError("Ошибка в выражении");
                return value;
            }

            private void Advance()
            {
                current = ReadToken();
            }

            private Token ReadToken()
            {
                while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
                if (pos >= text.Length) return new Token { Type = T.End };

                char c = text[pos];

                if (c == 'π') { pos++; return new Token { Type = T.Ident, Id = "pi" }; }
                if (c == '√') { pos++; return new Token { Type = T.Ident, Id = "sqrt" }; }
                if (c == '∛') { pos++; return new Token { Type = T.Ident, Id = "cbrt" }; }
                if (c == '²') { pos++; return new Token { Type = T.Square }; }
                if (c == '³') { pos++; return new Token { Type = T.Cube }; }
                if (c == '⁻' && pos + 1 < text.Length && text[pos + 1] == '¹')
                {
                    pos += 2;
                    return new Token { Type = T.Inv };
                }
                if (c == '∠') { pos++; return new Token { Type = T.Angle }; }
                if (c == '°') { pos++; return new Token { Type = T.Deg }; }
                if (c == '′') { pos++; return new Token { Type = T.Min }; }
                if (c == '″') { pos++; return new Token { Type = T.Sec }; }

                if (char.IsDigit(c) || c == '.')
                {
                    int start = pos;
                    while (pos < text.Length && (char.IsDigit(text[pos]) || text[pos] == '.')) pos++;
                    if (pos < text.Length && text[pos] == 'E')
                    {
                        int save = pos;
                        int p = pos + 1;
                        if (p < text.Length && (text[p] == '+' || text[p] == '-')) p++;
                        if (p < text.Length && char.IsDigit(text[p]))
                        {
                            pos = p;
                            while (pos < text.Length && char.IsDigit(text[pos])) pos++;
                        }
                        else pos = save;
                    }
                    string num = text.Substring(start, pos - start);
                    double v;
                    if (!double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                        throw new CalcError("Ошибка в числе");
                    return new Token { Type = T.Num, Num = v };
                }

                if (char.IsLetter(c))
                {
                    int start = pos;
                    while (pos < text.Length && char.IsLetterOrDigit(text[pos])) pos++;
                    if (pos < text.Length && text[pos] == 'i' && text.Substring(start, pos - start).ToLowerInvariant() == "ran")
                    {
                    }
                    string id = text.Substring(start, pos - start).ToLowerInvariant();
                    if (pos < text.Length && text[pos] == '#') pos++;
                    if (text.Substring(start, pos - start).ToLowerInvariant().EndsWith("#")) id += "#";
                    return new Token { Type = T.Ident, Id = id };
                }

                switch (c)
                {
                    case '+': pos++; return new Token { Type = T.Plus };
                    case '-': pos++; return new Token { Type = T.Minus };
                    case '*': pos++; return new Token { Type = T.Mul };
                    case '/': pos++; return new Token { Type = T.Div };
                    case '^': pos++; return new Token { Type = T.Pow };
                    case '(': pos++; return new Token { Type = T.LP };
                    case ')': pos++; return new Token { Type = T.RP };
                    case ',': pos++; return new Token { Type = T.Comma };
                    case '!': pos++; return new Token { Type = T.Fact };
                    case '%': pos++; return new Token { Type = T.Percent };
                }

                throw new CalcError("Символ не поддерживается");
            }

            private Cplx ParseExpression()
            {
                Cplx value = ParseTerm();
                while (true)
                {
                    if (current.Type == T.Plus) { Advance(); value = value + ParseTerm(); }
                    else if (current.Type == T.Minus) { Advance(); value = value - ParseTerm(); }
                    else if (current.Type == T.Angle)
                    {
                        Advance();
                        Cplx angle = ParseTerm();
                        if (Math.Abs(angle.Im) > 1e-14) throw new CalcError("Угол должен быть действительным");
                        value = Cplx.FromPolar(value.Magnitude, angle.Re * state.ToRadiansFactor);
                    }
                    else break;
                }
                return value;
            }

            private Cplx ParseTerm()
            {
                Cplx value = ParseUnary();
                while (true)
                {
                    if (current.Type == T.Mul)
                    {
                        Advance();
                        value = value * ParseUnary();
                    }
                    else if (current.Type == T.Div)
                    {
                        Advance();
                        Cplx d = ParseUnary();
                        if (d.Re == 0 && d.Im == 0) throw new CalcError("Деление на ноль");
                        value = value / d;
                    }
                    else if (StartsPrimary(current.Type))
                    {
                        value = value * ParseUnary();
                    }
                    else break;
                }
                return value;
            }

            private static bool StartsPrimary(T type)
            {
                return type == T.Num || type == T.Ident || type == T.LP;
            }

            private Cplx ParseUnary()
            {
                if (current.Type == T.Minus)
                {
                    Advance();
                    return -ParseUnary();
                }
                if (current.Type == T.Plus)
                {
                    Advance();
                    return ParseUnary();
                }
                return ParsePower();
            }

            private Cplx ParsePower()
            {
                Cplx value = ParsePostfix();
                if (current.Type == T.Pow)
                {
                    Advance();
                    Cplx exponent = ParseUnary();
                    return Cplx.Pow(value, exponent);
                }
                return value;
            }

            private Cplx ParsePostfix()
            {
                Cplx value = ParsePrimary();
                while (true)
                {
                    if (current.Type == T.Fact)
                    {
                        Advance();
                        value = Cplx.Real(Factorial(value));
                    }
                    else if (current.Type == T.Percent)
                    {
                        Advance();
                        value = value / Cplx.Real(100);
                    }
                    else if (current.Type == T.Inv)
                    {
                        Advance();
                        if (value.Re == 0 && value.Im == 0) throw new CalcError("Деление на ноль");
                        value = Cplx.One / value;
                    }
                    else if (current.Type == T.Square)
                    {
                        Advance();
                        value = value * value;
                    }
                    else if (current.Type == T.Cube)
                    {
                        Advance();
                        value = value * value * value;
                    }
                    else if (current.Type == T.Deg || current.Type == T.Min || current.Type == T.Sec)
                    {
                        double factor = 1;
                        if (state.Angle == AngleUnit.Radians) factor = Math.PI / 180.0;
                        else if (state.Angle == AngleUnit.Gradians) factor = 10.0 / 9.0;
                        if (current.Type == T.Min) factor /= 60.0;
                        if (current.Type == T.Sec) factor /= 3600.0;
                        Advance();
                        if (Math.Abs(value.Im) > 1e-14) throw new CalcError("Ошибка DMS");
                        value = Cplx.Real(value.Re * factor);
                    }
                    else break;
                }
                return value;
            }

            private Cplx ParsePrimary()
            {
                if (current.Type == T.Num)
                {
                    double v = current.Num;
                    Advance();
                    return Cplx.Real(v);
                }

                if (current.Type == T.LP)
                {
                    Advance();
                    Cplx v = ParseExpression();
                    if (current.Type != T.RP) throw new CalcError("Не закрыта скобка");
                    Advance();
                    return v;
                }

                if (current.Type == T.Ident)
                {
                    string id = current.Id;
                    Advance();

                    if (id == "pi") return Cplx.Real(Math.PI);
                    if (id == "e") return Cplx.Real(Math.E);
                    if (id == "i") return Cplx.I;
                    if (id == "ans") return state.Ans;

                    if (id.Length == 1 && id[0] >= 'a' && id[0] <= 'f')
                        return GetVariable(id.ToUpperInvariant()[0]);
                    if (id == "x") return GetVariable('X');
                    if (id == "y") return GetVariable('Y');
                    if (id == "m") return state.Memory;

                    if (id == "ran#") return Cplx.Real(Rng.NextDouble());
                    if (id == "rand") return Cplx.Real(Rng.NextDouble());

                    if (current.Type != T.LP) throw new CalcError("Нужна скобка");
                    Advance();

                    List<Cplx> args = new List<Cplx>();
                    if (current.Type != T.RP)
                    {
                        args.Add(ParseExpression());
                        while (current.Type == T.Comma)
                        {
                            Advance();
                            args.Add(ParseExpression());
                        }
                    }
                    if (current.Type != T.RP) throw new CalcError("Не закрыта скобка");
                    Advance();

                    return Apply(id, args);
                }

                throw new CalcError("Ошибка в выражении");
            }

            private Cplx GetVariable(char name)
            {
                Cplx value;
                if (state.Vars.TryGetValue(name, out value)) return value;
                return Cplx.Zero;
            }

            private Cplx Apply(string name, List<Cplx> a)
            {
                switch (name)
                {
                    case "sin": Require(a, 1, name); return Clean(Cplx.Sin(AngleArg(a[0])));
                    case "cos": Require(a, 1, name); return Clean(Cplx.Cos(AngleArg(a[0])));
                    case "tan":
                        Require(a, 1, name);
                        if (Math.Abs(a[0].Im) < 1e-14 && Math.Abs(Math.Cos(AngleArg(a[0]).Re)) < 1e-12)
                            throw new CalcError("tan: не определено");
                        return Clean(Cplx.Tan(AngleArg(a[0])));
                    case "asin":
                        Require(a, 1, name);
                        CheckRealDomain(a[0], -1, 1, "asin");
                        return Clean(AngleResult(Cplx.Asin(a[0])));
                    case "acos":
                        Require(a, 1, name);
                        CheckRealDomain(a[0], -1, 1, "acos");
                        return Clean(AngleResult(Cplx.Acos(a[0])));
                    case "atan":
                        Require(a, 1, name);
                        if (state.Mode != CalcMode.Cmplx && Math.Abs(a[0].Im) > 1e-14) throw new CalcError("Комплексное число");
                        return Clean(AngleResult(Cplx.Atan(a[0])));
                    case "sinh": Require(a, 1, name); return Clean(Cplx.Sinh(a[0]));
                    case "cosh": Require(a, 1, name); return Clean(Cplx.Cosh(a[0]));
                    case "tanh": Require(a, 1, name); return Clean(Cplx.Tanh(a[0]));
                    case "asinh": Require(a, 1, name); return Clean(Asinh(a[0]));
                    case "acosh":
                        Require(a, 1, name);
                        if (state.Mode != CalcMode.Cmplx && (Math.Abs(a[0].Im) > 1e-14 || a[0].Re < 1)) throw new CalcError("acosh: x ≥ 1");
                        return Clean(Acosh(a[0]));
                    case "atanh":
                        Require(a, 1, name);
                        if (state.Mode != CalcMode.Cmplx && (Math.Abs(a[0].Im) > 1e-14 || Math.Abs(a[0].Re) >= 1)) throw new CalcError("atanh: |x| < 1");
                        return Clean(Atanh(a[0]));
                    case "ln":
                        Require(a, 1, name);
                        CheckLogDomain(a[0], "ln");
                        return Clean(Cplx.Log(a[0]));
                    case "log":
                        Require(a, 1, name);
                        CheckLogDomain(a[0], "log");
                        return Clean(Cplx.Log(a[0]) / Cplx.Real(Math.Log(10)));
                    case "log2":
                        Require(a, 1, name);
                        CheckLogDomain(a[0], "log2");
                        return Clean(Cplx.Log(a[0]) / Cplx.Real(Math.Log(2)));
                    case "logb":
                        Require(a, 2, name);
                        CheckLogDomain(a[0], "logb");
                        CheckLogDomain(a[1], "logb");
                        return Clean(Cplx.Log(a[1]) / Cplx.Log(a[0]));
                    case "exp": Require(a, 1, name); return Clean(Cplx.Exp(a[0]));
                    case "sqrt":
                        Require(a, 1, name);
                        if (state.Mode != CalcMode.Cmplx && (Math.Abs(a[0].Im) > 1e-14 || a[0].Re < 0)) throw new CalcError("Корень из отрицательного");
                        return Clean(Cplx.Sqrt(a[0]));
                    case "cbrt":
                        Require(a, 1, name);
                        if (Math.Abs(a[0].Im) < 1e-14)
                            return Cplx.Real(Math.Sign(a[0].Re) * Math.Pow(Math.Abs(a[0].Re), 1.0 / 3.0));
                        return Clean(Cplx.Pow(a[0], Cplx.Real(1.0 / 3.0)));
                    case "nroot":
                    {
                        Require(a, 2, name);
                        double n = RealPart(a[0], "nroot");
                        if (!IsInteger(n) || n == 0) throw new CalcError("Корень: целое n ≠ 0");
                        if (Math.Abs(a[1].Im) < 1e-14 && Math.Abs(n % 2) == 1)
                            return Cplx.Real(Math.Sign(a[1].Re) * Math.Pow(Math.Abs(a[1].Re), 1.0 / Math.Abs(n)));
                        if (state.Mode != CalcMode.Cmplx && (a[1].Re < 0 || Math.Abs(a[1].Im) > 1e-14))
                            throw new CalcError("Корень из отрицательного");
                        return Clean(Cplx.Pow(a[1], Cplx.Real(1.0 / n)));
                    }
                    case "abs": Require(a, 1, name); return Cplx.Real(a[0].Magnitude);
                    case "arg": Require(a, 1, name); return Cplx.Real(a[0].Arg * state.FromRadiansFactor);
                    case "conj": Require(a, 1, name); return a[0].Conjugate;
                    case "re": Require(a, 1, name); return Cplx.Real(a[0].Re);
                    case "im": Require(a, 1, name); return Cplx.Real(a[0].Im);
                    case "int": Require(a, 1, name); return Cplx.Real(Math.Truncate(RealPart(a[0], "Int")));
                    case "frac":
                    {
                        Require(a, 1, name);
                        double v = RealPart(a[0], "Frac");
                        return Cplx.Real(v - Math.Truncate(v));
                    }
                    case "round": Require(a, 1, name); return Cplx.Real(Math.Round(RealPart(a[0], "Round")));
                    case "rnd": Require(a, 1, name); return RoundToDisplay(a[0]);
                    case "gcd":
                    {
                        Require(a, 2, name);
                        long x = LongValue(a[0], "GCD");
                        long y = LongValue(a[1], "GCD");
                        return Cplx.Real(Gcd(Math.Abs(x), Math.Abs(y)));
                    }
                    case "lcm":
                    {
                        Require(a, 2, name);
                        long x = LongValue(a[0], "LCM");
                        long y = LongValue(a[1], "LCM");
                        if (x == 0 || y == 0) return Cplx.Zero;
                        return Cplx.Real(Math.Abs(x / Gcd(Math.Abs(x), Math.Abs(y)) * y));
                    }
                    case "npr":
                    {
                        Require(a, 2, name);
                        long n = LongValue(a[0], "nPr");
                        long r = LongValue(a[1], "nPr");
                        if (n < 0 || r < 0 || r > n) throw new CalcError("nPr: неверные аргументы");
                        double result = 1;
                        for (long i = n - r + 1; i <= n; i++) result *= i;
                        return Cplx.Real(result);
                    }
                    case "ncr":
                    {
                        Require(a, 2, name);
                        long n = LongValue(a[0], "nCr");
                        long r = LongValue(a[1], "nCr");
                        if (n < 0 || r < 0 || r > n) throw new CalcError("nCr: неверные аргументы");
                        if (r > n - r) r = n - r;
                        double result = 1;
                        for (long i = 1; i <= r; i++) result = result * (n - r + i) / i;
                        return Cplx.Real(Math.Round(result));
                    }
                    case "ranint":
                    {
                        Require(a, 2, name);
                        long lo = LongValue(a[0], "RanInt");
                        long hi = LongValue(a[1], "RanInt");
                        if (lo > hi) { long t = lo; lo = hi; hi = t; }
                        if (hi - lo > int.MaxValue) throw new CalcError("Диапазон слишком велик");
                        return Cplx.Real(Rng.Next((int)lo, (int)hi + 1));
                    }
                    case "min":
                        Require(a, 2, name);
                        return a[0].Re <= a[1].Re ? a[0] : a[1];
                    case "max":
                        Require(a, 2, name);
                        return a[0].Re >= a[1].Re ? a[0] : a[1];
                    case "pol":
                    {
                        Require(a, 2, name);
                        double r = Math.Sqrt(a[0].Re * a[0].Re + a[1].Re * a[1].Re);
                        double theta = Math.Atan2(a[1].Re, a[0].Re);
                        return new Cplx(r, theta * state.FromRadiansFactor);
                    }
                    case "rec":
                    {
                        Require(a, 2, name);
                        double theta = a[1].Re * state.ToRadiansFactor;
                        return new Cplx(a[0].Re * Math.Cos(theta), a[0].Re * Math.Sin(theta));
                    }
                }

                throw new CalcError("Неизвестная функция");
            }

            private Cplx AngleArg(Cplx value)
            {
                return new Cplx(value.Re * state.ToRadiansFactor, value.Im);
            }

            private Cplx AngleResult(Cplx value)
            {
                return new Cplx(value.Re * state.FromRadiansFactor, value.Im);
            }

            private void CheckRealDomain(Cplx value, double min, double max, string name)
            {
                if (state.Mode == CalcMode.Cmplx) return;
                if (Math.Abs(value.Im) > 1e-14 || value.Re < min - 1e-12 || value.Re > max + 1e-12)
                    throw new CalcError(name + ": недопустимый аргумент");
            }

            private void CheckLogDomain(Cplx value, string name)
            {
                if (state.Mode == CalcMode.Cmplx) return;
                if (Math.Abs(value.Im) > 1e-14 || value.Re <= 0)
                    throw new CalcError(name + ": x > 0");
            }

            private double RealPart(Cplx value, string name)
            {
                if (Math.Abs(value.Im) > 1e-14) throw new CalcError(name + ": нужно действительное");
                return value.Re;
            }

            private long LongValue(Cplx value, string name)
            {
                double v = RealPart(value, name);
                if (!IsInteger(v) || Math.Abs(v) > 9e15) throw new CalcError(name + ": нужно целое");
                return (long)Math.Round(v);
            }

            private Cplx RoundToDisplay(Cplx value)
            {
                return new Cplx(Math.Round(value.Re, 10), Math.Round(value.Im, 10));
            }

            private static void Require(List<Cplx> args, int count, string name)
            {
                if (args.Count != count) throw new CalcError(name + ": аргументы");
            }

            private static double Factorial(Cplx value)
            {
                if (Math.Abs(value.Im) > 1e-14) throw new CalcError("Факториал: нужно целое");
                double x = value.Re;
                if (x < 0 || !IsInteger(x) || x > 170) throw new CalcError("Факториал: 0…170");
                double result = 1;
                for (int i = 2; i <= (int)Math.Round(x); i++) result *= i;
                return result;
            }

            private static double Gcd(long a, long b)
            {
                while (b != 0)
                {
                    long t = a % b;
                    a = b;
                    b = t;
                }
                return a;
            }

            private static Cplx Asinh(Cplx z)
            {
                return Cplx.Log(z + Cplx.Sqrt(z * z + Cplx.One));
            }

            private static Cplx Acosh(Cplx z)
            {
                return Cplx.Log(z + Cplx.Sqrt(z - Cplx.One) * Cplx.Sqrt(z + Cplx.One));
            }

            private static Cplx Atanh(Cplx z)
            {
                return Cplx.Real(0.5) * Cplx.Log((Cplx.One + z) / (Cplx.One - z));
            }
        }
    }
}
