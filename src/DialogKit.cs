using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace SC880
{
    internal static class UiKit
    {
        public static readonly Color Back = Color.FromArgb(250, 249, 246);
        public static readonly Color Panel = Color.FromArgb(240, 239, 234);
        public static readonly Color Ink = Color.FromArgb(35, 35, 33);
        public static readonly Color Muted = Color.FromArgb(105, 103, 98);
        public static readonly Color Accent = Color.FromArgb(186, 46, 32);
        public static readonly Color AccentDark = Color.FromArgb(148, 32, 22);
        public static readonly Color Line = Color.FromArgb(207, 203, 195);

        public static void Style(Form f, string title, int width, int height)
        {
            f.Text = title;
            f.ClientSize = new Size(width, height);
            f.StartPosition = FormStartPosition.CenterScreen;
            f.FormBorderStyle = FormBorderStyle.FixedDialog;
            f.MaximizeBox = false;
            f.MinimizeBox = false;
            f.ShowInTaskbar = false;
            f.ShowIcon = false;
            f.BackColor = Back;
            f.ForeColor = Ink;
            f.Font = new Font("Segoe UI", 9.25F, FontStyle.Regular, GraphicsUnit.Point);
            f.AutoScaleMode = AutoScaleMode.Font;
        }

        public static Label Label(string text, int x, int y, int width)
        {
            Label l = new Label();
            l.Text = text;
            l.Location = new Point(x, y);
            l.Size = new Size(width, 20);
            l.ForeColor = Ink;
            l.BackColor = Color.Transparent;
            return l;
        }

        public static Label Caption(string text, int x, int y, int width)
        {
            Label l = Label(text, x, y, width);
            l.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
            return l;
        }

        public static Label MutedLabel(string text, int x, int y, int width)
        {
            Label l = Label(text, x, y, width);
            l.ForeColor = Muted;
            l.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            return l;
        }

        public static TextBox TextBox(string text, int x, int y, int width)
        {
            TextBox t = new TextBox();
            t.Text = text;
            t.Location = new Point(x, y);
            t.Size = new Size(width, 24);
            t.BorderStyle = BorderStyle.FixedSingle;
            t.BackColor = Color.White;
            t.ForeColor = Ink;
            t.Font = new Font("Consolas", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            return t;
        }

        public static Button Button(string text, int x, int y, int width, int height)
        {
            Button b = new Button();
            b.Text = text;
            b.Location = new Point(x, y);
            b.Size = new Size(width, height);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Line;
            b.FlatAppearance.BorderSize = 1;
            b.BackColor = Color.FromArgb(246, 245, 241);
            b.ForeColor = Ink;
            b.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
            b.UseVisualStyleBackColor = false;
            b.AccessibleName = text;
            return b;
        }

        public static Button AccentButton(string text, int x, int y, int width, int height)
        {
            Button b = Button(text, x, y, width, height);
            b.BackColor = Accent;
            b.ForeColor = Color.White;
            b.FlatAppearance.BorderColor = AccentDark;
            return b;
        }

        public static Button DarkButton(string text, int x, int y, int width, int height)
        {
            Button b = Button(text, x, y, width, height);
            b.BackColor = Color.FromArgb(63, 64, 61);
            b.ForeColor = Color.White;
            b.FlatAppearance.BorderColor = Color.FromArgb(38, 39, 36);
            return b;
        }

        public static double ParseNumber(string text)
        {
            if (text == null) throw new FormatException("Пустое значение");
            string s = text.Trim().Replace('−', '-').Replace('–', '-').Replace('—', '-').Replace(',', '.');
            if (s.Length == 0) throw new FormatException("Пустое значение");
            double v;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
            int slash = s.IndexOf('/');
            if (slash > 0 && slash < s.Length - 1)
            {
                double a, b;
                if (double.TryParse(s.Substring(0, slash), NumberStyles.Float, CultureInfo.InvariantCulture, out a)
                    && double.TryParse(s.Substring(slash + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out b)
                    && b != 0)
                    return a / b;
            }
            throw new FormatException("Неверное число: " + text);
        }

        public static string FormatNumber(double v)
        {
            if (Math.Abs(v) < 5e-13) v = 0;
            return v.ToString("G12", CultureInfo.InvariantCulture);
        }

        public static void Error(IWin32Window owner, string message)
        {
            MessageBox.Show(owner, message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    internal sealed class InfoForm : Form
    {
        public InfoForm(string title, string text, int width, int height)
        {
            UiKit.Style(this, title, width, height);
            TextBox box = new TextBox();
            box.Multiline = true;
            box.ReadOnly = true;
            box.ScrollBars = ScrollBars.Both;
            box.WordWrap = false;
            box.Font = new Font("Consolas", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
            box.BackColor = Color.White;
            box.ForeColor = UiKit.Ink;
            box.BorderStyle = BorderStyle.FixedSingle;
            box.Text = text;
            box.TabStop = false;
            box.Select(0, 0);
            box.Location = new Point(14, 14);
            box.Size = new Size(width - 28, height - 62);
            box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Controls.Add(box);
            Button ok = UiKit.Button("Закрыть", width - 110, height - 40, 96, 28);
            ok.DialogResult = DialogResult.OK;
            Controls.Add(ok);
            AcceptButton = ok;
            CancelButton = ok;
        }

        public static void Show(IWin32Window owner, string title, string text, int width, int height)
        {
            using (InfoForm f = new InfoForm(title, text, width, height)) f.ShowDialog(owner);
        }
    }

    internal sealed class ListChoiceForm : Form
    {
        public int SelectedIndex = -1;
        private readonly ListBox list;

        public ListChoiceForm(string title, string prompt, string[] options, int initial)
        {
            UiKit.Style(this, title, 430, Math.Min(500, 160 + options.Length * 24));
            Label p = UiKit.Label(prompt, 16, 14, 398);
            Controls.Add(p);
            list = new ListBox();
            list.Location = new Point(16, 40);
            list.Size = new Size(398, Math.Min(340, options.Length * 23 + 6));
            list.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
            list.BorderStyle = BorderStyle.FixedSingle;
            list.BackColor = Color.White;
            list.ForeColor = UiKit.Ink;
            list.Items.AddRange(options);
            if (initial >= 0 && initial < options.Length) list.SelectedIndex = initial;
            Controls.Add(list);
            int y = list.Bottom + 10;
            Button ok = UiKit.AccentButton("Выбрать", 220, y, 96, 28);
            ok.DialogResult = DialogResult.OK;
            ok.Click += delegate { SelectedIndex = list.SelectedIndex; };
            Controls.Add(ok);
            Button cancel = UiKit.Button("Отмена", 322, y, 92, 28);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;
            list.DoubleClick += delegate
            {
                if (list.SelectedIndex >= 0)
                {
                    SelectedIndex = list.SelectedIndex;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };
            ClientSize = new Size(430, y + 42);
            Height = ClientSize.Height + (Height - ClientSize.Height);
        }

        public static int Choose(IWin32Window owner, string title, string prompt, string[] options, int initial)
        {
            using (ListChoiceForm f = new ListChoiceForm(title, prompt, options, initial))
            {
                if (f.ShowDialog(owner) == DialogResult.OK) return f.SelectedIndex;
            }
            return -1;
        }
    }

    internal sealed class FieldsForm : Form
    {
        private readonly TextBox[] boxes;
        public string[] Values;

        public FieldsForm(string title, string prompt, string[] labels, string[] defaults)
        {
            int n = labels.Length;
            UiKit.Style(this, title, 420, 100 + n * 36 + 40);
            Label p = UiKit.Label(prompt, 16, 14, 388);
            p.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            Controls.Add(p);
            boxes = new TextBox[n];
            for (int i = 0; i < n; i++)
            {
                int y = 44 + i * 36;
                Label l = UiKit.Label(labels[i], 16, y + 3, 150);
                Controls.Add(l);
                TextBox t = UiKit.TextBox(defaults[i], 172, y, 232);
                Controls.Add(t);
                boxes[i] = t;
            }
            int by = 54 + n * 36;
            Button ok = UiKit.AccentButton("OK", 212, by, 90, 28);
            ok.DialogResult = DialogResult.OK;
            ok.Click += delegate
            {
                Values = new string[n];
                for (int i = 0; i < n; i++) Values[i] = boxes[i].Text;
            };
            Controls.Add(ok);
            Button cancel = UiKit.Button("Отмена", 310, by, 94, 28);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        public static string[] Ask(IWin32Window owner, string title, string prompt, string[] labels, string[] defaults)
        {
            using (FieldsForm f = new FieldsForm(title, prompt, labels, defaults))
            {
                if (f.ShowDialog(owner) == DialogResult.OK) return f.Values;
            }
            return null;
        }
    }

    internal sealed class ModeForm : Form
    {
        public CalcMode SelectedMode;

        private static readonly CalcMode[] Modes =
        {
            CalcMode.Comp, CalcMode.Cmplx, CalcMode.Stat,
            CalcMode.BaseN, CalcMode.Eqn, CalcMode.Matrix,
            CalcMode.Vector, CalcMode.Table, CalcMode.Verif
        };

        private static readonly string[] Names =
        {
            "COMP", "CMPLX", "STAT", "BASE-N", "EQN", "MATRIX", "VECTOR", "TABLE", "VERIF"
        };

        private static readonly string[] Descriptions =
        {
            "Обычные вычисления", "Комплексные числа", "Статистика и регрессия",
            "Системы счисления", "Уравнения и системы", "Матрицы",
            "Векторы", "Таблица значений f(x)", "Проверка равенств"
        };

        public ModeForm(CalcMode current)
        {
            SelectedMode = current;
            UiKit.Style(this, "РЕЖИМ ВЫЧИСЛЕНИЙ", 620, 374);
            Label cap = UiKit.Caption("Выберите режим", 16, 12, 300);
            Controls.Add(cap);
            Label sub = UiKit.MutedLabel("Как клавиша MODE на SC-880: инструмент откроется сразу.", 16, 34, 580);
            Controls.Add(sub);
            for (int i = 0; i < Modes.Length; i++)
            {
                int col = i % 3;
                int row = i / 3;
                int x = 16 + col * 196;
                int y = 60 + row * 78;
                Button b = UiKit.Button((i + 1) + "   " + Names[i] + "\r\n" + Descriptions[i], x, y, 188, 68);
                b.TextAlign = ContentAlignment.MiddleLeft;
                b.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
                if (Modes[i] == current)
                {
                    b.BackColor = Color.FromArgb(232, 228, 219);
                    b.FlatAppearance.BorderColor = Color.FromArgb(120, 114, 102);
                }
                CalcMode mode = Modes[i];
                b.Click += delegate
                {
                    SelectedMode = mode;
                    DialogResult = DialogResult.OK;
                    Close();
                };
                Controls.Add(b);
                b.AccessibleName = Names[i];
            }
            Button cancel = UiKit.Button("Отмена", 508, 326, 94, 28);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);
            CancelButton = cancel;
        }

        public static bool Choose(IWin32Window owner, CalcMode current, out CalcMode chosen)
        {
            using (ModeForm f = new ModeForm(current))
            {
                if (f.ShowDialog(owner) == DialogResult.OK)
                {
                    chosen = f.SelectedMode;
                    return true;
                }
            }
            chosen = current;
            return false;
        }
    }

    internal sealed class SetupForm : Form
    {
        private readonly CalcState state;
        private readonly RadioButton deg;
        private readonly RadioButton rad;
        private readonly RadioButton grad;
        private readonly RadioButton norm1;
        private readonly RadioButton norm2;
        private readonly RadioButton fix;
        private readonly RadioButton sci;
        private readonly NumericUpDown digits;
        private readonly CheckBox fraction;
        private readonly RadioButton rect;
        private readonly RadioButton polar;
        private readonly CheckBox frequency;
        private readonly CheckBox insert;

        public SetupForm(CalcState state)
        {
            this.state = state;
            UiKit.Style(this, "НАСТРОЙКА (SET UP)", 466, 458);

            GroupBox angles = new GroupBox();
            angles.Text = "Угловая мера";
            angles.Location = new Point(16, 12);
            angles.Size = new Size(206, 104);
            angles.ForeColor = UiKit.Ink;
            Controls.Add(angles);
            deg = Radio("Градусы (DEG)", 12, 22, angles);
            rad = Radio("Радианы (RAD)", 12, 46, angles);
            grad = Radio("Грады (GRAD)", 12, 70, angles);
            (state.Angle == AngleUnit.Degrees ? deg : state.Angle == AngleUnit.Radians ? rad : grad).Checked = true;

            GroupBox format = new GroupBox();
            format.Text = "Формат числа";
            format.Location = new Point(234, 12);
            format.Size = new Size(214, 144);
            Controls.Add(format);
            norm1 = Radio("Norm 1", 12, 22, format);
            norm2 = Radio("Norm 2", 12, 46, format);
            fix = Radio("Fix", 12, 70, format);
            sci = Radio("Sci", 12, 94, format);
            if (state.Format == NumFormat.Norm1) norm1.Checked = true;
            else if (state.Format == NumFormat.Fix) fix.Checked = true;
            else if (state.Format == NumFormat.Sci) sci.Checked = true;
            else norm2.Checked = true;
            digits = new NumericUpDown();
            digits.Minimum = 0;
            digits.Maximum = 9;
            digits.Value = state.Digits;
            digits.Location = new Point(152, 68);
            digits.Size = new Size(48, 24);
            format.Controls.Add(digits);
            Label dl = new Label();
            dl.Text = "знаков";
            dl.Location = new Point(152, 94);
            dl.AutoSize = true;
            format.Controls.Add(dl);
            fix.CheckedChanged += delegate { digits.Enabled = fix.Checked || sci.Checked; };
            sci.CheckedChanged += delegate { digits.Enabled = fix.Checked || sci.Checked; };
            digits.Enabled = fix.Checked || sci.Checked;

            GroupBox display = new GroupBox();
            display.Text = "Отображение";
            display.Location = new Point(16, 164);
            display.Size = new Size(432, 126);
            Controls.Add(display);
            fraction = new CheckBox();
            fraction.Text = "Показывать результат дробью (S⇔D)";
            fraction.Location = new Point(12, 22);
            fraction.Size = new Size(320, 22);
            fraction.Checked = state.FractionResult;
            display.Controls.Add(fraction);
            rect = Radio("Комплексные: a+bi", 12, 50, display);
            polar = Radio("Комплексные: r∠θ", 12, 74, display);
            (state.ComplexDisplay == ComplexView.Rectangular ? rect : polar).Checked = true;
            frequency = new CheckBox();
            frequency.Text = "Частота в STAT";
            frequency.Location = new Point(12, 98);
            frequency.Size = new Size(220, 22);
            frequency.Checked = state.StatFrequency;
            display.Controls.Add(frequency);

            insert = new CheckBox();
            insert.Text = "Режим вставки (INS) вместо замены";
            insert.Location = new Point(20, 302);
            insert.Size = new Size(380, 22);
            insert.Checked = state.InsertMode;
            Controls.Add(insert);

            Button reset = UiKit.Button("Сброс", 20, 408, 92, 28);
            reset.Click += delegate
            {
                deg.Checked = true;
                norm2.Checked = true;
                fraction.Checked = false;
                rect.Checked = true;
                frequency.Checked = false;
                insert.Checked = true;
                digits.Value = 2;
            };
            Controls.Add(reset);
            Button ok = UiKit.AccentButton("Применить", 240, 408, 100, 28);
            ok.DialogResult = DialogResult.OK;
            ok.Click += delegate { Apply(); };
            Controls.Add(ok);
            Button cancel = UiKit.Button("Отмена", 348, 408, 100, 28);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private static RadioButton Radio(string text, int x, int y, Control parent)
        {
            RadioButton r = new RadioButton();
            r.Text = text;
            r.Location = new Point(x, y);
            r.AutoSize = true;
            parent.Controls.Add(r);
            return r;
        }

        private void Apply()
        {
            state.Angle = deg.Checked ? AngleUnit.Degrees : (rad.Checked ? AngleUnit.Radians : AngleUnit.Gradians);
            state.Format = norm1.Checked ? NumFormat.Norm1 : (fix.Checked ? NumFormat.Fix : (sci.Checked ? NumFormat.Sci : NumFormat.Norm2));
            state.Digits = (int)digits.Value;
            state.FractionResult = fraction.Checked;
            state.ComplexDisplay = rect.Checked ? ComplexView.Rectangular : ComplexView.Polar;
            state.StatFrequency = frequency.Checked;
            state.InsertMode = insert.Checked;
        }
    }
}
