using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SC880
{
    internal sealed class EqnForm : Form
    {
        private readonly Sc880Form owner;
        private readonly ComboBox type;
        private readonly FlowLayoutPanel fields;
        private readonly TextBox output;
        private readonly Label hint;
        private readonly List<TextBox> boxes = new List<TextBox>();
        private string insertText;
        private bool insertComplex;

        private static readonly string[] TypeNames =
        {
            "Система 2×2",
            "Система 3×3",
            "Квадратное уравнение",
            "Кубическое уравнение"
        };

        public EqnForm(Sc880Form owner)
        {
            this.owner = owner;
            UiKit.Style(this, "EQN — УРАВНЕНИЯ", 800, 566);
            Controls.Add(UiKit.Caption("Тип уравнения", 16, 12, 140));
            type = new ComboBox();
            type.DropDownStyle = ComboBoxStyle.DropDownList;
            type.Location = new Point(150, 9);
            type.Size = new Size(220, 24);
            type.Items.AddRange(TypeNames);
            type.SelectedIndex = 0;
            type.SelectedIndexChanged += delegate { BuildFields(); };
            Controls.Add(type);

            hint = UiKit.MutedLabel("", 390, 14, 390);
            Controls.Add(hint);

            GroupBox group = new GroupBox();
            group.Text = "Коэффициенты";
            group.Location = new Point(16, 46);
            group.Size = new Size(768, 160);
            Controls.Add(group);
            fields = new FlowLayoutPanel();
            fields.Location = new Point(10, 20);
            fields.Size = new Size(746, 132);
            fields.AutoScroll = true;
            fields.FlowDirection = FlowDirection.LeftToRight;
            fields.WrapContents = true;
            group.Controls.Add(fields);

            output = new TextBox();
            output.Location = new Point(16, 216);
            output.Size = new Size(768, 268);
            output.Multiline = true;
            output.ReadOnly = true;
            output.ScrollBars = ScrollBars.Vertical;
            output.Font = new Font("Consolas", 11F, FontStyle.Regular);
            output.BackColor = Color.White;
            output.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(output);

            Button solve = UiKit.AccentButton("Решить", 16, 502, 130, 34);
            solve.Click += delegate { Solve(); };
            Controls.Add(solve);
            Button insert = UiKit.Button("Вставить", 156, 502, 130, 34);
            insert.Click += delegate { Insert(); };
            Controls.Add(insert);
            Button close = UiKit.Button("Закрыть", 668, 502, 116, 34);
            close.DialogResult = DialogResult.Cancel;
            Controls.Add(close);
            CancelButton = close;
            AcceptButton = solve;

            BuildFields();
            Solve();
        }

        private void BuildFields()
        {
            fields.Controls.Clear();
            boxes.Clear();
            int t = type.SelectedIndex;
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
                Panel p = new Panel();
                p.Size = new Size(115, 54);
                Label l = UiKit.Label(labels[i], 4, 0, 50);
                p.Controls.Add(l);
                TextBox tb = UiKit.TextBox(defaults[i], 4, 22, 100);
                p.Controls.Add(tb);
                fields.Controls.Add(p);
                boxes.Add(tb);
            }
            output.Text = "";
            insertText = null;
        }

        private double[] ReadValues()
        {
            double[] v = new double[boxes.Count];
            for (int i = 0; i < boxes.Count; i++) v[i] = UiKit.ParseNumber(boxes[i].Text);
            return v;
        }

        private void Solve()
        {
            try
            {
                double[] c = ReadValues();
                int t = type.SelectedIndex;
                insertText = null;
                insertComplex = false;
                output.Text = "";
                if (t == 0)
                {
                    double[,] m = new double[,] { { c[0], c[1] }, { c[3], c[4] } };
                    double[] b = new double[] { c[2], c[5] };
                    double[] x = Numerics.SolveLinear(m, b);
                    output.Text = "a₁x + b₁y = c₁\r\na₂x + b₂y = c₂\r\n\r\n" +
                        "x = " + UiKit.FormatNumber(x[0]) + "\r\n" +
                        "y = " + UiKit.FormatNumber(x[1]);
                    insertText = UiKit.FormatNumber(x[0]);
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
                        "x = " + UiKit.FormatNumber(x[0]) + "\r\n" +
                        "y = " + UiKit.FormatNumber(x[1]) + "\r\n" +
                        "z = " + UiKit.FormatNumber(x[2]);
                    insertText = UiKit.FormatNumber(x[0]);
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
                    insertText = insertComplex ? ParseRoot(chosen) : UiKit.FormatNumber(chosen.Re);
                    output.Text += "\r\nСумма корней: " + FormatRoot(Sum(roots));
                }
            }
            catch (Exception ex)
            {
                output.Text = "Ошибка: " + ex.Message;
                insertText = null;
            }
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
            if (Math.Abs(z.Im) < 1e-8) return z.Re.ToString("G15", System.Globalization.CultureInfo.InvariantCulture).Replace('-', '−');
            return "(" + z.Re.ToString("G15", System.Globalization.CultureInfo.InvariantCulture).Replace('-', '−') +
                (z.Im < 0 ? "−" : "+") +
                Math.Abs(z.Im).ToString("G15", System.Globalization.CultureInfo.InvariantCulture) + "i)";
        }

        private void Insert()
        {
            if (string.IsNullOrEmpty(insertText))
            {
                UiKit.Error(this, "Сначала решите уравнение.");
                return;
            }
            if (insertComplex) owner.State.Mode = CalcMode.Cmplx;
            owner.InsertTextExternal(insertText);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
