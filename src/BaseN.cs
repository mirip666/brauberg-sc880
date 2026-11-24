using System;
using System.Globalization;
using System.Text;

namespace SC880
{
    public sealed class BaseNResult
    {
        public long Value;
        public int Base;
        public string Display;
        public string Dec;
        public string Hex;
        public string Bin;
        public string Oct;
    }

    public static class BaseN
    {
        private enum Tok
        {
            Number, Plus, Minus, Mul, Div, Mod, LP, RP,
            And, Or, Xor, Xnor, Not, Neg, Shl, Shr, End
        }

        private struct Token
        {
            public Tok Type;
            public long Value;
        }

        public static BaseNResult Evaluate(string expression, int numberBase)
        {
            if (numberBase != 2 && numberBase != 8 && numberBase != 10 && numberBase != 16)
                throw new CalcError("Основание: 2, 8, 10 или 16");
            Parser p = new Parser(expression, numberBase);
            long v = p.ParseAll();
            BaseNResult r = new BaseNResult();
            r.Value = v;
            r.Base = numberBase;
            r.Display = Format(v, numberBase);
            r.Dec = Format(v, 10);
            r.Hex = Format(v, 16);
            r.Bin = Format(v, 2);
            r.Oct = Format(v, 8);
            return r;
        }

        public static long Parse(string text, int numberBase)
        {
            if (text == null) throw new CalcError("Пустое число");
            string s = text.Trim();
            if (s.Length == 0) throw new CalcError("Пустое число");
            bool negative = false;
            if (s[0] == '-') { negative = true; s = s.Substring(1).Trim(); }
            if (s.Length == 0) throw new CalcError("Ошибка в числе");
            try
            {
                ulong v = 0;
                for (int i = 0; i < s.Length; i++)
                {
                    int d = DigitValue(s[i]);
                    if (d < 0 || d >= numberBase) throw new CalcError("Цифра недопустима для основания " + numberBase);
                    v = checked(v * (ulong)numberBase + (ulong)d);
                    if (v > (ulong)long.MaxValue) throw new CalcError("Переполнение");
                }
                long result = (long)v;
                return negative ? -result : result;
            }
            catch (OverflowException)
            {
                throw new CalcError("Переполнение");
            }
        }

        public static string Format(long value, int numberBase)
        {
            if (value == 0) return "0";
            bool negative = value < 0;
            ulong mag;
            if (negative) mag = (ulong)(-(value + 1)) + 1UL;
            else mag = (ulong)value;
            StringBuilder sb = new StringBuilder();
            while (mag > 0)
            {
                int d = (int)(mag % (ulong)numberBase);
                sb.Insert(0, DigitChar(d));
                mag /= (ulong)numberBase;
            }
            if (negative) sb.Insert(0, '-');
            return sb.ToString();
        }

        public static string FormatAll(long value)
        {
            return "DEC " + Format(value, 10) + "\nHEX " + Format(value, 16)
                + "\nBIN " + Format(value, 2) + "\nOCT " + Format(value, 8);
        }

        private static char DigitChar(int d)
        {
            if (d < 10) return (char)('0' + d);
            return (char)('A' + d - 10);
        }

        private static int DigitValue(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return -1;
        }

        private sealed class Parser
        {
            private readonly string text;
            private readonly int numberBase;
            private int pos;
            private Token current;

            public Parser(string source, int b)
            {
                text = Normalize(source);
                numberBase = b;
                pos = 0;
                current = Next();
            }

            private static string Normalize(string s)
            {
                StringBuilder sb = new StringBuilder(s.Length);
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    if (c == '×' || c == '∙') sb.Append('*');
                    else if (c == '÷') sb.Append('/');
                    else if (c == '−' || c == '–' || c == '—') sb.Append('-');
                    else sb.Append(c);
                }
                return sb.ToString();
            }

            public long ParseAll()
            {
                long v = ParseOr();
                if (current.Type != Tok.End) throw new CalcError("Ошибка в выражении");
                return v;
            }

            private void Advance()
            {
                current = Next();
            }

            private Token Next()
            {
                while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
                if (pos >= text.Length) return new Token { Type = Tok.End };

                char c = text[pos];
                if (c == '(') { pos++; return new Token { Type = Tok.LP }; }
                if (c == ')') { pos++; return new Token { Type = Tok.RP }; }
                if (c == '+') { pos++; return new Token { Type = Tok.Plus }; }
                if (c == '-') { pos++; return new Token { Type = Tok.Minus }; }
                if (c == '*') { pos++; return new Token { Type = Tok.Mul }; }
                if (c == '/') { pos++; return new Token { Type = Tok.Div }; }
                if (c == '%') { pos++; return new Token { Type = Tok.Mod }; }

                if (c == '<' && pos + 1 < text.Length && text[pos + 1] == '<')
                {
                    pos += 2;
                    return new Token { Type = Tok.Shl };
                }
                if (c == '>' && pos + 1 < text.Length && text[pos + 1] == '>')
                {
                    pos += 2;
                    return new Token { Type = Tok.Shr };
                }

                if (char.IsLetter(c))
                {
                    Tok? word = ReadWord();
                    if (word.HasValue) return new Token { Type = word.Value };
                    int start = pos;
                    while (pos < text.Length && IsDigitForBase(text[pos], numberBase)) pos++;
                    if (pos == start) throw new CalcError("Символ не поддерживается");
                    string num = text.Substring(start, pos - start);
                    return new Token { Type = Tok.Number, Value = Parse(num, numberBase) };
                }

                if (char.IsDigit(c))
                {
                    int start = pos;
                    while (pos < text.Length && IsDigitForBase(text[pos], numberBase)) pos++;
                    string num = text.Substring(start, pos - start);
                    return new Token { Type = Tok.Number, Value = Parse(num, numberBase) };
                }

                throw new CalcError("Символ не поддерживается");
            }

