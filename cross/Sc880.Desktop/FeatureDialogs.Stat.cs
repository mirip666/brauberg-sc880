using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace SC880.Desktop
{
    internal static partial class FeatureDialogs
    {
        private sealed class StatRow
        {
            public StackPanel Panel;
            public TextBox X;
            public TextBox Y;
            public TextBox F;
        }

        public static async Task StatAsync(Window owner, CalculatorView view)
        {
            Window w = Dlg.Make("STAT — СТАТИСТИКА", 800, 630);
            RadioButton oneVar = Dlg.Radio(w, "1 переменная", true, 16, 14, 120, "statVars");
            RadioButton twoVar = Dlg.Radio(w, "2 переменные", false, 140, 14, 120, "statVars");
            CheckBox freq = Dlg.Check(w, "Частота (Freq)", view.State.StatFrequency, 270, 14, 150);
            TextBlock regressionLabel = Dlg.Label(w, "Регрессия", 440, 15, 70);
            ComboBox regression = Dlg.Combo(w, new string[]
            {
                "Линейная", "Квадратичная", "Логарифмическая",
                "Экспоненциальная", "Степенная", "Обратная"
            }, 0, 512, 11, 160);
            regression.IsEnabled = false;

            ScrollViewer scroll = Dlg.Scroll(w, 16, 46, 460, 490);
            StackPanel rowsPanel = new StackPanel();
            rowsPanel.Orientation = Orientation.Vertical;
            rowsPanel.Spacing = 4;
            scroll.Content = rowsPanel;
            List<StatRow> rows = new List<StatRow>();

            TextBox report = Dlg.Text(w, "", 492, 46, 292, 528, true, true, true);
            ScrollViewer.SetVerticalScrollBarVisibility(report, ScrollBarVisibility.Auto);

            Action updateColumns = null;
            Action<StatRow> addRow = delegate(StatRow row)
            {
                StackPanel h = new StackPanel();
                h.Orientation = Orientation.Horizontal;
                h.Spacing = 4;
                row.Panel = h;
                row.X = Dlg.MakeText("", 136, 26);
                row.X.Width = 136;
                row.X.Height = 26;
                row.Y = Dlg.MakeText("", 136, 26);
                row.Y.Width = 136;
                row.Y.Height = 26;
                row.F = Dlg.MakeText("1", 136, 26);
                row.F.Width = 136;
                row.F.Height = 26;
                Button del = Dlg.MakeButton("×", 26, 26);
                h.Children.Add(row.X);
                h.Children.Add(row.Y);
                h.Children.Add(row.F);
                h.Children.Add(del);
                del.Click += delegate
                {
                    rows.Remove(row);
                    rowsPanel.Children.Remove(h);
                };
                rows.Add(row);
                rowsPanel.Children.Add(h);
                if (updateColumns != null) updateColumns();
            };

            updateColumns = delegate
            {
                bool two = twoVar.IsChecked == true;
                bool useFreq = freq.IsChecked == true;
                for (int i = 0; i < rows.Count; i++)
                {
                    rows[i].Y.IsVisible = two;
                    rows[i].F.IsVisible = useFreq;
                }
                regression.IsEnabled = two;
                regressionLabel.IsEnabled = two;
            };
            oneVar.IsCheckedChanged += delegate { updateColumns(); };
            twoVar.IsCheckedChanged += delegate { updateColumns(); };
            freq.IsCheckedChanged += delegate { updateColumns(); };

            Action load = delegate
            {
                rows.Clear();
                rowsPanel.Children.Clear();
                if (view.StatData.Count == 0)
                {
                    for (int i = 0; i < 3; i++) addRow(new StatRow());
                }
                else
                {
                    for (int i = 0; i < view.StatData.Count; i++)
                    {
                        StatPoint p = view.StatData[i];
                        StatRow r = new StatRow();
                        addRow(r);
                        r.X.Text = Dlg.Format(p.X);
                        r.Y.Text = Dlg.Format(p.Y);
                        r.F.Text = Dlg.Format(p.Freq);
                    }
                }
            };
            load();

            Button add = Dlg.Button(w, "+ Строка", 16, 548, 92, 28);
            add.Click += delegate { addRow(new StatRow()); };
            Button remove = Dlg.Button(w, "Удалить", 116, 548, 92, 28);
            remove.Click += delegate
            {
                if (rows.Count == 0) return;
                rowsPanel.Children.Remove(rows[rows.Count - 1].Panel);
                rows.RemoveAt(rows.Count - 1);
            };
            Button clear = Dlg.Button(w, "Очистить", 216, 548, 92, 28);
            clear.Click += delegate
            {
                view.StatData.Clear();
                report.Text = "";
                load();
            };

            Func<int, bool> capture = delegate(int notifyRow)
            {
                List<StatPoint> data = new List<StatPoint>();
                bool two = twoVar.IsChecked == true;
                bool useFreq = freq.IsChecked == true;
                for (int i = 0; i < rows.Count; i++)
                {
                    string xs = rows[i].X.Text;
                    string ys = rows[i].Y.Text;
                    string fs = rows[i].F.Text;
                    if (string.IsNullOrEmpty(xs) && string.IsNullOrEmpty(ys)) continue;
                    try
                    {
                        StatPoint p = new StatPoint();
                        p.X = Dlg.Parse(xs);
                        p.Y = two ? Dlg.Parse(ys) : 0;
                        p.Freq = useFreq ? Dlg.Parse(fs) : 1;
                        if (p.Freq <= 0) throw new FormatException("Частота должна быть > 0");
                        data.Add(p);
                    }
                    catch (Exception ex)
                    {
                        _ = Dlg.ErrorAsync(w, "Строка " + (i + 1) + ": " + ex.Message);
                        return false;
                    }
                }
                view.StatData.Clear();
                view.StatData.AddRange(data);
                view.State.StatFrequency = useFreq;
                return true;
            };

            Action compute = delegate
            {
                if (!capture(0)) return;
                try
                {
                    bool two = twoVar.IsChecked == true;
                    bool useFreq = freq.IsChecked == true;
                    RegressionType type = (RegressionType)Math.Max(0, regression.SelectedIndex);
                    StatResult r = Statistics.Compute(view.StatData, two, useFreq, type);
                    report.Text = Statistics.Report(r, two, useFreq);
                }
                catch (Exception ex)
                {
                    report.Text = "Ошибка: " + ex.Message;
                }
            };
            Button calc = Dlg.Button(w, "Вычислить", 316, 548, 160, 28, true);
            calc.Click += delegate { compute(); };

            Button close = Dlg.Button(w, "Закрыть", 684, 588, 100, 26);
            close.Click += delegate
            {
                capture(0);
                w.Close();
            };

            await w.ShowDialog(owner);
        }
    }
}
