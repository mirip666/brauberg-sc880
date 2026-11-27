using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace SC880.Desktop
{
    internal static partial class FeatureDialogs
    {
        // ---------------- SOLVE ----------------

        public static async Task SolveAsync(Window owner, CalculatorView view)
        {
            Window w = Dlg.Make("SOLVE — ЧИСЛЕННОЕ РЕШЕНИЕ", 540, 360);
            Dlg.Caption(w, "Уравнение или f(x) = 0", 16, 10, 360);
            Dlg.MutedLabel(w, "Используется переменная X. Можно ввести уравнение со знаком =.", 16, 34, 500);
            TextBox expr = Dlg.Text(w, view.ExpressionText.Length > 0 ? view.ExpressionText : "x²−2", 16, 56, 506);
            Dlg.Label(w, "Начальное приближение X", 16, 90, 200);
            TextBox guess = Dlg.Text(w, "0", 236, 86, 100);
            Button solve = Dlg.Button(w, "Решить", 360, 84, 100, 28, true);
            TextBox output = Dlg.Text(w, "", 16, 126, 506, 120, true, true, true);
            ScrollViewer.SetVerticalScrollBarVisibility(output, ScrollBarVisibility.Auto);

            Cplx insertValue = Cplx.Zero;
            bool hasInsert = false;
            string insertLabel = "";

            Action compute = delegate
            {
                try
                {
                    double g = Dlg.Parse(guess.Text);
                    SolveResult r = Numerics.Solve(expr.Text, view.State, 'X', g);
                    insertValue = Cplx.Real(r.Root);
                    hasInsert = true;
                    insertLabel = expr.Text;
                    output.Text = "x = " + Dlg.Format(r.Root) + "\r\n" +
                        "f(x) = " + Dlg.Format(r.Residual) + "\r\n" +
                        "Метод: " + r.Method + "\r\n" +
                        "Итераций: " + r.Iterations;
                }
                catch (Exception ex)
                {
                    hasInsert = false;
                    output.Text = "Ошибка: " + ex.Message;
                }
            };
            solve.Click += delegate { compute(); };

            Button insert = Dlg.Button(w, "Вставить x", 250, 310, 110, 28);
            insert.Click += delegate
            {
                if (!hasInsert)
                {
                    _ = Dlg.ErrorAsync(w, "Сначала найдите корень.");
                    return;
                }
                view.ShowExternalResult(insertLabel, insertValue);
                w.Close();
            };
            Button close = Dlg.Button(w, "Закрыть", 370, 310, 96, 28);
            close.Click += delegate { w.Close(); };
            w.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) compute();
            };
            compute();
            await w.ShowDialog(owner);
        }

        // ---------------- ИНТЕГРАЛ ----------------

        public static async Task IntegralAsync(Window owner, CalculatorView view)
        {
            Window w = Dlg.Make("ИНТЕГРАЛ", 540, 400);
            Dlg.Caption(w, "f(x)", 16, 10, 200);
            Dlg.MutedLabel(w, "Численное интегрирование методом Симпсона, переменная X.", 16, 34, 500);
            TextBox expr = Dlg.Text(w, view.ExpressionText.Length > 0 ? view.ExpressionText : "sin(x)", 16, 58, 506);
            Dlg.Label(w, "Нижний предел", 16, 96, 120);
            TextBox lower = Dlg.Text(w, "0", 140, 92, 90);
            Dlg.Label(w, "Верхний предел", 250, 96, 120);
            TextBox upper = Dlg.Text(w, "π", 374, 92, 90);
            Dlg.Label(w, "Разбиений", 16, 132, 120);
            TextBox segments = Dlg.Text(w, "1000", 140, 128, 90);
            Button calc = Dlg.Button(w, "Вычислить", 372, 126, 110, 28, true);
            TextBox output = Dlg.Text(w, "", 16, 168, 506, 126, true, true, true);
            ScrollViewer.SetVerticalScrollBarVisibility(output, ScrollBarVisibility.Auto);

            double value = 0;
            bool hasValue = false;

            Action compute = delegate
            {
                try
                {
                    double a = view.CurrentValueFrom(lower.Text).Re;
                    double b = view.CurrentValueFrom(upper.Text).Re;
                    int n = (int)Math.Round(Dlg.Parse(segments.Text));
                    if (n < 2) n = 2;
                    if (n % 2 != 0) n++;
                    value = Numerics.Integral(expr.Text, view.State, 'X', a, b, n);
                    hasValue = true;
                    output.Text = "∫ от " + Dlg.Format(a) + " до " + Dlg.Format(b) + "\r\n" +
                        "f(x) = " + expr.Text + "\r\n" +
                        "I = " + Dlg.Format(value) + "\r\n" +
                        "Разбиений: " + n;
                }
                catch (Exception ex)
                {
                    hasValue = false;
                    output.Text = "Ошибка: " + ex.Message;
                }
            };
            calc.Click += delegate { compute(); };

            Button insert = Dlg.Button(w, "Вставить", 250, 342, 110, 28);
            insert.Click += delegate
            {
                if (!hasValue)
                {
                    _ = Dlg.ErrorAsync(w, "Сначала вычислите интеграл.");
                    return;
                }
                view.ShowExternalResult("∫dx", Cplx.Real(value));
                w.Close();
            };
            Button close = Dlg.Button(w, "Закрыть", 370, 342, 96, 28);
            close.Click += delegate { w.Close(); };
            w.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) compute();
            };
            compute();
            await w.ShowDialog(owner);
        }

        // ---------------- ПРОИЗВОДНАЯ ----------------

        public static async Task DerivativeAsync(Window owner, CalculatorView view)
        {
            Window w = Dlg.Make("ПРОИЗВОДНАЯ d/dx", 520, 340);
            Dlg.Caption(w, "f(x)", 16, 10, 200);
            Dlg.MutedLabel(w, "Численная производная в точке, переменная X.", 16, 34, 480);
            TextBox expr = Dlg.Text(w, view.ExpressionText.Length > 0 ? view.ExpressionText : "sin(x)", 16, 58, 486);
            Dlg.Label(w, "Точка x", 16, 96, 100);
            TextBox point = Dlg.Text(w, "0", 120, 92, 100);
            Button calc = Dlg.Button(w, "Вычислить", 360, 90, 110, 28, true);
            TextBox output = Dlg.Text(w, "", 16, 132, 486, 118, true, true, true);
            ScrollViewer.SetVerticalScrollBarVisibility(output, ScrollBarVisibility.Auto);

            double value = 0;
            bool hasValue = false;

            Action compute = delegate
            {
                try
                {
                    double x = view.CurrentValueFrom(point.Text).Re;
                    value = Numerics.Derivative(expr.Text, view.State, 'X', x);
                    hasValue = true;
                    output.Text = "d/dx " + expr.Text + "  при x = " + Dlg.Format(x) + "\r\n" +
                        "f′(x) = " + Dlg.Format(value);
                }
                catch (Exception ex)
                {
                    hasValue = false;
                    output.Text = "Ошибка: " + ex.Message;
                }
            };
            calc.Click += delegate { compute(); };

            Button insert = Dlg.Button(w, "Вставить", 250, 284, 110, 28);
            insert.Click += delegate
            {
                if (!hasValue)
                {
                    _ = Dlg.ErrorAsync(w, "Сначала вычислите производную.");
                    return;
                }
                view.ShowExternalResult("d/dx", Cplx.Real(value));
                w.Close();
            };
            Button close = Dlg.Button(w, "Закрыть", 370, 284, 96, 28);
            close.Click += delegate { w.Close(); };
            w.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) compute();
            };
            compute();
            await w.ShowDialog(owner);
        }

        // ---------------- СУММА ----------------

        public static async Task SumAsync(Window owner, CalculatorView view)
        {
            Window w = Dlg.Make("СУММА Σ", 520, 340);
            Dlg.Caption(w, "f(x)", 16, 10, 200);
            Dlg.MutedLabel(w, "Сумма f(x) для целых x от нижней до верхней границы.", 16, 34, 480);
            TextBox expr = Dlg.Text(w, view.ExpressionText.Length > 0 ? view.ExpressionText : "x", 16, 58, 486);
            Dlg.Label(w, "От", 16, 96, 40);
            TextBox from = Dlg.Text(w, "1", 56, 92, 90);
            Dlg.Label(w, "До", 172, 96, 40);
            TextBox to = Dlg.Text(w, "10", 212, 92, 90);
            Button calc = Dlg.Button(w, "Вычислить", 360, 90, 110, 28, true);
            TextBox output = Dlg.Text(w, "", 16, 132, 486, 118, true, true, true);
            ScrollViewer.SetVerticalScrollBarVisibility(output, ScrollBarVisibility.Auto);

            double value = 0;
            bool hasValue = false;

            Action compute = delegate
            {
                try
                {
                    double a = view.CurrentValueFrom(from.Text).Re;
                    double b = view.CurrentValueFrom(to.Text).Re;
                    value = Numerics.Sum(expr.Text, view.State, 'X', a, b);
                    hasValue = true;
                    output.Text = "Σ " + expr.Text + "  от " + Dlg.Format(a) + " до " + Dlg.Format(b) + "\r\n" +
                        "S = " + Dlg.Format(value);
                }
                catch (Exception ex)
                {
                    hasValue = false;
                    output.Text = "Ошибка: " + ex.Message;
                }
            };
            calc.Click += delegate { compute(); };

            Button insert = Dlg.Button(w, "Вставить", 250, 284, 110, 28);
            insert.Click += delegate
            {
                if (!hasValue)
                {
                    _ = Dlg.ErrorAsync(w, "Сначала вычислите сумму.");
                    return;
                }
                view.ShowExternalResult("Σ", Cplx.Real(value));
                w.Close();
            };
            Button close = Dlg.Button(w, "Закрыть", 370, 284, 96, 28);
            close.Click += delegate { w.Close(); };
            w.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) compute();
            };
            compute();
            await w.ShowDialog(owner);
        }

        // ---------------- ТАБЛИЦА ----------------

        public static async Task TableAsync(Window owner, CalculatorView view)
        {
            Window w = Dlg.Make("ТАБЛИЦА ЗНАЧЕНИЙ", 540, 570);
            Dlg.Caption(w, "f(x)", 16, 10, 100);
            TextBox expr = Dlg.Text(w, view.ExpressionText.IndexOf('X') >= 0 ? view.ExpressionText : "x²", 16, 36, 508);
            Dlg.Label(w, "Начало", 16, 74, 60);
            TextBox start = Dlg.Text(w, "0", 72, 70, 80);
            Dlg.Label(w, "Конец", 170, 74, 60);
            TextBox end = Dlg.Text(w, "10", 226, 70, 80);
            Dlg.Label(w, "Шаг", 324, 74, 40);
            TextBox step = Dlg.Text(w, "1", 364, 70, 70);
            Button build = Dlg.Button(w, "Построить", 436, 68, 88, 28, true);
            TextBox output = Dlg.Text(w, "", 16, 108, 508, 388, true, true, true);
            ScrollViewer.SetVerticalScrollBarVisibility(output, ScrollBarVisibility.Auto);

            Action buildTable = delegate
            {
                try
                {
                    double a = view.CurrentValueFrom(start.Text).Re;
                    double b = view.CurrentValueFrom(end.Text).Re;
                    double h = view.CurrentValueFrom(step.Text).Re;
                    if (h == 0) throw new CalcError("Шаг не может быть нулём");
                    if ((b - a) / h > 400) throw new CalcError("Слишком много строк (макс. 400)");
                    System.Text.StringBuilder text = new System.Text.StringBuilder();
                    text.Append("x").Append('\t').Append("f(x)").Append("\r\n");
                    text.Append("————————————————————————").Append("\r\n");
                    for (double x = a; (h > 0 ? x <= b + 1e-12 : x >= b - 1e-12); x += h)
                    {
                        string fx;
                        try
                        {
                            double v = Numerics.EvalReal(expr.Text, view.State, 'X', x);
                            fx = Dlg.Format(v);
                        }
                        catch
                        {
                            fx = "ERROR";
                        }
                        text.Append(Dlg.Format(x)).Append("\t\t").Append(fx).Append("\r\n");
                    }
                    output.Text = text.ToString();
                }
                catch (Exception ex)
                {
                    _ = Dlg.ErrorAsync(w, ex.Message);
                }
            };
            build.Click += delegate { buildTable(); };

            Button close = Dlg.Button(w, "Закрыть", 428, 510, 96, 28);
            close.Click += delegate { w.Close(); };
            w.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) buildTable();
            };
            buildTable();
            await w.ShowDialog(owner);
        }

        // ---------------- КОНСТАНТЫ ----------------

        public static async Task ConstantsAsync(Window owner, CalculatorView view)
        {
            Window w = Dlg.Make("ФИЗИЧЕСКИЕ ПОСТОЯННЫЕ (CONST)", 620, 490);
            Dlg.Caption(w, "Выберите постоянную", 16, 10, 300);
            string[] items = new string[ConstantsData.Items.Count];
            for (int i = 0; i < items.Length; i++)
            {
                ConstantItem c = ConstantsData.Items[i];
                items[i] = c.Name + "  (" + c.Symbol + ")";
            }
            ListBox list = Dlg.List(w, items, 0, 16, 40, 360, 388);
            TextBox details = Dlg.Text(w, "", 392, 40, 212, 320, true, true, true);
            ScrollViewer.SetVerticalScrollBarVisibility(details, ScrollBarVisibility.Auto);

            Action show = delegate
            {
                if (list.SelectedIndex < 0) return;
                ConstantItem c = ConstantsData.Items[list.SelectedIndex];
                details.Text = c.Name + "\r\n\r\n" + c.Symbol + " = " + c.ValueText + "\r\n" + c.Unit;
            };
            list.SelectionChanged += delegate { show(); };
            list.DoubleTapped += delegate
            {
                if (list.SelectedIndex < 0) return;
                view.InsertTextExternal(ConstantsData.Items[list.SelectedIndex].ValueText);
                w.Close();
            };

            Button insert = Dlg.Button(w, "Вставить", 392, 374, 100, 30, true);
            insert.Click += delegate
            {
                if (list.SelectedIndex < 0) return;
                view.InsertTextExternal(ConstantsData.Items[list.SelectedIndex].ValueText);
                w.Close();
            };
            Button copy = Dlg.Button(w, "Копировать", 500, 374, 104, 30);
            copy.Click += delegate
            {
                if (list.SelectedIndex < 0) return;
                try
                {
                    TopLevel top = TopLevel.GetTopLevel(w);
                    if (top != null && top.Clipboard != null)
                        _ = top.Clipboard.SetTextAsync(ConstantsData.Items[list.SelectedIndex].ValueText);
                }
                catch
                {
                }
            };
            Button close = Dlg.Button(w, "Закрыть", 500, 414, 104, 28);
            close.Click += delegate { w.Close(); };
            show();
            await w.ShowDialog(owner);
        }

        // ---------------- ПЕРЕВОД ЕДИНИЦ ----------------

        public static async Task ConversionAsync(Window owner, CalculatorView view)
        {
            Window w = Dlg.Make("ПЕРЕВОД ЕДИНИЦ (CONV)", 560, 346);
            Dlg.Caption(w, "Категория", 16, 10, 200);
            string[] cats = new string[UnitsData.Categories.Count];
            for (int i = 0; i < cats.Length; i++) cats[i] = UnitsData.Categories[i].Name;
            ComboBox category = Dlg.Combo(w, cats, 0, 16, 36, 520);
            Dlg.Label(w, "Из", 16, 78, 40);
            ComboBox from = Dlg.Combo(w, new string[0], -1, 56, 74, 200);
            Dlg.Label(w, "В", 276, 78, 30);
            ComboBox to = Dlg.Combo(w, new string[0], -1, 310, 74, 200);
            Dlg.Label(w, "Значение", 16, 116, 90);
            TextBox valueBox = Dlg.Text(w, "1", 110, 112, 160);
            TextBlock result = Dlg.Label(w, "", 288, 110, 246, 30);
            result.FontFamily = Dlg.MonoFont;
            result.FontSize = 16;
            result.FontWeight = FontWeight.Bold;
            result.TextAlignment = TextAlignment.Right;
            result.HorizontalAlignment = HorizontalAlignment.Stretch;
            Dlg.MutedLabel(w, "Температура переводится с учётом смещения шкалы.", 16, 154, 500);

            bool filling = false;
            Action recalc = delegate
            {
                if (filling) return;
                try
                {
                    if (from.SelectedIndex < 0 || to.SelectedIndex < 0) return;
                    UnitCategory c = UnitsData.Categories[category.SelectedIndex];
                    double v = Dlg.Parse(valueBox.Text);
                    double r = UnitsData.Convert(c, from.SelectedIndex, to.SelectedIndex, v);
                    result.Text = Dlg.Format(r);
                }
                catch
                {
                    result.Text = "—";
                }
            };
            Action fill = delegate
            {
                filling = true;
                UnitCategory c = UnitsData.Categories[category.SelectedIndex];
                string[] units = new string[c.Units.Count];
                for (int i = 0; i < units.Length; i++) units[i] = c.Units[i].Name;
                from.ItemsSource = units;
                to.ItemsSource = units;
                from.SelectedIndex = 0;
                to.SelectedIndex = Math.Min(1, units.Length - 1);
                filling = false;
                recalc();
            };
            category.SelectionChanged += delegate { fill(); };
            from.SelectionChanged += delegate { recalc(); };
            to.SelectionChanged += delegate { recalc(); };
            valueBox.TextChanged += delegate { recalc(); };

            Button insert = Dlg.Button(w, "Вставить результат", 330, 292, 150, 30, true);
            insert.Click += delegate
            {
                try
                {
                    UnitCategory c = UnitsData.Categories[category.SelectedIndex];
                    double v = Dlg.Parse(valueBox.Text);
                    double r = UnitsData.Convert(c, from.SelectedIndex, to.SelectedIndex, v);
                    view.InsertTextExternal(Dlg.Format(r));
                    w.Close();
                }
                catch (Exception ex)
                {
                    _ = Dlg.ErrorAsync(w, ex.Message);
                }
            };
            Button close = Dlg.Button(w, "Закрыть", 486, 292, 70, 30);
            close.Click += delegate { w.Close(); };

            fill();
            if (view.HasLastValue && Math.Abs(view.LastValue.Im) < 1e-12)
                valueBox.Text = Dlg.Format(view.LastValue.Re);
            await w.ShowDialog(owner);
        }

        // ---------------- DRG ----------------

        public static async Task AngleConvertAsync(Window owner, CalculatorView view)
        {
            Window w = Dlg.Make("DRG▶ — ПЕРЕВОД УГЛОВ", 440, 268);
            Dlg.Caption(w, "Перевод значения между DEG, RAD и GRAD", 16, 10, 400);
            string[] units = { "DEG", "RAD", "GRAD" };
            int startIndex = view.State.Angle == AngleUnit.Degrees ? 0 : (view.State.Angle == AngleUnit.Radians ? 1 : 2);
            ComboBox from = Dlg.Combo(w, units, startIndex, 16, 50, 120);
            Dlg.Label(w, "→", 148, 54, 24);
            ComboBox to = Dlg.Combo(w, units, (startIndex + 1) % 3, 176, 50, 120);
            Dlg.Label(w, "Значение", 16, 94, 80);
            TextBox valueBox = Dlg.Text(w, view.HasLastValue && Math.Abs(view.LastValue.Im) < 1e-12
                ? Dlg.Format(view.LastValue.Re) : "90", 100, 90, 196);
            TextBlock result = Dlg.Label(w, "", 16, 128, 404, 34);
            result.FontFamily = Dlg.MonoFont;
            result.FontSize = 18;
            result.FontWeight = FontWeight.Bold;
            result.TextAlignment = TextAlignment.Right;
            result.HorizontalAlignment = HorizontalAlignment.Stretch;

            Func<int, AngleUnit> unit = i =>
            {
                if (i == 1) return AngleUnit.Radians;
                if (i == 2) return AngleUnit.Gradians;
                return AngleUnit.Degrees;
            };
            Action recalc = delegate
            {
                try
                {
                    double v = Dlg.Parse(valueBox.Text);
                    double r = Numerics.ConvertAngle(v, unit(from.SelectedIndex), unit(to.SelectedIndex));
                    result.Text = Dlg.Format(r);
                }
                catch
                {
                    result.Text = "—";
                }
            };
            from.SelectionChanged += delegate { recalc(); };
            to.SelectionChanged += delegate { recalc(); };
            valueBox.TextChanged += delegate { recalc(); };

            Button insert = Dlg.Button(w, "Вставить", 242, 210, 100, 30, true);
            insert.Click += delegate
            {
                try
                {
                    double v = Dlg.Parse(valueBox.Text);
                    double r = Numerics.ConvertAngle(v, unit(from.SelectedIndex), unit(to.SelectedIndex));
                    view.InsertTextExternal(Dlg.Format(r));
                    w.Close();
                }
                catch (Exception ex)
                {
                    _ = Dlg.ErrorAsync(w, ex.Message);
                }
            };
            Button close = Dlg.Button(w, "Закрыть", 350, 210, 70, 30);
            close.Click += delegate { w.Close(); };

            recalc();
            await w.ShowDialog(owner);
        }

        // ---------------- VERIF ----------------

        public static async Task VerifyAsync(Window owner, CalculatorView view)
        {
            Window w = Dlg.Make("VERIF — ПРОВЕРКА", 560, 330);
            Dlg.Caption(w, "Введите равенство или неравенство", 16, 10, 400);
            Dlg.MutedLabel(w, "Поддерживаются =, ≠, <, >, ≤, ≥. Пример: 2+2=4.", 16, 34, 520);
            TextBox expr = Dlg.Text(w, view.ExpressionText.Length > 0 ? view.ExpressionText : "2+2=4", 16, 58, 528);
            Button check = Dlg.Button(w, "Проверить", 404, 92, 140, 30, true);
            TextBlock verdict = Dlg.Label(w, "", 16, 138, 528, 44);
            verdict.FontFamily = Dlg.UiFont;
            verdict.FontSize = 22;
            verdict.FontWeight = FontWeight.Bold;
            verdict.TextAlignment = TextAlignment.Center;
            verdict.HorizontalAlignment = HorizontalAlignment.Stretch;
            TextBox output = Dlg.Text(w, "", 16, 190, 528, 54, true, true, true);

            Action verify = delegate
            {
                try
                {
                    VerifyResult r = Numerics.Verify(expr.Text, view.State);
                    verdict.Text = r.IsTrue ? "ВЕРНО" : "НЕВЕРНО";
                    verdict.Foreground = new SolidColorBrush(r.IsTrue ? Color.FromRgb(30, 120, 60) : Color.FromRgb(186, 46, 32));
                    output.Text = "Левая часть: " + Dlg.Format(r.Left) +
                        "    Правая часть: " + Dlg.Format(r.Right) +
                        "    Разность: " + Dlg.Format(r.Difference);
                }
                catch (Exception ex)
                {
                    verdict.Text = "ОШИБКА";
                    verdict.Foreground = Dlg.Accent;
                    output.Text = ex.Message;
                }
            };
            check.Click += delegate { verify(); };
            Button close = Dlg.Button(w, "Закрыть", 448, 266, 96, 28);
            close.Click += delegate { w.Close(); };
            w.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) verify();
            };
            verify();
            await w.ShowDialog(owner);
        }
    }
}
