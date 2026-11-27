using System;
using Avalonia;
using Avalonia.Media;

namespace SC880.Desktop
{
    internal sealed partial class CalculatorView
    {
        public override void Render(DrawingContext ctx)
        {
            base.Render(ctx);
            ctx.FillRectangle(Gfx.Solid(Palette.FormBack), new Rect(Bounds.Size));

            DrawBody(ctx);
            DrawHeader(ctx);
            DrawLcd(ctx);
            DrawTopLabels(ctx);
            DrawKeyLabels(ctx);
            DrawPad(ctx);
            DrawKeyBodies(ctx);
        }

        private double S(double value)
        {
            return value * scale;
        }

        private Rect Scaled(Rect r)
        {
            return new Rect(S(r.X), S(r.Y), S(r.Width), S(r.Height));
        }

        private void DrawBody(DrawingContext g)
        {
            Rect body = new Rect(S(10), S(10), S(460), S(860));
            for (int i = 5; i >= 1; i--)
            {
                Rect sh = new Rect(body.X + S(1), body.Y + S(2 + i), body.Width, body.Height);
                Gfx.FillRound(g, sh, S(38), Gfx.Solid(Color.FromArgb((byte)(5 + i), 30, 30, 28)));
            }
            Gfx.FillRound(g, body, S(38), Gfx.VGradient(Palette.BodyTop, Palette.BodyBottom));
            Gfx.DrawRound(g, body, S(38), Gfx.Pen(Palette.BodyLine, Math.Max(1, S(1.1))));

            Rect inner = new Rect(body.X + S(7), body.Y + S(7), body.Width - S(14), body.Height - S(14));
            Gfx.DrawRound(g, inner, S(32), Gfx.Pen(Color.FromArgb(130, 255, 255, 255), Math.Max(1, S(1.4))));

            Rect lower = new Rect(body.X + S(1), body.Bottom - S(52), body.Width - S(2), S(50));
            g.DrawGeometry(null, Gfx.Pen(Color.FromArgb(90, 180, 176, 166), Math.Max(1, S(1))),
                Gfx.RoundRectGeometry(lower, S(34)));
        }

        private void DrawHeader(DrawingContext g)
        {
            Rect mark = new Rect(S(46), S(25), S(32), S(32));
            Gfx.FillRound(g, mark, S(1.2), Gfx.Solid(Palette.Logo));
            using (g.PushGeometryClip(Gfx.RoundRectGeometry(mark, S(1.2))))
            {
                IPen p = new Pen(new SolidColorBrush(Palette.BodyTop), Math.Max(2, S(7)),
                    null, PenLineCap.Flat, PenLineJoin.Miter);
                g.DrawLine(p, new Point(S(42.8), S(35.6)), new Point(S(57.4), S(49.4)));
                g.DrawLine(p, new Point(S(57.4), S(49.4)), new Point(S(73.6), S(20.8)));
            }

            Gfx.TextLeft(g, "BRAUBERG", Fonts.UiBold, 28, Palette.Logo,
                new Rect(S(86), S(24), S(220), S(36)));
            double brandW = Gfx.Measure("BRAUBERG", Fonts.UiBold, 28);
            double regSize = S(11.6);
            double regX = S(86 + brandW + 1.8);
            double regY = S(26.6);
            g.DrawEllipse(null, Gfx.Pen(Palette.Logo, Math.Max(1, S(1.2))),
                new Point(regX + regSize / 2, regY + regSize / 2), regSize / 2, regSize / 2);
            Gfx.TextCenter(g, "R", Fonts.UiBold, 7.3, Palette.Logo,
                new Rect(regX, regY - S(0.3), regSize, regSize));
            Gfx.TextRight(g, "SC-880", Fonts.UiBold, 20.5, Palette.Ink,
                new Rect(S(316), S(27), S(120), S(30)));
        }

