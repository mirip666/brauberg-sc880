using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace SC880
{
    internal static class Palette
    {
        public static readonly Color FormBack = Color.FromArgb(233, 232, 227);
        public static readonly Color BodyTop = Color.FromArgb(252, 251, 248);
        public static readonly Color BodyBottom = Color.FromArgb(229, 226, 219);
        public static readonly Color BodyLine = Color.FromArgb(203, 199, 190);
        public static readonly Color BodyInner = Color.FromArgb(255, 255, 253);
        public static readonly Color Ink = Color.FromArgb(35, 35, 33);
        public static readonly Color MutedInk = Color.FromArgb(110, 108, 104);

        public static readonly Color LcdFrame = Color.FromArgb(99, 105, 94);
        public static readonly Color LcdTop = Color.FromArgb(176, 187, 168);
        public static readonly Color LcdBottom = Color.FromArgb(154, 168, 144);
        public static readonly Color LcdInk = Color.FromArgb(28, 55, 91);
        public static readonly Color LcdFaint = Color.FromArgb(70, 92, 104);

        public static readonly Color KeyDarkTop = Color.FromArgb(83, 84, 80);
        public static readonly Color KeyDarkBottom = Color.FromArgb(52, 53, 50);
        public static readonly Color KeyDarkLine = Color.FromArgb(38, 39, 36);
        public static readonly Color KeyWhiteTop = Color.FromArgb(253, 253, 251);
        public static readonly Color KeyWhiteBottom = Color.FromArgb(242, 241, 237);
        public static readonly Color KeyWhiteLine = Color.FromArgb(196, 191, 182);
        public static readonly Color KeyRedTop = Color.FromArgb(226, 52, 38);
        public static readonly Color KeyRedBottom = Color.FromArgb(185, 26, 19);
        public static readonly Color KeyRedLine = Color.FromArgb(150, 20, 14);
        public static readonly Color KeySoftTop = Color.FromArgb(226, 225, 220);
        public static readonly Color KeySoftBottom = Color.FromArgb(202, 200, 194);
        public static readonly Color KeySoftLine = Color.FromArgb(170, 167, 159);

        public static readonly Color Orange = Color.FromArgb(216, 130, 42);
        public static readonly Color Pink = Color.FromArgb(199, 64, 105);
        public static readonly Color Teal = Color.FromArgb(38, 146, 135);
        public static readonly Color Purple = Color.FromArgb(120, 82, 155);
        public static readonly Color Logo = Color.FromArgb(28, 28, 26);
    }

    internal static class Draw
    {
        public static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            float d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
            GraphicsPath path = new GraphicsPath();
            if (d <= 0.5f)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRound(Graphics g, RectangleF r, float radius, Brush brush)
        {
            using (GraphicsPath path = RoundRect(r, radius)) g.FillPath(brush, path);
        }

        public static void DrawRound(Graphics g, RectangleF r, float radius, Pen pen)
        {
            using (GraphicsPath path = RoundRect(r, radius)) g.DrawPath(pen, path);
        }

        public static void Text(Graphics g, string text, Font font, Color color, RectangleF rect, StringAlignment align, StringAlignment line)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = align;
                sf.LineAlignment = line;
                sf.FormatFlags = StringFormatFlags.NoWrap;
                sf.Trimming = StringTrimming.EllipsisCharacter;
                using (SolidBrush b = new SolidBrush(color)) g.DrawString(text, font, b, rect, sf);
            }
        }

        public static void TextPlain(Graphics g, string text, Font font, Color color, RectangleF rect, StringAlignment align)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = align;
                sf.LineAlignment = StringAlignment.Center;
                sf.FormatFlags = StringFormatFlags.NoWrap;
                using (SolidBrush b = new SolidBrush(color)) g.DrawString(text, font, b, rect, sf);
            }
        }

        public static void TextCenter(Graphics g, string text, Font font, Color color, RectangleF rect)
        {
            Text(g, text, font, color, rect, StringAlignment.Center, StringAlignment.Center);
        }

        public static void TextRight(Graphics g, string text, Font font, Color color, RectangleF rect)
        {
            Text(g, text, font, color, rect, StringAlignment.Far, StringAlignment.Center);
        }

        public static void TextLeft(Graphics g, string text, Font font, Color color, RectangleF rect)
        {
            Text(g, text, font, color, rect, StringAlignment.Near, StringAlignment.Center);
        }

        public static Color Mix(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }

    internal sealed class FontBank : IDisposable
    {
        private readonly System.Collections.Generic.Dictionary<string, Font> cache =
            new System.Collections.Generic.Dictionary<string, Font>();
        private float scale = 1f;

        public float Scale
        {
            get { return scale; }
        }

        public void SetScale(float value)
        {
            if (Math.Abs(value - scale) < 0.0001f) return;
            foreach (Font f in cache.Values) f.Dispose();
            cache.Clear();
            scale = value;
        }

        public Font Get(string family, float pixelSize, FontStyle style)
        {
            string key = family + "|" + pixelSize.ToString("F2") + "|" + (int)style;
            Font font;
            if (cache.TryGetValue(key, out font)) return font;
            float size = Math.Max(5f, pixelSize * scale);
            try
            {
                font = new Font(family, size, style, GraphicsUnit.Pixel);
            }
            catch
            {
                font = new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Pixel);
            }
            cache[key] = font;
            return font;
        }

        public Font Ui(float pixelSize)
        {
            return Get("Segoe UI", pixelSize, FontStyle.Regular);
        }

        public Font UiBold(float pixelSize)
        {
            return Get("Segoe UI", pixelSize, FontStyle.Bold);
        }

        public Font UiSemi(float pixelSize)
        {
            return Get("Segoe UI Semibold", pixelSize, FontStyle.Regular);
        }

        public Font Lcd(float pixelSize)
        {
            try
            {
                Font f = Get("Consolas", pixelSize, FontStyle.Regular);
                if (f.Name.IndexOf("Consolas", StringComparison.OrdinalIgnoreCase) >= 0) return f;
            }
            catch
            {
            }
            return Get("Lucida Console", pixelSize, FontStyle.Regular);
        }

        public void Dispose()
        {
            foreach (Font f in cache.Values) f.Dispose();
            cache.Clear();
        }
    }
}
