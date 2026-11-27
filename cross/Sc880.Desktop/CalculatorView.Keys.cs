using Avalonia;

namespace SC880.Desktop
{
    internal sealed partial class CalculatorView
    {
        private static double FCol(int i)
        {
            return 40.0 + i * 69.9;
        }

        private static double FRow(int i)
        {
            return 354.0 + i * 62.0;
        }

        private static double NCol(int i)
        {
            return 49.0 + i * 82.1;
        }

        private static double NRow(int i)
        {
            return 620.0 + i * 62.0;
        }

        private CalcKey AddKey(string id, string primary, string shiftText, string alphaText, string tertiary,
            KeyKind kind, double x, double y, double w, double h, double size)
        {
            CalcKey k = new CalcKey();
            k.Id = id;
            k.Primary = primary;
            k.Shift = shiftText;
            k.Alpha = alphaText;
            k.Tertiary = tertiary;
            k.Kind = kind;
            k.Rect = new Rect(x, y, w, h);
            k.PrimarySize = size;
            k.IsBlank = string.IsNullOrEmpty(primary);
            keys.Add(k);
            return k;
        }

        private CalcKey KeyById(string id)
        {
            for (int i = 0; i < keys.Count; i++) if (keys[i].Id == id) return keys[i];
            return null;
        }