        private void DrawLcd(DrawingContext g)
        {
            Rect bezel = new Rect(S(40), S(63), S(400), S(134));
            Gfx.FillRound(g, bezel, S(7), Gfx.Solid(Color.FromRgb(95, 101, 89)));
            Rect screen = new Rect(S(44), S(67), S(392), S(126));
            Gfx.FillRound(g, screen, S(3), Gfx.VGradient(Palette.LcdTop, Palette.LcdBottom));
            Gfx.DrawRound(g, screen, S(3), Gfx.Pen(Color.FromArgb(80, 55, 61, 51), Math.Max(1, S(1))));

            Point[] glare =
            {
                new Point(S(50), S(71)),
                new Point(S(240), S(71)),
                new Point(S(120), S(160)),
                new Point(S(50), S(160))
            };
            g.DrawGeometry(Gfx.Solid(Color.FromArgb(15, 255, 255, 255)), null, Gfx.Polygon(glare));

            if (powerOff)
            {
                Gfx.TextCenter(g, "OFF", Fonts.Lcd, 13, Color.FromArgb(90, 25, 40, 70),
                    new Rect(S(52), S(74), S(376), S(112)));
                return;
            }

            using (g.PushClip(new Rect(S(50), S(72), S(380), S(118))))
            {
                double ix = 55;
                if (state.Memory.Magnitude > 5e-14)
                {
                    Gfx.TextLeft(g, "M", Fonts.Lcd, 9.3, Palette.LcdInk, new Rect(S(ix), S(74), S(18), S(13)));
                    ix += 13;
                }
                if (shift)
                {
                    Gfx.TextLeft(g, "S", Fonts.Lcd, 9.3, Palette.LcdInk, new Rect(S(ix), S(74), S(14), S(13)));
                    ix += 12;
                }
                if (alpha)
                {
                    Gfx.TextLeft(g, "A", Fonts.Lcd, 9.3, Palette.LcdInk, new Rect(S(ix), S(74), S(14), S(13)));
                    ix += 12;
                }
                if (hyp)
                {
                    Gfx.TextLeft(g, "hyp", Fonts.Lcd, 9.3, Palette.LcdInk, new Rect(S(ix), S(74), S(28), S(13)));
                }

                string angle = state.Angle == AngleUnit.Degrees ? "D" : (state.Angle == AngleUnit.Radians ? "R" : "G");
                string right = state.ModeName + "  " + angle + "  Math";
                if (state.Mode == CalcMode.BaseN)
                    right = "BASE " + BaseName(baseValue);
                Gfx.TextRight(g, right, Fonts.Lcd, 9.3, Palette.LcdFaint,
                    new Rect(S(250), S(74), S(174), S(13)));

                if (string.IsNullOrEmpty(expression) && !showingResult && errorText == null)
                {
                    Gfx.TextRight(g, "0", Fonts.Lcd, 30, Color.FromArgb(200, Palette.LcdInk.R, Palette.LcdInk.G, Palette.LcdInk.B),
                        new Rect(S(60), S(136), S(366), S(48)));
                    return;
                }

                double exprTop = S(92);
                double exprHeight = S(43);
                double exprSize = FitSize(expression, S(368), 21, 12.5);
                DrawExpression(g, exprSize, new Rect(S(56), exprTop, S(368), exprHeight), showingResult);

                if (errorText != null)
                {
                    double errorSize = FitSize(errorText, S(360), 17, 10.5);
                    Gfx.TextLeft(g, errorText, Fonts.Lcd, errorSize, Palette.LcdInk,
                        new Rect(S(56), S(137), S(360), S(40)));
                }
                else if (showingResult && resultText.Length > 0)
                {
                    double resultSize = FitSize(resultText, S(366), 31, 14);
                    string shown = resultText;
                    if (Gfx.Measure(shown, Fonts.Lcd, resultSize) > S(366))
                    {
                        while (shown.Length > 1 && Gfx.Measure("…" + shown, Fonts.Lcd, resultSize) > S(366))
                            shown = shown.Substring(1);
                        shown = "…" + shown;
                    }
                    Gfx.TextRight(g, shown, Fonts.Lcd, resultSize, Palette.LcdInk,
                        new Rect(S(56), S(135), S(370), S(46)));
                }

                if (toast != null)
                {
                    Rect tr = new Rect(S(56), S(172), S(200), S(15));
                    g.FillRectangle(Gfx.Solid(Color.FromArgb(205, Palette.LcdBottom.R, Palette.LcdBottom.G, Palette.LcdBottom.B)), tr);
                    Gfx.TextLeft(g, toast, Fonts.Lcd, 8.2, Color.FromArgb(190, 30, 70, 68), tr);
                }

                if (!showingResult && errorText == null && caret >= 0)
                {
                    if (blink)
                    {
                        double cx = CaretX(expression, caret, S(56), S(368));
                        g.DrawLine(Gfx.Pen(Palette.LcdInk, Math.Max(1.3, S(2))),
                            new Point(cx, S(99)), new Point(cx, S(128)));
                    }
                }
            }
        }

