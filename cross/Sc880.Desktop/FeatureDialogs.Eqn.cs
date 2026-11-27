using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static async Task EqnAsync(Window owner, CalculatorView view)
        {
            Window w = Dlg.Make("EQN — УРАВНЕНИЯ", 800, 580);
            Dlg.Caption(w, "Тип уравнения", 16, 10, 140);
            ComboBox type = Dlg.Combo(w, new string[]
            {
                "Система 2×2", "Система 3×3", "Квадратное уравнение", "Кубическое уравнение"
            }, 0, 150, 8, 220);
            TextBlock hint = Dlg.MutedLabel(w, "", 390, 14, 390);
            ContentControl group = Dlg.Group(w, "Коэффициенты", 16, 46, 768, 160);
            WrapPanel wrap = new WrapPanel();
            wrap.Orientation = Orientation.Horizontal;
            group.Content = wrap;

            TextBox output = Dlg.Text(w, "", 16, 216, 768, 268, true, true, true);
            ScrollViewer.SetVerticalScrollBarVisibility(output, ScrollBarVisibility.Auto);

            List<TextBox> boxes = new List<TextBox>();
            string insertText = null;
            bool insertComplex = false;

            Action buildFields = delegate
            {
                wrap.Children.Clear();
                boxes.Clear();
                int t = Math.Max(0, type.SelectedIndex);
                string[] labels;
                string[] defaults;
                if (t == 0)
                {
                    labels = new string[] { "a₁", "b₁", "c₁", "a₂", "b₂", "c₂" };
                    defaults = new string[] { "1", "1", "5", "1", "−1", "1" };
                    hint.Text = "a₁x + b₁y = c₁;   a₂x + b₂y = c₂";
                }
                else if (t == 1)
                {
                    labels = new string[] { "a₁", "b₁", "c₁", "d₁", "a₂", "b₂", "c₂", "d₂", "a₃", "b₃", "c₃", "d₃" };
                    defaults = new string[] { "1", "1", "1", "6", "1", "−1", "1", "1", "2", "1", "−1", "1" };
                    hint.Text = "a₁x + b₁y + c₁z = d₁;  и так далее для трёх уравнений";
                }
                else if (t == 2)
                {
                    labels = new string[] { "a", "b", "c" };
                    defaults = new string[] { "1", "−3", "2" };
                    hint.Text = "ax² + bx + c = 0";
                }
                else
                {
                    labels = new string[] { "a", "b", "c", "d" };
                    defaults = new string[] { "1", "−6", "11", "−6" };
                    hint.Text = "ax³ + bx² + cx + d = 0";
                }
                for (int i = 0; i < labels.Length; i++)
                {
                    StackPanel p = new StackPanel();
                    p.Orientation = Orientation.Vertical;
                    p.Width = 118;
                    p.Height = 58;
                    p.Margin = new Thickness(2, 0, 2, 2);
                    TextBlock l = new TextBlock();
                    l.Text = labels[i];
                    l.FontFamily = Dlg.UiFont;
                    l.FontSize = 12;
                    l.Foreground = Dlg.Ink;
                    l.Height = 18;
                    p.Children.Add(l);
                    TextBox tb = Dlg.MakeText(defaults[i], 104, 26);
                    tb.Width = 104;
                    tb.Height = 26;
                    p.Children.Add(tb);
                    wrap.Children.Add(p);
                    boxes.Add(tb);
                }
                output.Text = "";
                insertText = null;
            };
            type.SelectionChanged += delegate { buildFields(); };

            Func<double[]> readValues = delegate
            {
                double[] v = new double[boxes.Count];
                for (int i = 0; i < boxes.Count; i++) v[i] = Dlg.Parse(boxes[i].Text);
                return v;
            };

            Action solve = delegate
            {
                try
                {
                    double[] c = readValues();
                    int t = Math.Max(0, type.SelectedIndex);
                    insertText = null;
                    insertComplex = false;
                    output.Text = "";
                    if (t == 0)
                    {
                        double[,] m = new double[,] { { c[0], c[1] }, { c[3], c[4] } };
                        double[] b = new double[] { c[2], c[5] };
                        double[] x = Numerics.SolveLinear(m, b);
                        output.Text = "a₁x + b₁y = c₁\r\na₂x + b₂y = c₂\r\n\r\n" +
                            "x = " + Dlg.Format(x[0]) + "\r\n" +
                            "y = " + Dlg.Format(x[1]);
                        insertText = Dlg.Format(x[0]);
                    }
                    else if (t == 1)
                    {
                        double[,] m = new double[3, 3];
                        double[] b = new double[3];
                        for (int i = 0; i < 3; i++)
                        {
                            m[i, 0] = c[i * 4];
                            m[i, 1] = c[i * 4 + 1];
                            m[i, 2] = c[i * 4 + 2];
                            b[i] = c[i * 4 + 3];
                        }
                        double[] x = Numerics.SolveLinear(m, b);
                        output.Text = "Система 3×3\r\n\r\n" +
                            "x = " + Dlg.Format(x[0]) + "\r\n" +
                            "y = " + Dlg.Format(x[1]) + "\r\n" +
                            "z = " + Dlg.Format(x[2]);
                        insertText = Dlg.Format(x[0]);
                    }
                    else
                    {
                        double[] coeff;
                        if (t == 2)
                        {
                            if (Math.Abs(c[0]) < 1e-14) throw new CalcError("a не может быть 0 для квадратного уравнения");
                            coeff = new double[] { c[0], c[1], c[2] };
                        }
                        else
                        {
                            if (Math.Abs(c[0]) < 1e-14) throw new CalcError("a не может быть 0 для кубического уравнения");
                            coeff = new double[] { c[0], c[1], c[2], c[3] };
                        }
                        Cplx[] roots = Numerics.PolynomialRoots(coeff);
                        output.Text = (t == 2 ? "ax² + bx + c = 0" : "ax³ + bx² + cx + d = 0") + "\r\n\r\n";
                        for (int i = 0; i < roots.Length; i++)
                        {
                            output.Text += "x" + (i + 1) + " = " + FormatRoot(roots[i]) + "\r\n";
                        }
                        Cplx chosen = roots[0];
                        for (int i = 0; i < roots.Length; i++)
                        {
                            if (Math.Abs(roots[i].Im) < 1e-8) { chosen = roots[i]; break; }
                        }
                        insertComplex = Math.Abs(chosen.Im) >= 1e-8;
                        insertText = insertComplex ? ParseRoot(chosen) : Dlg.Format(chosen.Re);
                        output.Text += "\r\nСумма корней: " + FormatRoot(Sum(roots));
                    }
                }
                catch (Exception ex)
                {
                    output.Text = "Ошибка: " + ex.Message;
                    insertText = null;
                }
            };

            Button solveButton = Dlg.Button(w, "Решить", 16, 502, 130, 34, true);
            solveButton.Click += delegate { solve(); };
            Button insert = Dlg.Button(w, "Вставить", 156, 502, 130, 34);
            insert.Click += delegate
            {
                if (string.IsNullOrEmpty(insertText))
                {
                    _ = Dlg.ErrorAsync(w, "Сначала решите уравнение.");
                    return;
                }
                if (insertComplex) view.State.Mode = CalcMode.Cmplx;
                view.InsertTextExternal(insertText);
                w.Close();
            };
            Button close = Dlg.Button(w, "Закрыть", 668, 502, 116, 34);
            close.Click += delegate { w.Close(); };

            buildFields();
            solve();
            await w.ShowDialog(owner);
        }

        private static Cplx Sum(Cplx[] roots)
        {
            Cplx s = Cplx.Zero;
            for (int i = 0; i < roots.Length; i++) s = s + roots[i];
            return s;
        }

        private static string FormatRoot(Cplx z)
        {
            if (Math.Abs(z.Im) < 1e-8) return Statistics.Num(z.Re);
            return Statistics.Num(z.Re) + (z.Im < 0 ? "−" : "+") + Statistics.Num(Math.Abs(z.Im)) + "i";
        }

        private static string ParseRoot(Cplx z)
        {
            if (Math.Abs(z.Im) < 1e-8)
                return z.Re.ToString("G15", CultureInfo.InvariantCulture).Replace('-', '−');
            return "(" + z.Re.ToString("G15", CultureInfo.InvariantCulture).Replace('-', '−') +
                (z.Im < 0 ? "−" : "+") +
                Math.Abs(z.Im).ToString("G15", CultureInfo.InvariantCulture) + "i)";
        }
    }
}
