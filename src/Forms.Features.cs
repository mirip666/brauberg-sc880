using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace SC880
{
    internal sealed class SolveForm : Form
    {
        private readonly Sc880Form owner;
        private readonly TextBox expr;
        private readonly TextBox guess;
        private readonly TextBox output;
        private Cplx insertValue;
        private bool hasInsert;
        private string insertLabel = "";

        public SolveForm(Sc880Form owner)
        {
            this.owner = owner;
            UiKit.Style(this, "SOLVE — ЧИСЛЕННОЕ РЕШЕНИЕ", 540, 356);
            Label cap = UiKit.Caption("Уравнение или f(x) = 0", 16, 12, 360);
            Controls.Add(cap);
            Label sub = UiKit.MutedLabel("Используется переменная X. Можно ввести уравнение со знаком =.", 16, 30, 500);
            Controls.Add(sub);
            expr = UiKit.TextBox(owner.ExpressionText.Length > 0 ? owner.ExpressionText : "x²−2", 16, 54, 506);
            Controls.Add(expr);
            Label gl = UiKit.Label("Начальное приближение X", 16, 88, 200);
            Controls.Add(gl);
            guess = UiKit.TextBox("0", 236, 84, 100);
            Controls.Add(guess);
            Button solve = UiKit.AccentButton("Решить", 360, 82, 100, 28);
            solve.Click += delegate { Compute(); };
            Controls.Add(solve);
            output = UiKit.TextBox("", 16, 126, 506);
            output.Multiline = true;
            output.ReadOnly = true;
            output.ScrollBars = ScrollBars.Vertical;
            output.Height = 120;
            Controls.Add(output);
            Button insert = UiKit.Button("Вставить x", 250, 308, 110, 28);
            insert.Click += delegate { Insert(); };
            Controls.Add(insert);
            Button close = UiKit.Button("Закрыть", 370, 308, 96, 28);
            close.DialogResult = DialogResult.Cancel;
            Controls.Add(close);
            CancelButton = close;
            AcceptButton = solve;
            Compute();
        }

        private void Compute()
        {
            try
            {
                double g = UiKit.ParseNumber(guess.Text);
                SolveResult r = Numerics.Solve(expr.Text, owner.State, 'X', g);
                insertValue = Cplx.Real(r.Root);
                hasInsert = true;
                insertLabel = expr.Text;
                output.Text = "x = " + UiKit.FormatNumber(r.Root) + "\r\n" +
                    "f(x) = " + UiKit.FormatNumber(r.Residual) + "\r\n" +
                    "Метод: " + r.Method + "\r\n" +
                    "Итераций: " + r.Iterations;
            }
            catch (Exception ex)
            {
                hasInsert = false;
                output.Text = "Ошибка: " + ex.Message;
            }
        }

        private void Insert()
        {
            if (!hasInsert)
            {
                UiKit.Error(this, "Сначала найдите корень.");
                return;
            }
            owner.ShowExternalResult(insertLabel, insertValue);
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal sealed class IntegralForm : Form
    {
        private readonly Sc880Form owner;
        private readonly TextBox expr;
        private readonly TextBox lower;
        private readonly TextBox upper;
        private readonly TextBox segments;
        private readonly TextBox output;
        private double value;
        private bool hasValue;

        public IntegralForm(Sc880Form owner)
        {
            this.owner = owner;
            UiKit.Style(this, "ИНТЕГРАЛ", 540, 392);
            Controls.Add(UiKit.Caption("f(x)", 16, 12, 200));
            Controls.Add(UiKit.MutedLabel("Численное интегрирование методом Симпсона, переменная X.", 16, 32, 500));
            expr = UiKit.TextBox(owner.ExpressionText.Length > 0 ? owner.ExpressionText : "sin(x)", 16, 56, 506);
            Controls.Add(expr);
            Controls.Add(UiKit.Label("Нижний предел", 16, 94, 120));
            lower = UiKit.TextBox("0", 140, 90, 90);
            Controls.Add(lower);
            Controls.Add(UiKit.Label("Верхний предел", 250, 94, 120));
            upper = UiKit.TextBox("π", 374, 90, 90);
            Controls.Add(upper);
            Controls.Add(UiKit.Label("Разбиений", 16, 130, 120));
            segments = UiKit.TextBox("1000", 140, 126, 90);
            Controls.Add(segments);
            Button calc = UiKit.AccentButton("Вычислить", 372, 124, 110, 28);
            calc.Click += delegate { Compute(); };
            Controls.Add(calc);
            output = UiKit.TextBox("", 16, 168, 506);
            output.Multiline = true;
            output.ReadOnly = true;
            output.Height = 118;
            output.ScrollBars = ScrollBars.Vertical;
            Controls.Add(output);
            Button insert = UiKit.Button("Вставить", 250, 338, 110, 28);
            insert.Click += delegate { Insert(); };
            Controls.Add(insert);
            Button close = UiKit.Button("Закрыть", 370, 338, 96, 28);
            close.DialogResult = DialogResult.Cancel;
            Controls.Add(close);
            CancelButton = close;
            AcceptButton = calc;
            Compute();
        }

        private void Compute()
        {
            try
            {
                double a = EvalArg(lower.Text);
                double b = EvalArg(upper.Text);
                int n = (int)Math.Round(UiKit.ParseNumber(segments.Text));
                if (n < 2) n = 2;
                if (n % 2 != 0) n++;
                value = Numerics.Integral(expr.Text, owner.State, 'X', a, b, n);
                hasValue = true;
                output.Text = "∫ от " + UiKit.FormatNumber(a) + " до " + UiKit.FormatNumber(b) + "\r\n" +
                    "f(x) = " + expr.Text + "\r\n" +
                    "I = " + UiKit.FormatNumber(value) + "\r\n" +
                    "Разбиений: " + n;
            }
            catch (Exception ex)
            {
                hasValue = false;
                output.Text = "Ошибка: " + ex.Message;
            }
        }

        private double EvalArg(string text)
        {
            Cplx v = owner.CurrentValueFrom(text);
            return v.Re;
        }

        private void Insert()
        {
            if (!hasValue)
            {
                UiKit.Error(this, "Сначала вычислите интеграл.");
                return;
            }
            owner.ShowExternalResult("∫dx", Cplx.Real(value));
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal sealed class DerivativeForm : Form
    {
        private readonly Sc880Form owner;
        private readonly TextBox expr;
        private readonly TextBox point;
        private readonly TextBox output;
        private double value;
        private bool hasValue;

        public DerivativeForm(Sc880Form owner)
        {
            this.owner = owner;
            UiKit.Style(this, "ПРОИЗВОДНАЯ d/dx", 520, 336);
            Controls.Add(UiKit.Caption("f(x)", 16, 12, 200));
            Controls.Add(UiKit.MutedLabel("Численная производная в точке, переменная X.", 16, 32, 480));
            expr = UiKit.TextBox(owner.ExpressionText.Length > 0 ? owner.ExpressionText : "sin(x)", 16, 56, 486);
            Controls.Add(expr);
            Controls.Add(UiKit.Label("Точка x", 16, 94, 100));
            point = UiKit.TextBox("0", 120, 90, 100);
            Controls.Add(point);
            Button calc = UiKit.AccentButton("Вычислить", 360, 88, 110, 28);
            calc.Click += delegate { Compute(); };
            Controls.Add(calc);
            output = UiKit.TextBox("", 16, 132, 486);
            output.Multiline = true;
            output.ReadOnly = true;
            output.Height = 118;
            Controls.Add(output);
            Button insert = UiKit.Button("Вставить", 250, 282, 110, 28);
            insert.Click += delegate { Insert(); };
            Controls.Add(insert);
            Button close = UiKit.Button("Закрыть", 370, 282, 96, 28);
            close.DialogResult = DialogResult.Cancel;
            Controls.Add(close);
            CancelButton = close;
            AcceptButton = calc;
            Compute();
        }

        private void Compute()
        {
            try
            {
                double x = owner.CurrentValueFrom(point.Text).Re;
                value = Numerics.Derivative(expr.Text, owner.State, 'X', x);
                hasValue = true;
                output.Text = "d/dx " + expr.Text + "  при x = " + UiKit.FormatNumber(x) + "\r\n" +
                    "f′(x) = " + UiKit.FormatNumber(value);
            }
            catch (Exception ex)
            {
                hasValue = false;
                output.Text = "Ошибка: " + ex.Message;
            }
        }

        private void Insert()
        {
            if (!hasValue)
            {
                UiKit.Error(this, "Сначала вычислите производную.");
                return;
            }
            owner.ShowExternalResult("d/dx", Cplx.Real(value));
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal sealed class SumForm : Form
    {
        private readonly Sc880Form owner;
        private readonly TextBox expr;
        private readonly TextBox from;
        private readonly TextBox to;
        private readonly TextBox output;
        private double value;
        private bool hasValue;

        public SumForm(Sc880Form owner)
        {
            this.owner = owner;
            UiKit.Style(this, "СУММА Σ", 520, 336);
            Controls.Add(UiKit.Caption("f(x)", 16, 12, 200));
            Controls.Add(UiKit.MutedLabel("Сумма f(x) для целых x от нижней до верхней границы.", 16, 32, 480));
            expr = UiKit.TextBox(owner.ExpressionText.Length > 0 ? owner.ExpressionText : "x", 16, 56, 486);
            Controls.Add(expr);
            Controls.Add(UiKit.Label("От", 16, 94, 40));
            from = UiKit.TextBox("1", 56, 90, 90);
            Controls.Add(from);
            Controls.Add(UiKit.Label("До", 172, 94, 40));
            to = UiKit.TextBox("10", 212, 90, 90);
            Controls.Add(to);
            Button calc = UiKit.AccentButton("Вычислить", 360, 88, 110, 28);
            calc.Click += delegate { Compute(); };
            Controls.Add(calc);
            output = UiKit.TextBox("", 16, 132, 486);
            output.Multiline = true;
            output.ReadOnly = true;
            output.Height = 118;
            Controls.Add(output);
            Button insert = UiKit.Button("Вставить", 250, 282, 110, 28);
            insert.Click += delegate { Insert(); };
            Controls.Add(insert);
            Button close = UiKit.Button("Закрыть", 370, 282, 96, 28);
            close.DialogResult = DialogResult.Cancel;
            Controls.Add(close);
            CancelButton = close;
            AcceptButton = calc;
            Compute();
        }

        private void Compute()
        {
            try
            {
                double a = owner.CurrentValueFrom(from.Text).Re;
                double b = owner.CurrentValueFrom(to.Text).Re;
                value = Numerics.Sum(expr.Text, owner.State, 'X', a, b);
                hasValue = true;
                output.Text = "Σ " + expr.Text + "  от " + UiKit.FormatNumber(a) + " до " + UiKit.FormatNumber(b) + "\r\n" +
                    "S = " + UiKit.FormatNumber(value);
            }
            catch (Exception ex)
            {
                hasValue = false;
                output.Text = "Ошибка: " + ex.Message;
            }
        }

        private void Insert()
        {
            if (!hasValue)
            {
                UiKit.Error(this, "Сначала вычислите сумму.");
                return;
            }
            owner.ShowExternalResult("Σ", Cplx.Real(value));
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal sealed class TableForm : Form
    {
        private readonly Sc880Form owner;
        private readonly TextBox expr;
        private readonly TextBox start;
        private readonly TextBox end;
        private readonly TextBox step;
        private readonly DataGridView grid;

        public TableForm(Sc880Form owner)
        {
            this.owner = owner;
            UiKit.Style(this, "ТАБЛИЦА ЗНАЧЕНИЙ", 540, 560);
            Controls.Add(UiKit.Caption("f(x)", 16, 12, 100));
            expr = UiKit.TextBox(owner.ExpressionText.IndexOf('X') >= 0 ? owner.ExpressionText : "x²", 16, 34, 508);
            Controls.Add(expr);
            Controls.Add(UiKit.Label("Начало", 16, 72, 60));
            start = UiKit.TextBox("0", 72, 68, 80);
            Controls.Add(start);
            Controls.Add(UiKit.Label("Конец", 170, 72, 60));
            end = UiKit.TextBox("10", 226, 68, 80);
            Controls.Add(end);
            Controls.Add(UiKit.Label("Шаг", 324, 72, 40));
            step = UiKit.TextBox("1", 364, 68, 70);
            Controls.Add(step);
            Button build = UiKit.AccentButton("Построить", 436, 66, 88, 28);
            build.Click += delegate { Build(); };
            Controls.Add(build);

            grid = new DataGridView();
            grid.Location = new Point(16, 108);
            grid.Size = new Size(508, 378);
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.GridColor = Color.FromArgb(225, 222, 214);
            grid.Font = new Font("Consolas", 9.75F, FontStyle.Regular);
            grid.Columns.Add("x", "x");
            grid.Columns.Add("fx", "f(x)");
            grid.Columns[0].Width = 180;
            grid.Columns[1].Width = 300;
            Controls.Add(grid);

            Button close = UiKit.Button("Закрыть", 428, 500, 96, 28);
            close.DialogResult = DialogResult.Cancel;
            Controls.Add(close);
            CancelButton = close;
            AcceptButton = build;
            Build();
        }

        private void Build()
        {
            try
            {
                double a = owner.CurrentValueFrom(start.Text).Re;
                double b = owner.CurrentValueFrom(end.Text).Re;
                double h = owner.CurrentValueFrom(step.Text).Re;
                if (h == 0) throw new CalcError("Шаг не может быть нулём");
                if ((b - a) / h > 400) throw new CalcError("Слишком много строк (макс. 400)");
                grid.Rows.Clear();
                for (double x = a; (h > 0 ? x <= b + 1e-12 : x >= b - 1e-12); x += h)
                {
                    string fx;
                    try
                    {
                        double v = Numerics.EvalReal(expr.Text, owner.State, 'X', x);
                        fx = UiKit.FormatNumber(v);
                    }
                    catch
                    {
                        fx = "ERROR";
                    }
                    grid.Rows.Add(UiKit.FormatNumber(x), fx);
                }
            }
            catch (Exception ex)
            {
                UiKit.Error(this, ex.Message);
            }
        }
    }

    internal sealed class ConstantsForm : Form
    {
        private readonly Sc880Form owner;
        private readonly ListBox list;
        private readonly Label details;

        public ConstantsForm(Sc880Form owner)
        {
            this.owner = owner;
            UiKit.Style(this, "ФИЗИЧЕСКИЕ ПОСТОЯННЫЕ (CONST)", 620, 480);
            Controls.Add(UiKit.Caption("Выберите постоянную", 16, 12, 300));
            list = new ListBox();
            list.Location = new Point(16, 40);
            list.Size = new Size(360, 388);
            list.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            list.BorderStyle = BorderStyle.FixedSingle;
            list.BackColor = Color.White;
            for (int i = 0; i < ConstantsData.Items.Count; i++)
            {
                ConstantItem c = ConstantsData.Items[i];
                list.Items.Add(c.Name + "  (" + c.Symbol + ")");
            }
            list.SelectedIndex = 0;
            list.SelectedIndexChanged += delegate { ShowDetails(); };
            list.DoubleClick += delegate { Insert(); };
            Controls.Add(list);

            details = new Label();
            details.Location = new Point(392, 40);
            details.Size = new Size(210, 320);
            details.Font = new Font("Consolas", 9.5F, FontStyle.Regular);
            details.ForeColor = UiKit.Ink;
            details.BorderStyle = BorderStyle.FixedSingle;
            details.BackColor = Color.White;
            details.Padding = new Padding(8);
            Controls.Add(details);

            Button insert = UiKit.AccentButton("Вставить", 392, 374, 100, 30);
            insert.Click += delegate { Insert(); };
            Controls.Add(insert);
            Button copy = UiKit.Button("Копировать", 500, 374, 100, 30);
            copy.Click += delegate
            {
                if (list.SelectedIndex < 0) return;
                Clipboard.SetText(ConstantsData.Items[list.SelectedIndex].ValueText);
            };
            Controls.Add(copy);
            Button close = UiKit.Button("Закрыть", 500, 410, 100, 28);
            close.DialogResult = DialogResult.Cancel;
            Controls.Add(close);
            CancelButton = close;
            ShowDetails();
        }

        private void ShowDetails()
        {
            if (list.SelectedIndex < 0) return;
            ConstantItem c = ConstantsData.Items[list.SelectedIndex];
            details.Text = c.Name + "\r\n\r\n" + c.Symbol + " = " + c.ValueText + "\r\n" + c.Unit;
        }

        private void Insert()
        {
            if (list.SelectedIndex < 0) return;
            ConstantItem c = ConstantsData.Items[list.SelectedIndex];
            owner.InsertTextExternal(c.ValueText);
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal sealed class ConversionForm : Form
    {
        private readonly Sc880Form owner;
        private readonly ComboBox category;
        private readonly ComboBox from;
        private readonly ComboBox to;
        private readonly TextBox valueBox;
        private readonly Label result;

        public ConversionForm(Sc880Form owner)
        {
            this.owner = owner;
            UiKit.Style(this, "ПЕРЕВОД ЕДИНИЦ (CONV)", 560, 336);
            Controls.Add(UiKit.Caption("Категория", 16, 12, 200));
            category = new ComboBox();
            category.DropDownStyle = ComboBoxStyle.DropDownList;
            category.Location = new Point(16, 34);
            category.Size = new Size(520, 24);
            for (int i = 0; i < UnitsData.Categories.Count; i++) category.Items.Add(UnitsData.Categories[i].Name);
            category.SelectedIndex = 0;
            category.SelectedIndexChanged += delegate { FillUnits(); };
            Controls.Add(category);

            Controls.Add(UiKit.Label("Из", 16, 74, 40));
            from = new ComboBox();
            from.DropDownStyle = ComboBoxStyle.DropDownList;
            from.Location = new Point(56, 70);
            from.Size = new Size(200, 24);
            from.SelectedIndexChanged += delegate { Recalc(); };
            Controls.Add(from);
            Controls.Add(UiKit.Label("В", 276, 74, 30));
            to = new ComboBox();
            to.DropDownStyle = ComboBoxStyle.DropDownList;
            to.Location = new Point(310, 70);
            to.Size = new Size(200, 24);
            to.SelectedIndexChanged += delegate { Recalc(); };
            Controls.Add(to);

            Controls.Add(UiKit.Label("Значение", 16, 112, 90));
            valueBox = UiKit.TextBox("1", 110, 108, 160);
            valueBox.TextChanged += delegate { Recalc(); };
            Controls.Add(valueBox);
            result = new Label();
            result.Location = new Point(288, 106);
            result.Size = new Size(246, 30);
            result.Font = new Font("Consolas", 13F, FontStyle.Bold);
            result.TextAlign = ContentAlignment.MiddleRight;
            result.ForeColor = UiKit.Ink;
            Controls.Add(result);

            Label note = UiKit.MutedLabel("Температура переводится с учётом смещения шкалы.", 16, 150, 500);
            Controls.Add(note);

            Button insert = UiKit.AccentButton("Вставить результат", 330, 282, 150, 30);
            insert.Click += delegate { Insert(); };
            Controls.Add(insert);
            Button close = UiKit.Button("Закрыть", 486, 282, 90, 30);
            close.DialogResult = DialogResult.Cancel;
            Controls.Add(close);
            CancelButton = close;

            FillUnits();
            if (owner.HasLastValue && Math.Abs(owner.LastValue.Im) < 1e-12)
                valueBox.Text = UiKit.FormatNumber(owner.LastValue.Re);
        }

        private void FillUnits()
        {
            UnitCategory c = UnitsData.Categories[category.SelectedIndex];
            from.Items.Clear();
            to.Items.Clear();
            for (int i = 0; i < c.Units.Count; i++)
            {
                from.Items.Add(c.Units[i].Name);
                to.Items.Add(c.Units[i].Name);
            }
            from.SelectedIndex = 0;
            to.SelectedIndex = Math.Min(1, c.Units.Count - 1);
            Recalc();
        }

        private void Recalc()
        {
            try
            {
                if (from.SelectedIndex < 0 || to.SelectedIndex < 0) return;
                UnitCategory c = UnitsData.Categories[category.SelectedIndex];
                double v = UiKit.ParseNumber(valueBox.Text);
                double r = UnitsData.Convert(c, from.SelectedIndex, to.SelectedIndex, v);
                result.Text = UiKit.FormatNumber(r);
            }
            catch
            {
                result.Text = "—";
            }
        }

        private void Insert()
        {
            try
            {
                UnitCategory c = UnitsData.Categories[category.SelectedIndex];
                double v = UiKit.ParseNumber(valueBox.Text);
                double r = UnitsData.Convert(c, from.SelectedIndex, to.SelectedIndex, v);
                owner.InsertTextExternal(UiKit.FormatNumber(r));
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                UiKit.Error(this, ex.Message);
            }
        }
    }

    internal sealed class AngleConvertForm : Form
    {
        private readonly Sc880Form owner;
        private readonly ComboBox from;
        private readonly ComboBox to;
        private readonly TextBox valueBox;
        private readonly Label result;

        public AngleConvertForm(Sc880Form owner)
        {
            this.owner = owner;
            UiKit.Style(this, "DRG▶ — ПЕРЕВОД УГЛОВ", 440, 260);
            Controls.Add(UiKit.Caption("Перевод значения между DEG, RAD и GRAD", 16, 12, 400));
            from = new ComboBox();
            from.DropDownStyle = ComboBoxStyle.DropDownList;
            from.Location = new Point(16, 48);
            from.Size = new Size(120, 24);
            from.Items.AddRange(new object[] { "DEG", "RAD", "GRAD" });
            from.SelectedIndex = owner.State.Angle == AngleUnit.Degrees ? 0 : (owner.State.Angle == AngleUnit.Radians ? 1 : 2);
            from.SelectedIndexChanged += delegate { Recalc(); };
            Controls.Add(from);
            Controls.Add(UiKit.Label("→", 148, 52, 24));
            to = new ComboBox();
            to.DropDownStyle = ComboBoxStyle.DropDownList;
            to.Location = new Point(176, 48);
            to.Size = new Size(120, 24);
            to.Items.AddRange(new object[] { "DEG", "RAD", "GRAD" });
            to.SelectedIndex = (from.SelectedIndex + 1) % 3;
            to.SelectedIndexChanged += delegate { Recalc(); };
            Controls.Add(to);
            Controls.Add(UiKit.Label("Значение", 16, 92, 80));
            valueBox = UiKit.TextBox(owner.HasLastValue && Math.Abs(owner.LastValue.Im) < 1e-12 ? UiKit.FormatNumber(owner.LastValue.Re) : "90", 100, 88, 196);
            valueBox.TextChanged += delegate { Recalc(); };
            Controls.Add(valueBox);
            result = new Label();
            result.Location = new Point(16, 126);
            result.Size = new Size(404, 34);
            result.Font = new Font("Consolas", 15F, FontStyle.Bold);
            result.TextAlign = ContentAlignment.MiddleRight;
            Controls.Add(result);
            Button insert = UiKit.AccentButton("Вставить", 242, 206, 100, 30);
            insert.Click += delegate { Insert(); };
            Controls.Add(insert);
            Button close = UiKit.Button("Закрыть", 350, 206, 70, 30);
            close.DialogResult = DialogResult.Cancel;
            Controls.Add(close);
            CancelButton = close;
            Recalc();
        }

        private AngleUnit Unit(int idx)
        {
            if (idx == 1) return AngleUnit.Radians;
            if (idx == 2) return AngleUnit.Gradians;
            return AngleUnit.Degrees;
        }

        private void Recalc()
        {
            try
            {
                double v = UiKit.ParseNumber(valueBox.Text);
                double r = Numerics.ConvertAngle(v, Unit(from.SelectedIndex), Unit(to.SelectedIndex));
                result.Text = UiKit.FormatNumber(r);
            }
            catch
            {
                result.Text = "—";
            }
        }

        private void Insert()
        {
            try
            {
                double v = UiKit.ParseNumber(valueBox.Text);
                double r = Numerics.ConvertAngle(v, Unit(from.SelectedIndex), Unit(to.SelectedIndex));
                owner.InsertTextExternal(UiKit.FormatNumber(r));
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                UiKit.Error(this, ex.Message);
            }
        }
    }

    internal sealed class VerifyForm : Form
    {
        private readonly Sc880Form owner;
        private readonly TextBox expr;
        private readonly TextBox output;
        private readonly Label verdict;

        public VerifyForm(Sc880Form owner)
        {
            this.owner = owner;
            UiKit.Style(this, "VERIF — ПРОВЕРКА", 560, 320);
            Controls.Add(UiKit.Caption("Введите равенство или неравенство", 16, 12, 400));
            Controls.Add(UiKit.MutedLabel("Поддерживаются =, ≠, <, >, ≤, ≥. Пример: 2+2=4.", 16, 32, 520));
            expr = UiKit.TextBox(owner.ExpressionText.Length > 0 ? owner.ExpressionText : "2+2=4", 16, 56, 528);
            Controls.Add(expr);
            Button check = UiKit.AccentButton("Проверить", 404, 90, 140, 30);
            check.Click += delegate { Check(); };
            Controls.Add(check);
            verdict = new Label();
            verdict.Location = new Point(16, 136);
            verdict.Size = new Size(528, 44);
            verdict.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
            verdict.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(verdict);
            output = UiKit.TextBox("", 16, 188, 528);
            output.Multiline = true;
            output.ReadOnly = true;
            output.Height = 50;
            output.Font = new Font("Consolas", 10F, FontStyle.Regular);
            Controls.Add(output);
            Button close = UiKit.Button("Закрыть", 448, 262, 96, 28);
            close.DialogResult = DialogResult.Cancel;
            Controls.Add(close);
            CancelButton = close;
            AcceptButton = check;
            Check();
        }

        private void Check()
        {
            try
            {
                VerifyResult r = Numerics.Verify(expr.Text, owner.State);
                verdict.Text = r.IsTrue ? "ВЕРНО" : "НЕВЕРНО";
                verdict.ForeColor = r.IsTrue ? Color.FromArgb(30, 120, 60) : UiKit.Accent;
                output.Text = "Левая часть: " + UiKit.FormatNumber(r.Left) +
                    "    Правая часть: " + UiKit.FormatNumber(r.Right) +
                    "    Разность: " + UiKit.FormatNumber(r.Difference);
            }
            catch (Exception ex)
            {
                verdict.Text = "ОШИБКА";
                verdict.ForeColor = UiKit.Accent;
                output.Text = ex.Message;
            }
        }
    }
}
