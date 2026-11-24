using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;

namespace SC880
{
    internal sealed partial class Sc880Form
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            string probeText = AccessibilitySnapshot();
            if (displayProbe.Text != probeText) displayProbe.Text = probeText;
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Palette.FormBack);

            DrawBody(g);
            DrawHeader(g);
            DrawLcd(g);
            DrawTopLabels(g);
            DrawKeyLabels(g);
            DrawPad(g);
            DrawKeyBodies(g);
        }

        private void DrawBody(Graphics g)
        {
            RectangleF body = new RectangleF(S(10), S(10), S(460), S(860));
            for (int i = 5; i >= 1; i--)
            {
                RectangleF sh = new RectangleF(body.X + S(1), body.Y + S(2 + i), body.Width, body.Height);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(5 + i, 30, 30, 28)))
                    Draw.FillRound(g, sh, S(38), b);
            }
            using (LinearGradientBrush br = new LinearGradientBrush(body, Palette.BodyTop, Palette.BodyBottom, 90f))
                Draw.FillRound(g, body, S(38), br);
            using (Pen p = new Pen(Palette.BodyLine, Math.Max(1f, S(1.1f))))
                Draw.DrawRound(g, body, S(38), p);
            RectangleF inner = new RectangleF(body.X + S(7), body.Y + S(7), body.Width - S(14), body.Height - S(14));
            using (Pen p = new Pen(Color.FromArgb(130, 255, 255, 255), Math.Max(1f, S(1.4f))))
                Draw.DrawRound(g, inner, S(32), p);
            using (Pen p = new Pen(Color.FromArgb(90, 180, 176, 166), Math.Max(1f, S(1f))))
            {
                RectangleF lower = new RectangleF(body.X + S(1), body.Bottom - S(52), body.Width - S(2), S(50));
                using (GraphicsPath path = Draw.RoundRect(lower, S(34))) g.DrawPath(p, path);
            }
        }

        private void DrawHeader(Graphics g)
        {
            RectangleF mark = new RectangleF(S(46), S(25), S(32), S(32));
            using (SolidBrush b = new SolidBrush(Palette.Logo)) Draw.FillRound(g, mark, S(1.2f), b);
            using (GraphicsPath clip = Draw.RoundRect(mark, S(1.2f)))
            {
                GraphicsState st = g.Save();
                g.SetClip(clip);
                using (Pen p = new Pen(Palette.BodyTop, Math.Max(2f, S(7f))))
                {
                    p.StartCap = LineCap.Flat;
                    p.EndCap = LineCap.Flat;
                    p.LineJoin = LineJoin.Miter;
                    g.DrawLines(p, new PointF[]
                    {
                        new PointF(S(42.8f), S(35.6f)),
                        new PointF(S(57.4f), S(49.4f)),
                        new PointF(S(73.6f), S(20.8f))
                    });
                }
                g.Restore(st);
            }

            Font brand = fonts.UiBold(28f);
            Draw.TextLeft(g, "BRAUBERG", brand, Palette.Logo,
                new RectangleF(S(86), S(24), S(220), S(36)));
            float brandW = scale > 0 ? Measure(g, "BRAUBERG", brand) / scale : 147f;
            float regSize = S(11.6f);
            float regX = S(86 + brandW + 1.8f);
            float regY = S(26.6f);
            using (Pen p = new Pen(Palette.Logo, Math.Max(1f, S(1.2f))))
                g.DrawEllipse(p, regX, regY, regSize, regSize);
            Draw.TextCenter(g, "R", fonts.UiBold(7.3f), Palette.Logo,
                new RectangleF(regX, regY - S(0.3f), regSize, regSize));
            Draw.TextRight(g, "SC-880", fonts.UiBold(20.5f), Palette.Ink,
                new RectangleF(S(316), S(27), S(120), S(30)));

            Draw3DButton(g);
        }

        private void Draw3DButton(Graphics g)
        {
            RectangleF r = new RectangleF(S(threeDButton.X), S(threeDButton.Y),
                S(threeDButton.Width), S(threeDButton.Height));
            int backAlpha = hover3D ? 30 : 10;
            using (SolidBrush b = new SolidBrush(Color.FromArgb(backAlpha, 45, 44, 41)))
                Draw.FillRound(g, r, S(9), b);
            using (Pen p = new Pen(Color.FromArgb(hover3D ? 80 : 34, 70, 68, 63), Math.Max(1f, S(1f))))
                Draw.DrawRound(g, r, S(9), p);

            float cx = S(273.5f);
            float cy = S(41.5f);
            float s = S(7.4f);
            PointF[] top =
            {
                new PointF(cx, cy - s),
                new PointF(cx + s, cy - s * 0.5f),
                new PointF(cx, cy),
                new PointF(cx - s, cy - s * 0.5f)
            };
            PointF[] left =
            {
                new PointF(cx - s, cy - s * 0.5f),
                new PointF(cx, cy),
                new PointF(cx, cy + s),
                new PointF(cx - s, cy + s * 0.5f)
            };
            PointF[] right =
            {
                new PointF(cx, cy),
                new PointF(cx + s, cy - s * 0.5f),
                new PointF(cx + s, cy + s * 0.5f),
                new PointF(cx, cy + s)
            };
            using (SolidBrush b = new SolidBrush(Color.FromArgb(215, Palette.Ink))) g.FillPolygon(b, top);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(145, Palette.Ink))) g.FillPolygon(b, left);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(180, Palette.Ink))) g.FillPolygon(b, right);
            using (Pen p = new Pen(Color.FromArgb(150, 255, 255, 253), Math.Max(1f, S(1f))))
            {
                g.DrawPolygon(p, top);
                g.DrawPolygon(p, left);
                g.DrawPolygon(p, right);
            }

            Draw.TextPlain(g, "3D", fonts.UiBold(8.4f),
                hover3D ? Palette.Ink : Palette.MutedInk,
                new RectangleF(S(284), S(31), S(22), S(22)), StringAlignment.Near);
        }

        private void DrawLcd(Graphics g)
        {
            RectangleF bezel = new RectangleF(S(40), S(63), S(400), S(134));
            using (SolidBrush b = new SolidBrush(Color.FromArgb(95, 101, 89))) Draw.FillRound(g, bezel, S(7), b);
            RectangleF screen = new RectangleF(S(44), S(67), S(392), S(126));
            using (LinearGradientBrush br = new LinearGradientBrush(screen, Palette.LcdTop, Palette.LcdBottom, 90f))
                Draw.FillRound(g, screen, S(3), br);
            using (Pen p = new Pen(Color.FromArgb(80, 55, 61, 51), Math.Max(1f, S(1f))))
                Draw.DrawRound(g, screen, S(3), p);

            using (GraphicsPath glare = new GraphicsPath())
            {
                glare.AddPolygon(new PointF[]
                {
                    new PointF(S(50), S(71)),
                    new PointF(S(240), S(71)),
                    new PointF(S(120), S(160)),
                    new PointF(S(50), S(160))
                });
                using (SolidBrush b = new SolidBrush(Color.FromArgb(15, 255, 255, 255))) g.FillPath(b, glare);
            }

            if (powerOff)
            {
                Draw.TextCenter(g, "OFF", fonts.Lcd(13f), Color.FromArgb(90, 25, 40, 70),
                    new RectangleF(S(52), S(74), S(376), S(112)));
                return;
            }

            GraphicsState st = g.Save();
            g.SetClip(new RectangleF(S(50), S(72), S(380), S(118)));

            Font ind = fonts.Lcd(9.3f);
            float ix = 55;
            if (state.Memory.Magnitude > 5e-14)
            {
                Draw.TextLeft(g, "M", ind, Palette.LcdInk, new RectangleF(S(ix), S(74), S(18), S(13)));
                ix += 13;
            }
            if (shift)
            {
                Draw.TextLeft(g, "S", ind, Palette.LcdInk, new RectangleF(S(ix), S(74), S(14), S(13)));
                ix += 12;
            }
            if (alpha)
            {
                Draw.TextLeft(g, "A", ind, Palette.LcdInk, new RectangleF(S(ix), S(74), S(14), S(13)));
                ix += 12;
            }
            if (hyp)
            {
                Draw.TextLeft(g, "hyp", ind, Palette.LcdInk, new RectangleF(S(ix), S(74), S(28), S(13)));
            }

            string angle = state.Angle == AngleUnit.Degrees ? "D" : (state.Angle == AngleUnit.Radians ? "R" : "G");
            string right = state.ModeName + "  " + angle + "  Math";
            if (state.Mode == CalcMode.BaseN)
                right = "BASE " + BaseName(baseValue);
            Draw.TextRight(g, right, fonts.Lcd(9.3f), Palette.LcdFaint,
                new RectangleF(S(250), S(74), S(174), S(13)));

            if (string.IsNullOrEmpty(expression) && !showingResult && errorText == null)
            {
                Draw.TextRight(g, "0", fonts.Lcd(30f), Color.FromArgb(200, Palette.LcdInk),
                    new RectangleF(S(60), S(136), S(366), S(48)));
                g.Restore(st);
                return;
            }

            float exprTop = S(92);
            float exprHeight = S(43);
            Font exprFont = FitFont(g, expression, S(368), 21f, 12.5f);
            DrawExpression(g, exprFont, new RectangleF(S(56), exprTop, S(368), exprHeight), showingResult);

            if (errorText != null)
            {
                Font errorFont = FitFont(g, errorText, S(360), 17f, 10.5f);
                Draw.TextLeft(g, errorText, errorFont, Palette.LcdInk,
                    new RectangleF(S(56), S(137), S(360), S(40)));
            }
            else if (showingResult && resultText.Length > 0)
            {
                Font resultFont = FitFont(g, resultText, S(366), 31f, 14f);
                string shown = resultText;
                if (Measure(g, shown, resultFont) > S(366))
                {
                    while (shown.Length > 1 && Measure(g, "…" + shown, resultFont) > S(366)) shown = shown.Substring(1);
                    shown = "…" + shown;
                }
                Draw.TextRight(g, shown, resultFont, Palette.LcdInk,
                    new RectangleF(S(56), S(135), S(370), S(46)));
            }

            if (toast != null)
            {
                Font tf = fonts.Lcd(8.2f);
                RectangleF tr = new RectangleF(S(56), S(172), S(200), S(15));
                using (SolidBrush b = new SolidBrush(Color.FromArgb(205, Palette.LcdBottom))) g.FillRectangle(b, tr);
                Draw.TextLeft(g, toast, tf, Color.FromArgb(190, 30, 70, 68), tr);
            }

            g.Restore(st);

            if (!showingResult && errorText == null && caret >= 0)
            {
                if (blink)
                {
                    using (Pen p = new Pen(Palette.LcdInk, Math.Max(1.3f, S(2f))))
                    {
                        float cx = CaretX(g, expression, caret, S(56), S(368));
                        g.DrawLine(p, cx, S(99), cx, S(128));
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

        private float Measure(Graphics g, string text, Font font)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return g.MeasureString(text, font, 10000, StringFormat.GenericTypographic).Width;
        }

        private Font FitFont(Graphics g, string text, float maxWidth, float start, float min)
        {
            float size = start;
            while (size > min)
            {
                Font f = fonts.Lcd(size);
                if (Measure(g, text, f) <= maxWidth) return f;
                size -= 1f;
            }
            return fonts.Lcd(min);
        }

        private void DrawExpression(Graphics g, Font font, RectangleF rect, bool withResult)
        {
            string text = expression;
            if (text.Length == 0) return;
            float max = rect.Width;
            Font f = font;
            string shown = text;
            int start = 0;
            if (Measure(g, shown, f) > max)
            {
                // keep the part around the caret visible
                int end = text.Length;
                start = caret;
                while (start > 0 && Measure(g, "…" + text.Substring(start - 1, Math.Max(0, end - start + 1)), f) <= max) start--;
                if (start > 0 && Measure(g, "…" + text.Substring(start, end - start), f) > max) start++;
                int s2 = start;
                while (end > start && Measure(g, "…" + text.Substring(start, end - start) + "…", f) > max) end--;
                shown = text.Substring(start, end - start);
                if (start > 0) shown = "…" + shown;
                if (end < text.Length) shown = shown + "…";
                if (Measure(g, shown, f) > max)
                {
                    while (shown.Length > 1 && Measure(g, shown, f) > max) shown = shown.Substring(1);
                }
            }
            Draw.TextLeft(g, shown, f, Palette.LcdInk, rect);
        }

        private float CaretX(Graphics g, string text, int caretIndex, float x, float maxWidth)
        {
            Font f = FitFont(g, text, maxWidth, 21f, 12.5f);
            string before = text.Substring(0, Math.Min(caretIndex, text.Length));
            float w = Measure(g, before, f);
            if (w > maxWidth) w = maxWidth;
            return x + w;
        }

        private void DrawTopLabels(Graphics g)
        {
            Font f = fonts.UiBold(8.3f);
            Draw.TextCenter(g, "SHIFT", f, shift ? Color.FromArgb(190, 70, 20) : Palette.Orange,
                new RectangleF(S(40), S(282), S(66), S(16)));
            Draw.TextCenter(g, "ALPHA", f, alpha ? Color.FromArgb(160, 20, 70) : Palette.Pink,
                new RectangleF(S(110), S(282), S(66), S(16)));
            Draw.TextCenter(g, "REPLAY", fonts.UiBold(8.7f), Palette.Ink,
                new RectangleF(S(180), S(270), S(120), S(16)));
            Draw.TextRight(g, "MODE", fonts.UiBold(7.8f), Palette.Ink,
                new RectangleF(S(286), S(286), S(46), S(16)));
            Draw.TextLeft(g, "SET UP", fonts.UiBold(7.8f), Palette.Orange,
                new RectangleF(S(335), S(286), S(52), S(16)));
            Draw.TextCenter(g, "ON", fonts.UiBold(8.5f), Palette.Ink,
                new RectangleF(S(380), S(274), S(68), S(16)));
        }

        private void DrawKeyLabels(Graphics g)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                CalcKey k = keys[i];
                if (k.Kind == KeyKind.Soft) continue;
                DrawSecondary(g, k);
            }
        }

        private void DrawSecondary(Graphics g, CalcKey k)
        {
            if (string.IsNullOrEmpty(k.Shift) && string.IsNullOrEmpty(k.Alpha) && string.IsNullOrEmpty(k.Tertiary)) return;
            string[] parts = new string[3];
            Color[] colors = new Color[3];
            int count = 0;
            if (!string.IsNullOrEmpty(k.Shift)) { parts[count] = k.Shift; colors[count] = k.ShiftColor; count++; }
            if (!string.IsNullOrEmpty(k.Alpha)) { parts[count] = k.Alpha; colors[count] = k.AlphaColor; count++; }
            if (!string.IsNullOrEmpty(k.Tertiary)) { parts[count] = k.Tertiary; colors[count] = k.TertiaryColor; count++; }

            float available = S(k.Rect.Width) * (k.Kind == KeyKind.Number ? 1.24f : 1.16f);
            float size = 8.2f;
            Font f = fonts.UiBold(size);
            float total = 0;
            for (int i = 0; i < count; i++) total += Measure(g, parts[i], f);
            total += (count - 1) * S(3);
            while (total > available && size > 6.2f)
            {
                size -= 0.4f;
                f = fonts.UiBold(size);
                total = 0;
                for (int i = 0; i < count; i++) total += Measure(g, parts[i], f);
                total += (count - 1) * S(2.4f);
            }
            float y = S(k.Rect.Y - 12.5f);
            float inset = k.Id == "exp" ? 6f : 2f;
            RectangleF full = new RectangleF(S(k.Rect.X + inset), y, S(k.Rect.Width - inset * 2), S(11));
            if (count == 1)
            {
                Draw.TextPlain(g, parts[0], f, colors[0], full, StringAlignment.Center);
            }
            else
            {
                Draw.TextPlain(g, parts[0], f, colors[0], full, StringAlignment.Near);
                if (count > 2)
                    Draw.TextPlain(g, parts[1], f, colors[1], full, StringAlignment.Center);
                Draw.TextPlain(g, parts[count - 1], f, colors[count - 1], full, StringAlignment.Far);
            }
        }

        private void DrawPad(Graphics g)
        {
            RectangleF r = Scaled(padRect);
            using (SolidBrush sh = new SolidBrush(Color.FromArgb(28, 20, 20, 18)))
                Draw.FillRound(g, new RectangleF(r.X, r.Y + S(2), r.Width, r.Height), S(16), sh);
            using (LinearGradientBrush br = new LinearGradientBrush(r, Palette.KeySoftTop, Palette.KeySoftBottom, 90f))
                Draw.FillRound(g, r, S(16), br);
            using (Pen p = new Pen(Palette.KeySoftLine, Math.Max(1f, S(1.1f))))
                Draw.DrawRound(g, r, S(16), p);
            RectangleF inner = new RectangleF(r.X + S(6), r.Y + S(6), r.Width - S(12), r.Height - S(12));
            using (Pen p = new Pen(Color.FromArgb(90, 255, 255, 255), Math.Max(1f, S(1.4f))))
                Draw.DrawRound(g, inner, S(11), p);

            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;
            Color arrow = Color.FromArgb(136, 134, 128);
            if (hoverPad == 1) arrow = Palette.Ink;
            DrawArrow(g, cx, cy - S(31), S(12), 0, arrow);
            if (hoverPad == 2) arrow = Palette.Ink; else arrow = Color.FromArgb(136, 134, 128);
            DrawArrow(g, cx, cy + S(31), S(12), 2, arrow);
            if (hoverPad == 3) arrow = Palette.Ink; else arrow = Color.FromArgb(136, 134, 128);
            DrawArrow(g, cx - S(37), cy, S(12), 3, arrow);
            if (hoverPad == 4) arrow = Palette.Ink; else arrow = Color.FromArgb(136, 134, 128);
            DrawArrow(g, cx + S(37), cy, S(12), 1, arrow);

            using (SolidBrush b = new SolidBrush(Color.FromArgb(196, 194, 187)))
                g.FillEllipse(b, cx - S(8), cy - S(8), S(16), S(16));
            using (Pen p = new Pen(Color.FromArgb(158, 155, 148), Math.Max(1f, S(1f))))
                g.DrawEllipse(p, cx - S(8), cy - S(8), S(16), S(16));
        }

        private void DrawArrow(Graphics g, float x, float y, float size, int direction, Color color)
        {
            PointF[] p;
            using (SolidBrush b = new SolidBrush(color))
            {
                switch (direction)
                {
                    case 0:
                        p = new PointF[] { new PointF(x, y - size / 2), new PointF(x - size / 2, y + size / 2), new PointF(x + size / 2, y + size / 2) };
                        break;
                    case 1:
                        p = new PointF[] { new PointF(x + size / 2, y), new PointF(x - size / 2, y - size / 2), new PointF(x - size / 2, y + size / 2) };
                        break;
                    case 2:
                        p = new PointF[] { new PointF(x, y + size / 2), new PointF(x - size / 2, y - size / 2), new PointF(x + size / 2, y - size / 2) };
                        break;
                    default:
                        p = new PointF[] { new PointF(x - size / 2, y), new PointF(x + size / 2, y - size / 2), new PointF(x + size / 2, y + size / 2) };
                        break;
                }
                g.FillPolygon(b, p);
            }
        }

        private void DrawKeyBodies(Graphics g)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                CalcKey k = keys[i];
                RectangleF r = Scaled(k.Rect);
                bool pressed = pressedId == k.Id;
                bool hover = hoverId == k.Id;
                if (pressed) r = new RectangleF(r.X, r.Y + S(1.2f), r.Width, r.Height - S(0.6f));

                Color top, bottom, line, text;
                switch (k.Kind)
                {
                    case KeyKind.Function:
                        top = Palette.KeyDarkTop; bottom = Palette.KeyDarkBottom; line = Palette.KeyDarkLine; text = Color.FromArgb(250, 250, 247);
                        break;
                    case KeyKind.Red:
                        top = Palette.KeyRedTop; bottom = Palette.KeyRedBottom; line = Palette.KeyRedLine; text = Color.White;
                        break;
                    case KeyKind.Soft:
                        top = Palette.KeySoftTop; bottom = Palette.KeySoftBottom; line = Palette.KeySoftLine; text = Palette.Ink;
                        break;
                    default:
                        top = Palette.KeyWhiteTop; bottom = Palette.KeyWhiteBottom; line = Palette.KeyWhiteLine; text = Color.FromArgb(62, 61, 57);
                        break;
                }
                if (pressed)
                {
                    top = Draw.Mix(top, Color.Black, 0.17f);
                    bottom = Draw.Mix(bottom, Color.Black, 0.17f);
                }
                else if (hover)
                {
                    top = Draw.Mix(top, Color.White, 0.06f);
                    bottom = Draw.Mix(bottom, Color.White, 0.04f);
                }

                if (k.Kind != KeyKind.Soft)
                {
                    using (SolidBrush sh = new SolidBrush(Color.FromArgb(k.Kind == KeyKind.Number ? 24 : 42, 20, 20, 18)))
                        Draw.FillRound(g, new RectangleF(r.X, r.Y + S(1.8f), r.Width, r.Height), S(6), sh);
                }

                float radius = k.Kind == KeyKind.Soft ? S(11) : S(6.5f);
                using (LinearGradientBrush br = new LinearGradientBrush(r, top, bottom, 90f))
                    Draw.FillRound(g, r, radius, br);
                using (Pen p = new Pen(line, Math.Max(1f, S(1f))))
                    Draw.DrawRound(g, r, radius, p);

                if (k.Kind == KeyKind.Number || k.Kind == KeyKind.Function || k.Kind == KeyKind.Red)
                {
                    using (Pen p = new Pen(Color.FromArgb(k.Kind == KeyKind.Number ? 90 : 55, 255, 255, 255), Math.Max(1f, S(0.9f))))
                    {
                        RectangleF hi = new RectangleF(r.X + S(3), r.Y + S(2.2f), r.Width - S(6), r.Height - S(7));
                        using (GraphicsPath path = Draw.RoundRect(hi, radius * 0.7f)) g.DrawPath(p, path);
                    }
                }

                if (!string.IsNullOrEmpty(k.Primary))
                {
                    Font pf;
                    if (k.Kind == KeyKind.Number)
                    {
                        if (k.Id == "equals" || k.Id == "times" || k.Id == "div" || k.Id == "plus" || k.Id == "minus")
                            pf = fonts.Ui(28f);
                        else if (k.Id == "del" || k.Id == "ac" || k.Id == "ans")
                            pf = fonts.UiBold(16f);
                        else if (k.Id == "exp")
                            pf = fonts.UiBold(17f);
                        else pf = fonts.Ui(27f);
                    }
                    else
                    {
                        pf = fonts.UiBold(k.PrimarySize);
                    }
                    RectangleF tr = new RectangleF(r.X + S(2), r.Y + S(1.5f), r.Width - S(4), r.Height - S(3));
                    if (k.Id == "exp")
                    {
                        float w = Measure(g, "×10", pf);
                        float tx = r.X + r.Width / 2f - w / 2f - S(2);
                        Draw.TextCenter(g, "×10", pf, text,
                            new RectangleF(r.X + S(2), r.Y + S(1.5f), r.Width - S(9), r.Height - S(3)));
                        Draw.TextCenter(g, "x", fonts.UiBold(9.5f), text,
                            new RectangleF(tx + w + S(1), r.Y + S(2), S(12), S(18)));
                    }
                    else
                    {
                        Draw.TextCenter(g, k.Primary, pf, text, tr);
                    }
                }
            }
        }
    }
}
