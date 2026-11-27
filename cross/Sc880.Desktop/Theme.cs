using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace SC880.Desktop
{
    internal static class Palette
    {
        public static readonly Color FormBack = Color.FromRgb(233, 232, 227);
        public static readonly Color BodyTop = Color.FromRgb(252, 251, 248);
        public static readonly Color BodyBottom = Color.FromRgb(229, 226, 219);
        public static readonly Color BodyLine = Color.FromRgb(203, 199, 190);
        public static readonly Color BodyInner = Color.FromRgb(255, 255, 253);
        public static readonly Color Ink = Color.FromRgb(35, 35, 33);
        public static readonly Color MutedInk = Color.FromRgb(110, 108, 104);

        public static readonly Color LcdFrame = Color.FromRgb(99, 105, 94);
        public static readonly Color LcdTop = Color.FromRgb(176, 187, 168);
        public static readonly Color LcdBottom = Color.FromRgb(154, 168, 144);
        public static readonly Color LcdInk = Color.FromRgb(28, 55, 91);
        public static readonly Color LcdFaint = Color.FromRgb(70, 92, 104);

        public static readonly Color KeyDarkTop = Color.FromRgb(83, 84, 80);
        public static readonly Color KeyDarkBottom = Color.FromRgb(52, 53, 50);
        public static readonly Color KeyDarkLine = Color.FromRgb(38, 39, 36);
        public static readonly Color KeyWhiteTop = Color.FromRgb(253, 253, 251);
        public static readonly Color KeyWhiteBottom = Color.FromRgb(242, 241, 237);
        public static readonly Color KeyWhiteLine = Color.FromRgb(196, 191, 182);
        public static readonly Color KeyRedTop = Color.FromRgb(226, 52, 38);
        public static readonly Color KeyRedBottom = Color.FromRgb(185, 26, 19);
        public static readonly Color KeyRedLine = Color.FromRgb(150, 20, 14);
        public static readonly Color KeySoftTop = Color.FromRgb(226, 225, 220);
        public static readonly Color KeySoftBottom = Color.FromRgb(202, 200, 194);
        public static readonly Color KeySoftLine = Color.FromRgb(170, 167, 159);

        public static readonly Color Orange = Color.FromRgb(216, 130, 42);
        public static readonly Color Pink = Color.FromRgb(199, 64, 105);
        public static readonly Color Teal = Color.FromRgb(38, 146, 135);
        public static readonly Color Purple = Color.FromRgb(120, 82, 155);
        public static readonly Color Logo = Color.FromRgb(28, 28, 26);
    }

    internal static class Fonts
    {
        private static readonly FontFamily UiFamily =
            new FontFamily("avares://BraubergSC880/Assets/Fonts#Inter");

        private static readonly FontFamily LcdFamily =
            new FontFamily("avares://BraubergSC880/Assets/Fonts#JetBrains Mono");

        public static readonly Typeface Ui = new Typeface(UiFamily, FontStyle.Normal, FontWeight.Normal);
        public static readonly Typeface UiSemi = new Typeface(UiFamily, FontStyle.Normal, FontWeight.SemiBold);
        public static readonly Typeface UiBold = new Typeface(UiFamily, FontStyle.Normal, FontWeight.Bold);
        public static readonly Typeface Lcd = new Typeface(LcdFamily, FontStyle.Normal, FontWeight.Normal);
    }

    internal static class Gfx
    {
        public static IBrush Solid(Color color)
        {
            return new SolidColorBrush(color);
        }

        public static IPen Pen(Color color, double width)
        {
            return new Pen(new SolidColorBrush(color), width);
        }

        public static IBrush VGradient(Color top, Color bottom)
        {
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative);
            brush.EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative);
            brush.GradientStops.Add(new GradientStop(top, 0));
            brush.GradientStops.Add(new GradientStop(bottom, 1));
            return brush;
        }

        public static FormattedText Text(string text, Typeface typeface, double size, Color color)
        {
            return new FormattedText(
                text ?? "",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                Math.Max(1, size),
                new SolidColorBrush(color));
        }

        public static double Measure(string text, Typeface typeface, double size)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return Text(text, typeface, size, Colors.Black).Width;
        }

        public static void FillRound(DrawingContext ctx, Rect r, double radius, IBrush brush)
        {
            ctx.DrawRectangle(brush, null, r, radius, radius);
        }

        public static void DrawRound(DrawingContext ctx, Rect r, double radius, IPen pen)
        {
            ctx.DrawRectangle(null, pen, r, radius, radius);
        }

        public static void TextIn(DrawingContext ctx, string text, Typeface typeface, double size,
            Color color, Rect rect, TextAlign align)
        {
            FormattedText ft = Text(text, typeface, size, color);
            double x = rect.X;
            if (align == TextAlign.Center) x = rect.X + (rect.Width - ft.Width) / 2.0;
            else if (align == TextAlign.Right) x = rect.Right - ft.Width;
            double y = rect.Y + (rect.Height - ft.Height) / 2.0;
            ctx.DrawText(ft, new Point(x, y));
        }

        public static void TextLeft(DrawingContext ctx, string text, Typeface typeface, double size,
            Color color, Rect rect)
        {
            TextIn(ctx, text, typeface, size, color, rect, TextAlign.Left);
        }

        public static void TextCenter(DrawingContext ctx, string text, Typeface typeface, double size,
            Color color, Rect rect)
        {
            TextIn(ctx, text, typeface, size, color, rect, TextAlign.Center);
        }

        public static void TextRight(DrawingContext ctx, string text, Typeface typeface, double size,
            Color color, Rect rect)
        {
            TextIn(ctx, text, typeface, size, color, rect, TextAlign.Right);
        }

        public static Color Mix(Color a, Color b, double t)
        {
            return Color.FromArgb(
                (byte)(a.A + (b.A - a.A) * t),
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t));
        }

        public static Geometry Polygon(params Point[] points)
        {
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext g = geometry.Open())
            {
                g.BeginFigure(points[0], true);
                for (int i = 1; i < points.Length; i++) g.LineTo(points[i]);
                g.EndFigure(true);
            }
            return geometry;
        }

        public static Geometry RoundRectGeometry(Rect r, double radius)
        {
            double d = Math.Min(radius * 2.0, Math.Min(r.Width, r.Height));
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext g = geometry.Open())
            {
                if (d <= 0.5)
                {
                    g.BeginFigure(r.TopLeft, true);
                    g.LineTo(r.TopRight);
                    g.LineTo(r.BottomRight);
                    g.LineTo(r.BottomLeft);
                    g.EndFigure(true);
                    return geometry;
                }
                g.BeginFigure(new Point(r.X + d / 2, r.Y), true);
                g.LineTo(new Point(r.Right - d / 2, r.Y));
                g.ArcTo(new Point(r.Right, r.Y + d / 2), new Size(d / 2, d / 2), 0, false, SweepDirection.Clockwise);
                g.LineTo(new Point(r.Right, r.Bottom - d / 2));
                g.ArcTo(new Point(r.Right - d / 2, r.Bottom), new Size(d / 2, d / 2), 0, false, SweepDirection.Clockwise);
                g.LineTo(new Point(r.X + d / 2, r.Bottom));
                g.ArcTo(new Point(r.X, r.Bottom - d / 2), new Size(d / 2, d / 2), 0, false, SweepDirection.Clockwise);
                g.LineTo(new Point(r.X, r.Y + d / 2));
                g.ArcTo(new Point(r.X + d / 2, r.Y), new Size(d / 2, d / 2), 0, false, SweepDirection.Clockwise);
                g.EndFigure(true);
            }
            return geometry;
        }
    }

    internal enum TextAlign
    {
        Left,
        Center,
        Right
    }
}
