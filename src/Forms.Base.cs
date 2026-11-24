using System;
using System.Drawing;
using System.Windows.Forms;

namespace SC880
{
    internal sealed class BaseForm : Form
    {
        private readonly Sc880Form owner;
        private readonly ComboBox baseBox;
        private readonly TextBox expr;
        private readonly TextBox output;
        private int selectedBase;

        public BaseForm(Sc880Form owner, int initialBase)
        {
            this.owner = owner;
            selectedBase = initialBase;
            UiKit.Style(this, "BASE-N — СИСТЕМЫ СЧИСЛЕНИЯ", 620, 520);
            Controls.Add(UiKit.Caption("Система счисления", 16, 12, 180));
            baseBox = new ComboBox();
            baseBox.DropDownStyle = ComboBoxStyle.DropDownList;
            baseBox.Location = new Point(170, 10);
            baseBox.Size = new Size(170, 24);
            baseBox.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
            baseBox.Items.AddRange(new object[] { "DEC (10)", "HEX (16)", "BIN (2)", "OCT (8)" });
            baseBox.SelectedIndex = BaseIndex(initialBase);
            baseBox.SelectedIndexChanged += delegate
            {
                selectedBase = BaseValue(baseBox.SelectedIndex);
                owner.BaseValue = selectedBase;
                output.Text = "Основание: " + BaseLabel(selectedBase);
            };
            Controls.Add(baseBox);
            Controls.Add(UiKit.MutedLabel("AND, OR, XOR, NOT, <<, >>, скобки, A–F в HEX.", 350, 14, 250));

            expr = UiKit.TextBox("", 16, 48, 588);
            expr.Font = new Font("Consolas", 15F, FontStyle.Regular);
            expr.Height = 30;
            Controls.Add(expr);
            output = UiKit.TextBox("", 16, 90, 588);
            output.Multiline = true;
            output.ReadOnly = true;
            output.Height = 74;
            output.Font = new Font("Consolas", 11F, FontStyle.Regular);
            output.ScrollBars = ScrollBars.Vertical;
            Controls.Add(output);

            string[] labels = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F",
                "(", ")", "+", "−", "×", "÷", "AND", "OR", "XOR", "NOT", "<<", ">>", "DEL", "AC" };
            int x0 = 16, y0 = 176, bw = 88, bh = 34, gap = 6;
            for (int i = 0; i < labels.Length; i++)
            {
                int col = i % 6;
                int row = i / 6;
                string text = labels[i];
                Button b = UiKit.Button(text, x0 + col * (bw + gap), y0 + row * (bh + gap), bw, bh);
                b.Font = new Font("Segoe UI Semibold", text.Length > 1 ? 8.5F : 11F, FontStyle.Regular);
                b.Click += delegate { PressToken(text); };
                Controls.Add(b);
            }
            Button equals = UiKit.AccentButton("=", 16, 418, 180, 34);
            equals.Click += delegate { Compute(); };
            Controls.Add(equals);
            Button insert = UiKit.Button("Вставить в калькулятор", 204, 418, 190, 34);
            insert.Click += delegate { Insert(); };
            Controls.Add(insert);
            Button close = UiKit.Button("Закрыть", 516, 468, 88, 28);
            close.DialogResult = DialogResult.Cancel;
            Controls.Add(close);
            CancelButton = close;
            AcceptButton = equals;
            output.Text = "Основание: " + BaseLabel(selectedBase) + "\r\nВведите выражение, например FF+1";
        }

        private static int BaseIndex(int b)
        {
            if (b == 16) return 1;
            if (b == 2) return 2;
            if (b == 8) return 3;
            return 0;
        }

        private static int BaseValue(int idx)
        {
            if (idx == 1) return 16;
            if (idx == 2) return 2;
            if (idx == 3) return 8;
            return 10;
        }

        private static string BaseLabel(int b)
        {
            if (b == 16) return "HEX";
            if (b == 2) return "BIN";
            if (b == 8) return "OCT";
            return "DEC";
        }

        private void PressToken(string token)
        {
            if (token == "DEL")
            {
                int start = expr.SelectionStart;
                if (start > 0)
                {
                    expr.Text = expr.Text.Remove(start - 1, 1);
                    expr.SelectionStart = start - 1;
                }
                return;
            }
            if (token == "AC")
            {
                expr.Clear();
                output.Clear();
                return;
            }
            string insert = token;
            if (token == "AND" || token == "OR" || token == "XOR" || token == "NOT" || token == "<<" || token == ">>")
                insert = " " + token + " ";
            int pos = expr.SelectionStart;
            expr.Text = expr.Text.Insert(pos, insert);
            expr.SelectionStart = pos + insert.Length;
            expr.Focus();
        }

        private void Compute()
        {
            try
            {
                BaseNResult r = BaseN.Evaluate(expr.Text, selectedBase);
                output.Text = "DEC " + r.Dec + "\r\nHEX " + r.Hex + "\r\nBIN " + r.Bin + "\r\nOCT " + r.Oct;
            }
            catch (Exception ex)
            {
                output.Text = "Ошибка: " + ex.Message;
            }
        }

        private void Insert()
        {
            try
            {
                BaseNResult r = BaseN.Evaluate(expr.Text, selectedBase);
                owner.UseBaseResult(expr.Text, r.Value, selectedBase);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                UiKit.Error(this, ex.Message);
            }
        }
    }
}
