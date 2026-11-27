using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace SC880.Desktop
{
    internal static class Dlg
    {
        public static readonly FontFamily UiFont = new FontFamily("avares://BraubergSC880/Assets/Fonts#Inter");
        public static readonly FontFamily MonoFont = new FontFamily("avares://BraubergSC880/Assets/Fonts#JetBrains Mono");

        public static readonly IBrush Back = new SolidColorBrush(Color.FromRgb(250, 249, 246));
        public static readonly IBrush Panel = new SolidColorBrush(Color.FromRgb(240, 239, 234));
        public static readonly IBrush Ink = new SolidColorBrush(Color.FromRgb(35, 35, 33));
        public static readonly IBrush Muted = new SolidColorBrush(Color.FromRgb(105, 103, 98));
        public static readonly IBrush Accent = new SolidColorBrush(Color.FromRgb(186, 46, 32));
        public static readonly IBrush AccentDark = new SolidColorBrush(Color.FromRgb(148, 32, 22));
        public static readonly IBrush Line = new SolidColorBrush(Color.FromRgb(207, 203, 195));
        public static readonly IBrush FieldBack = new SolidColorBrush(Colors.White);
        public static readonly IBrush ButtonBack = new SolidColorBrush(Color.FromRgb(246, 245, 241));
        public static readonly IBrush Dark = new SolidColorBrush(Color.FromRgb(63, 64, 61));

        public static Window Make(string title, double width, double height)
        {
            Window w = new Window();
            w.Title = title;
            w.SizeToContent = SizeToContent.WidthAndHeight;
            w.CanResize = false;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.ShowInTaskbar = false;
            w.Background = Back;
            w.FontFamily = UiFont;
            w.FontSize = 13;
            Canvas canvas = new Canvas();
            canvas.Width = width;
            canvas.Height = height;
            canvas.Background = Back;
            w.Content = canvas;
            w.AddHandler(InputElement.KeyDownEvent, delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    w.Close();
                }
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            return w;
        }

        private static Canvas Layout(Window w)
        {
            Canvas c = w.Content as Canvas;
            if (c == null)
            {
                c = new Canvas();
                c.Background = Back;
                w.Content = c;
            }
            return c;
        }

        private static void Place(Control control, double x, double y, double width, double height)
        {
            control.Width = width;
            if (height > 0) control.Height = height;
            Canvas.SetLeft(control, x);
            Canvas.SetTop(control, y);
        }

        public static TextBlock Label(Window w, string text, double x, double y, double width, double height = 20)
        {
            TextBlock t = new TextBlock();
            t.Text = text;
            t.FontFamily = UiFont;
            t.FontSize = 12.5;
            t.Foreground = Ink;
            t.VerticalAlignment = VerticalAlignment.Center;
            Place(t, x, y, width, height);
            Layout(w).Children.Add(t);
            return t;
        }

        public static TextBlock Caption(Window w, string text, double x, double y, double width, double height = 24)
        {
            TextBlock t = Label(w, text, x, y, width, height);
            t.FontWeight = FontWeight.SemiBold;
            t.FontSize = 14.5;
            return t;
        }

        public static TextBlock MutedLabel(Window w, string text, double x, double y, double width, double height = 20)
        {
            TextBlock t = Label(w, text, x, y, width, height);
            t.Foreground = Muted;
            t.FontSize = 11.5;
            return t;
        }

        public static TextBox Text(Window w, string text, double x, double y, double width, double height = 26,
            bool mono = true, bool multiline = false, bool readOnly = false)
        {
            TextBox t = MakeText(text, width, height, mono, multiline, readOnly);
            Place(t, x, y, width, height);
            Layout(w).Children.Add(t);
            return t;
        }

        public static TextBox MakeText(string text, double width, double height = 26,
            bool mono = true, bool multiline = false, bool readOnly = false)
        {
            TextBox t = new TextBox();
            t.Text = text ?? "";
            t.FontFamily = mono ? MonoFont : UiFont;
            t.FontSize = mono ? 13 : 13;
            t.Background = FieldBack;
            t.Foreground = Ink;
            t.BorderBrush = Line;
            t.BorderThickness = new Thickness(1);
            t.CornerRadius = new CornerRadius(3);
            t.Padding = new Thickness(6, 3, 6, 3);
            t.VerticalContentAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center;
            if (multiline)
            {
                t.AcceptsReturn = true;
                t.TextWrapping = TextWrapping.NoWrap;
            }
            t.IsReadOnly = readOnly;
            t.Width = width;
            if (height > 0) t.Height = height;
            return t;
        }

        public static Button Button(Window w, string text, double x, double y, double width, double height,
            bool accent = false, bool dark = false)
        {
            Button b = MakeButton(text, width, height, accent, dark);
            Place(b, x, y, width, height);
            Layout(w).Children.Add(b);
            return b;
        }

        public static Button MakeButton(string text, double width, double height, bool accent = false, bool dark = false)
        {
            Button b = new Button();
            b.Content = text;
            b.FontFamily = UiFont;
            b.FontSize = 12;
            b.FontWeight = FontWeight.SemiBold;
            b.Foreground = accent || dark ? new SolidColorBrush(Colors.White) : Ink;
            b.Background = accent ? Accent : (dark ? Dark : ButtonBack);
            b.BorderBrush = accent ? AccentDark : Line;
            b.BorderThickness = new Thickness(1);
            b.CornerRadius = new CornerRadius(3);
            b.Padding = new Thickness(4, 0, 4, 0);
            b.HorizontalContentAlignment = HorizontalAlignment.Center;
            b.VerticalContentAlignment = VerticalAlignment.Center;
            return b;
        }

        public static ContentControl Group(Window w, string title, double x, double y, double width, double height)
        {
            Border frame = new Border();
            frame.BorderBrush = Line;
            frame.BorderThickness = new Thickness(1);
            frame.CornerRadius = new CornerRadius(3);
            frame.Background = new SolidColorBrush(Colors.Transparent);
            frame.Padding = new Thickness(8, 4, 8, 8);
            Place(frame, x, y, width, height);
            Layout(w).Children.Add(frame);

            DockPanel dock = new DockPanel();
            frame.Child = dock;
            TextBlock header = new TextBlock();
            header.Text = title;
            header.FontFamily = UiFont;
            header.FontSize = 11.5;
            header.Foreground = Muted;
            header.Margin = new Thickness(0, 0, 0, 4);
            DockPanel.SetDock(header, Dock.Top);
            dock.Children.Add(header);

            ContentControl host = new ContentControl();
            dock.Children.Add(host);
            return host;
        }

        public static ComboBox Combo(Window w, string[] items, int selected, double x, double y, double width, double height = 26)
        {
            ComboBox c = new ComboBox();
            c.ItemsSource = items;
            c.SelectedIndex = selected;
            c.FontFamily = UiFont;
            c.FontSize = 12.5;
            c.HorizontalAlignment = HorizontalAlignment.Left;
            Place(c, x, y, width, height);
            Layout(w).Children.Add(c);
            return c;
        }

        public static NumericUpDown Number(Window w, double value, double min, double max, double x, double y, double width,
            double height = 26)
        {
            NumericUpDown n = new NumericUpDown();
            n.Minimum = (decimal)min;
            n.Maximum = (decimal)max;
            n.Increment = 1;
            n.Value = (decimal)value;
            n.FontFamily = UiFont;
            n.FontSize = 12.5;
            Place(n, x, y, width, height);
            Layout(w).Children.Add(n);
            return n;
        }

        public static CheckBox Check(Window w, string text, bool value, double x, double y, double width, double height = 24)
        {
            CheckBox c = new CheckBox();
            c.Content = text;
            c.IsChecked = value;
            c.FontFamily = UiFont;
            c.FontSize = 12.5;
            c.Foreground = Ink;
            Place(c, x, y, width, height);
            Layout(w).Children.Add(c);
            return c;
        }

        public static RadioButton Radio(Window w, string text, bool value, double x, double y, double width, string group,
            double height = 24)
        {
            RadioButton r = new RadioButton();
            r.Content = text;
            r.IsChecked = value;
            r.GroupName = group;
            r.FontFamily = UiFont;
            r.FontSize = 12.5;
            r.Foreground = Ink;
            Place(r, x, y, width, height);
            Layout(w).Children.Add(r);
            return r;
        }

        public static ListBox List(Window w, string[] items, int selected, double x, double y, double width, double height)
        {
            ListBox l = new ListBox();
            l.ItemsSource = items;
            l.SelectedIndex = selected;
            l.FontFamily = UiFont;
            l.FontSize = 13;
            l.Background = FieldBack;
            l.BorderBrush = Line;
            l.BorderThickness = new Thickness(1);
            l.Padding = new Thickness(4);
            Place(l, x, y, width, height);
            Layout(w).Children.Add(l);
            return l;
        }

        public static StackPanel Stack(Window w, double x, double y, double width, double height)
        {
            StackPanel p = new StackPanel();
            p.Orientation = Orientation.Vertical;
            p.Spacing = 3;
            Place(p, x, y, width, height);
            Layout(w).Children.Add(p);
            return p;
        }

        public static ScrollViewer Scroll(Window w, double x, double y, double width, double height)
        {
            ScrollViewer s = new ScrollViewer();
            s.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            s.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            s.Background = FieldBack;
            s.BorderBrush = Line;
            s.BorderThickness = new Thickness(1);
            Place(s, x, y, width, height);
            Layout(w).Children.Add(s);
            return s;
        }

        public static double Parse(string text)
        {
            return Numbers.Parse(text);
        }

        public static string Format(double v)
        {
            return Numbers.Format(v);
        }

        public static async Task ErrorAsync(Window owner, string message)
        {
            Window w = Make("Ошибка", 460, 190);
            TextBlock text = Label(w, message, 18, 18, 424, 100);
            text.TextWrapping = TextWrapping.Wrap;
            text.VerticalAlignment = VerticalAlignment.Top;
            Button ok = Button(w, "OK", 344, 132, 96, 28, true);
            ok.Click += delegate { w.Close(); };
            w.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) w.Close();
            };
            if (owner != null) await w.ShowDialog(owner);
            else w.Show();
        }

        public static async Task<string[]> FieldsAsync(Window owner, string title, string prompt,
            string[] labels, string[] defaults, double width = 420)
        {
            int n = labels.Length;
            double height = 120 + n * 38;
            Window w = Make(title, width, height);
            TextBlock p = MutedLabel(w, prompt, 16, 12, width - 32, 30);
            p.TextWrapping = TextWrapping.Wrap;
            TextBox[] boxes = new TextBox[n];
            for (int i = 0; i < n; i++)
            {
                Label(w, labels[i], 16, 48 + i * 38, 150);
                boxes[i] = Text(w, defaults != null && i < defaults.Length ? defaults[i] : "", 172, 44 + i * 38, width - 188);
            }
            string[] result = null;
            Button ok = Button(w, "OK", width - 208, height - 50, 90, 28, true);
            Button cancel = Button(w, "Отмена", width - 110, height - 50, 94, 28);
            ok.Click += delegate
            {
                string[] values = new string[n];
                for (int i = 0; i < n; i++) values[i] = boxes[i].Text ?? "";
                result = values;
                w.Close();
            };
            cancel.Click += delegate { w.Close(); };
            w.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) ok.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            };
            if (owner != null) await w.ShowDialog(owner);
            else w.Show();
            return result;
        }

        public static Window Message(string title, string text, double width, double height)
        {
            Window w = Make(title, width, height);
            TextBox box = Text(w, text, 14, 14, width - 28, height - 62, true, true, true);
            ScrollViewer.SetVerticalScrollBarVisibility(box, ScrollBarVisibility.Auto);
            Button ok = Button(w, "Закрыть", width - 110, height - 40, 96, 28, true);
            ok.Click += delegate { w.Close(); };
            w.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) w.Close();
            };
            return w;
        }
    }
}