            private Tok? ReadWord()
            {
                string[] names = { "AND", "OR", "XOR", "XNOR", "NOT", "NEG", "MOD", "SHL", "SHR" };
                for (int i = 0; i < names.Length; i++)
                {
                    string name = names[i];
                    if (pos + name.Length > text.Length) continue;
                    if (string.Compare(text, pos, name, 0, name.Length, true, CultureInfo.InvariantCulture) != 0) continue;
                    int end = pos + name.Length;
                    if (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_')) continue;
                    pos = end;
                    switch (name)
                    {
                        case "AND": return Tok.And;
                        case "OR": return Tok.Or;
                        case "XOR": return Tok.Xor;
                        case "XNOR": return Tok.Xnor;
                        case "NOT": return Tok.Not;
                        case "NEG": return Tok.Neg;
                        case "MOD": return Tok.Mod;
                        case "SHL": return Tok.Shl;
                        default: return Tok.Shr;
                    }
                }
                return null;
            }

            private static bool IsDigitForBase(char c, int b)
            {
                int d = DigitValue(c);
                return d >= 0 && d < b;
            }

            private long ParseOr()
            {
                long v = ParseXor();
                while (current.Type == Tok.Or)
                {
                    Advance();
                    v |= ParseXor();
                }
                return v;
            }

            private long ParseXor()
            {
                long v = ParseAnd();
                while (current.Type == Tok.Xor || current.Type == Tok.Xnor)
                {
                    Tok t = current.Type;
                    Advance();
                    long r = ParseAnd();
                    v = t == Tok.Xor ? (v ^ r) : ~(v ^ r);
                }
                return v;
            }

            private long ParseAnd()
            {
                long v = ParseShift();
                while (current.Type == Tok.And)
                {
                    Advance();
                    v &= ParseShift();
                }
                return v;
            }

            private long ParseShift()
            {
                long v = ParseAdd();
                while (current.Type == Tok.Shl || current.Type == Tok.Shr)
                {
                    Tok t = current.Type;
                    Advance();
                    long r = ParseAdd();
                    if (r < 0 || r > 63) throw new CalcError("Сдвиг 0…63");
                    v = t == Tok.Shl ? (v << (int)r) : (v >> (int)r);
                }
                return v;
            }

            private long ParseAdd()
            {
                long v = ParseMul();
                while (current.Type == Tok.Plus || current.Type == Tok.Minus)
                {
                    Tok t = current.Type;
                    Advance();
                    long r = ParseMul();
                    try
                    {
                        v = checked(t == Tok.Plus ? v + r : v - r);
                    }
                    catch (OverflowException)
                    {
                        throw new CalcError("Переполнение");
                    }
                }
                return v;
            }

            private long ParseMul()
            {
                long v = ParseUnary();
                while (current.Type == Tok.Mul || current.Type == Tok.Div || current.Type == Tok.Mod)
                {
                    Tok t = current.Type;
                    Advance();
                    long r = ParseUnary();
                    try
                    {
                        if (t == Tok.Mul) v = checked(v * r);
                        else if (t == Tok.Div)
                        {
                            if (r == 0) throw new CalcError("Деление на ноль");
                            v = v / r;
                        }
                        else
                        {
                            if (r == 0) throw new CalcError("Деление на ноль");
                            v = v % r;
                        }
                    }
                    catch (OverflowException)
                    {
                        throw new CalcError("Переполнение");
                    }
                }
                return v;
            }

            private long ParseUnary()
            {
                if (current.Type == Tok.Minus || current.Type == Tok.Neg)
                {
                    Advance();
                    long v = ParseUnary();
                    try { return checked(-v); }
                    catch (OverflowException) { throw new CalcError("Переполнение"); }
                }
                if (current.Type == Tok.Plus)
                {
                    Advance();
                    return ParseUnary();
                }
                if (current.Type == Tok.Not)
                {
                    Advance();
                    return ~ParseUnary();
                }
                if (current.Type == Tok.Number)
                {
                    long v = current.Value;
                    Advance();
                    return v;
                }
                if (current.Type == Tok.LP)
                {
                    Advance();
                    long v = ParseOr();
                    if (current.Type != Tok.RP) throw new CalcError("Не закрыта скобка");
                    Advance();
                    return v;
                }
                throw new CalcError("Ошибка в выражении");
            }
        }
    }
}
