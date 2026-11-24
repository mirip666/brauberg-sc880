using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SC880
{
    internal sealed class StatForm : Form
    {
        private readonly Sc880Form owner;
        private readonly DataGridView grid;
        private readonly TextBox report;
        private readonly RadioButton oneVar;
        private readonly RadioButton twoVar;
        private readonly CheckBox freq;
        private readonly ComboBox regression;
        private readonly Label regressionLabel;

        public StatForm(Sc880Form owner)
        {
            this.owner = owner;
            UiKit.Style(this, "STAT — СТАТИСТИКА", 800, 620);

            oneVar = new RadioButton();
            oneVar.Text = "1 переменная";
            oneVar.Location = new Point(16, 14);
            oneVar.AutoSize = true;
            oneVar.Checked = true;
            oneVar.CheckedChanged += delegate { UpdateColumns(); };
            Controls.Add(oneVar);
            twoVar = new RadioButton();
            twoVar.Text = "2 переменные";
            twoVar.Location = new Point(140, 14);
            twoVar.AutoSize = true;
            Controls.Add(twoVar);
            freq = new CheckBox();
            freq.Text = "Частота (Freq)";
            freq.Location = new Point(270, 14);
            freq.AutoSize = true;
            freq.Checked = owner.State.StatFrequency;
            freq.CheckedChanged += delegate { UpdateColumns(); };
            Controls.Add(freq);
            regressionLabel = UiKit.Label("Регрессия", 440, 15, 70);
            Controls.Add(regressionLabel);
            regression = new ComboBox();
            regression.DropDownStyle = ComboBoxStyle.DropDownList;
            regression.Location = new Point(512, 11);
            regression.Size = new Size(150, 24);
            regression.Items.AddRange(new object[] { "Линейная", "Квадратичная", "Логарифмическая", "Экспоненциальная", "Степенная", "Обратная" });
            regression.SelectedIndex = 0;
            Controls.Add(regression);

            grid = new DataGridView();
            grid.Location = new Point(16, 46);
            grid.Size = new Size(460, 490);
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.RowHeadersVisible = false;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.GridColor = Color.FromArgb(225, 222, 214);
            grid.Font = new Font("Consolas", 10F, FontStyle.Regular);
            grid.ColumnHeadersHeight = 26;
            grid.EditMode = DataGridViewEditMode.EditOnEnter;
            grid.Columns.Add("x", "X");
            grid.Columns.Add("y", "Y");
            grid.Columns.Add("f", "Freq");
            grid.Columns[0].Width = 142;
            grid.Columns[1].Width = 142;
            grid.Columns[2].Width = 142;
            Controls.Add(grid);

            Button add = UiKit.Button("+ Строка", 16, 546, 92, 28);
            add.Click += delegate { grid.Rows.Add("", "", "1"); };
            Controls.Add(add);
            Button del = UiKit.Button("Удалить", 116, 546, 92, 28);
            del.Click += delegate
            {
                if (grid.SelectedCells.Count > 0) grid.Rows.RemoveAt(grid.SelectedCells[0].RowIndex);
            };
            Controls.Add(del);
            Button clear = UiKit.Button("Очистить", 216, 546, 92, 28);
            clear.Click += delegate
            {
                grid.Rows.Clear();
                owner.StatData.Clear();
                report.Text = "";
                grid.Rows.Add("", "", "1");
            };
            Controls.Add(clear);
            Button calc = UiKit.AccentButton("Вычислить", 316, 546, 160, 28);
            calc.Click += delegate { Compute(); };
            Controls.Add(calc);

            report = new TextBox();
            report.Location = new Point(492, 46);
            report.Size = new Size(292, 528);
            report.Multiline = true;
            report.ReadOnly = true;
            report.ScrollBars = ScrollBars.Vertical;
            report.Font = new Font("Consolas", 9.75F, FontStyle.Regular);
            report.BackColor = Color.White;
            report.BorderStyle = BorderStyle.FixedSingle;
            report.WordWrap = false;
            Controls.Add(report);

            Button close = UiKit.Button("Закрыть", 684, 584, 100, 26);
            close.DialogResult = DialogResult.Cancel;
            Controls.Add(close);
            CancelButton = close;

            LoadData();
            UpdateColumns();
        }

        private void LoadData()
        {
            grid.Rows.Clear();
            if (owner.StatData.Count == 0)
            {
                grid.Rows.Add("", "", "1");
                grid.Rows.Add("", "", "1");
                grid.Rows.Add("", "", "1");
            }
            else
            {
                for (int i = 0; i < owner.StatData.Count; i++)
                {
                    StatPoint p = owner.StatData[i];
                    grid.Rows.Add(UiKit.FormatNumber(p.X), UiKit.FormatNumber(p.Y), UiKit.FormatNumber(p.Freq));
                }
            }
        }

        private void UpdateColumns()
        {
            grid.Columns[1].Visible = twoVar.Checked;
            grid.Columns[2].Visible = freq.Checked;
            regression.Enabled = twoVar.Checked;
            regressionLabel.Enabled = twoVar.Checked;
        }

        private bool CaptureData()
        {
            List<StatPoint> data = new List<StatPoint>();
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                DataGridViewRow row = grid.Rows[i];
                string xs = Convert.ToString(row.Cells[0].Value);
                string ys = Convert.ToString(row.Cells[1].Value);
                string fs = Convert.ToString(row.Cells[2].Value);
                if (string.IsNullOrEmpty(xs) && string.IsNullOrEmpty(ys)) continue;
                try
                {
                    StatPoint p = new StatPoint();
                    p.X = UiKit.ParseNumber(xs);
                    p.Y = twoVar.Checked ? UiKit.ParseNumber(ys) : 0;
                    p.Freq = freq.Checked ? UiKit.ParseNumber(fs) : 1;
                    if (p.Freq <= 0) throw new FormatException("Частота должна быть > 0");
                    data.Add(p);
                }
                catch (Exception ex)
                {
                    UiKit.Error(this, "Строка " + (i + 1) + ": " + ex.Message);
                    return false;
                }
            }
            owner.StatData.Clear();
            owner.StatData.AddRange(data);
            owner.State.StatFrequency = freq.Checked;
            return true;
        }

        private void Compute()
        {
            if (!CaptureData()) return;
            try
            {
                RegressionType type = (RegressionType)regression.SelectedIndex;
                StatResult r = Statistics.Compute(owner.StatData, twoVar.Checked, freq.Checked, type);
                report.Text = Statistics.Report(r, twoVar.Checked, freq.Checked);
            }
            catch (Exception ex)
            {
                report.Text = "Ошибка: " + ex.Message;
            }
        }
    }
}
