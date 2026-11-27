using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace SC880.Desktop
{
    internal static class Dialogs
    {
        private static readonly FontFamily UiFont = new FontFamily("avares://BraubergSC880/Assets/Fonts#Inter");
        private static readonly FontFamily MonoFont = new FontFamily("avares://BraubergSC880/Assets/Fonts#JetBrains Mono");
        private static readonly IBrush DialogBack = new SolidColorBrush(Color.FromRgb(250, 249, 246));
        private static readonly IBrush Ink = new SolidColorBrush(Color.FromRgb(35, 35, 33));
        private static readonly IBrush Muted = new SolidColorBrush(Color.FromRgb(105, 103, 98));
        private static readonly IBrush FieldBack = new SolidColorBrush(Colors.White);
        private static readonly IBrush Line = new SolidColorBrush(Color.FromRgb(207, 203, 195));

        private static Window MakeWindow(string title, double width, double height)
        {
            Window w = new Window();
            w.Title = title;
            w.Width = width;
            w.Height = height;
            w.CanResize = false;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.ShowInTaskbar = false;
            w.Background = DialogBack;
            w.FontFamily = UiFont;
            w.FontSize = 13;
            return w;
        }

        private static TextBlock Caption(string text)
        {
            TextBlock t = new TextBlock();
            t.Text = text;
            t.FontFamily = UiFont;
            t.FontWeight = FontWeight.SemiBold;
            t.FontSize = 14;
            t.Foreground = Ink;
            t.Margin = new Thickness(0, 0, 0, 6);
            return t;
        }

        private static TextBlock Hint(string text)
        {
            TextBlock t = new TextBlock();
            t.Text = text;
            t.FontFamily = UiFont;
            t.FontSize = 12;
            t.Foreground = Muted;
            t.TextWrapping = TextWrapping.Wrap;
            t.Margin = new Thickness(0, 0, 0, 10);
            return t;
        }

        private static Button MakeButton(string text, bool accent)
        {
            Button b = new Button();
            b.Content = text;
            b.MinWidth = 96;
            b.Height = 32;
            b.Margin = new Thickness(8, 0, 0, 0);
            b.FontFamily = UiFont;
            b.FontWeight = FontWeight.SemiBold;
            if (accent) b.Classes.Add("accent");
            return b;
        }

        private static StackPanel Buttons(Button ok, Button cancel)
        {
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            panel.HorizontalAlignment = HorizontalAlignment.Right;
            panel.Margin = new Thickness(0, 14, 0, 0);
            if (cancel != null) panel.Children.Add(cancel);
            panel.Children.Add(ok);
            return panel;
        }

        public static async Task InfoAsync(Window owner, string title, string text, double width, double height)
        {
            Window w = MakeWindow(title, width, height);
            TextBox box = new TextBox();
            box.Text = text;
            box.IsReadOnly = true;
            box.AcceptsReturn = true;
            box.TextWrapping = TextWrapping.NoWrap;
            box.FontFamily = MonoFont;
            box.FontSize = 12;
            box.Background = FieldBack;
            box.BorderBrush = Line;
            box.BorderThickness = new Thickness(1);
            box.CornerRadius = new CornerRadius(4);
            box.Padding = new Thickness(10);
            box.VerticalContentAlignment = VerticalAlignment.Top;
            ScrollViewer.SetVerticalScrollBarVisibility(box, Avalonia.Controls.Primitives.ScrollBarVisibility.Auto);

            Button ok = MakeButton("OK", true);
            ok.Click += delegate { w.Close(); };

            DockPanel panel = new DockPanel();
            panel.Margin = new Thickness(16);
            DockPanel.SetDock(ok, Dock.Bottom);
            Button holder = ok;
            holder.HorizontalAlignment = HorizontalAlignment.Right;
            panel.Children.Add(holder);
            panel.Children.Add(box);
            w.Content = panel;
            if (owner != null) await w.ShowDialog(owner);
            else w.Show();
        }

        public static async Task<int> ListAsync(Window owner, string title, string subtitle, string[] options, int selected)
        {
            Window w = MakeWindow(title, 500, 520);
            ListBox list = new ListBox();
            list.ItemsSource = options;
            list.SelectedIndex = selected;
            list.FontFamily = UiFont;
            list.FontSize = 13;
            list.Background = FieldBack;
            list.BorderBrush = Line;
            list.BorderThickness = new Thickness(1);
            list.Padding = new Thickness(4);

            int result = -1;
            Button ok = MakeButton("Выбрать", true);
            Button cancel = MakeButton("Отмена", false);
            ok.Click += delegate { result = list.SelectedIndex; w.Close(); };
            cancel.Click += delegate { w.Close(); };
            list.DoubleTapped += delegate { result = list.SelectedIndex; w.Close(); };
            w.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) { result = list.SelectedIndex; w.Close(); }
                else if (e.Key == Key.Escape) w.Close();
            };

            DockPanel panel = new DockPanel();
            panel.Margin = new Thickness(16);
            StackPanel buttons = Buttons(ok, cancel);
            DockPanel.SetDock(buttons, Dock.Bottom);
            panel.Children.Add(buttons);
            StackPanel head = new StackPanel();
            head.Children.Add(Caption(subtitle));
            DockPanel.SetDock(head, Dock.Top);
            panel.Children.Add(head);
            panel.Children.Add(list);
            w.Content = panel;
            if (owner != null) await w.ShowDialog(owner);
            else w.Show();
            return result;
        }

        public static async Task<string[]> FieldsAsync(Window owner, string title, string subtitle, string[] labels, string[] defaults)
        {
            int rows = labels.Length;
            double height = Math.Min(640, 190 + rows * 46);
            Window w = MakeWindow(title, 480, height);

            StackPanel fields = new StackPanel();
            List<TextBox> boxes = new List<TextBox>();
            for (int i = 0; i < rows; i++)
            {
                TextBlock label = new TextBlock();
                label.Text = labels[i];
                label.FontFamily = UiFont;
                label.FontSize = 12.5;
                label.Foreground = Ink;
                label.Margin = new Thickness(0, 6, 0, 3);
                fields.Children.Add(label);

                TextBox box = new TextBox();
                box.Text = defaults != null && i < defaults.Length ? defaults[i] : "";
                box.FontFamily = MonoFont;
                box.FontSize = 13;
                box.Height = 30;
                box.Background = FieldBack;
                box.BorderBrush = Line;
                box.BorderThickness = new Thickness(1);
                box.CornerRadius = new CornerRadius(4);
                boxes.Add(box);
                fields.Children.Add(box);
            }

            string[] result = null;
            Button ok = MakeButton("Вычислить", true);
            Button cancel = MakeButton("Отмена", false);
            ok.Click += delegate
            {
                string[] values = new string[boxes.Count];
                for (int i = 0; i < boxes.Count; i++) values[i] = boxes[i].Text ?? "";
                result = values;
                w.Close();
            };
            cancel.Click += delegate { w.Close(); };
            w.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) { ok.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); }
                else if (e.Key == Key.Escape) w.Close();
            };

            DockPanel panel = new DockPanel();
            panel.Margin = new Thickness(16);
            StackPanel buttons = Buttons(ok, cancel);
            DockPanel.SetDock(buttons, Dock.Bottom);
            panel.Children.Add(buttons);
            StackPanel head = new StackPanel();
            head.Children.Add(Caption(subtitle));
            DockPanel.SetDock(head, Dock.Top);
            panel.Children.Add(head);
            ScrollViewer scroll = new ScrollViewer();
            scroll.Content = fields;
            scroll.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
            panel.Children.Add(scroll);
            w.Content = panel;
            if (owner != null) await w.ShowDialog(owner);
            else w.Show();
            return result;
        }

        public static async Task<CalcMode?> ModeAsync(Window owner, CalcMode current)
        {
            string[] names = { "COMP", "CMPLX", "STAT", "BASE-N", "EQN", "MATRIX", "VECTOR", "TABLE", "VERIF" };
            string[] descriptions =
            {
                "обычные вычисления",
                "комплексные числа",
                "статистика и регрессия",
                "системы счисления",
                "уравнения",
                "матрицы A/B/C",
                "векторы",
                "таблица значений",
                "проверка выражений"
            };
            CalcMode[] modes =
            {
                CalcMode.Comp, CalcMode.Cmplx, CalcMode.Stat, CalcMode.BaseN, CalcMode.Eqn,
                CalcMode.Matrix, CalcMode.Vector, CalcMode.Table, CalcMode.Verif
            };
            string[] items = new string[names.Length];
            for (int i = 0; i < names.Length; i++) items[i] = names[i] + "   —   " + descriptions[i];

            Window w = MakeWindow("РЕЖИМ РАБОТЫ", 500, 560);
            ListBox list = new ListBox();
            list.ItemsSource = items;
            list.SelectedIndex = Array.IndexOf(modes, current);
            list.FontFamily = UiFont;
            list.FontSize = 13;
            list.Background = FieldBack;
            list.BorderBrush = Line;
            list.BorderThickness = new Thickness(1);
            list.Padding = new Thickness(4);

            CalcMode? result = null;
            Button ok = MakeButton("Выбрать", true);
            Button cancel = MakeButton("Отмена", false);
            ok.Click += delegate
            {
                int i = list.SelectedIndex;
                if (i >= 0 && i < modes.Length) result = modes[i];
                w.Close();
            };
            cancel.Click += delegate { w.Close(); };
            list.DoubleTapped += delegate
            {
                int i = list.SelectedIndex;
                if (i >= 0 && i < modes.Length) result = modes[i];
                w.Close();
            };
            w.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) { ok.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); }
                else if (e.Key == Key.Escape) w.Close();
            };

            DockPanel panel = new DockPanel();
            panel.Margin = new Thickness(16);
            StackPanel buttons = Buttons(ok, cancel);
            DockPanel.SetDock(buttons, Dock.Bottom);
            panel.Children.Add(buttons);
            StackPanel head = new StackPanel();
            head.Children.Add(Caption("Выберите режим калькулятора"));
            DockPanel.SetDock(head, Dock.Top);
            panel.Children.Add(head);
            panel.Children.Add(list);
            w.Content = panel;
            if (owner != null) await w.ShowDialog(owner);
            else w.Show();
            return result;
        }

        public static async Task<bool> SetupAsync(Window owner, CalcState state)
        {
            Window w = MakeWindow("НАСТРОЙКА", 460, 560);

            StackPanel fields = new StackPanel();

            TextBlock angleLabel = new TextBlock();
            angleLabel.Text = "Угловая единица";
            angleLabel.FontFamily = UiFont;
            angleLabel.FontSize = 12.5;
            angleLabel.Foreground = Ink;
            angleLabel.Margin = new Thickness(0, 4, 0, 4);
            fields.Children.Add(angleLabel);

            ComboBox angle = new ComboBox();
            angle.ItemsSource = new string[] { "Градусы (DEG)", "Радианы (RAD)", "Грады (GRAD)" };
            angle.SelectedIndex = state.Angle == AngleUnit.Degrees ? 0 : (state.Angle == AngleUnit.Radians ? 1 : 2);
            angle.HorizontalAlignment = HorizontalAlignment.Stretch;
            angle.Margin = new Thickness(0, 0, 0, 10);
            fields.Children.Add(angle);

            TextBlock formatLabel = new TextBlock();
            formatLabel.Text = "Формат числа";
            formatLabel.FontFamily = UiFont;
            formatLabel.FontSize = 12.5;
            formatLabel.Foreground = Ink;
            formatLabel.Margin = new Thickness(0, 0, 0, 4);
            fields.Children.Add(formatLabel);

            ComboBox format = new ComboBox();
            format.ItemsSource = new string[] { "Norm 1", "Norm 2", "Fix (знаков после запятой)", "Sci (научный)" };
            format.SelectedIndex = (int)state.Format;
            format.HorizontalAlignment = HorizontalAlignment.Stretch;
            format.Margin = new Thickness(0, 0, 0, 10);
            fields.Children.Add(format);

            TextBlock digitsLabel = new TextBlock();
            digitsLabel.Text = "Количество знаков для Fix";
            digitsLabel.FontFamily = UiFont;
            digitsLabel.FontSize = 12.5;
            digitsLabel.Foreground = Ink;
            digitsLabel.Margin = new Thickness(0, 0, 0, 4);
            fields.Children.Add(digitsLabel);

            NumericUpDown digits = new NumericUpDown();
            digits.Minimum = 0;
            digits.Maximum = 9;
            digits.Increment = 1;
            digits.Value = state.Digits;
            digits.HorizontalAlignment = HorizontalAlignment.Left;
            digits.Width = 120;
            digits.Margin = new Thickness(0, 0, 0, 10);
            fields.Children.Add(digits);

            CheckBox fraction = new CheckBox();
            fraction.Content = "Показывать обыкновенные дроби (S⇔D)";
            fraction.IsChecked = state.FractionResult;
            fraction.FontFamily = UiFont;
            fraction.FontSize = 12.5;
            fraction.Margin = new Thickness(0, 2, 0, 10);
            fields.Children.Add(fraction);

            TextBlock complexLabel = new TextBlock();
            complexLabel.Text = "Форма комплексного числа";
            complexLabel.FontFamily = UiFont;
            complexLabel.FontSize = 12.5;
            complexLabel.Foreground = Ink;
            complexLabel.Margin = new Thickness(0, 0, 0, 4);
            fields.Children.Add(complexLabel);

            ComboBox complex = new ComboBox();
            complex.ItemsSource = new string[] { "Прямоугольная (a+bi)", "Полярная (r∠θ)" };
            complex.SelectedIndex = state.ComplexDisplay == ComplexView.Rectangular ? 0 : 1;
            complex.HorizontalAlignment = HorizontalAlignment.Stretch;
            complex.Margin = new Thickness(0, 0, 0, 10);
            fields.Children.Add(complex);

            CheckBox statFrequency = new CheckBox();
            statFrequency.Content = "Частоты в режиме STAT";
            statFrequency.IsChecked = state.StatFrequency;
            statFrequency.FontFamily = UiFont;
            statFrequency.FontSize = 12.5;
            statFrequency.Margin = new Thickness(0, 2, 0, 10);
            fields.Children.Add(statFrequency);

            CheckBox insertMode = new CheckBox();
            insertMode.Content = "Режим вставки (INS)";
            insertMode.IsChecked = state.InsertMode;
            insertMode.FontFamily = UiFont;
            insertMode.FontSize = 12.5;
            insertMode.Margin = new Thickness(0, 2, 0, 4);
            fields.Children.Add(insertMode);

            bool applied = false;
            Button ok = MakeButton("Применить", true);
            Button cancel = MakeButton("Отмена", false);
            ok.Click += delegate
            {
                state.Angle = angle.SelectedIndex == 1 ? AngleUnit.Radians :
                    (angle.SelectedIndex == 2 ? AngleUnit.Gradians : AngleUnit.Degrees);
                state.Format = (NumFormat)Math.Max(0, format.SelectedIndex);
                state.Digits = (int)(digits.Value ?? 2);
                state.FractionResult = fraction.IsChecked ?? false;
                state.ComplexDisplay = complex.SelectedIndex == 1 ? ComplexView.Polar : ComplexView.Rectangular;
                state.StatFrequency = statFrequency.IsChecked ?? false;
                state.InsertMode = insertMode.IsChecked ?? true;
                applied = true;
                w.Close();
            };
            cancel.Click += delegate { w.Close(); };
            w.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) w.Close();
            };

            DockPanel panel = new DockPanel();
            panel.Margin = new Thickness(16);
            StackPanel buttons = Buttons(ok, cancel);
            DockPanel.SetDock(buttons, Dock.Bottom);
            panel.Children.Add(buttons);
            StackPanel head = new StackPanel();
            head.Children.Add(Caption("Параметры вычислений"));
            DockPanel.SetDock(head, Dock.Top);
            panel.Children.Add(head);
            ScrollViewer scroll = new ScrollViewer();
            scroll.Content = fields;
            panel.Children.Add(scroll);
            w.Content = panel;
            if (owner != null) await w.ShowDialog(owner);
            else w.Show();
            return applied;
        }
    }
}