        private string BaseName(int b)
        {
            if (b == 16) return "HEX";
            if (b == 2) return "BIN";
            if (b == 8) return "OCT";
            return "DEC";
        }

        private double FitSize(string text, double maxWidth, double start, double min)
        {
            double size = start;
            while (size > min)
            {
                if (Gfx.Measure(text, Fonts.Lcd, size) <= maxWidth) return size;
                size -= 1;
            }
            return min;
        }

        private void DrawExpression(DrawingContext g, double size, Rect rect, bool withResult)
        {
            string text = expression;
            if (text.Length == 0) return;
            double max = rect.Width;
            string shown = text;
            int start = 0;
            if (Gfx.Measure(shown, Fonts.Lcd, size) > max)
            {
                int end = text.Length;
                start = caret;
                while (start > 0 && Gfx.Measure("…" + text.Substring(start - 1, Math.Max(0, end - start + 1)), Fonts.Lcd, size) <= max)
                    start--;
                if (start > 0 && Gfx.Measure("…" + text.Substring(start, end - start), Fonts.Lcd, size) > max) start++;
                while (end > start && Gfx.Measure("…" + text.Substring(start, end - start) + "…", Fonts.Lcd, size) > max) end--;
                shown = text.Substring(start, end - start);
                if (start > 0) shown = "…" + shown;
                if (end < text.Length) shown = shown + "…";
                if (Gfx.Measure(shown, Fonts.Lcd, size) > max)
                {
                    while (shown.Length > 1 && Gfx.Measure(shown, Fonts.Lcd, size) > max)
                        shown = shown.Substring(1);
                }
            }
            Gfx.TextLeft(g, shown, Fonts.Lcd, size, Palette.LcdInk, rect);
        }

        private double CaretX(string text, int caretIndex, double x, double maxWidth)
        {
            double size = FitSize(text, maxWidth, 21, 12.5);
            string before = text.Substring(0, Math.Min(caretIndex, text.Length));
            double w = Gfx.Measure(before, Fonts.Lcd, size);
            if (w > maxWidth) w = maxWidth;
            return x + w;
        }

        private void DrawTopLabels(DrawingContext g)
        {
            Gfx.TextCenter(g, "SHIFT", Fonts.UiBold, 8.3, shift ? Color.FromRgb(190, 70, 20) : Palette.Orange,
                new Rect(S(40), S(282), S(66), S(16)));
            Gfx.TextCenter(g, "ALPHA", Fonts.UiBold, 8.3, alpha ? Color.FromRgb(160, 20, 70) : Palette.Pink,
                new Rect(S(110), S(282), S(66), S(16)));
            Gfx.TextCenter(g, "REPLAY", Fonts.UiBold, 8.7, Palette.Ink,
                new Rect(S(180), S(270), S(120), S(16)));
            Gfx.TextRight(g, "MODE", Fonts.UiBold, 7.8, Palette.Ink,
                new Rect(S(286), S(286), S(46), S(16)));
            Gfx.TextLeft(g, "SET UP", Fonts.UiBold, 7.8, Palette.Orange,
                new Rect(S(335), S(286), S(52), S(16)));
            Gfx.TextCenter(g, "ON", Fonts.UiBold, 8.5, Palette.Ink,
                new Rect(S(380), S(274), S(68), S(16)));
        }

