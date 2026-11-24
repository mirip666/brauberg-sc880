using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SC880
{
    internal static class ThreeDTextures
    {
        private static readonly Typeface Ui = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        private static readonly Typeface UiBold = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        private static readonly Typeface UiSemi = new Typeface(new FontFamily("Segoe UI Semibold"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        private static readonly Typeface Lcd = new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        private delegate void DrawProc(DrawingContext dc);

        public static ImageBrush BrushFor(BitmapSource bitmap)
        {
            ImageBrush brush = new ImageBrush(bitmap);
            brush.Stretch = Stretch.Fill;
            brush.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
            brush.Viewport = new Rect(0, 0, 1, 1);
            brush.Freeze();
            return brush;
        }

        public static ImageBrush EnsureBrush(ImageBrush brush)
        {
            if (brush != null && brush.CanFreeze && !brush.IsFrozen) brush.Freeze();
            return brush;
        }

        private static BitmapSource Render(int width, int height, double scale, DrawProc draw)
        {
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.PushTransform(new ScaleTransform(scale, scale));
                draw(dc);
                dc.Pop();
            }
            RenderTargetBitmap bmp = new RenderTargetBitmap(
                Math.Max(1, (int)Math.Ceiling(width * scale)),
                Math.Max(1, (int)Math.Ceiling(height * scale)),
                96, 96, PixelFormats.Pbgra32);
            bmp.Render(visual);
            bmp.Freeze();
            return bmp;
        }

        private static SolidColorBrush Brush(Color color)
        {
            SolidColorBrush b = new SolidColorBrush(color);
            b.Freeze();
            return b;
        }

        private static SolidColorBrush Brush(Color color, byte alpha)
        {
            SolidColorBrush b = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            b.Freeze();
            return b;
        }

        private static LinearGradientBrush Gradient(Color top, Color bottom)
        {
            LinearGradientBrush b = new LinearGradientBrush(top, bottom, new Point(0, 0), new Point(0, 1));
            b.MappingMode = BrushMappingMode.RelativeToBoundingBox;
            b.Freeze();
            return b;
        }

        private static Pen Pen(Color color, double thickness)
        {
            Pen p = new Pen(Brush(color), thickness);
            p.Freeze();
            return p;
        }

        private static Pen Pen(Color color, byte alpha, double thickness)
        {
            Pen p = new Pen(Brush(color, alpha), thickness);
            p.Freeze();
            return p;
        }

        private static FormattedText Text(string text, double size, Color color, bool bold, string family)
        {
            Typeface face = family == "lcd" ? Lcd : (family == "semi" ? UiSemi : (bold ? UiBold : Ui));
            return new FormattedText(text == null ? "" : text,
                CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, face, size,
                Brush(color), 1.0);
        }

        private static FormattedText Fit(string text, double start, double min, double maxWidth,
            Color color, bool bold, string family)
        {
            double size = start;
            FormattedText ft = Text(text, size, color, bold, family);
            while (ft.Width > maxWidth && size > min)
            {
                size -= 0.4;
                ft = Text(text, size, color, bold, family);
            }
            return ft;
        }

        private static void DrawCenter(DrawingContext dc, FormattedText ft, double cx, double cy)
        {
            dc.DrawText(ft, new Point(cx - ft.Width / 2.0, cy - ft.Height / 2.0));
        }

        private static void DrawInRect(DrawingContext dc, FormattedText ft, Rect rect, TextAlignment align)
        {
            double x = rect.X;
            if (align == TextAlignment.Center) x = rect.X + (rect.Width - ft.Width) / 2.0;
            else if (align == TextAlignment.Right) x = rect.Right - ft.Width;
            double y = rect.Y + (rect.Height - ft.Height) / 2.0;
            dc.DrawText(ft, new Point(x, y));
        }

        private static string TrimLeft(string text, double maxWidth, double size, Color color, bool bold, string family)
        {
            if (Text(text, size, color, bold, family).Width <= maxWidth) return text;
            string ellipsis = "\u2026";
            for (int i = 1; i < text.Length; i++)
            {
                string candidate = ellipsis + text.Substring(i);
                if (Text(candidate, size, color, bold, family).Width <= maxWidth) return candidate;
            }
            return ellipsis;
        }

        private static Color FromDrawing(System.Drawing.Color c)
        {
            return Color.FromArgb(c.A, c.R, c.G, c.B);
        }

        private static string SoftLabel(string id)
        {
            switch (id)
            {
                case "shift": return "SHIFT";
                case "alpha": return "ALPHA";
                case "mode": return "MODE";
                case "on": return "ON";
            }
            return "";
        }

        public static BitmapSource Key(CalcKey key, double scale)
        {
            double w = key.Rect.Width;
            double h = key.Rect.Height;
            return Render((int)Math.Ceiling(w), (int)Math.Ceiling(h), scale, delegate(DrawingContext dc)
            {
                Color top;
                Color bottom;
                Color line;
                Color text;
                bool bold = key.Kind != KeyKind.Number;
                switch (key.Kind)
                {
                    case KeyKind.Function:
                        top = ThreeDColors.KeyDarkTop; bottom = ThreeDColors.KeyDarkBottom;
                        line = ThreeDColors.KeyDarkLine; text = Color.FromRgb(250, 250, 247);
                        break;
                    case KeyKind.Red:
                        top = ThreeDColors.KeyRedTop; bottom = ThreeDColors.KeyRedBottom;
                        line = ThreeDColors.KeyRedLine; text = Colors.White;
                        break;
                    case KeyKind.Soft:
                        top = ThreeDColors.KeySoftTop; bottom = ThreeDColors.KeySoftBottom;
                        line = ThreeDColors.KeySoftLine; text = ThreeDColors.Ink;
                        break;
                    default:
                        top = ThreeDColors.KeyWhiteTop; bottom = ThreeDColors.KeyWhiteBottom;
                        line = ThreeDColors.KeyWhiteLine; text = Color.FromRgb(62, 61, 57);
                        break;
                }
                double radius = key.Kind == KeyKind.Soft ? 8.0 : 5.2;
                Rect r = new Rect(1.2, 1.2, w - 2.4, h - 2.4);
                dc.DrawRoundedRectangle(Gradient(top, bottom), Pen(line, 0.9), r, radius, radius);
                if (key.Kind != KeyKind.Soft)
                {
                    Pen shine = Pen(Colors.White, key.Kind == KeyKind.Number ? (byte)84 : (byte)48, 0.8);
                    dc.DrawRoundedRectangle(null, shine, new Rect(3.0, 2.3, w - 6.0, h - 6.6),
                        Math.Max(1.0, radius - 1.4), Math.Max(1.0, radius - 1.4));
                }

                if (key.Id == "uiclose")
                {
                    Pen cross = new Pen(Brush(Colors.White), 2.4);
                    cross.StartLineCap = PenLineCap.Round;
                    cross.EndLineCap = PenLineCap.Round;
                    cross.Freeze();
                    double cxp = w / 2.0;
                    double cyp = h / 2.0;
                    double arm = 5.8;
                    dc.DrawLine(cross, new Point(cxp - arm, cyp - arm), new Point(cxp + arm, cyp + arm));
                    dc.DrawLine(cross, new Point(cxp + arm, cyp - arm), new Point(cxp - arm, cyp + arm));
                    return;
                }

                string label = key.Primary;
                if (string.IsNullOrEmpty(label) && key.Kind == KeyKind.Soft) label = SoftLabel(key.Id);
                if (string.IsNullOrEmpty(label)) return;

                double size = key.Kind == KeyKind.Number ? 27.0 : key.PrimarySize;
                if (key.Kind == KeyKind.Soft) size = 9.2;
                if (key.Id == "del" || key.Id == "ac" || key.Id == "ans") size = 16.0;
                if (key.Id == "exp") size = 17.0;
                if (key.Id == "ui2d") size = 12.0;
                if (key.Id == "uireset") size = 11.2;
                if (key.Id == "uiclose") size = 17.0;

                if (key.Id == "exp")
                {
                    FormattedText main = Fit("\u00d710", size, 10, w - 16, text, true, "ui");
                    double total = main.Width + 7;
                    double left = (w - total) / 2.0;
                    dc.DrawText(main, new Point(left, (h - main.Height) / 2.0));
                    FormattedText sup = Text("x", 8.6, text, true, "ui");
                    dc.DrawText(sup, new Point(left + main.Width + 0.5, 4.2));
                    return;
                }

                FormattedText ft = Fit(label, size, key.Kind == KeyKind.Soft ? 6.8 : 8.0,
                    w - 10, text, bold, "ui");
                DrawCenter(dc, ft, w / 2.0, h / 2.0 + 0.4);
            });
        }

        public static BitmapSource Pad(double scale)
        {
            double w = 108;
            double h = 93;
            return Render((int)w, (int)h, scale, delegate(DrawingContext dc)
            {
                Rect r = new Rect(1.6, 1.6, w - 3.2, h - 3.2);
                dc.DrawRoundedRectangle(Gradient(ThreeDColors.KeySoftTop, ThreeDColors.KeySoftBottom),
                    Pen(ThreeDColors.KeySoftLine, 1.0), r, 15, 15);
                dc.DrawRoundedRectangle(null, Pen(Colors.White, 100, 1.2), new Rect(6, 6, w - 12, h - 12), 11, 11);

                double cx = w / 2.0;
                double cy = h / 2.0;
                Color arrow = Color.FromRgb(136, 134, 128);
                DrawArrow(dc, cx, cy - 31, 12, 0, arrow);
                DrawArrow(dc, cx, cy + 31, 12, 2, arrow);
                DrawArrow(dc, cx - 37, cy, 12, 3, arrow);
                DrawArrow(dc, cx + 37, cy, 12, 1, arrow);
                dc.DrawEllipse(Brush(Color.FromRgb(196, 194, 187)), Pen(Color.FromRgb(158, 155, 148), 0.8),
                    new Point(cx, cy), 8, 8);
            });
        }

        private static void DrawArrow(DrawingContext dc, double x, double y, double size, int direction, Color color)
        {
            Point[] p;
            if (direction == 0)
                p = new Point[] { new Point(x, y - size / 2), new Point(x - size / 2, y + size / 2), new Point(x + size / 2, y + size / 2) };
            else if (direction == 1)
                p = new Point[] { new Point(x + size / 2, y), new Point(x - size / 2, y - size / 2), new Point(x - size / 2, y + size / 2) };
            else if (direction == 2)
                p = new Point[] { new Point(x, y + size / 2), new Point(x - size / 2, y - size / 2), new Point(x + size / 2, y - size / 2) };
            else
                p = new Point[] { new Point(x - size / 2, y), new Point(x + size / 2, y - size / 2), new Point(x + size / 2, y + size / 2) };
            StreamGeometry g = new StreamGeometry();
            using (StreamGeometryContext c = g.Open())
            {
                c.BeginFigure(p[0], true, true);
                c.LineTo(p[1], true, false);
                c.LineTo(p[2], true, false);
            }
            g.Freeze();
            dc.DrawGeometry(Brush(color), null, g);
        }

        public static BitmapSource FaceOverlay(List<CalcKey> keys, double scale)
        {
            return Render(460, 860, scale, delegate(DrawingContext dc)
            {
                // soft key/top labels
                DrawTopLabel(dc, "SHIFT", ThreeDColors.Orange, 30, 272, 66, 16, 8.3, "semi");
                DrawTopLabel(dc, "ALPHA", ThreeDColors.Pink, 100, 272, 66, 16, 8.3, "semi");
                DrawTopLabel(dc, "REPLAY", ThreeDColors.Ink, 170, 260, 120, 16, 8.7, "semi");
                DrawTopLabel(dc, "MODE", ThreeDColors.Ink, 276, 276, 46, 16, 7.8, "semi");
                DrawTopLabel(dc, "SET UP", ThreeDColors.Orange, 325, 276, 52, 16, 7.8, "semi");
                DrawTopLabel(dc, "ON", ThreeDColors.Ink, 370, 264, 68, 16, 8.5, "semi");

                // key shadows
                for (int i = 0; i < keys.Count; i++)
                {
                    CalcKey k = keys[i];
                    if (k.Kind == KeyKind.Soft) continue;
                    double radius = 6.5;
                    Rect shadow = new Rect(k.Rect.X - 10 - 0.6, k.Rect.Y - 10 + 1.4, k.Rect.Width + 1.2, k.Rect.Height + 1.2);
                    dc.DrawRoundedRectangle(Brush(Color.FromRgb(40, 38, 34), 10), null, shadow, radius, radius);
                    Rect shadow2 = new Rect(shadow.X - 1.2, shadow.Y - 0.7, shadow.Width + 2.4, shadow.Height + 2.4);
                    dc.DrawRoundedRectangle(Brush(Color.FromRgb(40, 38, 34), 8), null, shadow2, radius + 1, radius + 1);
                }
                Rect padShadow = new Rect(186 - 10 - 1, 295 - 10 + 1.5, 108 + 2, 93 + 2);
                dc.DrawRoundedRectangle(Brush(Color.FromRgb(40, 38, 34), 12), null, padShadow, 17, 17);

                // secondary labels printed on the face
                for (int i = 0; i < keys.Count; i++)
                {
                    CalcKey k = keys[i];
                    if (k.Kind == KeyKind.Soft) continue;
                    DrawSecondary(dc, k);
                }

                // brand header
                Rect mark = new Rect(36, 15, 32, 32);
                dc.DrawRoundedRectangle(Brush(Color.FromRgb(31, 31, 29)), null, mark, 1.6, 1.6);
                dc.PushClip(new RectangleGeometry(mark, 1.6, 1.6));
                Pen check = new Pen(Brush(ThreeDColors.BodyFront), 7);
                check.StartLineCap = PenLineCap.Flat;
                check.EndLineCap = PenLineCap.Flat;
                check.LineJoin = PenLineJoin.Miter;
                check.Freeze();
                dc.DrawLine(check, new Point(32.8, 25.6), new Point(47.4, 39.4));
                dc.DrawLine(check, new Point(47.4, 39.4), new Point(63.6, 10.8));
                dc.Pop();

                FormattedText brand = Text("BRAUBERG", 28, ThreeDColors.Ink, true, "ui");
                dc.DrawText(brand, new Point(76, 14 + (36 - brand.Height) / 2.0));
                double regX = 76 + brand.Width + 2.4;
                dc.DrawEllipse(null, Pen(ThreeDColors.Ink, 1.1), new Point(regX + 5.8, 30.8), 5.8, 5.8);
                FormattedText reg = Text("R", 7.3, ThreeDColors.Ink, true, "ui");
                DrawCenter(dc, reg, regX + 5.8, 30.8);
                FormattedText model = Text("SC-880", 20.5, ThreeDColors.Ink, true, "ui");
                dc.DrawText(model, new Point(426 - model.Width, 17 + (30 - model.Height) / 2.0));
            });
        }

        private static void DrawTopLabel(DrawingContext dc, string text, Color color, double x, double y,
            double w, double h, double size, string family)
        {
            FormattedText ft = Fit(text, size, 6, w, color, family == "semi", family == "lcd" ? "lcd" : "ui");
            Rect r = new Rect(x, y, w, h);
            DrawInRect(dc, ft, r, TextAlignment.Center);
        }

        private static void DrawSecondary(DrawingContext dc, CalcKey k)
        {
            if (string.IsNullOrEmpty(k.Shift) && string.IsNullOrEmpty(k.Alpha) && string.IsNullOrEmpty(k.Tertiary)) return;
            List<string> parts = new List<string>();
            List<Color> colors = new List<Color>();
            if (!string.IsNullOrEmpty(k.Shift)) { parts.Add(k.Shift); colors.Add(FromDrawing(k.ShiftColor)); }
            if (!string.IsNullOrEmpty(k.Alpha)) { parts.Add(k.Alpha); colors.Add(FromDrawing(k.AlphaColor)); }
            if (!string.IsNullOrEmpty(k.Tertiary)) { parts.Add(k.Tertiary); colors.Add(FromDrawing(k.TertiaryColor)); }

            double available = k.Rect.Width * (k.Kind == KeyKind.Number ? 1.24 : 1.16);
            double size = 8.2;
            double total = 0;
            List<FormattedText> fts = new List<FormattedText>();
            for (int i = 0; i < parts.Count; i++)
            {
                FormattedText ft = Text(parts[i], size, colors[i], true, "ui");
                fts.Add(ft);
                total += ft.Width;
            }
            total += (parts.Count - 1) * 3;
            while (total > available && size > 6.2)
            {
                size -= 0.4;
                total = 0;
                fts.Clear();
                for (int i = 0; i < parts.Count; i++)
                {
                    FormattedText ft = Text(parts[i], size, colors[i], true, "ui");
                    fts.Add(ft);
                    total += ft.Width;
                }
                total += (parts.Count - 1) * 2.4;
            }

            double inset = k.Id == "exp" ? 6 : 2;
            double x = k.Rect.X - 10 + inset;
            double y = k.Rect.Y - 10 - 12.5;
            double w = k.Rect.Width - inset * 2;
            if (parts.Count == 1)
            {
                DrawInRect(dc, fts[0], new Rect(x, y, w, 11), TextAlignment.Center);
            }
            else
            {
                DrawInRect(dc, fts[0], new Rect(x, y, w, 11), TextAlignment.Left);
                if (parts.Count > 2)
                    DrawInRect(dc, fts[1], new Rect(x, y, w, 11), TextAlignment.Center);
                DrawInRect(dc, fts[fts.Count - 1], new Rect(x, y, w, 11), TextAlignment.Right);
            }
        }

        public static BitmapSource Screen(Calc3DSnapshot s, double scale)
        {
            double w = 392;
            double h = 126;
            return Render((int)w, (int)h, scale, delegate(DrawingContext dc)
            {
                Rect screen = new Rect(0, 0, w, h);
                dc.DrawRoundedRectangle(Gradient(ThreeDColors.LcdTop, ThreeDColors.LcdBottom),
                    Pen(Color.FromRgb(65, 72, 62), 0.8), screen, 3, 3);

                StreamGeometry glare = new StreamGeometry();
                using (StreamGeometryContext c = glare.Open())
                {
                    c.BeginFigure(new Point(6, 4), true, true);
                    c.LineTo(new Point(196, 4), true, false);
                    c.LineTo(new Point(76, 93), true, false);
                    c.LineTo(new Point(6, 93), true, false);
                }
                glare.Freeze();
                dc.DrawGeometry(Brush(Colors.White, 15), null, glare);

                if (s == null)
                {
                    FormattedText off = Text("OFF", 16, Color.FromArgb(120, 25, 40, 70), true, "lcd");
                    DrawCenter(dc, off, w / 2, h / 2);
                    return;
                }

                if (s.PowerOff)
                {
                    FormattedText off = Text("OFF", 18, Color.FromArgb(120, 25, 40, 70), true, "lcd");
                    DrawCenter(dc, off, w / 2, h / 2);
                    return;
                }

                FormattedText ind = Text(s.Indicators, 9.3, ThreeDColors.LcdInk, false, "lcd");
                dc.DrawText(ind, new Point(11, 6));
                FormattedText mode = Text(s.Mode, 9.3, ThreeDColors.LcdFaint, false, "lcd");
                dc.DrawText(mode, new Point(w - 10 - mode.Width, 6));

                if (string.IsNullOrEmpty(s.Expression) && !s.ShowingResult && string.IsNullOrEmpty(s.Error))
                {
                    FormattedText zero = Text("0", 30, Color.FromArgb(200, ThreeDColors.LcdInk.R,
                        ThreeDColors.LcdInk.G, ThreeDColors.LcdInk.B), false, "lcd");
                    dc.DrawText(zero, new Point(w - 12 - zero.Width, 60));
                    return;
                }

                string expression = s.Expression ?? "";
                FormattedText expr = Fit(expression, 20, 12.5, 366, ThreeDColors.LcdInk, false, "lcd");
                if (expr.Width > 366) expression = TrimLeft(expression, 366, 20, ThreeDColors.LcdInk, false, "lcd");
                expr = Text(expression, 20, ThreeDColors.LcdInk, false, "lcd");
                if (expr.Width > 366)
                {
                    expr = Fit(expression, 20, 10.5, 366, ThreeDColors.LcdInk, false, "lcd");
                }
                dc.DrawText(expr, new Point(11, 27));

                if (!string.IsNullOrEmpty(s.Error))
                {
                    FormattedText err = Fit(s.Error, 14, 9, 368, ThreeDColors.LcdInk, false, "lcd");
                    dc.DrawText(err, new Point(11, 73));
                }
                else if (s.ShowingResult && !string.IsNullOrEmpty(s.Result))
                {
                    FormattedText result = Fit(s.Result, 29, 12.5, 366, ThreeDColors.LcdInk, false, "lcd");
                    string shown = s.Result;
                    if (result.Width > 366) shown = TrimLeft(shown, 366, 29, ThreeDColors.LcdInk, false, "lcd");
                    result = Fit(shown, 29, 11, 366, ThreeDColors.LcdInk, false, "lcd");
                    dc.DrawText(result, new Point(w - 11 - result.Width, 67));
                }

                if (!string.IsNullOrEmpty(s.Toast))
                {
                    FormattedText toast = Fit(s.Toast, 8.2, 6.5, 220, Color.FromArgb(190, 30, 70, 68), false, "lcd");
                    Rect tr = new Rect(11, 106, Math.Min(230, toast.Width + 6), 14);
                    dc.DrawRectangle(Brush(ThreeDColors.LcdBottom, 205), null, tr);
                    dc.DrawText(toast, new Point(14, 107));
                }

                if (!s.ShowingResult && string.IsNullOrEmpty(s.Error) && s.Blink)
                {
                    double cx = 11 + expr.Width;
                    if (cx > 380) cx = 380;
                    dc.DrawRectangle(Brush(ThreeDColors.LcdInk), null, new Rect(cx, 31, 1.5, 25));
                }
            });
        }

        public static BitmapSource Back(double scale)
        {
            return Render(460, 860, scale, delegate(DrawingContext dc)
            {
                Rect body = new Rect(0, 0, 460, 860);
                dc.DrawRoundedRectangle(Gradient(Color.FromRgb(244, 241, 234), Color.FromRgb(206, 202, 193)),
                    Pen(Color.FromRgb(177, 172, 163), 1.2), body, 38, 38);
                dc.DrawRoundedRectangle(null, Pen(Colors.White, 100, 1.4), new Rect(7, 7, 446, 846), 32, 32);
                dc.DrawRoundedRectangle(null, Pen(Color.FromRgb(180, 175, 166), 0.8), new Rect(15, 15, 430, 830), 28, 28);

                // screws
                DrawScrew(dc, 26, 26);
                DrawScrew(dc, 434, 26);
                DrawScrew(dc, 26, 834);
                DrawScrew(dc, 434, 834);

                FormattedText brand = Text("BRAUBERG", 22, Color.FromRgb(44, 44, 41), true, "ui");
                DrawCenter(dc, brand, 230, 89);
                FormattedText model = Text("SC-880", 34, Color.FromRgb(44, 44, 41), true, "ui");
                DrawCenter(dc, model, 230, 126);
                FormattedText sub = Text("10-DIGIT SCIENTIFIC CALCULATOR", 8.6, ThreeDColors.MutedInk, true, "ui");
                DrawCenter(dc, sub, 230, 153);

                // battery compartment
                Rect door = new Rect(145, 540, 170, 235);
                dc.DrawRoundedRectangle(Brush(Color.FromRgb(235, 232, 224)), Pen(Color.FromRgb(169, 164, 155), 1.1), door, 14, 14);
                dc.DrawRoundedRectangle(null, Pen(Colors.White, 110, 1.0), new Rect(door.X + 5, door.Y + 5, door.Width - 10, door.Height - 10), 10, 10);
                for (int i = 0; i < 5; i++)
                    dc.DrawRoundedRectangle(Brush(Color.FromRgb(181, 176, 167)), null,
                        new Rect(door.X + 28 + i * 22, door.Y + 170, 12, 38), 3, 3);
                FormattedText open = Text("OPEN", 9.0, ThreeDColors.MutedInk, true, "ui");
                DrawCenter(dc, open, door.X + door.Width / 2, door.Y + 29);
                FormattedText batt = Text("2 \u00d7 AAA  1.5 V", 9.2, ThreeDColors.MutedInk, false, "ui");
                DrawCenter(dc, batt, door.X + door.Width / 2, door.Y + 205);

                FormattedText side = Text("BRAUBERG  \u00b7  MODEL SC-880  \u00b7  MADE IN CHINA", 7.4, Color.FromRgb(128, 124, 117), false, "ui");
                DrawCenter(dc, side, 230, 808);
                FormattedText serial = Text("S/N  880-000000", 7.2, Color.FromRgb(142, 138, 131), false, "lcd");
                DrawCenter(dc, serial, 230, 824);
            });
        }

        private static void DrawScrew(DrawingContext dc, double x, double y)
        {
            dc.DrawEllipse(Brush(Color.FromRgb(189, 184, 175)), Pen(Color.FromRgb(152, 147, 139), 0.8),
                new Point(x, y), 7, 7);
            dc.DrawLine(Pen(Color.FromRgb(130, 126, 119), 1.2), new Point(x - 4, y), new Point(x + 4, y));
            dc.DrawLine(Pen(Color.FromRgb(130, 126, 119), 1.2), new Point(x, y - 4), new Point(x, y + 4));
        }
    }
}
