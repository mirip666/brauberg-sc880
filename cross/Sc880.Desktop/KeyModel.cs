using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace SC880.Desktop
{
    internal enum KeyKind
    {
        Function,
        Number,
        Red,
        Soft
    }

    internal sealed class CalcKey
    {
        public string Id;
        public string Primary;
        public string Shift;
        public string Alpha;
        public string Tertiary;
        public Color ShiftColor = Palette.Orange;
        public Color AlphaColor = Palette.Pink;
        public Color TertiaryColor = Palette.Teal;
        public KeyKind Kind;
        public Rect Rect;
        public double PrimarySize = 13;
        public bool IsBlank;
    }

    internal sealed class HistoryEntry
    {
        public string Expression;
        public string Result;
        public Cplx Value;
    }

    internal static class Numbers
    {
        public static double Parse(string text)
        {
            if (text == null) throw new FormatException("Пустое значение");
            string s = text.Trim().Replace('−', '-').Replace('–', '-').Replace('—', '-').Replace(',', '.');
            if (s.Length == 0) throw new FormatException("Пустое значение");
            double v;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
            int slash = s.IndexOf('/');
            if (slash > 0 && slash < s.Length - 1)
            {
                double a, b;
                if (double.TryParse(s.Substring(0, slash), NumberStyles.Float, CultureInfo.InvariantCulture, out a)
                    && double.TryParse(s.Substring(slash + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out b)
                    && b != 0)
                    return a / b;
            }
            throw new FormatException("Неверное число: " + text);
        }

        public static string Format(double v)
        {
            if (Math.Abs(v) < 5e-13) v = 0;
            return v.ToString("G12", CultureInfo.InvariantCulture);
        }
    }
}