        private void DrawKeyLabels(DrawingContext g)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                CalcKey k = keys[i];
                if (k.Kind == KeyKind.Soft) continue;
                DrawSecondary(g, k);
            }
        }

        private void DrawSecondary(DrawingContext g, CalcKey k)
        {
            if (string.IsNullOrEmpty(k.Shift) && string.IsNullOrEmpty(k.Alpha) && string.IsNullOrEmpty(k.Tertiary)) return;
            string[] parts = new string[3];
            Color[] colors = new Color[3];
            int count = 0;
            if (!string.IsNullOrEmpty(k.Shift)) { parts[count] = k.Shift; colors[count] = k.ShiftColor; count++; }
            if (!string.IsNullOrEmpty(k.Alpha)) { parts[count] = k.Alpha; colors[count] = k.AlphaColor; count++; }
            if (!string.IsNullOrEmpty(k.Tertiary)) { parts[count] = k.Tertiary; colors[count] = k.TertiaryColor; count++; }

            double available = S(k.Rect.Width) * (k.Kind == KeyKind.Number ? 1.24 : 1.16);
            double size = 8.2;
            double total = 0;
            for (int i = 0; i < count; i++) total += Gfx.Measure(parts[i], Fonts.UiBold, size);
            total += (count - 1) * S(3);
            while (total > available && size > 6.2)
            {
                size -= 0.4;
                total = 0;
                for (int i = 0; i < count; i++) total += Gfx.Measure(parts[i], Fonts.UiBold, size);
                total += (count - 1) * S(2.4);
            }
            double y = S(k.Rect.Y - 12.5);
            double inset = k.Id == "exp" ? 6 : 2;
            Rect full = new Rect(S(k.Rect.X + inset), y, S(k.Rect.Width - inset * 2), S(11));
            if (count == 1)
            {
                Gfx.TextCenter(g, parts[0], Fonts.UiBold, size, colors[0], full);
            }
            else
            {
                Gfx.TextLeft(g, parts[0], Fonts.UiBold, size, colors[0], full);
                if (count > 2) Gfx.TextCenter(g, parts[1], Fonts.UiBold, size, colors[1], full);
                Gfx.TextRight(g, parts[count - 1], Fonts.UiBold, size, colors[count - 1], full);
            }
        }

        private void DrawPad(DrawingContext g)
        {
            Rect r = Scaled(padRect);
            Gfx.FillRound(g, new Rect(r.X, r.Y + S(2), r.Width, r.Height), S(16),
                Gfx.Solid(Color.FromArgb(28, 20, 20, 18)));
            Gfx.FillRound(g, r, S(16), Gfx.VGradient(Palette.KeySoftTop, Palette.KeySoftBottom));
            Gfx.DrawRound(g, r, S(16), Gfx.Pen(Palette.KeySoftLine, Math.Max(1, S(1.1))));
            Rect inner = new Rect(r.X + S(6), r.Y + S(6), r.Width - S(12), r.Height - S(12));
            Gfx.DrawRound(g, inner, S(11), Gfx.Pen(Color.FromArgb(90, 255, 255, 255), Math.Max(1, S(1.4))));

            double cx = r.X + r.Width / 2;
            double cy = r.Y + r.Height / 2;
            Color arrow = Color.FromRgb(136, 134, 128);
            if (hoverPad == 1) arrow = Palette.Ink;
            DrawArrow(g, cx, cy - S(31), S(12), 0, arrow);
            arrow = hoverPad == 2 ? Palette.Ink : Color.FromRgb(136, 134, 128);
            DrawArrow(g, cx, cy + S(31), S(12), 2, arrow);
            arrow = hoverPad == 3 ? Palette.Ink : Color.FromRgb(136, 134, 128);
            DrawArrow(g, cx - S(37), cy, S(12), 3, arrow);
            arrow = hoverPad == 4 ? Palette.Ink : Color.FromRgb(136, 134, 128);
            DrawArrow(g, cx + S(37), cy, S(12), 1, arrow);

            g.DrawEllipse(Gfx.Solid(Color.FromRgb(196, 194, 187)),
                Gfx.Pen(Color.FromRgb(158, 155, 148), Math.Max(1, S(1))),
                new Point(cx, cy), S(8), S(8));
        }

        private void DrawArrow(DrawingContext g, double x, double y, double size, int direction, Color color)
        {
            Point[] p;
            switch (direction)
            {
                case 0:
                    p = new[] { new Point(x, y - size / 2), new Point(x - size / 2, y + size / 2), new Point(x + size / 2, y + size / 2) };
                    break;
                case 1:
                    p = new[] { new Point(x + size / 2, y), new Point(x - size / 2, y - size / 2), new Point(x - size / 2, y + size / 2) };
                    break;
                case 2:
                    p = new[] { new Point(x, y + size / 2), new Point(x - size / 2, y - size / 2), new Point(x + size / 2, y - size / 2) };
                    break;
                default:
                    p = new[] { new Point(x - size / 2, y), new Point(x + size / 2, y - size / 2), new Point(x + size / 2, y + size / 2) };
                    break;
            }
            g.DrawGeometry(Gfx.Solid(color), null, Gfx.Polygon(p));
        }

        private void DrawKeyBodies(DrawingContext g)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                CalcKey k = keys[i];
                Rect r = Scaled(k.Rect);
                bool pressed = pressedId == k.Id;
                bool hover = hoverId == k.Id;
                if (pressed) r = new Rect(r.X, r.Y + S(1.2), r.Width, r.Height - S(0.6));

                Color top, bottom, line, text;
                switch (k.Kind)
                {
                    case KeyKind.Function:
                        top = Palette.KeyDarkTop; bottom = Palette.KeyDarkBottom; line = Palette.KeyDarkLine;
                        text = Color.FromRgb(250, 250, 247);
                        break;
                    case KeyKind.Red:
                        top = Palette.KeyRedTop; bottom = Palette.KeyRedBottom; line = Palette.KeyRedLine;
                        text = Colors.White;
                        break;
                    case KeyKind.Soft:
                        top = Palette.KeySoftTop; bottom = Palette.KeySoftBottom; line = Palette.KeySoftLine;
                        text = Palette.Ink;
                        break;
                    default:
                        top = Palette.KeyWhiteTop; bottom = Palette.KeyWhiteBottom; line = Palette.KeyWhiteLine;
                        text = Color.FromRgb(62, 61, 57);
                        break;
                }
                if (pressed)
                {
                    top = Gfx.Mix(top, Colors.Black, 0.17);
                    bottom = Gfx.Mix(bottom, Colors.Black, 0.17);
                }
                else if (hover)
                {
                    top = Gfx.Mix(top, Colors.White, 0.06);
                    bottom = Gfx.Mix(bottom, Colors.White, 0.04);
                }

                if (k.Kind != KeyKind.Soft)
                {
                    Gfx.FillRound(g, new Rect(r.X, r.Y + S(1.8), r.Width, r.Height), S(6),
                        Gfx.Solid(Color.FromArgb(k.Kind == KeyKind.Number ? (byte)24 : (byte)42, 20, 20, 18)));
                }

                double radius = k.Kind == KeyKind.Soft ? S(11) : S(6.5);
                Gfx.FillRound(g, r, radius, Gfx.VGradient(top, bottom));
                Gfx.DrawRound(g, r, radius, Gfx.Pen(line, Math.Max(1, S(1))));

                if (k.Kind == KeyKind.Number || k.Kind == KeyKind.Function || k.Kind == KeyKind.Red)
                {
                    Rect hi = new Rect(r.X + S(3), r.Y + S(2.2), r.Width - S(6), r.Height - S(7));
                    g.DrawGeometry(null,
                        Gfx.Pen(Color.FromArgb(k.Kind == KeyKind.Number ? (byte)90 : (byte)55, 255, 255, 255),
                            Math.Max(1, S(0.9))),
                        Gfx.RoundRectGeometry(hi, radius * 0.7));
                }

                if (!string.IsNullOrEmpty(k.Primary))
                {
                    Typeface tf;
                    double size;
                    if (k.Kind == KeyKind.Number)
                    {
                        tf = Fonts.Ui;
                        if (k.Id == "equals" || k.Id == "times" || k.Id == "div" || k.Id == "plus" || k.Id == "minus")
                            size = 28;
                        else if (k.Id == "del" || k.Id == "ac" || k.Id == "ans")
                        { tf = Fonts.UiBold; size = 16; }
                        else if (k.Id == "exp")
                        { tf = Fonts.UiBold; size = 17; }
                        else size = 27;
                    }
                    else
                    {
                        tf = Fonts.UiBold;
                        size = k.PrimarySize;
                    }
                    Rect tr = new Rect(r.X + S(2), r.Y + S(1.5), r.Width - S(4), r.Height - S(3));
                    if (k.Id == "exp")
                    {
                        double w = Gfx.Measure("×10", tf, size);
                        double tx = r.X + r.Width / 2 - w / 2 - S(2);
                        Gfx.TextCenter(g, "×10", tf, size, text,
                            new Rect(r.X + S(2), r.Y + S(1.5), r.Width - S(9), r.Height - S(3)));
                        Gfx.TextCenter(g, "x", Fonts.UiBold, 9.5, text,
                            new Rect(tx + w + S(1), r.Y + S(2), S(12), S(18)));
                    }
                    else
                    {
                        Gfx.TextCenter(g, k.Primary, tf, size, text, tr);
                    }
                }
            }
        }
    }
}