        private void BuildKeys()
        {
            AddKey("shift", "", null, null, null, KeyKind.Soft, 44, 300, 56, 34, 10);
            AddKey("alpha", "", null, null, null, KeyKind.Soft, 114, 300, 56, 34, 10);
            AddKey("mode", "", null, null, null, KeyKind.Soft, 310, 304, 56, 34, 10);
            AddKey("on", "", null, null, null, KeyKind.Soft, 386, 295, 56, 34, 10);

            AddKey("calc", "CALC", "SOLVE", null, "=", KeyKind.Function, FCol(0), FRow(0), 63.5, 46, 12.5);
            AddKey("integral", "∫dx", "d/dx", null, ":", KeyKind.Function, FCol(1), FRow(0), 63.5, 46, 15);
            AddKey("xinv", "x⁻¹", "x!", null, null, KeyKind.Function, FCol(4), FRow(0), 63.5, 46, 16);
            AddKey("logbox", "log□", "Σ", null, null, KeyKind.Function, FCol(5), FRow(0), 63.5, 46, 14);

            AddKey("frac", "a b/c", "d/c", null, null, KeyKind.Function, FCol(0), FRow(1), 63.5, 46, 12.5);
            AddKey("sqrt", "√", "³√", null, null, KeyKind.Function, FCol(1), FRow(1), 63.5, 46, 21);
            AddKey("square", "x²", "x³", null, "DEC", KeyKind.Function, FCol(2), FRow(1), 63.5, 46, 17);
            AddKey("power", "xʸ", "ⁿ√", null, "HEX", KeyKind.Function, FCol(3), FRow(1), 63.5, 46, 16);
            AddKey("log", "log", "10ˣ", null, "BIN", KeyKind.Function, FCol(4), FRow(1), 63.5, 46, 16);
            AddKey("ln", "ln", "eˣ", null, "OCT", KeyKind.Function, FCol(5), FRow(1), 63.5, 46, 17);

            AddKey("neg", "(−)", "r∠θ", "A", null, KeyKind.Function, FCol(0), FRow(2), 63.5, 46, 14);
            AddKey("dms", "° ' \"", "←", "B", null, KeyKind.Function, FCol(1), FRow(2), 63.5, 46, 14);
            AddKey("hyp", "hyp", "Abs", "C", null, KeyKind.Function, FCol(2), FRow(2), 63.5, 46, 14);
            AddKey("sin", "sin", "sin⁻¹", "D", null, KeyKind.Function, FCol(3), FRow(2), 63.5, 46, 15);
            AddKey("cos", "cos", "cos⁻¹", "E", null, KeyKind.Function, FCol(4), FRow(2), 63.5, 46, 15);
            AddKey("tan", "tan", "tan⁻¹", "F", null, KeyKind.Function, FCol(5), FRow(2), 63.5, 46, 15);

            AddKey("rcl", "RCL", "STO", null, null, KeyKind.Function, FCol(0), FRow(3), 63.5, 46, 13);
            AddKey("eng", "ENG", "←", "i", null, KeyKind.Function, FCol(1), FRow(3), 63.5, 46, 13);
            AddKey("lparen", "(", "%", "X", null, KeyKind.Function, FCol(2), FRow(3), 63.5, 46, 17);
            AddKey("rparen", ")", ",", "Y", null, KeyKind.Function, FCol(3), FRow(3), 63.5, 46, 17);
            AddKey("sd", "S⇔D", "a b/c⇄d/c", null, null, KeyKind.Function, FCol(4), FRow(3), 63.5, 46, 12.5);
            AddKey("mplus", "M+", "M−", "M", null, KeyKind.Function, FCol(5), FRow(3), 63.5, 46, 15);

            AddKey("7", "7", "CONST", null, null, KeyKind.Number, NCol(0), NRow(0), 74.8, 46, 21);
            AddKey("8", "8", "CONV", null, null, KeyKind.Number, NCol(1), NRow(0), 74.8, 46, 21);
            AddKey("9", "9", "CLR", null, null, KeyKind.Number, NCol(2), NRow(0), 74.8, 46, 21);
            AddKey("del", "DEL", "INS", null, null, KeyKind.Red, NCol(3), NRow(0), 74.8, 46, 15);
            AddKey("ac", "AC", "OFF", null, null, KeyKind.Red, NCol(4), NRow(0), 74.8, 46, 15);

            AddKey("4", "4", "MATRIX", null, null, KeyKind.Number, NCol(0), NRow(1), 74.8, 46, 21);
            AddKey("5", "5", "VECTOR", null, null, KeyKind.Number, NCol(1), NRow(1), 74.8, 46, 21);
            AddKey("6", "6", null, null, null, KeyKind.Number, NCol(2), NRow(1), 74.8, 46, 21);
            AddKey("times", "×", "nPr", null, null, KeyKind.Number, NCol(3), NRow(1), 74.8, 46, 22);
            AddKey("div", "÷", "nCr", null, null, KeyKind.Number, NCol(4), NRow(1), 74.8, 46, 22);

            AddKey("1", "1", "STAT", null, null, KeyKind.Number, NCol(0), NRow(2), 74.8, 46, 21);
            AddKey("2", "2", "CMPLX", null, null, KeyKind.Number, NCol(1), NRow(2), 74.8, 46, 21);
            AddKey("3", "3", "BASE", null, null, KeyKind.Number, NCol(2), NRow(2), 74.8, 46, 21);
            AddKey("plus", "+", "Pol", null, null, KeyKind.Number, NCol(3), NRow(2), 74.8, 46, 23);
            AddKey("minus", "−", "Rec", null, null, KeyKind.Number, NCol(4), NRow(2), 74.8, 46, 23);

            AddKey("0", "0", "Rnd", null, null, KeyKind.Number, NCol(0), NRow(3), 74.8, 46, 21);
            AddKey("dot", ".", "Ran#", "RanInt", null, KeyKind.Number, NCol(1), NRow(3), 74.8, 46, 23);
            AddKey("exp", "×10", "π", "e", null, KeyKind.Number, NCol(2), NRow(3), 74.8, 46, 15);
            AddKey("ans", "Ans", "DRG▶", null, null, KeyKind.Number, NCol(3), NRow(3), 74.8, 46, 16);
            AddKey("equals", "=", null, null, null, KeyKind.Number, NCol(4), NRow(3), 74.8, 46, 23);

            KeyById("eng").AlphaColor = Palette.Purple;
            KeyById("exp").AlphaColor = Palette.Pink;
            KeyById("neg").AlphaColor = Palette.Pink;
            KeyById("calc").TertiaryColor = Palette.Pink;
            KeyById("integral").TertiaryColor = Palette.Pink;
        }
    }
}
